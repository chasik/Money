﻿using System;
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
        public ParametrsForTest(int _id, List<string> _instruments, int _i0, float _i1, int _i2, int _i3, int _i4, int _i5, int _i6, int _i7, int _i8, int _delay)
        {
            instruments = _instruments;
            id = _id;
            glassHeight = _i0;
            averageValue = _i1;
            profitLongValue = _i2;
            lossLongValue = _i3;
            indicatorLongValue = _i4;
            martingValue = _i5;
            lossShortValue = _i6;
            profitShortValue = _i7;
            indicatorShortValue = _i8;
            delay = _delay;
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
            indicatorLongValue = _params[r.Next(0,1)].indicatorLongValue;
            indicatorShortValue = _params[r.Next(0, 1)].indicatorShortValue;
            martingValue = _params[r.Next(0,1)].martingValue;
            lossShortValue = _params[r.Next(0,1)].lossShortValue;
            profitShortValue = _params[r.Next(0, 1)].profitShortValue;
            delay = _params[r.Next(0, 1)].delay;
        }

        public bool Compare(ParametrsForTest p1, ParametrsForTest p2)
        {
            if (p1.indicatorShortValue.CompareTo(p2.indicatorShortValue) == 0 && p1.indicatorLongValue.CompareTo(p2.indicatorLongValue) == 0 
                && p1.glassHeight.CompareTo(p2.glassHeight) == 0 && p1.lossLongValue.CompareTo(p2.lossLongValue) == 0 && p1.lossShortValue.CompareTo(p2.lossShortValue) == 0 
                && p1.martingValue.CompareTo(p2.martingValue) == 0 && p1.profitLongValue.CompareTo(p2.profitLongValue) == 0 && p1.profitShortValue.CompareTo(p2.profitShortValue) == 0)
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
                case 7: indicatorLongValue = newVal;
                break;
                case 8: indicatorShortValue = newVal;
                break;
                case 9: martingValue = newVal;
                break;
                case 10: delay = newVal;
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
        public int indicatorLongValue; // значение индикатора для входа
        public int indicatorShortValue; // значение индикатора для входа
        public int delay; // длительность, при которой индикатор не опускается ниже заданного уровня
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
        private TimeSpan dtext;
        public SubDealInfo()
        {
            lotsCount = 0;
            priceEnter = 0;
            priceExit = 0;
        }

        public SubDealInfo(DateTime _dt, int _lotCount, float _priceEnter, float _curPrice, float _delt, int _indicValue, float _lossValueTemp = 0, float _profitValueTemp = 0)
        {
            actiond = ActionDeal.none;
            dtEnter = _dt.TimeOfDay;
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
            dtExit = _dt.TimeOfDay;
        }
        public SubDealInfo parentDeal = null;
        public TimeSpan dtEnter { get; set; }
        public TimeSpan dtExit {
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
            dtEnter = _dtEnter.TimeOfDay;
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
        public int indicLongVal { get; set; }
        public int indicShortVal { get; set; }
        public int delay { get; set; }
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


    public class ResultBestProfitFactor : IComparable
    {
        public ResultBestProfitFactor(int _idparam, float _profitfactor, float _margin)
        {
            idparam = _idparam;
            profitFactor = _profitfactor;
            margin = _margin;
        }
        public int idparam { get; set; }
        public float profitFactor { get; set; }
        public float margin { get; set; }
        public int CompareTo(object j)
        {
            ResultBestProfitFactor compareObj = j as ResultBestProfitFactor;
            if (this.idparam == compareObj.idparam || (this.profitFactor == compareObj.profitFactor && this.margin == compareObj.margin))
                return 0;
            if (this.margin > compareObj.margin || (this.profitFactor > compareObj.profitFactor && this.profitFactor != 999))
                return -1;
            else
                return 1;
        }
    }

    public class ClaimInfo
    {
        public ClaimInfo(double _priceenter, SmartCOM3Lib.StOrder_Action _action)
        {
            priceEnter = _priceenter;
            action = _action;
        }

        public double priceEnter = 0;
        public SmartCOM3Lib.StOrder_Action action;
        public double realPriceEnter = 0;
        public string orderid;
        public string orderno;
        public string tradeno;
    }

    public class AllClaimsInfo
    {

        public delegate void DoTradeEvent(int _cookie, SmartCOM3Lib.StOrder_Action _ordAction, SmartCOM3Lib.StOrder_Type _ordType);
        public event DoTradeEvent OnDoTrade;

        public Dictionary<int, ClaimInfo> dicAllClaims = new Dictionary<int, ClaimInfo>();
        private int _activecookie = 0;
        public int ActiveCookie {
            get { return _activecookie; }
            set {
                LastActiveCookie = _activecookie;
                _activecookie = value;
            }
        }
        public int LastActiveCookie { get; set; }
        public int ProfitLevel { get; set; }
        public int LossLevel { get; set; }

        public AllClaimsInfo()
        {
            ActiveCookie = 0;
            ProfitLevel = 0;
            LossLevel = 0;
        }
        public void Add(int _cookie, double _priceent, SmartCOM3Lib.StOrder_Action _action)
        {
            dicAllClaims.Add(_cookie, new ClaimInfo(_priceent, _action));
        }
        public void AddTradeNo(int _cook, string _tradeno)
        {
            if (!dicAllClaims.ContainsKey(_cook))
                return;
            if (!_tradeno.Equals("0"))
                dicAllClaims[_cook].tradeno = _tradeno;
        }
        public void AddOrderIdAndOrderNo(int _cook, string _ordid, string _ordno)
        {
            if (!dicAllClaims.ContainsKey(_cook))
                return;
            if (!_ordid.Equals("0"))
                dicAllClaims[_cook].orderid = _ordid;
            if (!_ordno.Equals("0"))
                dicAllClaims[_cook].orderno = _ordno;
        }

        public int GetCookie(string _idstr)
        {
            int r = 0;
            foreach (int k in dicAllClaims.Keys)
            {
                if (dicAllClaims[k].orderid == _idstr || dicAllClaims[k].orderno == _idstr || dicAllClaims[k].tradeno == _idstr)
                {
                    r = k;
                    break;
                }
            }
            return r;
        }

        public double SetRealPrice(int _cook, double _price)
        {
            if (!dicAllClaims.ContainsKey(_cook))
                return 0;
            return dicAllClaims[_cook].realPriceEnter = _price;
        }

        public double GetRealPrice(int _cook)
        {
            if (!dicAllClaims.ContainsKey(_cook))
                return 0;
            return dicAllClaims[_cook].realPriceEnter;
        }
        public void NewQuotes(double _bid, double _ask)
        {
            if (ActiveCookie == 0)
                return;
            if (dicAllClaims[ActiveCookie].action == SmartCOM3Lib.StOrder_Action.StOrder_Action_Buy)
            {
                if ((int)_bid > dicAllClaims[ActiveCookie].realPriceEnter + ProfitLevel)
                    OnDoTrade(ActiveCookie + 10000, SmartCOM3Lib.StOrder_Action.StOrder_Action_Sell, SmartCOM3Lib.StOrder_Type.StOrder_Type_Market);
            }
            else if (dicAllClaims[ActiveCookie].action == SmartCOM3Lib.StOrder_Action.StOrder_Action_Sell)
            {
                if ((int)_ask < dicAllClaims[ActiveCookie].realPriceEnter - ProfitLevel)
                    OnDoTrade(ActiveCookie + 10000, SmartCOM3Lib.StOrder_Action.StOrder_Action_Buy, SmartCOM3Lib.StOrder_Type.StOrder_Type_Market);
            }
        }
    }

}
