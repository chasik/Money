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
        private int _idinstrument = 0;
        private string login = "";
        private string password = "";
        private bool instrumentIsFound = false;

        Dictionary<string, string> tablesnames = new Dictionary<string, string>() { 
            {"ticks", ""},
            {"bidask", ""}, 
            {"quotes", ""}
        };

        private SmartCom smartCom;
        private SqlConnection sqlconn;
        private DataTable dtable;

        public string Instrument { get { return instrument; } set { instrument = value; } }
        public int IdInstrument { get { return _idinstrument; } set { _idinstrument = value; } }

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

            instrument = _instrument.ToUpper();
            login = _login;
            password = _password;

            smartCom = new SmartCom("mx.ittrade.ru", 8443, login, password);
            smartCom.SmartC.Connected       += new SmartCOM3Lib._IStClient_ConnectedEventHandler(this.ShowConnected);
            smartCom.SmartC.Disconnected    += new SmartCOM3Lib._IStClient_DisconnectedEventHandler(this.ShowDisconnected);
            smartCom.SmartC.AddSymbol       += new SmartCOM3Lib._IStClient_AddSymbolEventHandler(this.AddSymbol);
            smartCom.ConnectDataSource();
        }


        private void ShowConnected()
        {
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

        private void AddSymbol(int row, int nrows, string symbol, string short_name, string long_name, string type, int decimals, int lot_size, double punkt, double step, string sec_ext_id, string sec_exch_name, DateTime expiry_date, double days_before_expiry, double strike)
        {
            if (symbol.ToUpper().Contains(instrument) && !instrumentIsFound)
            {
                instrumentIsFound = true;
                smartCom.SmartC.UpdateBidAsk += SmartC_UpdateBidAsk;
                smartCom.SmartC.ListenBidAsks(symbol);
                smartCom.SmartC.AddTick += SmartC_AddTick;
                smartCom.SmartC.ListenTicks(symbol);
                StartQuotesToBD(short_name, long_name, type, decimals, lot_size, punkt, step, sec_ext_id, sec_exch_name, expiry_date, days_before_expiry, strike);
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

        private void SmartC_AddTick(string symbol, DateTime datetime, double price, double volume, string tradeno, StOrder_Action action)
        {
            SmartWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    SmartWindow.textBox1.AppendText("" + symbol + "\n");
                }
            );
        }

        private void CheckTables(string[] _arr)
        {
            _idinstrument = int.Parse(new SqlCommand(@"SELECT idinstrument FROM instruments WHERE name = '" + instrument + "'", sqlconn).ExecuteScalar().ToString());

            foreach(string typetablename in _arr){
                object atabob = new SqlCommand(@"SELECT i.name + '_' + cast(convert(date, a.datecre) as varchar) + '_' + t.name FROM alltables a 
                                        JOIN typetable t ON a.idtypetable = t.idtypetable AND t.name = '" + typetablename + @"' AND convert(date, a.datecre) = convert(date, GETDATE())
                                        JOIN instruments i ON i.idinstrument = a.idinstrument AND i.idinstrument = " + IdInstrument.ToString()
                    , sqlconn).ExecuteScalar();
                if (atabob == null)
                {
                    string idtypetable = new SqlCommand("SELECT idtypetable FROM typetable WHERE name = '" + typetablename + "'", sqlconn).ExecuteScalar().ToString();
                    new SqlCommand(@"INSERT INTO alltables (idinstrument, idtypetable, datecre) 
                                        VALUES (" + _idinstrument.ToString() + @", " + idtypetable + ", GETDATE())"
                        
                        , sqlconn).ExecuteNonQuery();
                    string cretabq = "";
                    switch (typetablename)
                    {
                        case "ticks":
                            break;
                        case "bidask":
                            break;
                        case "quotes":
                            break;
                        default:
                            MessageBox.Show("Не могу определить тип таблицы!");
                            break;
                    }
                }
                else
                {
                    tablesnames[typetablename] = atabob.ToString();
                }
            }
        }

        private void StartQuotesToBD(string short_name, string long_name, string type, int decimals, int lot_size, double punkt, double step, string sec_ext_id, string sec_exch_name, DateTime expiry_date, double days_before_expiry, double strike)
        {
            // проверяем, использовался ли ранее инструмент - если да - обновляем дату записи данных, иначе - создаем запись в instruments
            if (new SqlCommand("SELECT idinstrument FROM instruments WHERE name = '" + instrument + "'", sqlconn).ExecuteScalar() == null)
            {
                new SqlCommand(@"INSERT INTO instruments (name, shortname, longname, codetype, decimals, lotsize, punkt, step, secextid, secexchname, expirydate, strike, datecre, datelast) 
                                    VALUES ('" + instrument + "', '" + short_name + "', '" + long_name + "', '" + type + "', " + decimals.ToString().Replace(',', '.')
                                               + ", " + lot_size.ToString().Replace(',', '.') + ", " + punkt.ToString().Replace(',', '.') + ", " + step.ToString().Replace(',', '.')
                                               + ", '" + sec_ext_id + "', '" + sec_exch_name + "', '" + expiry_date.ToString()
                                               + "', '" + strike.ToString().Replace(',', '.') + "', GETDATE(), GETDATE());", sqlconn).ExecuteNonQuery();
            }
            else
            {
                new SqlCommand("UPDATE instruments SET datelast = GETDATE() WHERE name = '" + instrument + "';", sqlconn).ExecuteNonQuery();
            }

            // проверка, создавались ли уже таблицы по данному инструменту за текущую дату - если нет - создаем соответсвующую
            CheckTables(new string[] {"ticks", "bidask", "quotes"});
        }

    }
}

//
// создание таблиц
//
//DBCC CHECKIDENT (alltables,RESEED, 0)
/*create table instruments(
	idinstrument int not null identity(1,1),
	name varchar(50) not null,
	shortname varchar(50),
	longname varchar(100),
	codetype varchar(100),
	decimals int,
	lotsize int,
	punkt real,
	step real,
	secextid varchar(100),
	secexchname varchar(100),
	expirydate datetime,
	strike real,
	datecre datetime,
	datelast datetime
)
 
 create table alltables (
	idtable int not null identity(1,1),
	name varchar(100),
	idinstrument int,
	typetable int,
	datecre date
)

create table typetable(
	idtypetable int not null identity(1,1),
	name varchar(30)
)
 
 insert into typetable (name) values ('ticks')
 
 */