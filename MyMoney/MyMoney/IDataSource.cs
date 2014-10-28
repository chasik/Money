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
    public delegate void ThreadStarted(string message);
    public delegate void GetInformation(string _mess);
    public delegate void ChangeGlass(double _p, double _v, int _row, ActionGlassItem _a);
    public delegate void AddTick(double _p, double _v, ActionGlassItem _a);
    public delegate void ChangeVisualIndicator(int[] _ind, int[] _indAverage);

    public interface IDataSource
    {
        DataTable dtInstruments {get;set;}
        void ConnectToDataSource();
        void ThreadConnect();
        void GetAllInstruments();
        void ThreadInstruments();
        event ConnectedHandler OnConnected;
        event GetInstrumentsHandler OnGetInstruments;
        event GetInformation OnInformation;

        event ChangeGlass OnChangeGlass;
        event ChangeVisualIndicator OnChangeVisualIndicator;
        event AddTick OnAddTick;

    }
}
