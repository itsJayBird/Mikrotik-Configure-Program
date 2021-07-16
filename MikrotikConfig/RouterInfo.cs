using System;

namespace MikrotikConfig
{
    class RouterInfo
    {
        public string host { get; set; }
        public string user { get; set; }
        public string password { get; set; }
        public string model { get; set; }
        public string currentFW { get; set; }
        public string upgradeFW { get; set; }
        public string masterFW { get; }
        public Tuple<bool, bool> upgradeChecks { get; set; }

        public RouterInfo() { }
        public RouterInfo(string host, string user, string password)
        {
            this.host = host;
            this.user = user;
            this.password = password;
            this.masterFW = "6.48.3";
        }

        public void setHost(string host)
        {
            this.host = host;
        }
        public void setUser(string user)
        {
            this.user = user;
        }
        public void setPassword(string password)
        {
            this.password = password;
        }
        public void setModel(string model)
        {
            this.model = model;
        }
        public void setCFW(string currentFW)
        {
            this.currentFW = currentFW;
        }
        public void setUFW(string upgradeFW)
        {
            this.upgradeFW = upgradeFW;
        }
        public void setUpgradeChecks(Tuple<bool, bool> checks)
        {
            this.upgradeChecks = checks;
        }
    }
}
