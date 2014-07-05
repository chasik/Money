using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSources
{
    public interface IDataSource
    {
        string ServerIp { get; set; }
        ushort ServerPort { get; set; }
        string Login { get; set; }
        string Password { get; set; }

        void ConnectDataSource();
    }
}
