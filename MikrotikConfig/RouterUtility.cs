using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Renci.SshNet;

namespace MikrotikConfig
{
    class RouterUtility
    {
        private readonly string defUser = "admin";
        private readonly string defPass = "";
        private readonly string defIP = "192.168.88.1";
        public RouterUtility() { }


        /*
         *  Tuple needs to return a bool (if it has connected) and a number
         *  0: successfully connected
         *  1: socket exception
         *  2: authentication exception
         */
        public Tuple<bool,int> isNewRouter()
        {
            SshClient client = new SshClient(defIP, defUser, defPass);
            try
            {
                client.Connect();
            }
            catch (System.Net.Sockets.SocketException)
            {
                // socket exception
                return new Tuple<bool, int>(false, 1); 
            }
            catch (Renci.SshNet.Common.SshAuthenticationException)
            {
                // authentication exception
                return new Tuple<bool, int>(false, 2);
            }

            // successfully connected
            return new Tuple<bool, int>(true, 0);
        }

        public RouterInfo defaultCredentials()
        {
            RouterInfo ri = new RouterInfo(defIP, defUser, defPass);
            return ri;
        }

        public Tuple<string, bool> getModel(string host, string user, string password)
        {
            // connect to router and create a file with router info
            var ghost = host;
            SshClient client = new SshClient(ghost, user, password);
            try
            {
                client.Connect();
            }
            catch (System.Net.Sockets.SocketException)
            {
                ghost = "192.168.88.1";
                client = new SshClient(ghost, user, password);
                client.Connect();
            }
            client.RunCommand("system routerboard print file=\"model.txt\"");
            // create scp client to get file
            ScpClient scpClient = new ScpClient(ghost, user, password);
            scpClient.Connect();
            // create stream for file to write to, ensure the FileAccess and FileShare are .ReadWrite so that
            // we are able to edit once we open the file
            var r = new Random();
            string path = Directory.GetCurrentDirectory() + $"\\junk\\{r.Next(100000)}.txt";
            Stream file1 = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            scpClient.Download("/model.txt", file1);
            // wait while we download file
            Thread.Sleep(100);
            // remove file from router
            client.RunCommand("/file remove model.txt");
            // disconnect clients
            client.Disconnect();
            client.Dispose();
            scpClient.Disconnect();
            scpClient.Dispose();

            // we are using "using()" so that we can separately handle the thread to open the file and read it
            using (FileStream fileStream = File.Open(path,
                   FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                // same concept for the reader
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    int i = 0;
                    // iterate through the lines until we find a line that contains our model
                    while (streamReader.Peek() > -1)
                    {
                        string line = streamReader.ReadLine();
                        if (line.Contains("RBD52G"))
                        {
                            return new Tuple<string, bool>("RBD52G", true);
                        }

                        if (line.Contains("951Ui") || line.Contains("952Ui"))
                        {
                            return new Tuple<string, bool>("951Ui", false);
                        }
                        i++;
                    }
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
        public string[] getCredentials()
        {
            string[,] defaultCredentials = new string[2, 5] { { "admin", "admin", "admin", "admin", "wroknet" },
                                                            { "R3sound810", "R3sound810!", "Rn3t720", "Rn3t720!", "H4rv3st3r!" } };
            // loop through defaultCredentials until we find the correct credentials
            for (int i = 0; i < defaultCredentials.GetLength(0); i++)
            {
                SshClient client = new SshClient(defIP, defaultCredentials[0, i], defaultCredentials[1, i]);
                try
                {
                    client.Connect();
                }
                catch (Renci.SshNet.Common.SshAuthenticationException) { }

                if (client.IsConnected == true)
                {
                    return new string[] { defaultCredentials[0, i], defaultCredentials[1, i] };
                }
            }
            return null;
        }

        public void pingRouter(RouterInfo routerinfo)
        {
            string cmd = $"/C ping -t {routerinfo.host}";
            System.Diagnostics.Process.Start("CMD.exe", cmd);
        }
    }
}
