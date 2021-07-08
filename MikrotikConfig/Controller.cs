using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;

namespace MikrotikConfig
{
    class Controller
    {
        RouterUtility ru = new RouterUtility();
        public string name { get; set; }
        public Controller() { }
        public Tuple<bool, int> initiateConfiguration()
        {
            // first we test to see if we are on a default router
            var decision = ru.isNewRouter();

            return decision;
        }

        public RouterInfo getDefaultCredentials()
        {
            var defaultCredentials = ru.defaultCredentials();
            return defaultCredentials;
        }

        public void scriptHeader()
        {
            var r = new Random();
            name = $"log-{r.Next(100000)}.rsc";

            // this line will be at the start of every script so that we can run the script on reboot
            string header = "/system script add name=defaultConfig policy=ftp,read,write,policy,test,reboot,sniff,sensitive,romon,password source=\"\n";
            using (FileStream stream = File.Open(Directory.GetCurrentDirectory() + $"\\junk\\{name}",
                  FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(header);
                }
            }
        }

        public void ptpConfig(List<string[]> devices, string wanIP)
        {
            if (wanIP == "")
            {
                wanIP = "192.168.88.1";
            }
            // create a list of commands we are going to be sending the router
            List<string> commands = new List<string>();
            commands.Add(":delay 10s;");
            //  add each forwarding rule to the list for each device in the links we are adding
            int port = 5001;
            foreach (string[] device in devices)
            {
                int portNum = getPort(device[1]);
                string devName = "\\\"" + device[1] + "\\\"";
                string tmpRule = $"/ip firewall nat add chain=dstnat action=dst-nat to-addresses={device[0]} to-ports={portNum} " +
                                 $"protocol=tcp dst-address={wanIP} in-interface=ether1-WAN dst-port={port} comment=\\\"{device[1]}\\\"";
                commands.Add(tmpRule);
                port++;
            }
            commands.Add("/ip address add address=192.168.1.1/24 network=192.168.1.0 interface=bridge comment=\\\"PTP-Subnet\\\"");
            string commandList = "\n";
            foreach (string command in commands)
            {
                commandList += command + "\n";
            }
            appendScript(commandList);
        }

        private int getPort(string device)
        {
            if (device.Contains("Mikrotik"))
            {
                return 8291;
            }
            return 443;
        }
        public void uploadScript(string username, string password, string model)
        {
            string final = "";
            List<string> commands = new List<string>();
            commands.Add("/system script remove defaultConfig");
            commands.Add("/system scheduler remove runConfig");
            commands.Add($"/file remove {name}");
            foreach (string command in commands)
            {
                final += command + "\n";
            }
            final += "\"";
            appendScript(final);
            string host = "192.168.88.1";
            SftpClient client = new SftpClient(host, username, password);
            client.Connect();

            string path = Directory.GetCurrentDirectory() + $"\\junk\\{name}";
            FileInfo info = new FileInfo(path);
            string uploadFile = info.FullName;

            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                client.UploadFile(stream, info.Name, null);
            }


            SshClient ssh = new SshClient(host, username, password);
            ssh.Connect();
            ssh.RunCommand("/system scheduler add name=runConfig on-event=defaultConfig start-time=startup interval=0");
            ssh.RunCommand($"/import {name}");
            updateRouter(model, host, username, password);
            Thread.Sleep(20000);
            try
            {
                ssh.RunCommand("/system reboot");
            }
            catch (Renci.SshNet.Common.SshConnectionException) { }
            client.Disconnect();
            ssh.Disconnect();
        }

        private void updateRouter(string model, string host, string username, string password)
        {
            // get the path for the model specific fw
            string resName = getResourceName(model);
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);

            //convert file to a stream and reconstruct file
            FileStream fileStream = File.Open(Directory.GetCurrentDirectory() + "\\ros.npk",
                                                FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            for (int i = 0; i < stream.Length; i++)
            {
                fileStream.WriteByte((byte)stream.ReadByte());
            }
            //now we create the sftp client and connect
            SftpClient sftpClient = new SftpClient(host, username, password);
            sftpClient.Connect();
            string path = Directory.GetCurrentDirectory() + "\\ros.npk";
            FileInfo f = new FileInfo(path);
            string uploadFile = f.FullName;
            using (FileStream fwStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                sftpClient.UploadFile(fwStream, f.Name, null);
            }
            //var fwStream = new FileStream(uploadFile, FileMode.Open);         
            fileStream.Close();
        }

        public string getResourceName(string resName)
        {
            var list = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (string name in list)
            {
                if (name.Contains(resName))
                {
                    return name;
                }
            }
            return null;

        }


        private string createString(string q1, string q2, string q3, bool isDual, string secondaryIP)
        {
            List<string> commands = new List<string>();
            string subnetIP = secondaryIP + "/24";
            // set identity
            commands.Add(":delay 10s;");
            commands.Add($"/system identity set name=\\\"{q1}\\\"");
            // snmp settings
            commands.Add("/snmp set enabled=yes");
            commands.Add("/snmp set contact=\\\"Resound Networks LLC\\\"");
            commands.Add($"/snmp set location=\\\"{q1}\\\"");
            commands.Add("/snmp community add name=\\\"Resound1104\\\"");
            commands.Add("/snmp set trap-community=\\\"Resound1104\\\"");
            commands.Add("/snmp set trap-version=\\\"1\\\"");
            //ntp settings
            commands.Add("/system ntp client set enabled=yes");
            commands.Add("/system ntp client set primary-ntp=\\\"129.6.15.28\\\"");
            commands.Add("/system ntp client set secondary-ntp=\\\"129.6.15.29\\\"");
            //dns settings
            commands.Add("/ip dns set allow-remote-requests=no");
            commands.Add("/ip dns set servers=\\\"8.8.8.8,1.1.1.1\\\"");
            //wireless settings
            commands.Add("/interface wireless security-profiles set [find default=yes] authentication-types=wpa2-psk " +
                  $"eap-methods=\\\"\\\" mode=dynamic-keys supplicant-identity=Mikrotik wpa2-pre-shared-key=\\\"{q3}\\\"");
            commands.Add("/interface wireless set wlan1 mode=ap-bridge band=2ghz-b/g/n " +
                              "disabled=no wireless-protocol=802.11 distance=indoors channel-width=20mhz " +
                              $"frequency=auto ssid=\\\"{q2}-2ghz\\\" frequency-mode=regulatory-domain country=\\\"united states3\\\"");
            if (isDual == true)
            {
                commands.Add("/interface wireless set wlan2 mode=ap-bridge band=5ghz-a/n/ac disabled=no wireless-protocol=802.11" +
                                  $" distance=indoors channel-width=20mhz frequency=auto ssid=\\\"{q2}-5ghz\\\" " +
                                  "frequency-mode=regulatory-domain country=\\\"united states3\\\"");
            }
            //port maintenance
            commands.Add("/interface ethernet set 0 name=\\\"ether1-WAN\\\"");
            commands.Add("/interface ethernet set 1 name=\\\"ether2\\\"");
            commands.Add("/interface ethernet set 2 name=\\\"ether3\\\"");
            commands.Add("/interface ethernet set 3 name=\\\"ether4\\\"");
            commands.Add("/interface ethernet set 4 name=\\\"ether5\\\"");
            commands.Add("/interface bridge port add interface=ether1-WAN trust=no priority=80 path-cost=10 bridge=bridge");
            //upnp settings
            commands.Add("/ip upnp set allow-disable-external-interface=yes");
            commands.Add("/ip upnp set enabled=yes");
            commands.Add("/ip upnp set show-dummy-rule=yes");
            commands.Add("/ip upnp interfaces add interface=\\\"ether1-WAN\\\" type=external");
            commands.Add("/ip upnp interfaces add interface=bridge type=internal");
            //remove firewall rules
            commands.Add("/ip firewall filter remove 1,2,3,4,5,6,7,8,9,10");
            //DHCP settings
            commands.Add("/ip dhcp-server network set dns-server=\\\"8.8.8.8,1.1.1.1\\\" 0");
            commands.Add("/ip dhcp-client remove numbers=0");
            commands.Add("/ip dhcp-relay add name=relay1 interface=bridge dhcp-server=192.168.88.1 disabled=no");
            commands.Add("/ip dhcp-server remove numbers=0");
            //Address settings
            commands.Add($"/ip address add address={subnetIP} interface=bridge network=192.168.1.0");
            //Finally set the password
            commands.Add("/password old-password=\\\"\\\" new-password=\\\"R3sound810\\\" confirm-new-password=\\\"R3sound810\\\"");
            commands.Add("/ip address remove numbers=0");

            string script = "";
            foreach (string command in commands)
            {
                script += command + "\n";
            }

            return script;
        }
        private string createString(string q1, string q2, string q3, bool isDual)
        {
            List<string> commands = new List<string>();
            commands.Add(":delay 10s;");
            commands.Add($"/system identity set name=\\\"{q1}\\\"");
            commands.Add("/snmp set enabled=yes");
            commands.Add("/snmp set contact=\\\"Resound Networks LLC\\\"");
            commands.Add($"/snmp set location=\\\"{q1}\\\"");
            commands.Add("/snmp community add name=\\\"Resound1104\\\"");
            commands.Add("/snmp set trap-community=\\\"Resound1104\\\"");
            commands.Add("/snmp set trap-version=\\\"1\\\"");
            //ntp settings
            commands.Add("/system ntp client set enabled=yes");
            commands.Add("/system ntp client set primary-ntp=\\\"129.6.15.28\\\"");
            commands.Add("/system ntp client set secondary-ntp=\\\"129.6.15.29\\\"");
            //dns settings
            commands.Add("/ip dns set allow-remote-requests=no");
            commands.Add("/ip dns set servers=\\\"8.8.8.8,1.1.1.1\\\"");
            //wireless settings
            commands.Add("/interface wireless security-profiles set [find default=yes] authentication-types=wpa2-psk " +
                  $"eap-methods=\\\"\\\" mode=dynamic-keys supplicant-identity=Mikrotik wpa2-pre-shared-key=\\\"{q3}\\\"");
            commands.Add("/interface wireless set wlan1 mode=ap-bridge band=2ghz-b/g/n " +
                              "disabled=no wireless-protocol=802.11 distance=indoors channel-width=20mhz " +
                              $"frequency=auto ssid=\\\"{q2}-2ghz\\\" frequency-mode=regulatory-domain country=\\\"united states3\\\"");
            if (isDual == true)
            {
                commands.Add("/interface wireless set wlan2 mode=ap-bridge band=5ghz-a/n/ac disabled=no wireless-protocol=802.11" +
                                  $" distance=indoors channel-width=20mhz frequency=auto ssid=\\\"{q2}-5ghz\\\" " +
                                  "frequency-mode=regulatory-domain country=\\\"united states3\\\"");
            }
            //port maintenance
            commands.Add("/interface ethernet set 0 name=\\\"ether1-WAN\\\"");
            commands.Add("/interface ethernet set 1 name=\\\"ether2\\\"");
            commands.Add("/interface ethernet set 2 name=\\\"ether3\\\"");
            commands.Add("/interface ethernet set 3 name=\\\"ether4\\\"");
            commands.Add("/interface ethernet set 4 name=\\\"ether5\\\"");
            //upnp settings
            commands.Add("/ip upnp set allow-disable-external-interface=yes");
            commands.Add("/ip upnp set enabled=yes");
            commands.Add("/ip upnp set show-dummy-rule=yes");
            commands.Add("/ip upnp interfaces add interface=\\\"ether1-WAN\\\" type=external");
            commands.Add("/ip upnp interfaces add interface=bridge type=internal");
            //remove firewall rules
            commands.Add("/ip firewall filter remove 1,2,3,4,5,6,7,8,9,10");
            commands.Add("/ip firewall nat set 0 action=masquerade chain=srcnat " +
                              "comment=\\\"defconf: masquerade\\\" out-interface=\\\"ether1-WAN\\\"");
            //DHCP settings
            commands.Add("/ip dhcp-client set interface=\\\"ether1-WAN\\\" 0");
            commands.Add("/ip dhcp-server network set dns-server=\\\"8.8.8.8,1.1.1.1\\\" 0");
            //Finally set the password
            commands.Add("/password old-password=\\\"\\\" new-password=\\\"R3sound810\\\" confirm-new-password=\\\"R3sound810\\\"");

            string script = "";
            foreach (string command in commands)
            {
                script += command + "\n";
            }

            return script;
        }
        // function to initiate making the script, inserts the header which will always be on the script and  
        // inserts the questions from the new configure questionaire
        public void makeScript(string q1, string q2, string q3, bool isDual)
        {
            string script = createString(q1, q2, q3, isDual);
            appendScript(script);
        }
        public void makeScript(string q1, string q2, string q3, bool isDual, string q4)
        {
            string script = createString(q1, q2, q3, isDual, q4);
            appendScript(script);
        }
        // function to add something to the script
        private void appendScript(string appendText)
        {
            string path = Directory.GetCurrentDirectory() + $"\\junk\\{name}";
            using (StreamWriter writer = File.AppendText(path))
            {
                writer.WriteLine(appendText);
            }
        }

        public void setName(string name)
        {
            this.name = name;
        }
    }
}
