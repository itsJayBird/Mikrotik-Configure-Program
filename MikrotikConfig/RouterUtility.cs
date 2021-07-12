using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Renci.SshNet;
using tik4net;

namespace MikrotikConfig
{
    class RouterUtility
    {
        private readonly string host = "192.168.88.1";
        private readonly string[,] defaultCredentials = { { "admin", "admin", "admin", "admin", "wroknet" },
                                                            { "R3sound810", "R3sound810!", "Rn3t720", "Rn3t720!", "H4rv3st3r!" } };
        public RouterUtility() { }


        /*
         *  Tuple needs to return a bool (if it has connected) and a number
         *  0: successfully connected
         *  1: socket exception
         *  2: authentication exception
         */
         
        public RouterInfo initializeRouter()
        {
            ITikConnection connection = ConnectionFactory.CreateConnection(TikConnectionType.Api);
            RouterInfo routerinfo = new RouterInfo(null, null, null);
            bool connected = false;
            int i = 0;
            while (!connected)
            {
                try
                {
                    connection.Open(host, defaultCredentials[0, i], defaultCredentials[1, i]);
                    routerinfo.setHost(host);
                    routerinfo.setUser(defaultCredentials[0, i]);
                    routerinfo.setPassword(defaultCredentials[1, i]);
                    connected = true;
                }
                catch (Exception) { }
                i++;
            }
            connection.Close();
            return routerinfo;
        }

        public string getModel(RouterInfo routerinfo)
        {
            // create the connection
            ITikConnection connection = ConnectionFactory.CreateConnection(TikConnectionType.Api);
            connection.Open(routerinfo.host, routerinfo.user, routerinfo.password);

            // create the command to retrieve the model
            ITikCommand getModel = connection.CreateCommand("/system/resource/print");
            var list = getModel.ExecuteList();
            foreach (ITikReSentence item in list)
            {
                if (item.ToString().Contains("arm"))
                {
                    return "arm";
                }
                if (item.ToString().Contains("mipsbe"))
                {
                    return "mipsbe";
                }
            }
            return null;
        }

        public string getWANIP(string user, string password)
        {
            // connect to client
            SshClient sshClient = new SshClient(defIP, user, password);
            try
            {
                sshClient.Connect();
            }
            catch (Exception) { }
            // create file with ip info
            sshClient.RunCommand("/ip address print file=WAN.txt where interface=ether1");
            // connect again via scp and download file to read
            ScpClient scpClient = new ScpClient(defIP, user, password);
            try
            {
                scpClient.Connect();
            }
            catch (Exception) { }
            var r = new Random();
            string path = Directory.GetCurrentDirectory() + $"\\junk\\ipFile-{r.Next(100000)}.txt";
            Stream ipFile = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            scpClient.Download("/WAN.txt", ipFile);
            Thread.Sleep(500);
            sshClient.RunCommand("/file remove WAN.txt");
            sshClient.Disconnect();
            scpClient.Disconnect();
            sshClient.Dispose();
            scpClient.Dispose();
            string addr = "";
            // this bit here uses regex to extract IP from file
            using (FileStream fileStream = File.Open(path,
                   FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    int i = 0;
                    while (reader.Peek() > -1)
                    {
                        string line = reader.ReadLine();
                        Regex regex = new Regex("\\b(?:[0-9]{1,3}\\.){3}[0-9]{1,3}\\b");
                        if (regex.Match(line).Success == true)
                        {
                            addr = regex.Match(line).ToString();
                            break;
                        }
                        i++;
                    }
                }
            }
            return addr;
        }
        public void resetConfig(string user, string pass)
        {
            // create an ssh client to log in and reset config
            var client = new SshClient(defIP, user, pass);
            client.Connect();
            try
            {
                client.RunCommand("/system reset-configuration");
            }
            catch (Renci.SshNet.Common.SshConnectionException) { }
            client.Dispose();
        }

        public void pingRouter(RouterInfo routerinfo)
        {
            string cmd = $"/C ping -t {routerinfo.host}";
            System.Diagnostics.Process.Start("CMD.exe", cmd);
        }
    }
}
