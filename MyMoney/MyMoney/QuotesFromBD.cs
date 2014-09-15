using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Data;

namespace MyMoney
{
    public class QuotesFromBD : IDataSource
    {
        private object lockObj = new Object();
        public int countThreads = 6;
        private List<Thread> listThreads;
        private SqlConnection sqlconn;
        private SqlCommand sqlcommand = new SqlCommand();
        public DataTable dtInstruments { get; set; }
        public DataTable dtAllTables { get; set; }
        public Dictionary<string, int> dictInstruments;
        public Dictionary<string, tableInfo> dictAllTables;
        public string connectionstr = "user id=sa;password=WaNo11998811mssql;server=localhost;database=smartcom;MultipleActiveResultSets=true";

        private List<ParametrsForTest> parametrsList;
        public Dictionary<string, diapasonTestParam> dicDiapasonParams;

        public event ConnectedHandler OnConnected;

        public event GetInstrumentsHandler OnGetInstruments;

        public event ThreadStarted OnThreadTesterStart;
        public QuotesFromBD() 
        {
            dtInstruments = new DataTable();
            dtAllTables = new DataTable();
            dictInstruments = new Dictionary<string, int>();
            dictAllTables = new Dictionary<string, tableInfo>();
            dicDiapasonParams = new Dictionary<string, diapasonTestParam>();
            parametrsList = new List<ParametrsForTest>();
            listThreads = new List<Thread>();
        }

        public void ConnectToDataSource()
        {
            new Thread(new ThreadStart(ThreadConnect)).Start();
            return;
        }

        public void ThreadConnect() {
            sqlconn = new SqlConnection(connectionstr);
            sqlconn.Open();
            if (OnConnected != null)
                OnConnected("Соединение с базой данных установлено");
        }

        public void GetAllInstruments(){
            new Thread(new ThreadStart(ThreadInstruments)).Start();
        }

        public void ThreadInstruments()
        {
            lock (lockObj)
            {
                sqlcommand.Connection = sqlconn;
                sqlcommand.CommandText = "SELECT * FROM instruments";
                dtInstruments.Load(sqlcommand.ExecuteReader());
                foreach (DataRow item in dtInstruments.Rows)
                {
                    dictInstruments.Add(item["name"].ToString(), int.Parse(item["idinstrument"].ToString()));
                }
                if (OnGetInstruments != null)
                    OnGetInstruments();
            }
        }

        public void GetAllTables(int _idinst)
        {
            lock (lockObj)
            {
                dictAllTables.Clear();
                sqlcommand.CommandText = @"
                    select at.idtable, at.idinstrument, i.name, tt.name as typetname, convert(date, at.datecre) as shortdate, at.datecre from alltables at 
	                    join instruments i on at.idinstrument = i.idinstrument
	                    join typetable tt on at.idtypetable = tt.idtypetable
                    where at.idinstrument = " + _idinst.ToString();
                dtAllTables.Clear();
                dtAllTables.Load(sqlcommand.ExecuteReader());

                foreach (DataRow item in dtAllTables.Rows)
                {
                    Regex r = new Regex(@"((\d+)\.(\d+)\.(\d+))");
                    MatchCollection mc = r.Matches(item["shortdate"].ToString().Remove(10));
                    string sname = item["name"].ToString() + "_" + mc[0].Groups[4] + "-" + mc[0].Groups[3] + "-" + mc[0].Groups[2];
                    string lname = sname + "_" + item["typetname"].ToString();
                    if (!dictAllTables.ContainsKey(lname))
                    {
                        tableInfo ti = new tableInfo();
                        ti.tableType = item["typetname"].ToString();
                        ti.shortName = sname;
                        ti.fullName = lname;
                        ti.dateTable = item["shortdate"].ToString().Remove(10);
                        ti.dayNum = int.Parse(mc[0].Groups[4].ToString());
                        ti.monthNum = int.Parse(mc[0].Groups[3].ToString());
                        ti.yearNum = int.Parse(mc[0].Groups[2].ToString());
                        ti.isntrumentName = item["name"].ToString();
                        dictAllTables.Add(lname, ti);
                    }
                }
            }
        }

        public int GetCountRecrodInTable(string _nameTable)
        {
            sqlcommand.CommandText = "SELECT count(*) FROM [" + _nameTable + "];";
            return (int) sqlcommand.ExecuteScalar();
        }

        public void StartTester()
        {
            parametrsList.Clear();
            for (int i1 = dicDiapasonParams["averageValue"].start; i1 <= dicDiapasonParams["averageValue"].finish; i1 = i1 + dicDiapasonParams["averageValue"].step)
			{
                for (int i2 = dicDiapasonParams["profitValue"].start; i2 <= dicDiapasonParams["profitValue"].finish; i2 = i2 + dicDiapasonParams["profitValue"].step)
                {
                    for (int i3 = dicDiapasonParams["lossValue"].start; i3 <= dicDiapasonParams["lossValue"].finish; i3 = i3 + dicDiapasonParams["lossValue"].step)
                    {
                        for (int i4 = dicDiapasonParams["indicatorValue"].start; i4 <= dicDiapasonParams["indicatorValue"].finish; i4 = i4 + dicDiapasonParams["indicatorValue"].step)
                        {
                            for (int i5 = dicDiapasonParams["martingValue"].start; i5 <= dicDiapasonParams["martingValue"].finish; i5 = i5 + dicDiapasonParams["martingValue"].step)
                            {
                                parametrsList.Add(new ParametrsForTest(i1, i2, i3, i4, i5));
                            }
                        }
                    }
                }
			}
            new Thread(new ThreadStart(DicspatcherThread)).Start();
        }

        private void DicspatcherThread()
        {
            string connectionString = "user id=sa;password=WaNo11998811mssql;server=localhost;database=smartcom;MultipleActiveResultSets=true";
            SqlConnection connectionTh = new SqlConnection(connectionString);
            connectionTh.Open();

            SqlCommand sqlcom = new SqlCommand();
            sqlcom.Connection = connectionTh;
            sqlcom.CommandTimeout = 300;
            sqlcom.CommandText = @"
                SELECT dtserver, price as price, volume as volume, null as bid, null as ask, null AS priceTick, null AS volumetick, null AS idaction, null AS tradeno
	            FROM [RTS-9.14_FT_2014-09-11_bidask] rts 
	            WHERE (convert(time, dtserver, 108) > timefromparts(10, 0, 0, 0, 0)) and (convert(time, dtserver, 108) < timefromparts(23, 0, 0, 0, 0)) 

	            UNION ALL

                SELECT dtserver, null as price, null as volume, bid as bid, ask as ask, null AS priceTick, null AS volumetick, null AS idaction, null AS tradeno
	            FROM [RTS-9.14_FT_2014-09-11_quotes] rts2 
	            WHERE (convert(time, dtserver, 108) > timefromparts(10, 0, 0, 0, 0)) and (convert(time, dtserver, 108) < timefromparts(23, 0, 0, 0, 0)) 

  	            UNION ALL

                SELECT dtserver, null as price, null as volume, null as bid, null as ask, price AS priceTick, volume AS volumetick, idaction AS idaction, tradeno AS tradeno
	            FROM [RTS-9.14_FT_2014-09-11_ticks] rft
	            WHERE (convert(time, dtserver, 108) > timefromparts(10, 0, 0, 0, 0)) AND (convert(time, dtserver, 108) < timefromparts(23, 0, 0, 0, 0)) 

            	ORDER BY dtserver ASC;
            ";

            DataTable dt = new DataTable();
            dt.Load(sqlcom.ExecuteReader());

            while (parametrsList.Count > 0)
            {
                while (listThreads.Count < countThreads && parametrsList.Count > 0)
                {
                    ParametrsForTest pt = parametrsList.First();
                    listThreads.Add(new Thread(new ParameterizedThreadStart(OneThreadTester)));
                    listThreads.Last().Start(new ParametrsForTestObj(pt));
                    parametrsList.Remove(pt);
                }
                List<Thread> listThreadsForDelete = new List<Thread>();
                foreach (Thread t in listThreads)
                {
                    if (!t.IsAlive)
                        listThreadsForDelete.Add(t);
                }
                foreach (Thread t in listThreadsForDelete)
                {
                    listThreads.Remove(t);
                }
                listThreadsForDelete.Clear();
            }

        }

        private void OneThreadTester(object p)
        {

        }
    }
}
