using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMoney
{
    public enum ActionDeal
	{
        sell = 1,
        buy = 2
	};
    public enum ResultConnectToDataSource
    {
    }

    public struct tableInfo
    {
        public string fullName;
        public string shortName;
        public string isntrumentName;
        public string tableType;
        public string dateTable;
        public int dayNum;
        public int monthNum;
        public int yearNum;
    }
    public struct diapasonTestParam
    {
        public diapasonTestParam(string _start, string _finish, string _step)
        {
            start = int.Parse( _start);
            finish = int.Parse(_finish);
            step = int.Parse(_step);
        }
        public int start;
        public int finish;
        public int step;

    }
    public struct ParametrsForTest
    {
        public ParametrsForTest(int _id, List<string> _instruments, int _i0, float _i1, int _i2, int _i3, int _i4, int _i5)
        {
            instruments = _instruments;
            id = _id;
            glassHeight = _i0;
            averageValue = _i1;
            profitValue = _i2;
            lossValue = _i3;
            indicatorValue = _i4;
            martingValue = _i5;
        }
        public int id;
        public List<string> instruments;
        public int glassHeight;
        public float averageValue; // отношение среднего по стакану к каждой позиции (если более averageValue - в рассчет не берется)
        public int profitValue; // значение профита
        public int lossValue; // значение убытка
        public int indicatorValue; // значение индикатора для входа
        public int martingValue; // допустимое количество раз усреднения
    }

    public class ParametrsForTestObj
    {
        public ParametrsForTestObj(ParametrsForTest _p, Dictionary<string, DataTable> _dictionaryDT)
        {
            paramS = _p;
            dictionaryDT = _dictionaryDT;
        }
        public ParametrsForTest paramS;
        public Dictionary<string, DataTable> dictionaryDT;
    }

    public class SubDealInfo
    {
        private float pexit;
        private DateTime dtext;
        public SubDealInfo()
        {
            lotsCount = 0;
            priceEnter = 0;
            priceExit = 0;
        }

        public SubDealInfo(DateTime _dt, int _lotCount, float _priceEnter, float _curPrice, float _delt, float _lossValueTemp = 0, float _profitValueTemp = 0)
        {
            dtEnter = _dt;
            lotsCount = _lotCount;
            priceEnter = _priceEnter;
            currentPrice = _curPrice;
            delt = _delt;
            lossValueTemp = _lossValueTemp;
            profitValueTemp = _profitValueTemp;
        }

        public void DoExit(DateTime _dt, float _pexit)
        {
            priceExit = _pexit;
            dtExit = _dt;
        }
        public SubDealInfo parentDeal = null;
        public DateTime dtEnter { get; set; }
        public DateTime dtExit {
            get { return dtext; }
            set { dtDealLength = value.Subtract(dtEnter); dtext = value; }
        }
        public TimeSpan dtDealLength { get; set; }
        public ActionDeal actiond { get; set; }
        public int lotsCount { get; set; }
        public float pointsCount { get; set; }
        public float margin { get; set; }
        public float delt { get; set; }
        public float currentPrice { get; set; }
        public float lossValueTemp { get; set; }
        public float profitValueTemp { get; set; }
        public float priceEnter { get; set; }
        public float priceExit { 
            get {
                return pexit;
            } 
            set {
                float pEnt = priceEnter;
                if (lstSubDeal.Count > 0)
                    pEnt = lstSubDeal.Last().priceEnter;
                if (actiond == ActionDeal.sell)
                {
                    margin = (pEnt - value) * lotsCount;
                    pointsCount = pEnt - value;
                }
                else if (actiond == ActionDeal.buy)
                {
                    margin = (value - pEnt) * lotsCount;
                    pointsCount = value - pEnt;
                }
                pexit = value;
            }
        }
        public List<SubDealInfo> lstSubDeal = new List<SubDealInfo>();
    }

    public class DealInfo : SubDealInfo
    {
        public DealInfo(ActionDeal _actiond, DateTime _dtEnter, int _lotsCount, float _pEnter, float _lossValueTemp = 0, float _profitValueTemp = 0)
        {
            actiond = _actiond;
            dtEnter = _dtEnter;
            lotsCount = _lotsCount;
            priceEnter = _pEnter;
            lossValueTemp = _lossValueTemp;
            profitValueTemp = _profitValueTemp;
        }

    }
    public class ResultOneThread
    {
        public ResultOneThread()
        {
            countLDeal = 0;
            countPDeal = 0;
            profit = 0;
            loss = 0;
        }
        public List<DealInfo> lstAllDeals = new List<DealInfo>();
        public string shortName { get; set; }
        public int idParam { get; set; }
        public float profitFac { get; set; }
        public int margin { get; set; }
        public int profit { get; set; }
        public int loss { get; set; }
        public int countPDeal { get; set; }
        public int countLDeal { get; set; }
        public int glassH { get; set; }
        public float averageVal { get; set; }
        public int profLevel { get; set; }
        public int lossLevel { get; set; }
        public int indicVal { get; set; }
        public int martinLevel { get; set; }
    }

    public class ResultOneThreadSumm : ResultOneThread
    {
        public List<ResultOneThread> lstResults = new List<ResultOneThread>();

        public void AddOneDayResult(ResultOneThread _result)
        {
            this.countPDeal += _result.countPDeal;
            this.countLDeal += _result.countLDeal;
            this.loss += _result.loss;
            this.profit += _result.profit;
            this.lstResults.Add(_result);
        }
    }
}
