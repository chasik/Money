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
        none = 0,
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
        public diapasonTestParam(int _idParam, string _start, string _finish, string _step)
        {
            idParam = _idParam;
            start = int.Parse( _start);
            finish = int.Parse(_finish);
            step = int.Parse(_step);
        }
        public int idParam;
        public int start;
        public int finish;
        public int step;

    }
    public struct ParametrsForTest 
    {
        public ParametrsForTest(int _id, List<string> _instruments, int _i0, float _i1, int _i2, int _i3, int _i4, int _i5, int _i6, int _i7)
        {
            instruments = _instruments;
            id = _id;
            glassHeight = _i0;
            averageValue = _i1;
            profitLongValue = _i2;
            lossLongValue = _i3;
            indicatorValue = _i4;
            martingValue = _i5;
            lossShortValue = _i6;
            profitShortValue = _i7;
        }

        public ParametrsForTest(ParametrsForTest[] _params)
        {
            Random r = new Random();
            id = 0;
            instruments = _params[r.Next(0,1)].instruments;
            glassHeight = _params[r.Next(0,1)].glassHeight;
            averageValue = _params[r.Next(0,1)].averageValue;
            profitLongValue = _params[r.Next(0,1)].profitLongValue;
            lossLongValue = _params[r.Next(0,1)].lossLongValue;
            indicatorValue = _params[r.Next(0,1)].indicatorValue;
            martingValue = _params[r.Next(0,1)].martingValue;
            lossShortValue = _params[r.Next(0,1)].lossShortValue;
            profitShortValue = _params[r.Next(0, 1)].profitShortValue;
        }

        public bool Compare(ParametrsForTest p1, ParametrsForTest p2)
        {
            if (p1.indicatorValue.CompareTo(p2.indicatorValue) == 0 && p1.glassHeight.CompareTo(p2.glassHeight) == 0 //&& p1.instruments == p2.instruments
                && p1.lossLongValue.CompareTo(p2.lossLongValue) == 0 && p1.lossShortValue.CompareTo(p2.lossShortValue) == 0 && p1.martingValue.CompareTo(p2.martingValue) == 0
                && p1.profitLongValue.CompareTo(p2.profitLongValue) == 0 && p1.profitShortValue.CompareTo(p2.profitShortValue) == 0)
                return true;
            else 
                return false;
        }

        public void Mutation(diapasonTestParam _diapParam)
        {
            Random rnd = new Random();
            int colIter = (_diapParam.finish - _diapParam.start) / _diapParam.step;
            int randomStep = rnd.Next(0, colIter);
            int newVal = _diapParam.start + _diapParam.step * randomStep;
            switch (_diapParam.idParam)
            {
                case 1: glassHeight = newVal;
                break;
                case 2: averageValue = newVal;
                break;
                case 3: profitLongValue = newVal;
                break;
                case 4: lossLongValue = newVal;
                break;
                case 5: profitShortValue = newVal;
                break;
                case 6: lossShortValue = newVal;
                break;
                case 7: indicatorValue = newVal;
                break;
                case 8: martingValue = newVal;
                break;
                default:
                    break;
            }

        }

        public int id;
        public List<string> instruments;
        public int glassHeight;
        public float averageValue; // отношение среднего по стакану к каждой позиции (если более averageValue - в рассчет не берется)
        public int profitLongValue; // значение профита лонга
        public int lossLongValue; // значение убытка лонга
        public int profitShortValue; // значение профита шорта
        public int lossShortValue; // значение убытка шорта
        public int indicatorValue; // значение индикатора для входа
        public int martingValue; // допустимое количество раз усреднения
    }

    public class ParametrsForTestObj
    {
        public ParametrsForTestObj(ParametrsForTest _p, Dictionary<string, DataTable> _dictionaryDT, int _numThread = 0)
        {
            paramS = _p;
            dictionaryDT = _dictionaryDT;
            this.numThread = _numThread;
        }
        public int numThread;
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

        public SubDealInfo(DateTime _dt, int _lotCount, float _priceEnter, float _curPrice, float _delt, int _indicValue, float _lossValueTemp = 0, float _profitValueTemp = 0)
        {
            actiond = ActionDeal.none;
            dtEnter = _dt;
            lotsCount = _lotCount;
            priceEnter = _priceEnter;
            currentPrice = _curPrice;
            delt = _delt;
            lossValueTemp = _lossValueTemp;
            profitValueTemp = _profitValueTemp;
            indicValue = _indicValue;
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
            set { 
                dtDealLength = value.Subtract(dtEnter); 
                dtext = value;
                if (this.lstSubDeal.Count > 0)
                    this.lstSubDeal.Last().dtDealLength = value.Subtract(this.lstSubDeal.Last().dtEnter);
            }
        }
        public TimeSpan dtDealLength { get; set; }
        public ActionDeal actiond { get; set; }
        public int lotsCount { get; set; }
        public float pointsCount { get; set; }
        public float margin { get; set; }
        public float delt { get; set; }
        public int indicValue { get; set; }
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
        public float currentPrice { get; set; }
        public float lossValueTemp { get; set; }
        public float profitValueTemp { get; set; }

        public List<SubDealInfo> lstSubDeal = new List<SubDealInfo>();
    }

    public class DealInfo : SubDealInfo
    {
        public DealInfo(ActionDeal _actiond, DateTime _dtEnter, int _lotsCount, float _pEnter, int _indicValue, float _lossValueTemp = 0, float _profitValueTemp = 0)
        {
            actiond = _actiond;
            dtEnter = _dtEnter;
            lotsCount = _lotsCount;
            priceEnter = _pEnter;
            lossValueTemp = _lossValueTemp;
            profitValueTemp = _profitValueTemp;
            indicValue = _indicValue;
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
        public int idCycle { get; set; }
        public int idParam { get; set; }
        public float profitFac { get; set; }
        public int margin { get; set; }
        public int profit { get; set; }
        public int loss { get; set; }
        public int countPDeal { get; set; }
        public int countLDeal { get; set; }
        public int glassH { get; set; }
        public float averageVal { get; set; }
        public int profLongLevel { get; set; }
        public int lossLongLevel { get; set; }
        public int profShortLevel { get; set; }
        public int lossShortLevel { get; set; }
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

        public ParametrsForTest paramForTest;
    }

    public class ResultBestMargin : IComparable
    {
        public ResultBestMargin(int _idparam, float _margin)
        {
            idparam = _idparam;
            margin = _margin;
        }
        
        public int idparam { get; set; }
        public float margin { get; set; }
        public int CompareTo(object j)
        {
            if (this.margin < (j as ResultBestMargin).margin)
                return 1;
            if (this.margin == (j as ResultBestMargin).margin)
                return 0;
            if (this.margin > (j as ResultBestMargin).margin)
                return -1;
            return 0;
        }
    }

    public class ResultBestProfitFactor : IComparable
    {
        public ResultBestProfitFactor(int _idparam, float _profitfactor)
        {
            idparam = _idparam;
            profitFactor = _profitfactor;
        }
        public int idparam { get; set; }
        public float profitFactor { get; set; }
        public int CompareTo(object j)
        {
            if (this.profitFactor < (j as ResultBestProfitFactor).profitFactor)
                return 1;
            if (this.profitFactor == (j as ResultBestProfitFactor).profitFactor)
                return 0;
            if (this.profitFactor > (j as ResultBestProfitFactor).profitFactor)
                return -1;
            return 0;
        }
    }

}
