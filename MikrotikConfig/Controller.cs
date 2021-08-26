using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using tik4net;
using WinSCP;

namespace MikrotikConfig
{
    class Controller
    {
        RouterUtility ru = new RouterUtility();
        public Controller() { }

        public RouterInfo initializeRouter()
        {
            return ru.initializeRouter();
        }
        
        public RouterInfo getRouterModel(RouterInfo routerinfo)
        {
            return ru.getModel(routerinfo);
        }

        public void ptpConfiguration(List<string[]> devices, string wanIP, RouterInfo routerinfo)
        {
            if(wanIP == "")
            {
                wanIP = "192.168.88.1";
            }
            // create connection
            ITikConnection connection = ConnectionFactory.CreateConnection(TikConnectionType.Api);
            connection.Open(routerinfo.host, routerinfo.user, routerinfo.password);

            // dummy command
            ITikCommand cmd;
            string source = "PTP Programmed by Mikrotik Programming Buddy!\n\n";

            // add subnet
            try
            {
                cmd = connection.CreateCommandAndParameters("/ip/address/add",
                                                        "=address", "192.168.1.1/24",
                                                        "=interface", "bridge",
                                                        "=comment", "PTP-Subnet");
                cmd.ExecuteList();
                source += "## ADDRESS SETTINGS\nADDED ADDRESS: 192.168.1.1/24 TO INTERFACE: bridge\n\n";
            }
            catch (Exception) { }

            // loop through each device and add rules
            int port = 5001;
            source += "## PORTS FORWARDED\n\n";
            foreach (string[] device in devices)
            {
                int portNum = getPort(device[1]);
                cmd = connection.CreateCommandAndParameters("/ip/firewall/nat/add",
                                                            "=chain", "dstnat",
                                                            "=action", "dst-nat",
                                                            "=to-addresses", device[0],
                                                            "=to-ports", portNum.ToString(),
                                                            "=protocol", "tcp",
                                                            "=dst-address", wanIP,
                                                            "=in-interface", "ether1-WAN",
                                                            "=dst-port", port.ToString(),
                                                            "=comment", device[1]);
                cmd.ExecuteList();
                source += $"DEVICE: {device[1]}\nADDRESS: {device[0]}\nPORT: {portNum.ToString()} to {port.ToString()}\n\n";
                port++;
            }

            // set the signature
            DateTime local = DateTime.Now;
            var d = local.Date;
            Regex r = new Regex("\\b(?:[0-9]{1,2}\\/){2}[0-9]{4}\\b");
            var formatted = r.Match(d.ToString()).ToString().Replace('/', '-');
            try
            {
                cmd = connection.CreateCommandAndParameters("/file/print",
                                                            "=file", $"ptp-{formatted}.txt");
                cmd.ExecuteList();

                Thread.Sleep(2000);

                cmd = connection.CreateCommandAndParameters("file/set",
                                                            "=numbers", $"ptp-{formatted}.txt",
                                                            "=contents", source);
                cmd.ExecuteList();
            }
            catch (Exception) { }
        }

        public void pingRouter(RouterInfo routerinfo)
        {
            ru.pingRouter(routerinfo);
        }

        private int getPort(string device)
        {
            if (device.Contains("Mikrotik"))
            {
                return 8291;
            }
            return 443;
        }
        public void setUpgradeScript(RouterInfo routerinfo)
        {
            ru.setUpgradeScript(routerinfo);
        }
        public void forceUpgrade(RouterInfo routerinfo)
        {
            ru.forceUpgrade(routerinfo);
        }

        public void updateRouter(RouterInfo routerinfo)
        {
            // get path for ros

            // create teh connection
            ITikConnection connection = ConnectionFactory.CreateConnection(TikConnectionType.Api);
            Session session = new Session();

            // session options for winscp
            SessionOptions sOP = new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = routerinfo.host,
                UserName = routerinfo.user,
                Password = routerinfo.password,
                SshHostKeyPolicy = SshHostKeyPolicy.GiveUpSecurityAndAcceptAny
            };

            // create the FW file
            // get the path for the model specific fw
            string resName = getResourceName(routerinfo.model);
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);

            // path for the new file
            var path = Directory.GetCurrentDirectory() + $"\\{routerinfo.model}.npk";

            //convert file to a stream and reconstruct file
            BinaryReader br = new BinaryReader(stream);
            FileStream fs = new FileStream(path, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            byte[] ba = new byte[stream.Length];
            stream.Read(ba, 0, ba.Length);
            bw.Write(ba);
            br.Close();
            bw.Close();
            stream.Close();

            // connect
            connection.Open(routerinfo.host, routerinfo.user, routerinfo.password);
            session.Open(sOP);

            // dummy command
            ITikCommand cmd;

            // upload the file
            TransferOptions tOP = new TransferOptions()
            {
                TransferMode = TransferMode.Binary
            };
            TransferOperationResult tResults;
            tResults = session.PutFiles(path, "/", false, tOP);

            tResults.Check();

            //create command to reboot the router
            cmd = connection.CreateCommand("/system/reboot");
            cmd.ExecuteList();

            // close connectoins
            session.Close();
            connection.Close();
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

        public void executeConfiguration(string customerName, string wifiName, string wifiPassword,
                                          string secondaryIP, RouterInfo routerinfo)
        {
            // create connection
            ITikConnection connection = ConnectionFactory.CreateConnection(TikConnectionType.Api);
            connection.Open(routerinfo.host, routerinfo.user, routerinfo.password);

            // dummy command
            ITikCommand cmd;
            string source = "Programmed by Mikrotik Programming Buddy! \n";

            // set identity
            cmd = connection.CreateCommandAndParameters("/system/identity/set",
                                                        "=name", customerName);
            cmd.ExecuteList();
            source += $"Identity: {customerName}\n\n";

            // set snmp
            cmd = connection.CreateCommandAndParameters("/snmp/community/add",
                                                        "=name", "Resound1104");
            cmd.ExecuteList();
            cmd = connection.CreateCommandAndParameters("/snmp/set",
                                                        "=enabled", "yes",
                                                        "=contact", "Resound Networks LLC",
                                                        "=location", customerName,
                                                        "=trap-community", "Resound1104",
                                                        "=trap-version", "1");
            cmd.ExecuteList();
            source += $"##SNMP\nCOMMUNITY ADDED: Resound1104\nENABLED: YES\nCONTACT: Resound Networks LLC\nLOCATION: {customerName}\nTRAP COMMUNITY: Resound1104\nTRAP VERSION: 1\n\n";

            // set ntp
            cmd = connection.CreateCommandAndParameters("/system/ntp/client/set",
                                                        "=enabled", "yes",
                                                        "=primary-ntp", "129.6.15.28",
                                                        "=secondary-ntp", "129.6.15.29");
            cmd.ExecuteList();
            source += "##NTP\nENABLED: YES\nPRIMARY: 129.6.15.28\nSECONDARY: 129.6.15.29\n\n";

            // set dns
            cmd = connection.CreateCommandAndParameters("/ip/dns/set",
                                                        "=allow-remote-requests", "no",
                                                        "=servers", "8.8.8.8,1.1.1.1");
            cmd.ExecuteList();
            source += "## DNS\nALLOW REMOTE REQUESTS: NO\nSERVERS: 8.8.8.8, 1.1.1.1\n\n";

            // set wireless settings
            cmd = connection.CreateCommandAndParameters("/interface/wireless/security-profiles/set",
                                                       "=numbers", "default",
                                                       "=authentication-types", "wpa2-psk",
                                                       "=mode", "dynamic-keys",
                                                       "=supplicant-identity", "Mikrotik",
                                                       "=wpa2-pre-shared-key", wifiPassword);
            cmd.ExecuteList();
            cmd = connection.CreateCommandAndParameters("/interface/wireless/set",
                                                        "=numbers", "wlan1",
                                                        "=disabled", "no",
                                                        "=channel-width", "20mhz",
                                                        "=frequency", "auto",
                                                        "=ssid", $"{wifiName}-2ghz",
                                                        "=frequency-mode", "regulatory-domain",
                                                        "=country", "united states3");
            cmd.ExecuteList();
            source += $"##WIRELESS SETTINGS\nAUTHENTICATION: WPA2-PSK\nPASSWORD: {wifiPassword}\nSSID0: {wifiName}-2ghz\n";

            if (routerinfo.wifiBands == 2)
            {
                cmd = connection.CreateCommandAndParameters("/interface/wireless/set",
                                                            "=numbers", "wlan2",
                                                            "=disabled", "no",
                                                            "=channel-width", "20mhz",
                                                            "=frequency", "auto",
                                                            "=ssid", $"{wifiName}-5ghz",
                                                            "=frequency-mode", "regulatory-domain",
                                                            "=country", "united states3");
                cmd.ExecuteList();
                source += $"SSID1: {wifiName}-5ghz";
            }

           // set port names
           cmd = connection.CreateCommandAndParameters("/interface/ethernet/set",
                                                       "=numbers", "0",
                                                       "=name", "ether1-WAN");
            cmd.ExecuteList();
            cmd = connection.CreateCommandAndParameters("/interface/bridge/port/add",
                                                        "=interface", "ether1-WAN",
                                                        "=bridge", "bridge");
            cmd.ExecuteList();
            source += "\n##PORT SETTINGS\nPORT0: eher1-WAN\nSET PORT ether1-WAN ON TO BRIDGE: bridge\n\n";

            // remove firewall rules
            if (routerinfo.model == "arm" || routerinfo.routerBoard == "hapLite")
            {
                cmd = connection.CreateCommandAndParameters("/ip/firewall/filter/remove",
                                                        "=numbers", "1,2,3,4,5,6,7,8,9,10,11");
                cmd.ExecuteList();
            } else
            if (routerinfo.model == "mipsbe")
            {
                cmd = connection.CreateCommandAndParameters("/ip/firewall/filter/remove",
                                                        "=numbers", "1,2,3,4,5,6,7,8,9,10");
                cmd.ExecuteList();
            }
            source += "##FIREWALL SETTINGS\nREMOVED FILTER RULES\n\n";

            // set DHCP settings
            cmd = connection.CreateCommandAndParameters("/ip/dhcp-server/network/set",
                                                        "=dns-server", "8.8.8.8,1.1.1.1",
                                                        "=address", "192.168.1.0/24",
                                                        "=gateway", "192.168.1.1",
                                                        "=numbers", "0");
            cmd.ExecuteList();
            cmd = connection.CreateCommandAndParameters("/ip/dhcp-client/remove",
                                                        "=numbers", "0");
            cmd.ExecuteList();
            cmd = connection.CreateCommandAndParameters("/ip/dhcp-relay/add",
                                                        "=name", "relay1",
                                                        "=interface", "bridge",
                                                        "=dhcp-server", "192.168.88.1",
                                                        "=disabled", "no");
            cmd.ExecuteList();
            cmd = connection.CreateCommandAndParameters("/ip/dhcp-server/remove",
                                                        "=numbers", "0");
            cmd.ExecuteList();
            source += "## DHCP SETTINGS\nREMOVED DHCP SERVER\nREMOVED DHCP CLIENT\nADDED DHCP RELAY TO ADDRESS: 192.168.88.1\n\n";

            // set address
            cmd = connection.CreateCommandAndParameters("/ip/address/add",
                                                         "=address", secondaryIP,
                                                         "=interface", "bridge",
                                                         "=comment", "PTP-Subnet");
            cmd.ExecuteList();
            source += $"## ADDRESS SETTINGS\n SET NEW ADDRES TO: {secondaryIP} ON INTERFACE: bridge\nREMOVED PREVIOUS ADDRESS OF: 192.168.88.1\n\n";
            source += "##PASSWORD SETTINGS\n CHANGED PASSWORD TO: R3sound810\n\n\n CONGRATULATIONS CONFIGURATION HAS BEEN SUCCESSFULLY SET\n IF YOU SEE ANY DISCREPANCIES PLEASE EMAIL JESSE@RESOUNDNEWORKS.COM WITH ANY ISSUES";

            // set signature
            DateTime local = DateTime.Now;
            var d = local.Date;
            Regex r = new Regex("\\b(?:[0-9]{1,2}\\/){2}[0-9]{4}\\b");
            var formatted = r.Match(d.ToString()).ToString().Replace('/', '-');
            try
            {
                cmd = connection.CreateCommandAndParameters("/file/print",
                                                            "=file", $"log-{formatted}.txt");
                cmd.ExecuteList();

                Thread.Sleep(5000);

                cmd = connection.CreateCommandAndParameters("file/set",
                                                            "=numbers", $"log-{formatted}.txt",
                                                            "=contents", source);
                cmd.ExecuteList();
            }
            catch (Exception) { }

            // reconnect on new IP
            connection.Close();
            Thread.Sleep(1000);
            connection.Open(secondaryIP, routerinfo.user, routerinfo.password);

            cmd = connection.CreateCommandAndParameters("/ip/address/remove",
                                                        "=numbers", "0");
            cmd.ExecuteList();

            // set password
            cmd = connection.CreateCommandAndParameters("/password",
                                                        "=old-password", "",
                                                        "=new-password", "R3sound810",
                                                        "=confirm-new-password", "R3sound810");
            cmd.ExecuteList();

            // close connection
            connection.Close();
        }
        public void executeConfiguration(string customerName, string wifiName, string wifiPassword, RouterInfo routerinfo)
        {
            // create connection
            ITikConnection connection = ConnectionFactory.CreateConnection(TikConnectionType.Api);
            connection.Open(routerinfo.host, routerinfo.user, routerinfo.password);
            string source = "Programmed by Mikrotik Programming Buddy! \n";

            // dummy command
            ITikCommand cmd;

            // set identity
            cmd = connection.CreateCommandAndParameters("/system/identity/set",
                                                        "=name", customerName);
            cmd.ExecuteList();
            source += $"Identity: {customerName}\n\n";

            // set snmp
            cmd = connection.CreateCommandAndParameters("/snmp/community/add",
                                                        "=name", "Resound1104");
            cmd.ExecuteList();
            cmd = connection.CreateCommandAndParameters("/snmp/set",
                                                        "=enabled", "yes",
                                                        "=contact", "Resound Networks LLC",
                                                        "=location", customerName,
                                                        "=trap-community", "Resound1104",
                                                        "=trap-version", "1");
            cmd.ExecuteList();
            source += $"##SNMP\nCOMMUNITY ADDED: Resound1104\nENABLED: YES\nCONTACT: Resound Networks LLC\nLOCATION: {customerName}\nTRAP COMMUNITY: Resound1104\nTRAP VERSION: 1\n\n";

            // set ntp
            cmd = connection.CreateCommandAndParameters("/system/ntp/client/set",
                                                        "=enabled", "yes",
                                                        "=primary-ntp", "129.6.15.28",
                                                        "=secondary-ntp", "129.6.15.29");
            cmd.ExecuteList();
            source += "##NTP\nENABLED: YES\nPRIMARY: 129.6.15.28\nSECONDARY: 129.6.15.29\n\n";

            // set dns
            cmd = connection.CreateCommandAndParameters("/ip/dns/set",
                                                        "=allow-remote-requests", "no",
                                                        "=servers", "8.8.8.8,1.1.1.1");
            cmd.ExecuteList();
            source += "## DNS\nALLOW REMOTE REQUESTS: NO\nSERVERS: 8.8.8.8, 1.1.1.1\n\n";

            // set wireless settings
            cmd = connection.CreateCommandAndParameters("/interface/wireless/security-profiles/set",
                                                        "=numbers", "default",
                                                        "=authentication-types", "wpa2-psk",
                                                        "=mode", "dynamic-keys",
                                                        "=supplicant-identity", "Mikrotik",
                                                        "=wpa2-pre-shared-key", wifiPassword);
            cmd.ExecuteList();
            
            cmd = connection.CreateCommandAndParameters("/interface/wireless/set",
                                                        "=numbers", "wlan1",
                                                        "=disabled", "no",
                                                        "=channel-width", "20mhz",
                                                        "=frequency", "auto",
                                                        "=ssid", $"{wifiName}-2ghz",
                                                        "=frequency-mode", "regulatory-domain",
                                                        "=country", "united states3");
            cmd.ExecuteList();

            source += $"##WIRELESS SETTINGS\nAUTHENTICATION: WPA2-PSK\nPASSWORD: {wifiPassword}\nSSID0: {wifiName}-2ghz\n";

            if (routerinfo.wifiBands == 2)
            {
                cmd = connection.CreateCommandAndParameters("/interface/wireless/set",
                                                            "=numbers", "wlan2",
                                                            "=disabled", "no",
                                                            "=channel-width", "20mhz",
                                                            "=frequency", "auto",
                                                            "=ssid", $"{wifiName}-5ghz",
                                                            "=frequency-mode", "regulatory-domain",
                                                            "=country", "united states3");
                cmd.ExecuteList();
                source += $"SSID1: {wifiName}-5ghz";
            }

            // set port names
            cmd = connection.CreateCommandAndParameters("/interface/ethernet/set",
                                                        "=numbers", "0",
                                                        "=name", "ether1-WAN");
            cmd.ExecuteList();
            source += "\n##PORT SETTINGS\nPORT0: eher1-WAN\n\n";

            // set upnp settings
            cmd = connection.CreateCommandAndParameters("/ip/upnp/set",
                                                        "=allow-disable-external-interface", "yes",
                                                        "=enabled", "yes",
                                                        "=show-dummy-rule", "yes");
            cmd.ExecuteList();

            cmd = connection.CreateCommandAndParameters("/ip/upnp/interfaces/add",
                                                        "=interface", "ether1-WAN",
                                                        "=type", "external");
            cmd.ExecuteList();

            cmd = connection.CreateCommandAndParameters("/ip/upnp/interfaces/add",
                                                        "=interface", "bridge",
                                                        "=type", "internal");
            cmd.ExecuteList();
            source += "##UPNP SETTINGS\nALLOW DISABLE EXTERNAL INTERFACE: YES\nENABLED: YES\nSHOW DUMMY RULE: YES\nADD EXTERNAL INTERFACE: ether1-WAN\nADD INTERNAL INTERFACE: bridge\n\n";

            // remove firewall rules
            if(routerinfo.model == "arm" || routerinfo.routerBoard == "hapLite")
            {
                cmd = connection.CreateCommandAndParameters("/ip/firewall/filter/remove",
                                                        "=numbers", "1,2,3,4,5,6,7,8,9,10,11");
                cmd.ExecuteList();
            } else
            if (routerinfo.model == "mipsbe")
            {
                cmd = connection.CreateCommandAndParameters("/ip/firewall/filter/remove",
                                                        "=numbers", "1,2,3,4,5,6,7,8,9,10");
                cmd.ExecuteList();
            }
            
            cmd = connection.CreateCommandAndParameters("/ip/firewall/nat/set",
                                                        "=numbers", "0",
                                                        "=out-interface", "ether1-WAN");
            cmd.ExecuteList();
            source += "##FIREWALL SETTINGS:\nREMOVED FILTER RULES\nADDED MASQUERADE RULE TO PORT: ether1-WAN\n\n";

            // set DHCP settings
            cmd = connection.CreateCommandAndParameters("/ip/dhcp-client/set",
                                                        "=interface", "ether1-WAN",
                                                        "=numbers", "0");
            cmd.ExecuteList();
            cmd = connection.CreateCommandAndParameters("/ip/dhcp-server/network/set",
                                                        "=dns-server", "8.8.8.8,1.1.1.1",
                                                        "=numbers", "0");
            cmd.ExecuteList();
            source += "##DHCP SETTINGS\nSET DHCP CLIENT TO PORT: ether1-WAN\nCHANGED DNS SERVERS TO: 8.8.8.8, 1.1.1.1\n\n";
            source += "##PASSWORD SETTINGS\n CHANGED PASSWORD TO: R3sound810\n\n\n CONGRATULATIONS CONFIGURATION HAS BEEN SUCCESSFULLY SET\n IF YOU SEE ANY DISCREPANCIES PLEASE EMAIL JESSE@RESOUNDNEWORKS.COM WITH ANY ISSUES";


            // set the signature
            DateTime local = DateTime.Now;
            var d = local.Date;
            Regex r = new Regex("\\b(?:[0-9]{1,2}\\/){2}[0-9]{4}\\b");
            var formatted = r.Match(d.ToString()).ToString().Replace('/', '-');
            try
            {
                cmd = connection.CreateCommandAndParameters("/file/print",
                                                            "=file", $"log-{formatted}.txt");
                cmd.ExecuteList();

                Thread.Sleep(5000);

                cmd = connection.CreateCommandAndParameters("file/set",
                                                            "=numbers", $"log-{formatted}.txt",
                                                            "=contents", source);
                cmd.ExecuteList();
            }
            catch (Exception) { }


            // set password
            cmd = connection.CreateCommandAndParameters("/password",
                                                        "=old-password", "",
                                                        "=new-password", "R3sound810",
                                                        "=confirm-new-password", "R3sound810");
            cmd.ExecuteList();

            // close connection
            connection.Close();
        }
    }
}
