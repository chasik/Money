using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MyMoney
{
    public class QuotesFromSmartCom : IDataSource
    {
        private string login;
        private string password;
        public SmartCOM3Lib.StServerClass scom;
        public DataTable dtInstruments { get; set; }

        public event ConnectedHandler OnConnected;

        public event GetInstrumentsHandler OnGetInstruments;

        public QuotesFromSmartCom(string _login, string _pass)
        {
            login = _login;
            password = _pass;
        }
        public void ConnectToDataSource()
        {
            scom = new SmartCOM3Lib.StServerClass();
            scom.ConfigureClient("logLevel=1");
            scom.ConfigureServer("logLevel=1;pingTimeOut=5");
            scom.connect("mx.ittrade.ru", 8443, login, password);
            scom.Connected += scom_Connected;
        }

        void scom_Connected()
        {
            MessageBox.Show("Connected!!!");
            scom.ListenBidAsks("RTS-12.14_FT");
            scom.UpdateBidAsk += scom_UpdateBidAsk;
            scom.ListenQuotes("RTS-12.14_FT");
            scom.UpdateQuote += scom_UpdateQuote;
            scom.ListenTicks("RTS-12.14_FT");
            scom.AddTick += scom_AddTick;
        }

        void scom_AddTick(string symbol, DateTime datetime, double price, double volume, string tradeno, SmartCOM3Lib.StOrder_Action action)
        {
            throw new NotImplementedException();
        }

        void scom_UpdateQuote(string symbol, DateTime datetime, double open, double high, double low, double close, double last, double volume, double size, double bid, double ask, double bidsize, double asksize, double open_int, double go_buy, double go_sell, double go_base, double go_base_backed, double high_limit, double low_limit, int trading_status, double volat, double theor_price)
        {
            throw new NotImplementedException();
        }

        void scom_UpdateBidAsk(string symbol, int row, int nrows, double bid, double bidsize, double ask, double asksize)
        {
            throw new NotImplementedException();
        }
        public void ThreadInstruments()
        { 

        }

        public void ThreadConnect() 
        { 

        }
        public void GetAllInstruments() 
        {
 
        }
    }
}
