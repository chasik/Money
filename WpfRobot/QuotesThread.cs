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

        Dictionary<string, string> glass = new Dictionary<string, string>();

        Dictionary<string, string> lastprice = new Dictionary<string, string>() { 
            {"lastbid", ""},
            {"lastask", ""}
        };

        private SmartCom smartCom;
        private SqlConnection sqlconn;

        public string Instrument { get { return instrument; } set { instrument = value; } }
        public int IdInstrument { get { return _idinstrument; } set { _idinstrument = value; } }

        public MainWindow SmartWindow;

        public QuotesThread(MainWindow _mw, string _instrument, string _login, string _password) 
        {
            SmartWindow = _mw;

            string connectionStr = "user id=sa;password=WaNo11998811mssql;server=localhost;database=smartcom;MultipleActiveResultSets=true";
            sqlconn = new SqlConnection(connectionStr);
            sqlconn.Open();

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
            MessageBox.Show("Отключение. Причина: " + _reason);
            smartCom.ConnectDataSource();
        }

        private void AddSymbol(int row, int nrows, string symbol, string short_name, string long_name, string type, int decimals, int lot_size, double punkt, double step, string sec_ext_id, string sec_exch_name, DateTime expiry_date, double days_before_expiry, double strike)
        {
            if (symbol.ToUpper().Contains(instrument) && !instrumentIsFound)
            {
                instrumentIsFound = true;
                smartCom.SmartC.UpdateBidAsk               += SmartC_UpdateBidAsk;
                smartCom.SmartC.ListenBidAsks(symbol);
                smartCom.SmartC.AddTick                    += SmartC_AddTick;
                smartCom.SmartC.ListenTicks(symbol);
                smartCom.SmartC.UpdateQuote                += SmartC_UpdateQuote;
                smartCom.SmartC.ListenQuotes(symbol);
                StartQuotesToBD(short_name, long_name, type, decimals, lot_size, punkt, step, sec_ext_id, sec_exch_name, expiry_date, days_before_expiry, strike);
            }
        }

        private void SmartC_UpdateQuote(string symbol, DateTime datetime, double open, double high, double low, double close, double last, double volume, double size, double bid, 
            double ask, double bidsize, double asksize, double open_int, double go_buy, double go_sell, double go_base, double go_base_backed, double high_limit, double low_limit,
            int trading_status, double volat, double theor_price)
        {
            SmartWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                }
            );

            if (tablesnames["quotes"] == "")
                return;

            string bidstr = bid.ToString().Replace(',', '.');
            string askstr = ask.ToString().Replace(',', '.');
            string bidsizestr = bidsize.ToString().Replace(',', '.');
            string asksizestr = asksize.ToString().Replace(',', '.');
            string laststr = last.ToString().Replace(',', '.');
            string lastsizestr = size.ToString().Replace(',', '.');
            string allvolume = volume.ToString().Replace(',', '.');

            string openintstr = open_int.ToString().Replace(',', '.');
            string volatstr = volat.ToString().Replace(',', '.');
            string theorpricestr = theor_price.ToString().Replace(',', '.');


            if (!lastprice["lastbid"].Equals(bidstr) || !lastprice["lastask"].Equals(askstr))
            {
                new SqlCommand("INSERT INTO [" + tablesnames["quotes"] + "]"
                                 + " (dtlasttick, lastprice, lastvolume, allvolume, bid, ask, bidsize, asksize, openint, volat, theorprice)"
                                 + " VALUES ('" + datetime.ToString() + "', " + laststr + ", " + lastsizestr + ", " + allvolume + ", " + bidstr + ", " + askstr + ", "
                                 + bidsizestr + ", " + asksizestr + ", " + openintstr + ", " + volatstr + ", " + theorpricestr + ");"
                    , sqlconn).ExecuteNonQueryAsync();

                lastprice["lastbid"] = bidstr;
                lastprice["lastask"] = askstr;
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
            if (tablesnames["bidask"] == "")
                return;

            string bidstr = bid.ToString().Replace(',', '.');
            string askstr = ask.ToString().Replace(',', '.');
            string bidsizestr = bidsize.ToString().Replace(',', '.');
            string asksizestr = asksize.ToString().Replace(',', '.');

            bool updatebid = false, updateask = false;

            if (!glass.ContainsKey(bidstr))
            {
                updatebid = true;
                glass.Add(bidstr, bidsizestr);
            }
            else if (!glass[bidstr].Equals(bidsizestr))
            {
                updatebid = true;
                glass[bidstr] = bidsizestr;
            }


            if (updatebid)
            {
                new SqlCommand("INSERT INTO [" + tablesnames["bidask"] + "]"
                                 + " (price, volume)"
                                 + " VALUES (" + bidstr + ", " + bidsizestr + ");"
                    , sqlconn).ExecuteNonQueryAsync();
            }


            if (!glass.ContainsKey(askstr))
            {
                updateask = true;
                glass.Add(askstr, asksizestr);
            }
            else if (!glass[askstr].Equals(asksizestr))
            {
                updateask = true;
                glass[askstr] = asksizestr;
            }

            if (updateask)
            {
                new SqlCommand("INSERT INTO [" + tablesnames["bidask"] + "]"
                                 + " (price, volume)"
                                 + " VALUES (" + askstr + ", " + asksizestr + ");"
                    , sqlconn).ExecuteNonQueryAsync();
            }

        }

        private void SmartC_AddTick(string symbol, DateTime datetime, double price, double volume, string tradeno, StOrder_Action action)
        {
            SmartWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                }
            );

            if (tablesnames["ticks"] == "")
                return;

            string a = "0";
            switch (action)
	        {
		        case StOrder_Action.StOrder_Action_Buy:
                    a = "1";
                    break;
                case StOrder_Action.StOrder_Action_Cover:
                    break;
                case StOrder_Action.StOrder_Action_Sell:
                    a = "2";
                    break;
                case StOrder_Action.StOrder_Action_Short:
                    break;
                default:
                    break;
	        }

            new SqlCommand("INSERT INTO [" + tablesnames["ticks"] + "]"
                             + " (dttick, price, volume, tradeno, idaction)"
                             + " VALUES ('" + datetime.ToString() + "', " + price.ToString().Replace(',', '.') + ", " + volume.ToString().Replace(',', '.') + ", '" + tradeno + "', " + a + ")"
                , sqlconn).ExecuteNonQueryAsync();
        }

        private void CheckTables(string[] _arr)
        {
            _idinstrument = int.Parse(new SqlCommand(@"SELECT idinstrument FROM instruments WHERE name = '" + instrument + "'", sqlconn).ExecuteScalar().ToString());

            foreach(string typetablename in _arr){
                object atabob = GetTableName(typetablename);
                if (atabob == null)
                {
                    string idtypetable = new SqlCommand("SELECT idtypetable FROM typetable WHERE name = '" + typetablename + "'", sqlconn).ExecuteScalar().ToString();

                    new SqlCommand(@"INSERT INTO alltables (idinstrument, idtypetable, datecre) 
                                        VALUES (" + IdInstrument.ToString() + @", " + idtypetable + ", GETDATE())"
                        , sqlconn).ExecuteNonQuery();

                    string cretabq = "";
                    switch (typetablename)
                    {
                        case "ticks":
                            cretabq = @"idtick int not null IDENTITY(1,1),
                                        dtserver datetime2 DEFAULT SYSDATETIME(),
                                        dttick datetime,
                                        price real,
                                        volume real,
                                        tradeno varchar(12),
                                        idaction tinyint";
                            break;
                        case "bidask":
                            cretabq = @"idbidask int not null IDENTITY(1,1),
                                        dtserver datetime2 DEFAULT SYSDATETIME(),
                                        price real,
                                        volume real";
                            break;
                        case "quotes":
                            cretabq = @"idquote int not null IDENTITY(1,1),
                                        dtserver datetime2 DEFAULT SYSDATETIME(),
                                        dtlasttick datetime,
                                        lastprice real,
                                        lastvolume real,
                                        allvolume real, 
                                        bid real,
                                        ask real,
                                        bidsize real,
                                        asksize real,
                                        openint real,
                                        volat real, 
                                        theorprice real
                                       ";
                            break;
                        default:
                            MessageBox.Show("Не могу определить тип таблицы!");
                            break;
                    }
                    atabob = GetTableName(typetablename);
                    cretabq = "CREATE TABLE [" + atabob + "] (" + cretabq + ")";

                    new SqlCommand(cretabq, sqlconn).ExecuteNonQuery();
                }

                tablesnames[typetablename] = atabob.ToString();

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

        private object GetTableName(string typetablename)
        {
            return new SqlCommand(@"SELECT i.name + '_' + cast(convert(date, a.datecre) as varchar) + '_' + t.name FROM alltables a 
                                        JOIN typetable t ON a.idtypetable = t.idtypetable AND t.name = '" + typetablename + @"' AND convert(date, a.datecre) = convert(date, GETDATE())
                                        JOIN instruments i ON i.idinstrument = a.idinstrument AND i.idinstrument = " + IdInstrument.ToString()
                    , sqlconn).ExecuteScalar();
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