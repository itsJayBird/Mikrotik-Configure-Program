using System;
using System.Collections.Generic;
using System.Text;

namespace MikrotikConfig
{
    class RouterInfo
    {
        public string host { get; set; }
        public string user { get; set; }
        public string password { get; set; }
        public string model { get; set; }
        public string fileName { get; set; }

        public RouterInfo() { }
        public RouterInfo(string host, string user, string password)
        {
            this.host = host;
            this.user = user;
            this.password = password;
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
        public void setFileName(string fileName)
        {
            this.fileName = fileName;
        }
    }
}
