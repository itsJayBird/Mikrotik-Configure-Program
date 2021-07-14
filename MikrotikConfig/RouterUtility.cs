﻿using System;
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
        private readonly string[,] defaultCredentials = { { "admin", "admin", "admin", "admin", "admin", "wroknet" },
                                                            { "", "R3sound810", "R3sound810!", "Rn3t720", "Rn3t720!", "H4rv3st3r!" } };
        private readonly string masterFW = "6.48.3";
        public RouterUtility() { }


        /*
         *  Tuple needs to return a bool (if it has connected) and a number
         *  0: successfully connected
         *  1: socket exception
         *  2: authentication exception
         */
         
        public RouterInfo initializeRouter()
        {
            // create connection
            ITikConnection connection = ConnectionFactory.CreateConnection(TikConnectionType.Api);

            // create blanked router info
            RouterInfo routerinfo = new RouterInfo(null, null, null);

            // find credentials
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

            // next find firmware version
            var fw = getFirmwareVersion(routerinfo);
            routerinfo.setCFW(fw[0]);
            routerinfo.setUFW(fw[1]);

            // check if the FW needs to be updated


            connection.Close();
            return routerinfo;
        }
        
        // returns tuple with two values
        // first if we need to update
        // second if we need to upgrade
        private Tuple<bool, bool> checkForUpgrade(RouterInfo routerinfo)
        {
            var master = masterFW.Split('.');

            // first check if the current FW is up to date
            var fw = routerinfo.currentFW.Split('.');
            if (int.Parse(fw[0]) >= int.Parse(master[0]))
            {
                if (int.Parse(fw[1]) >= int.Parse(master[1]))
                {
                    if (int.Parse(fw[2]) >= int.Parse(master[2]))
                    {
                        return new Tuple<bool, bool>(false, false);
                    }
                }
            }

            // if we check through it and we see that current FW is lower than the master
            // we will check if the upgrade fw is the same if it's not we will check to see if that
            // is more up to date than the master, if so all we need to do is initiate upgrade
            if(routerinfo.upgradeFW != masterFW)
            {
                fw = routerinfo.upgradeFW.Split('.');
                if (int.Parse(fw[0]) >= int.Parse(master[0]))
                {
                    if (int.Parse(fw[1]) >= int.Parse(master[1]))
                    {
                        if (int.Parse(fw[2]) >= int.Parse(master[2]))
                        {
                            return new Tuple<bool, bool>(false, true);
                        }
                    }
                }
            }

            // if this fails for some reason, return no need to upgrade
            return new Tuple<bool, bool>(true, true);
        }
        private string[] getFirmwareVersion(RouterInfo routerinfo)
        {
            // create connection
            ITikConnection connection = ConnectionFactory.CreateConnection(TikConnectionType.Api);
            connection.Open(routerinfo.host, routerinfo.user, routerinfo.password);

            // dummy command and regex
            ITikCommand cmd;
            Regex regex = new Regex("\\b(?:[0-9]{1,2}\\.){2}[0-9]{1,2}\\b");
            string[] firmware = new string[2];

            cmd = connection.CreateCommand("/system/routerboard/get");
            var response = cmd.ExecuteScalar().Split(';');
            
            // grab the current and ugprade firmware from the response
            foreach(string item in response)
            {
                if (item.Contains("current-firmware"))
                {
                    firmware[0] = regex.Match(item).ToString();
                }
                if (item.Contains("upgrade-firmware"))
                {
                    firmware[1] = regex.Match(item).ToString();
                }
            }

            return firmware;
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
        public string getWANIP(RouterInfo routerinfo)
        {
            // create connection
            ITikConnection connection = ConnectionFactory.CreateConnection(TikConnectionType.Api);
            connection.Open(routerinfo.host, routerinfo.user, routerinfo.password);

            // create command to grab IP of ether1-WAN IP
            ITikCommand cmd = connection.CreateCommandAndParameters("/ip/address/print",
                                                                    "interface", "ether1-WAN");
            var response = cmd.ExecuteList();

            // extract IP from the response
            foreach(ITikReSentence item in response)
            {
                Regex regex = new Regex("\\b(?:[0-9]{1,3}\\.){3}[0-9]{1,3}\\b");
                if (regex.Match(item.ToString()).Success)
                {
                    return regex.Match(item.ToString()).ToString();
                }
            }
            
            // return blank if ether1 not found
            return "";
        }
        public void resetConfig(RouterInfo routerinfo)
        {
            // create the connection
            ITikConnection connection = ConnectionFactory.CreateConnection(TikConnectionType.Api);
            connection.Open(routerinfo.host, routerinfo.user, routerinfo.password);

            // create the command to reset
            ITikCommand cmd = connection.CreateCommand("/system/reset-configuration");
            cmd.ExecuteList();
        }

        public void pingRouter(RouterInfo routerinfo)
        {
            string cmd = $"/C ping -t {routerinfo.host}";
            System.Diagnostics.Process.Start("CMD.exe", cmd);
        }
    }
}
