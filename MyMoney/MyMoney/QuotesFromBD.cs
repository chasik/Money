using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Data;
using System.Collections.ObjectModel;

namespace MyMoney
{
    public class QuotesFromBD : IDataSource
    {
        private object lockObj = new Object();
        public int countThreads = 0;
        private List<Thread> listThreads;
        private SqlConnection sqlconn;
        private SqlCommand sqlcommand = new SqlCommand();
        public DataTable dtInstruments { get; set; }
        public DataTable dtAllTables { get; set; }
        public Dictionary<string, int> dictInstruments;
        public Dictionary<string, tableInfo> dictAllTables;
        public Dictionary<string, DataTable> dicSelectedDataTables;
        public List<string> selectedSessionList;

        public SortedDictionary<ResultBestProfitFactor, ResultOneThreadSumm> dicAllProfitResult = new SortedDictionary<ResultBestProfitFactor, ResultOneThreadSumm>();

        public string connectionstr = "user id=sa;password=WaNo11998811mssql;server=localhost;database=smartcom;MultipleActiveResultSets=true";

        private List<ParametrsForTest> parametrsList;
        public Dictionary<string, diapasonTestParam> dicDiapasonParams;

        public delegate void ChangeProgressEvent(int minval, int maxval, int val, string mes = "", bool showProgress = true);
        public delegate void FinishOneThread(ResultOneThreadSumm resTh);

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

        public void GetAllTables(int[] _idinst)
        {
            string idinstINstr = "";
            for (int i = 0; i < _idinst.Length; i++)
            {
                idinstINstr += _idinst[i];
                if (i != _idinst.Length - 1)
                    idinstINstr += ", ";
            }
            lock (lockObj)
            {
                dictAllTables.Clear();
                sqlcommand.CommandText = @"
                    select at.idtable, at.idinstrument, i.name, tt.name as typetname, convert(date, at.datecre) as shortdate, at.datecre from alltables at 
	                    join instruments i on at.idinstrument = i.idinstrument
	                    join typetable tt on at.idtypetable = tt.idtypetable
                    where at.idinstrument in (" + idinstINstr + ")";
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
            int idParametrsRow = 0;
            parametrsList.Clear();
            selectedSessionList.ForEach((string selectedIsntr) =>
            { });
            for (int i0 = dicDiapasonParams["glassHeight"].start; i0 <= dicDiapasonParams["glassHeight"].finish; i0 = i0 + dicDiapasonParams["glassHeight"].step)
            {
                for (int i3 = dicDiapasonParams["lossLongValue"].start; i3 <= dicDiapasonParams["lossLongValue"].finish; i3 = i3 + dicDiapasonParams["lossLongValue"].step)
                {
                    for (int i2 = dicDiapasonParams["profitLongValue"].start; i2 <= dicDiapasonParams["profitLongValue"].finish; i2 = i2 + dicDiapasonParams["profitLongValue"].step)
                    {
                        for (int i1 = dicDiapasonParams["averageValue"].start; i1 <= dicDiapasonParams["averageValue"].finish; i1 = i1 + dicDiapasonParams["averageValue"].step)
                        {
                            for (int i4 = dicDiapasonParams["indicatorLongValue"].start; i4 <= dicDiapasonParams["indicatorLongValue"].finish; i4 = i4 + dicDiapasonParams["indicatorLongValue"].step)
                            {
                                for (int i8 = dicDiapasonParams["indicatorShortValue"].start; i8 <= dicDiapasonParams["indicatorShortValue"].finish; i8 = i8 + dicDiapasonParams["indicatorShortValue"].step)
                                {
                                    for (int i5 = dicDiapasonParams["martingValue"].start; i5 <= dicDiapasonParams["martingValue"].finish; i5 = i5 + dicDiapasonParams["martingValue"].step)
                                    {
                                        for (int i6 = dicDiapasonParams["lossShortValue"].start; i6 <= dicDiapasonParams["lossShortValue"].finish; i6 = i6 + dicDiapasonParams["lossShortValue"].step)
                                        {
                                            for (int i7 = dicDiapasonParams["profitShortValue"].start; i7 <= dicDiapasonParams["profitShortValue"].finish; i7 = i7 + dicDiapasonParams["profitShortValue"].step)
                                            {
                                                idParametrsRow++;
                                                parametrsList.Add(new ParametrsForTest(idParametrsRow, selectedSessionList, i0, i1, i2, i3, i4, i5, i6, i7, i8));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Thread dispatcherForAllThreads = new Thread(new ThreadStart(DicspatcherThread));
            dispatcherForAllThreads.IsBackground = true;
            dispatcherForAllThreads.Start();
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
            "
	         /*   UNION ALL

                SELECT dtserver, null as price, null as volume, bid as bid, ask as ask, null AS priceTick, null AS volumetick, null AS idaction, null AS tradeno
	            FROM [" + tabNam + @"_quotes] rts2 
	            WHERE (convert(time, dtserver, 108) > timefromparts(10, 0, 0, 0, 0)) and (convert(time, dtserver, 108) < timefromparts(23, 0, 0, 0, 0)) 
              */
            + @"
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
            Random rnd = new Random();
            while (parametrsList.Count > 0)
            {
                while (listThreads.Count < countThreads && parametrsList.Count > 0)
                {
                    ParametrsForTest pt;
                    if (dicAllProfitResult.Count < 27)
                        pt = parametrsList[new Random().Next(0, parametrsList.Count)];
                    else
                    {
                        int o1 = rnd.Next(1, 5);
                        int o2 = rnd.Next(1, 25);
                        if (o1 == o2)
                            o2 += 1;
                        ParametrsForTest param1 = parametrsList[new Random().Next(0, parametrsList.Count - 1)];
                        ParametrsForTest param2 = parametrsList[new Random().Next(0, parametrsList.Count - 1)];
                        lock (lockObj)
                        {
                            foreach (ResultBestProfitFactor item in dicAllProfitResult.Keys)
                            {
                                o1--; o2--;
                                if (o1 < 0 && o2 < 0)
                                    break;
                                if (o1 == 0 && dicAllProfitResult.ContainsKey(item))
                                    param1 = dicAllProfitResult[item].paramForTest;
                                if (o2 == 0 && dicAllProfitResult.ContainsKey(item))
                                    param2 = dicAllProfitResult[item].paramForTest;
                            }
                        }
                        ParametrsForTest[] ptForTest = { param1, param2 };
                        ParametrsForTest ptTemp = new ParametrsForTest(ptForTest);

                        pt = parametrsList.Find(x => x.Compare(x, ptTemp));
                        while (pt.id == 0)
                        {
                            pt = parametrsList.Find(x => x.Compare(x, ptTemp));
                            int mutantParamId = rnd.Next(1, 9);
                            foreach (diapasonTestParam dp in dicDiapasonParams.Values)
	                        {
		                        if (dp.idParam == mutantParamId)
                                    ptTemp.Mutation(dp);
	                        }
                        }
                    }
                    listThreads.Add(new Thread(new ParameterizedThreadStart(OneThreadTester)));
                    listThreads.Last().IsBackground = true;
                    listThreads.Last().Start(new ParametrsForTestObj(pt, dicSelectedDataTables, plStartCount - parametrsList.Count));
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
            ResultOneThreadSumm resTh = new ResultOneThreadSumm();
            List<int> oldGlassValue = new List<int>();
            List<int> tempListForIndicator = new List<int>();
            SortedDictionary<int, int> glass = new SortedDictionary<int, int>();
            int priceEnterLong, priceEnterShort ;
            int lotCount = 1;
            int? bid = 0, ask = 0;
            int? pricetick = 0;
            byte? actiontick = 0;
            Dictionary<string, DataTable> dictionaryDT = (p as ParametrsForTestObj).dictionaryDT;
            ParametrsForTest paramTh = (p as ParametrsForTestObj).paramS;
            int lossLongValueTemp = paramTh.lossLongValue, profitLongValueTemp = paramTh.profitLongValue;
            int lossShortValueTemp = paramTh.lossShortValue, profitShortValueTemp = paramTh.profitShortValue;
            foreach (string k in dictionaryDT.Keys)
            {
                priceEnterLong = 0; priceEnterShort = 0;
                ResultOneThread resThTemp = new ResultOneThread();
                resThTemp.shortName = k;
                DealInfo dealTemp = null;
                DataTable dt = dictionaryDT[k];//.Copy();
                int indicator = 0;
                int iterationNum = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    #region торговля
                    iterationNum++;
                    if (iterationNum == dt.Rows.Count)
                    {
                        #region Закрыть последнюю сделку, если данные закончились (эмитируем отключение программы)
                        if (priceEnterLong != 0)
                        {
                            if (bid - priceEnterLong > 0)
                            {
                                resThTemp.countPDeal++;
                                resThTemp.profit += ((int)bid - priceEnterLong) * lotCount;
                            }
                            else
                            {
                                resThTemp.countLDeal++;
                                resThTemp.loss += (priceEnterLong - (int)bid) * lotCount;
                            }
                            dealTemp.DoExit(dr.Field<DateTime>("dtserver"), (float)bid);
                            resThTemp.lstAllDeals.Add(dealTemp);
                            priceEnterLong = 0;
                            lotCount = 1;
                        }
                        if (priceEnterShort != 0)
                        {
                            if (priceEnterShort - ask > 0)
                            {
                                resThTemp.countPDeal++;
                                resThTemp.profit += (priceEnterShort - (int)ask) * lotCount;
                            }
                            else
                            {
                                resThTemp.countLDeal++;
                                resThTemp.loss += ((int)ask - priceEnterShort) * lotCount;
                            }
                            dealTemp.DoExit(dr.Field<DateTime>("dtserver"), (float)ask);
                            resThTemp.lstAllDeals.Add(dealTemp);
                            priceEnterShort = 0;
                            lotCount = 1;
                        }
                        continue;
                        #endregion
                    }
                    // совершена сделка
                    if (!dr.IsNull("priceTick"))
                    {
                        pricetick = (int?)dr.Field<float?>("priceTick");
                        actiontick = dr.Field<byte?>("idaction");
                        if (actiontick == 1)
                        {
                            ask = pricetick;
                            if (priceEnterShort != 0)
                            {
                                // профит короткая
                                if (priceEnterShort - profitShortValueTemp >= ask)
                                {
                                    resThTemp.countPDeal++;
                                    resThTemp.profit += (priceEnterShort - (int)ask) * lotCount;
                                    priceEnterShort = 0;
                                    dealTemp.DoExit(dr.Field<DateTime>("dtserver"), (float)ask);
                                    resThTemp.lstAllDeals.Add(dealTemp);
                                }
                                // лосс короткая
                                else if (priceEnterShort + lossShortValueTemp <= ask)
                                {
                                    if (paramTh.martingValue >= lotCount)// && indicator < 0)
                                    {
                                        lotCount += 1;
                                        dealTemp.lotsCount = lotCount;
                                        int delt = (int)Math.Truncate((double)((int)ask - priceEnterShort) / lotCount / 10) * 10;

                                        profitShortValueTemp += delt;
                                        lossShortValueTemp += delt;

                                        priceEnterShort = priceEnterShort + delt;

                                        if (dealTemp.lstSubDeal.Count > 0)
                                            dealTemp.lstSubDeal.Last().dtDealLength = dr.Field<DateTime>("dtserver").Subtract(dealTemp.lstSubDeal.Last().dtEnter);
                                        dealTemp.lstSubDeal.Add(new SubDealInfo(dr.Field<DateTime>("dtserver"), lotCount, (float)priceEnterShort, (float)ask, (float)delt, indicator, (float)lossShortValueTemp, (float)profitShortValueTemp));
                                    }
                                    else
                                    {
                                        resThTemp.countLDeal++;
                                        int g = ((int)ask - priceEnterShort) * lotCount;
                                        resThTemp.loss += g;
                                        dealTemp.DoExit(dr.Field<DateTime>("dtserver"), (float)ask);
                                        resThTemp.lstAllDeals.Add(dealTemp);
                                        priceEnterShort = 0;
                                        lotCount = 1;
                                    }
                                }
                            }
                        }
                        else if (actiontick == 2)
                        {
                            bid = pricetick;
                            if (priceEnterLong != 0)
                            {
                                // профит длиная
                                if (priceEnterLong + profitLongValueTemp <= bid)
                                {
                                    resThTemp.countPDeal++;
                                    resThTemp.profit += ((int)bid - priceEnterLong) * lotCount;
                                    priceEnterLong = 0;
                                    dealTemp.DoExit(dr.Field<DateTime>("dtserver"), (float)bid);
                                    resThTemp.lstAllDeals.Add(dealTemp);
                                }
                                // лосс длиная
                                else if (priceEnterLong - lossLongValueTemp >= bid)
                                {
                                    if (paramTh.martingValue >= lotCount) //&& indicator > 0)
                                    {
                                        lotCount += 1;
                                        dealTemp.lotsCount = lotCount;
                                        int delt = (int)Math.Truncate((double)(priceEnterLong - (int)bid) / lotCount /10) * 10;

                                        profitLongValueTemp += delt;
                                        lossLongValueTemp += delt;

                                        priceEnterLong = priceEnterLong - delt;

                                        if (dealTemp.lstSubDeal.Count > 0)
                                            dealTemp.lstSubDeal.Last().dtDealLength = dr.Field<DateTime>("dtserver").Subtract(dealTemp.lstSubDeal.Last().dtEnter);
                                        dealTemp.lstSubDeal.Add(new SubDealInfo(dr.Field<DateTime>("dtserver"), lotCount, (float)priceEnterLong, (float)bid, (float)delt, indicator, (float)lossLongValueTemp, (float)profitLongValueTemp));
                                    }
                                    else
                                    {
                                        resThTemp.countLDeal++;
                                        int g = (priceEnterLong - (int)bid) * lotCount;
                                        resThTemp.loss += g;
                                        dealTemp.DoExit(dr.Field<DateTime>("dtserver"), (float)bid);
                                        resThTemp.lstAllDeals.Add(dealTemp);
                                        priceEnterLong = 0;
                                        lotCount = 1;
                                    }
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
                                // старый вариант работы стакана (до использования SortedDictionary)
                                /*oldGlassValue.Clear();
                                foreach (int pkey in glass.Keys)
                                {
                                    if (pkey > ask + paramTh.glassHeight * 10 || pkey < bid - paramTh.glassHeight * 10)
                                        oldGlassValue.Add(pkey);
                                    else if (pkey >= ask || pkey <= bid)
                                        sumGlass += glass[pkey];
                                }
                                // удаляем из стакана значения, выпадающие за пределы глубины стакана
                                oldGlassValue.ForEach((int i) => { glass.Remove(i); });*/

                                // среднее значение по стакану
                                for (int i = 0; i < paramTh.glassHeight; i++)
                                {
                                    sumGlass += glass.ContainsKey((int)ask + i * 10) ? glass[(int)ask + i * 10] : 0;
                                    sumGlass += glass.ContainsKey((int)bid - i * 10) ? glass[(int)bid - i * 10] : 0;
                                }
                                int averageGlass = (int)sumGlass / (paramTh.glassHeight * 2);
                                int sumlong = 0, sumshort = 0;

                                tempListForIndicator.Clear();
                                // новая версия, более взвешенное значение (как год назад)
                                for (int i = 0; i < paramTh.glassHeight; i++)
                                {
                                    sumlong += glass.ContainsKey((int)ask + i * 10)
                                        && glass[(int)ask + i * 10] < averageGlass * paramTh.averageValue 
                                        ? glass[(int)ask + i * 10] : 0;
                                    sumshort += glass.ContainsKey((int)bid - i * 10)
                                        && glass[(int)bid - i * 10] < averageGlass * paramTh.averageValue
                                        ? glass[(int)bid - i * 10] : 0;
                                    if (sumlong + sumshort == 0) 
                                        continue;
                                    tempListForIndicator.Add((int) (sumlong - sumshort) * 100 / (sumlong + sumshort));
                                }
                                int s = 0;
                                foreach (int i in tempListForIndicator)
                                {
                                    s += i;
                                }
                                indicator = (int) s / paramTh.glassHeight;
                                // старая версия индикатора
                                /*foreach (int pkey in glass.Keys)
                                {
                                    if (pkey >= ask && glass[pkey] < averageGlass * paramTh.averageValue)
                                        sumlong += glass[pkey];
                                    else if (pkey <= bid && glass[pkey] < averageGlass * paramTh.averageValue)
                                        sumshort += glass[pkey];
                                }*/
                                //indicator = (sumlong + sumshort) != 0 ? (int)(sumlong - sumshort) * 100 / (sumlong + sumshort) : 0;
                                // вход лонг
                                if (indicator >= paramTh.indicatorLongValue && priceEnterLong == 0 && priceEnterShort == 0)
                                {
                                    lossLongValueTemp = paramTh.lossLongValue;
                                    profitLongValueTemp = paramTh.profitLongValue;
                                    priceEnterLong = (int)ask;
                                    lotCount = 1;
                                    dealTemp = new DealInfo(ActionDeal.buy, dr.Field<DateTime>("dtserver"), 1, priceEnterLong, indicator);

                                }
                                // вход шорт
                                else if (indicator <= -paramTh.indicatorShortValue && priceEnterLong == 0 && priceEnterShort == 0)
                                {
                                    lossShortValueTemp = paramTh.lossShortValue;
                                    profitShortValueTemp = paramTh.profitShortValue;
                                    priceEnterShort = (int)bid;
                                    lotCount = 1;
                                    dealTemp = new DealInfo(ActionDeal.sell, dr.Field<DateTime>("dtserver"), 1, priceEnterShort, indicator);
                                }
                            }
                        }

                    }
                    //else if (!dr.IsNull("bid"))
                    //{
                    //    bid = (int?)dr.Field<float?>("bid");
                    //    ask = (int?)dr.Field<float?>("ask");
                    //}
                    #endregion торговля
                }
                resThTemp.margin = resThTemp.profit - resThTemp.loss;
                resThTemp.profitFac = (float)resThTemp.profit / (float)resThTemp.loss;
                resTh.AddOneDayResult(resThTemp);
            }
            lock (lockObj)
            {
                resTh.idCycle = (p as ParametrsForTestObj).numThread;
                resTh.idParam = paramTh.id;
                resTh.paramForTest = paramTh;
                resTh.glassH = paramTh.glassHeight;
                resTh.indicLongVal = paramTh.indicatorLongValue;
                resTh.indicShortVal = paramTh.indicatorShortValue;
                resTh.profLongLevel = paramTh.profitLongValue;
                resTh.lossLongLevel = paramTh.lossLongValue;
                resTh.profShortLevel = paramTh.profitShortValue;
                resTh.lossShortLevel = paramTh.lossShortValue;
                resTh.martinLevel = paramTh.martingValue;
                resTh.averageVal = paramTh.averageValue;

                resTh.margin = resTh.profit - resTh.loss;
                if (resTh.loss == 0)
                    resTh.profitFac = 999;
                else
                    resTh.profitFac = (float)resTh.profit / (float)resTh.loss;

                ResultBestProfitFactor rp = new ResultBestProfitFactor(resTh.idParam, resTh.profitFac, resTh.margin);
                if (!dicAllProfitResult.ContainsKey(rp))
                    dicAllProfitResult.Add(rp, resTh);

                if (OnFinishOneThread != null)
                    OnFinishOneThread(resTh);
            }
        }

    }
}
