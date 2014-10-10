using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataSources;
using SC3Lib = SmartCOM3Lib;

namespace SmartComClass
{
    class SmartCom : IDataSource
    {
        public string ServerIp { get; set; }
        public ushort ServerPort { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }

        public SC3Lib.StServerClass SmartC = null;
        public SmartCom() {}
        public SmartCom(string ip, ushort port, string login, string password)
        {
            this.ServerIp = ip;
            this.ServerPort = port;
            this.Login = login;
            this.Password = password;
            SmartC = new SC3Lib.StServerClass();
        }

        public void ConnectDataSource()
        {
            SmartC.ConfigureClient("logLevel=1");
            SmartC.ConfigureServer("logLevel=1;pingTimeOut=10");
            SmartC.connect(ServerIp, ServerPort, Login, Password);
        }
    }
}
