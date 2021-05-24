using System;
using System.Collections.Generic;
using System.Text;
using Renci.SshNet;

namespace MikrotikConfig
{
    class RouterUtility
    {
        private readonly string[] user = { "admin", "wroknet" };
        private readonly string[] passwords = { "R3sound810", "R3sound810!", "Rn3t720", "Rn3t720!" };
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
                return new Tuple<bool, int>(false, 1); 
            }
            catch (Renci.SshNet.Common.SshAuthenticationException)
            {
                return new Tuple<bool, int>(false, 2);
            }

            return new Tuple<bool, int>(true, 0);
        }
    }
}
