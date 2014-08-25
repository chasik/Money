using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Data;
using System.Data.SqlClient;
using SmartComClass;
using System.Windows.Threading;
using SmartCOM3Lib;

namespace WpfRobot
{
    class QuotesThread 
    {
        private string instrument = "";
        private string login = "";
        private string password = "";
        private SmartCom smartCom;
        private SqlConnection sqlconn;
        private DataTable dtable;

        public string Instrument { get { return instrument; } set { instrument = value; } }

        public MainWindow SmartWindow;

        public QuotesThread(MainWindow _mw, string _instrument, string _login, string _password) 
        {
            SmartWindow = _mw;
            string connectionStr = "user id=sa;password=WaNo11998811mssql;server=localhost;database=smartcom";
            sqlconn = new SqlConnection(connectionStr);
            sqlconn.Open();

            //SqlCommand sq = new SqlCommand("select * from test", sqlconn);
            //SqlDataReader sr = sq.ExecuteReader();
            //while (sr.Read()){
                //SmartWindow.textBox1.AppendText(sr[1] + "\n");
            //}
            //sr.Close();

            instrument = _instrument;
            login = _login;
            password = _password;

            smartCom = new SmartCom("mx.ittrade.ru", 8443, login, password);
            smartCom.SmartC.Connected       += new SmartCOM3Lib._IStClient_ConnectedEventHandler(this.ShowConnected);
            smartCom.SmartC.Disconnected    += new SmartCOM3Lib._IStClient_DisconnectedEventHandler(this.ShowDisconnected);
            smartCom.SmartC.AddPortfolio    += new SmartCOM3Lib._IStClient_AddPortfolioEventHandler(this.AddPortfolio);
            smartCom.SmartC.AddSymbol       += new SmartCOM3Lib._IStClient_AddSymbolEventHandler(this.AddSymbol);
            smartCom.ConnectDataSource();
        }

        private void ShowConnected()
        {
            smartCom.SmartC.GetPrortfolioList();
            smartCom.SmartC.GetSymbols();
            SmartWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    SmartWindow.textBox1.AppendText(instrument +  " -- Connected!!! \n");
                }
            );
        }

        private void ShowDisconnected(string _reason)
        {
            MessageBox.Show("Отключение: " + _reason);
        }

        private void AddPortfolio(int row, int nrows, string portfolioName, string portfolioExch, StPortfolioStatus portfolioStatus)
        {
            SmartWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    SmartWindow.textBox1.AppendText("Инструмент: " + portfolioName + "\n");
                }
            );
        }

        private void AddSymbol(int row, int nrows, string symbol, string short_name, string long_name, string type, int decimals, int lot_size, double punkt, double step, string sec_ext_id, string sec_exch_name, DateTime expiry_date, double days_before_expiry, double strike)
        {
            if (symbol.ToUpper().Contains(instrument))
            {
                smartCom.SmartC.ListenBidAsks(symbol);
                smartCom.SmartC.UpdateBidAsk += SmartC_UpdateBidAsk;
            }
        }

        private void SmartC_UpdateBidAsk(string symbol, int row, int nrows, double bid, double bidsize, double ask, double asksize)
        {
            SmartWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    SmartWindow.Label2.Content = instrument + "\t" + symbol + "\t" + bid.ToString() + "\t" + ask.ToString() + "\n";
                }
            );
        }

    }
}
