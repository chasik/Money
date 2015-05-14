using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyMoney
{
    public enum InfoElement
    {
        logfile = 0,
        tbInformation = 1,
        tbInfo2 = 2
    }
    public enum TypeWorkOrder
    {
        none = 0,
        order = 1,
        profit = 2,
        loss = 3
    }
    public enum ActionDeal
	{
        none = 0,
        sell = 1,
        buy = 2,
        subsell = 3,
        subbuy = 4
	};
    public enum ResultConnectToDataSource
    {
    }

    public struct TableInfo
    {
        public string fullName;
        public string shortName;
        public string isntrumentName;
        public string tableType;
        public string dateTable;
        public DateTime dtTable;
        public double symbolstep;
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
            indicatorEnterValue = _i4;
            martingValue = _i5;
            lossShortValue = _i6;
            profitShortValue = _i7;
            indicatorExitValue = _i8;
            delay = _delay;
        }

        public ParametrsForTest(ParametrsForTest[] _params)
        {
            Random r = new Random(DateTime.Now.Millisecond);
            id = 0;
            instruments = _params[r.Next(0,1)].instruments;
            glassHeight = _params[r.Next(0,1)].glassHeight;
            averageValue = _params[r.Next(0,1)].averageValue;
            profitLongValue = _params[r.Next(0,1)].profitLongValue;
            lossLongValue = _params[r.Next(0,1)].lossLongValue;
            indicatorEnterValue = _params[r.Next(0,1)].indicatorEnterValue;
            indicatorExitValue = _params[r.Next(0, 1)].indicatorExitValue;
            martingValue = _params[r.Next(0,1)].martingValue;
            lossShortValue = _params[r.Next(0,1)].lossShortValue;
            profitShortValue = _params[r.Next(0, 1)].profitShortValue;
            delay = _params[r.Next(0, 1)].delay;
        }

        public bool Compare(ParametrsForTest p1, ParametrsForTest p2)
        {
            if (p1.indicatorExitValue.CompareTo(p2.indicatorExitValue) == 0 && p1.indicatorEnterValue.CompareTo(p2.indicatorEnterValue) == 0 
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
                case 7: indicatorEnterValue = newVal;
                break;
                case 8: indicatorExitValue = newVal;
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
        public int indicatorEnterValue; // значение индикатора входа
        public int indicatorExitValue; // значение индикатора выхода
        public int delay; // длительность, при которой индикатор не опускается ниже заданного уровня
        public int martingValue; // допустимое количество раз усреднения
    }

    public class ParametrsForTestObj
    {
        public ParametrsForTestObj(ParametrsForTest _p, Dictionary<string, DataTableWithCalcValues> _dictionaryDT, int _numThread = 0, int _mutationCount = 0)
        {
            paramS = _p;
            dictionaryDT = _dictionaryDT;
            this.numThread = _numThread;
            this.mutationCount = _mutationCount;
        }
        public int numThread;
        public ParametrsForTest paramS;
        public Dictionary<string, DataTableWithCalcValues> dictionaryDT;
        public int mutationCount;
    }

    public class SubDealInfo
    {
        private float pexit;
        private TimeSpan dtext;
        public DateTime datetimeExit, datetimeEnter;
        public string shortName;
        public SubDealInfo()
        {
            lotsCount = 0;
            priceEnter = 0;
            priceExit = 0;
        }

        public SubDealInfo(DateTime _dt, int _lotCount, ActionDeal _act, float _priceEnter, float _curPrice, float _delt, int _indicValue, float _lossValueTemp = 0, float _profitValueTemp = 0)
        {
            datetimeEnter = _dt;
            actiond = _act;
            dtEnter = _dt.TimeOfDay;
            lotsCount = _lotCount;
            priceEnter = _priceEnter;
            currentPrice = _curPrice;
            delt = _delt;
            lossValueTemp = _lossValueTemp;
            profitValueTemp = _profitValueTemp;
            indicValue = _indicValue;
            switch (_act)
            {
                case ActionDeal.subsell:
                    profitLevel = _priceEnter - _profitValueTemp;
                    lossLevel = _priceEnter + _lossValueTemp;
                    break;
                case ActionDeal.subbuy:
                    profitLevel = _priceEnter + _profitValueTemp;
                    lossLevel = _priceEnter - _lossValueTemp;
                    break;
                default:
                    break;
            }
        }

        public void DoExit(DateTime _dt, float _pexit)
        {
            datetimeExit = _dt;
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
        public float aggreeIndV { get; set; }
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
        public float profitValueTemp { get; set; }
        public float profitLevel { get; set; }
        public float lossValueTemp { get; set; }
        public float lossLevel { get; set; }

        public List<SubDealInfo> lstSubDeal = new List<SubDealInfo>();
    }

    public class DealInfo : SubDealInfo
    {
        public DealInfo(ActionDeal _actiond, DateTime _dtEnter, int _lotsCount, float _pEnter, int _indicValue, float _lossValueTemp = 0, float _profitValueTemp = 0)
        {
            actiond = _actiond;
            datetimeEnter = _dtEnter;
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
        public string mutC { get; set; }
        public int idParam { get; set; }
        public float profitFac { get; set; }
        public int margin { get; set; }
        public float matExp { get; set; }
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
            this.shortName = _result.shortName;
            this.lstResults.Add(_result);
        }

        public ParametrsForTest paramForTest;
    }


    public class ResultBestProfitFactor : IComparable
    {
        public ResultBestProfitFactor(int _idparam, float _profitfactor, float _margin, float _matexp)
        {
            idparam = _idparam;
            profitFactor = _profitfactor;
            margin = _margin;
        }
        public int idparam { get; set; }
        public float profitFactor { get; set; }
        public float margin { get; set; }
        public float matExp { get; set; }
        public int CompareTo(object j)
        {
            ResultBestProfitFactor compareObj = j as ResultBestProfitFactor;
            if (this.idparam == compareObj.idparam || (this.profitFactor == compareObj.profitFactor && this.margin == compareObj.margin && this.matExp == compareObj.matExp))
                return 0;
            //if (this.matExp > compareObj.matExp)
            if (this.margin > compareObj.margin || this.matExp > compareObj.matExp)
            //if (this.margin > compareObj.margin || this.matExp > compareObj.matExp || (this.profitFactor > compareObj.profitFactor && this.profitFactor != 999))
                return -1;
            else
                return 1;
        }
    }

    public class ClaimInfo
    {
        public ClaimInfo(DateTime _dtenter, double _priceenter, int _lotcount, SmartCOM3Lib.StOrder_Action _action)
        {
            priceEnter = _priceenter;
            action = _action;
            lotcount = _lotcount;
            dtEnter = _dtenter;
        }

        public double priceEnter = 0;
        public DateTime dtEnter;
        public double realPriceEnter = 0;
        public DateTime realDtEnter;
        public DateTime dt_State_Pending, dt_State_Open, dt_State_Cancel, dt_State_Filled, dt_State_Partial;
        public double priceExit = 0;
        public double realPriceExit = 0;
        public SmartCOM3Lib.StOrder_Action action;
        public string orderid;
        public string orderno;
        public string tradeno;
        public int lotcount;
        public int ProfitLevel { get; set; }
        public int LossLevel { get; set; }
    }

    public class AllClaimsInfo
    {
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

        public AllClaimsInfo()
        {
            ActiveCookie = 0;
        }
        public ClaimInfo Add(int _cookie, DateTime _dtEnter, double _priceent, int _lotcount, SmartCOM3Lib.StOrder_Action _action)
        {
            dicAllClaims.Add(_cookie, new ClaimInfo(_dtEnter, _priceent, _lotcount, _action));
            return dicAllClaims[_cookie];
        }
        public void AddTradeNo(int _cook, string _tradeno)
        {
            if (!dicAllClaims.ContainsKey(_cook))
                return;
            if (!_tradeno.Equals("0"))
                dicAllClaims[_cook].tradeno = _tradeno;
        }
        public void AddOrderId(int _cook, string _orderid)
        {
            if (!dicAllClaims.ContainsKey(_cook))
                return;
            if (!_orderid.Equals("0"))
                dicAllClaims[_cook].orderid = _orderid;
        }
        public void AddOrderIdAndOrderNo(int _cook, double _amount, SmartCOM3Lib.StOrder_Action _action, string _ordid, string _ordno)
        {
            if (!dicAllClaims.ContainsKey(_cook))
            {
                ClaimInfo lastc = this.Add(_cook, DateTime.Now, 0, (int)_amount, _action);
                int cookId = GetCookieId(_cook);
                foreach(int c in dicAllClaims.Keys)
                {
                    if (GetCookieId(c) == cookId)
                    { 
                        if (dicAllClaims[c].ProfitLevel > lastc.ProfitLevel)
                            lastc.ProfitLevel = dicAllClaims[c].ProfitLevel;
                        if (dicAllClaims[c].LossLevel > lastc.LossLevel)
                            lastc.LossLevel = dicAllClaims[c].LossLevel;
                    }
                }
            }
                
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

        public double SetRealPrice(int _cook, double _price, DateTime _realdtenter)
        {
            if (!dicAllClaims.ContainsKey(_cook))
                return 0;
            dicAllClaims[_cook].realDtEnter = _realdtenter;
            return dicAllClaims[_cook].realPriceEnter = _price;
        }

        public double GetRealPrice(int _cook)
        {
            if (!dicAllClaims.ContainsKey(_cook))
                return 0;
            return dicAllClaims[_cook].realPriceEnter;
        }

        public string GetOrderId(int _cook, TypeWorkOrder _torder, int _martinL = 0)
        {
            string _orderid = "";
            int cookieId = GetCookieId(_cook);
            int tmpcookie = GetCookieIdFromWorkType(cookieId, _torder, _martinL);
            if (cookieId != 0 && dicAllClaims.ContainsKey(tmpcookie))
                _orderid = dicAllClaims[tmpcookie].orderid;

            return _orderid;
        }

        public int GetCookieId(int _cook)
        {
            return _cook % 1000000;
        }

        public int GetCookieIdFromWorkType(int _cookieId, TypeWorkOrder _torder,  int _martinL = 0)
        {
            switch (_torder)
            {
                case TypeWorkOrder.order:
                    return _cookieId + 1000000;
                case TypeWorkOrder.profit:
                    return _cookieId + (10000000 * (_martinL + 1));
                case TypeWorkOrder.loss:
                    return _cookieId + (100000000 * (_martinL + 1));
                default:
                    break;
            }
            return 0;
        }

        public TypeWorkOrder GetTypeCookie(int _cook)
        {
            TypeWorkOrder t = TypeWorkOrder.none;
            if (_cook < 10000000 && _cook > 10) t = TypeWorkOrder.order;
            else if (_cook < 100000000) t = TypeWorkOrder.profit;
            else if (_cook > 100000000) t = TypeWorkOrder.loss;
            return t;
        }
        public TypeWorkOrder GetTypeOrderId(string _orderid)
        {
            TypeWorkOrder t = TypeWorkOrder.none;
            int _cook = GetCookie(_orderid);
            t = GetTypeCookie(_cook);
            return t;
        }
        public double GetAveragePrice(int _cook, int _martinL, out int _countTrade)
        {
            double sumTrade = 0;
            _countTrade = 0;
            int cookId = GetCookieId(_cook);
            foreach (int c in dicAllClaims.Keys)
            {
                if (c % 1000000 == cookId && GetTypeCookie(c) != TypeWorkOrder.profit)
                {
                    sumTrade += dicAllClaims[c].realPriceEnter;
                    _countTrade++;
                }
            }
            return sumTrade / _countTrade;
        }
    }

}

public enum IndicatorCommand
{
    none = 0, up = 1, down = -1
}
public class GraphAreaForGlass
{
    public static List<GraphAreaForGlass> areasList = new List<GraphAreaForGlass>();
    private static List<Rectangle> allBarList = new List<Rectangle>();

    public IndicatorCommand lastIndicatorCommand;
    public double enterX, enterY;
    public double exitX, exitY;
    public double minY, maxY;
    public static void AddData(IndicatorCommand _lastIndicatorCommand, double _xAll, double _yAsk, double _yBid){
        if (areasList.Count == 0
            || (areasList[areasList.Count - 1].lastIndicatorCommand != _lastIndicatorCommand
                && _lastIndicatorCommand != IndicatorCommand.none))
            areasList.Add(new GraphAreaForGlass(_lastIndicatorCommand, _xAll, _yAsk, _yBid));
        else
            AddDataToCurrentItem(_lastIndicatorCommand, _xAll, _yAsk, _yBid);
    }

    private static void AddDataToCurrentItem(IndicatorCommand _lastIndicatorCommand, double _xAll, double _yAsk, double _yBid)
    {
        GraphAreaForGlass ga = areasList[areasList.Count - 1];
        ga.exitX = _xAll;
        switch (ga.lastIndicatorCommand)
        {
            case IndicatorCommand.none:
                break;
            case IndicatorCommand.up:
                ga.maxY = Math.Max(ga.maxY, _yBid);
                ga.minY = Math.Min(ga.minY, _yBid);
                break;
            case IndicatorCommand.down:
                ga.maxY = Math.Max(ga.maxY, _yAsk);
                ga.minY = Math.Min(ga.minY, _yAsk);
                break;
        }
    }
    internal static void ShowAllBars(Canvas _canvas)
    {

        foreach (Rectangle r in allBarList)
            _canvas.Children.Remove(r);

        allBarList.Clear();
        foreach (GraphAreaForGlass baritem in GraphAreaForGlass.areasList)
        {
            Rectangle r = new Rectangle()
            {
                Height = Math.Abs(baritem.maxY - baritem.minY),
                Width = baritem.exitX - baritem.enterX,
                Opacity = 0.3,
                Fill = baritem.lastIndicatorCommand == IndicatorCommand.up ? Brushes.Blue : Brushes.Red
            };
            Canvas.SetZIndex(r, 1);
            Canvas.SetTop(r, baritem.minY);
            Canvas.SetLeft(r, baritem.enterX);
            _canvas.Children.Add(r);
            allBarList.Add(r);
        }
    }
    private GraphAreaForGlass(IndicatorCommand _lastIndicatorCommand, double _xAll, double _yAsk, double _yBid)
    {
        lastIndicatorCommand = _lastIndicatorCommand;
        enterX = exitX = _xAll;
        switch (_lastIndicatorCommand)
        {
            case IndicatorCommand.none:
                break;
            case IndicatorCommand.up:
                minY = maxY = enterY = _yBid;
                break;
            case IndicatorCommand.down:
                minY = maxY = enterY = _yAsk;
                break;
        }
    }
}
