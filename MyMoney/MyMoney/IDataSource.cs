using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMoney
{
    public delegate void ConnectedHandler(string message);
    public delegate void GetInstrumentsHandler();
    public interface IDataSource
    {
        DataTable dtInstruments {get;set;}
        void ConnectToDataSource();
        void ThreadConnect();
        void GetAllInstruments();
        void ThreadInstruments();
        event ConnectedHandler OnConnected;
        event GetInstrumentsHandler OnGetInstruments;
    }
}
