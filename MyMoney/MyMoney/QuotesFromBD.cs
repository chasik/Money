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
        public int countThreads = 5;
        private List<Thread> listThreads;
        private SqlConnection sqlconn;
        private SqlCommand sqlcommand = new SqlCommand();
        public DataTable dtInstruments { get; set; }
        public DataTable dtAllTables { get; set; }
        public Dictionary<string, int> dictInstruments;
        public Dictionary<string, tableInfo> dictAllTables;
        public Dictionary<string, DataTable> dicSelectedDataTables;
        public List<string> selectedSessionList;
        public string connectionstr = "user id=sa;password=WaNo11998811mssql;server=localhost;database=smartcom;MultipleActiveResultSets=true";

        private List<ParametrsForTest> parametrsList;
        public Dictionary<string, diapasonTestParam> dicDiapasonParams;

        public delegate void ChangeProgressEvent(int minval, int maxval, int val, string mes = "", bool showProgress = true);
        public delegate void FinishOneThread(ParametrsForTest paramTh, ResultOneThread resTh);

        public event FinishOneThread OnFinishOneThread;
        public event ChangeProgressEvent OnChangeProgress;

        public event ConnectedHandler OnConnected;

        public event GetInstrumentsHandler OnGetInstruments;

        public event ThreadStarted OnThreadTesterStart;
        public QuotesFromBD() 
        {
            dtInstruments = new DataTable();
            dtAllTables = new DataTable();
            dictInstruments = new Dictionary<string, int>();
            dictAllTables = new Dictionary<string, tableInfo>();
            dicSelectedDataTables = new Dictionary<string, DataTable>();
            selectedSessionList = new List<string>();
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
                        ti.dayNum = int.Parse(mc[0].Groups[2].ToString());
                        ti.monthNum = int.Parse(mc[0].Groups[3].ToString());
                        ti.yearNum = int.Parse(mc[0].Groups[4].ToString());
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
            selectedSessionList.ForEach((string selectedIsntr) =>
            {
                for (int i0 = dicDiapasonParams["glassHeight"].start; i0 <= dicDiapasonParams["glassHeight"].finish; i0 = i0 + dicDiapasonParams["glassHeight"].step)
                {
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
                                        parametrsList.Add(new ParametrsForTest(selectedIsntr, i0, i1, i2, i3, i4, i5));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            );
            new Thread(new ThreadStart(DicspatcherThread)).Start();
        }

        private void DicspatcherThread()
        {
            string connectionString = "user id=sa;password=WaNo11998811mssql;server=localhost;database=smartcom;MultipleActiveResultSets=true";
            SqlConnection connectionTh = new SqlConnection(connectionString);
            connectionTh.Open();
            //загрузка данных в datatable для всех выбранных дат
            int stepProgress = 1;
            selectedSessionList.ForEach((string tabNam) =>
            {
                if (OnChangeProgress != null)
                    OnChangeProgress(0, selectedSessionList.Count, stepProgress++);
                SqlCommand sqlcom = new SqlCommand();
                sqlcom.Connection = connectionTh;
                sqlcom.CommandTimeout = 300;
                sqlcom.CommandText = @"
                SELECT dtserver, price as price, volume as volume, null as bid, null as ask, null AS priceTick, null AS volumetick, null AS idaction, null AS tradeno
	            FROM [" + tabNam + @"_bidask] rts 
	            WHERE (convert(time, dtserver, 108) > timefromparts(10, 0, 0, 0, 0)) and (convert(time, dtserver, 108) < timefromparts(23, 0, 0, 0, 0)) 

	            UNION ALL

                SELECT dtserver, null as price, null as volume, bid as bid, ask as ask, null AS priceTick, null AS volumetick, null AS idaction, null AS tradeno
	            FROM [" + tabNam + @"_quotes] rts2 
	            WHERE (convert(time, dtserver, 108) > timefromparts(10, 0, 0, 0, 0)) and (convert(time, dtserver, 108) < timefromparts(23, 0, 0, 0, 0)) 

  	            UNION ALL

                SELECT dtserver, null as price, null as volume, null as bid, null as ask, price AS priceTick, volume AS volumetick, idaction AS idaction, tradeno AS tradeno
	            FROM [" + tabNam + @"_ticks] rft
	            WHERE (convert(time, dtserver, 108) > timefromparts(10, 0, 0, 0, 0)) AND (convert(time, dtserver, 108) < timefromparts(23, 0, 0, 0, 0)) 

            	ORDER BY dtserver ASC;
            ";
                DataTable dt = new DataTable();
                dt.Load(sqlcom.ExecuteReader());
                dicSelectedDataTables.Add(tabNam, dt);
            });
            if (OnChangeProgress != null)
                OnChangeProgress(0, 0, 0, "", false);
            int plStartCount = parametrsList.Count;
            while (parametrsList.Count > 0)
            {
                while (listThreads.Count < countThreads && parametrsList.Count > 0)
                {
                    ParametrsForTest pt = parametrsList.First();
                    listThreads.Add(new Thread(new ParameterizedThreadStart(OneThreadTester)));
                    listThreads.Last().IsBackground = true;
                    listThreads.Last().Start(new ParametrsForTestObj(pt, dicSelectedDataTables[pt.shortName].Copy()));
                    parametrsList.Remove(pt);
                    if (OnChangeProgress != null)
                        OnChangeProgress(0, plStartCount, plStartCount - parametrsList.Count);
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
            ResultOneThread resTh = new ResultOneThread();
            List<int> oldGlassValue = new List<int>();
            Dictionary<int, int> glass = new Dictionary<int, int>();
            int priceEnterLong = 0, priceEnterShort = 0;
            int? bid = 0, ask = 0;
            int? pricetick = 0;
            byte? actiontick = 0;
            DataTable dt = (p as ParametrsForTestObj).dataTableCopy;
            ParametrsForTest paramTh = (p as ParametrsForTestObj).paramS;
            foreach (DataRow dr in dt.Rows)
            {
                // совершена сделка
                if (!dr.IsNull("priceTick"))
                {
                    pricetick = (int?) dr.Field<float?>("priceTick");
                    actiontick = dr.Field<byte?>("idaction");
                    if (actiontick == 1)
                    {
                        ask = pricetick;
                        if (priceEnterShort != 0)
                        {
                            if (priceEnterShort - paramTh.profitValue >= ask)
                            {
                                resTh.countProfitDeal++;
                                resTh.profit += priceEnterShort - (int)ask;
                                priceEnterShort = 0;
                            }
                            else if (priceEnterShort + paramTh.lossValue <= ask)
                            {
                                resTh.countLossDeal++;
                                resTh.loss += (int)ask - priceEnterShort;
                                priceEnterShort = 0;
                            }
                        }
                    }
                    else if (actiontick == 2)
                    {
                        bid = pricetick;
                        if (priceEnterLong != 0)
                        {
                            if (priceEnterLong + paramTh.profitValue <= bid)
                            {
                                resTh.countProfitDeal++;
                                resTh.profit += (int)bid - priceEnterLong;
                                priceEnterLong = 0;
                            }
                            else if (priceEnterLong - paramTh.lossValue >= bid)
                            {
                                resTh.countLossDeal++;
                                resTh.loss += priceEnterLong - (int)bid;
                                priceEnterLong = 0;
                            }
                        }
                    }
                }
                // изменение в стакане
                else if (!dr.IsNull("price"))
                {
                    int updatepricegl = (int)dr.Field<float?>("price");
                    int updatevolumegl = (int)dr.Field<float?>("volume");
                    if (!glass.ContainsKey(updatepricegl))
                        glass.Add(updatepricegl, updatevolumegl);
                    else
                    {
                        glass[updatepricegl] = updatevolumegl;
                        if (glass.Count > 40)
                        {
                            int sumGlass = 0;
                            oldGlassValue.Clear();
                            foreach (int pkey in glass.Keys)
                            {
                                if (pkey > ask + 300 || pkey < bid - 300)
                                    oldGlassValue.Add(pkey);
                                else if (pkey >= ask || pkey <= bid)
                                    sumGlass += glass[pkey];
                            }
                            // удаляем из стакана значения, выпадающие за пределы глубины стакана
                            oldGlassValue.ForEach((int i) => {
                                glass.Remove(i);
                            });
                            // среднее значение по стакану
                            int averageGlass = (int)sumGlass / 60;
                            int sumlong = 0, sumshort = 0;
                            foreach (int pkey in glass.Keys)
                            {
                                if (pkey >= ask && glass[pkey] < averageGlass * paramTh.averageValue)
                                {
                                    sumlong += glass[pkey];
                                }
                                else if (pkey <= bid && glass[pkey] < averageGlass * paramTh.averageValue)
                                {
                                    sumshort += glass[pkey];
                                }
                            }
                            int indicator = (sumlong + sumshort) != 0 ? (int) (sumlong - sumshort) * 100 / (sumlong + sumshort) : 0;
                            if (indicator >= paramTh.indicatorValue && priceEnterLong == 0 && priceEnterShort == 0)
                            {
                                priceEnterLong = (int) ask;
                            }
                            else if (indicator <= -paramTh.indicatorValue && priceEnterLong == 0 && priceEnterShort == 0)
                            {
                                priceEnterShort = (int) bid;
                            }
                        }
                    }

                }
                else if (!dr.IsNull("bid"))
                {
                    bid = (int?) dr.Field<float?>("bid");
                    ask = (int?) dr.Field<float?>("ask");
                }
            }

            lock (lockObj)
            {
                resTh.profitFactor = (float)resTh.profit / (float)resTh.loss;
                if (OnFinishOneThread != null)
                    OnFinishOneThread(paramTh, resTh);
            }
        }

    }
}
