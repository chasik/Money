using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SmartCOM3Lib;
using System.IO;

namespace MyMoney
{
    public class QuotesFromSmartCom : IDataSource
    {
        public delegate void ChangeIndicator(string _value);
        private Boolean dotrading = false;
        public Boolean Trading {
            get{ return dotrading; }
            set{ dotrading = value; }
        }
        private string pathLogs = @"c:\logssmartcom";
        private int _martinlevel = 0;
        private string login;
        private string password;
        public string workPortfolioName = "";
        private string workSymbol = "";
        private double workStep = 0;
        public double lastBid = 0;
        public double lastAsk = 0;
        //public int indicator = 0;
        public int priceEnterLong = 0, priceEnterShort = 0;
        public int lossLongValueTemp = 0, lossShortValueTemp = 0;
        public int profitShortValueTemp = 0;
        private int profitlongvaluetemp = 0;
        public int profitLongValueTemp
        {
            get { return profitlongvaluetemp; }
            set
            {
                profitlongvaluetemp = value;
                if (OnInformation != null)
                    OnInformation(InfoElement.tbInformation, DateTime.Now.ToString() + " profitLongValueTemp:" + value.ToString());
            }
        }
        public int lotCount = 1;
        public int lotCountTemp;

        public int cookieId = 0;
        public AllClaimsInfo allClaims = new AllClaimsInfo();
        public Dictionary<DateTime, int> activeAmounts = new Dictionary<DateTime, int>();
        public List<DateTime> oldAmounts = new List<DateTime>();
        public TimeSpan intervalT = new TimeSpan(0, 0, 30);
        public int activeTradingVolume;
        public int activeTradingDiraction;
        public bool TradeAtVolume = false;

        public SmartCOM3Lib.StServerClass scom;
        public DataTable dtInstruments { get; set; }
        public int MartinLevel { get { return _martinlevel; } set { _martinlevel = value; } }
        public int LongShotCount { get; set; }
        public int ShortShotCount { get; set; }

        public event ConnectedHandler OnConnected;
        public event GetInstrumentsHandler OnGetInstruments;
        public event ChangeIndicator OnChangeIndicator;
        public event GetInformation OnInformation;

        public event ChangeGlass OnChangeGlass;
        public event ChangeVisualIndicator OnChangeVisualIndicator;

        public GlassGraph glassgraph;
        public event AddTick OnAddTick;

        SortedDictionary<double, double> glass = new SortedDictionary<double, double>();
        List<int> tempListForIndicator = new List<int>();
        List<int> tempListForIndicatorAverage = new List<int>();

        ParametrsForTest _paramTh = new ParametrsForTest();
        public ParametrsForTest paramTh {
            get { return _paramTh; }
            set {
                _paramTh = value;
                lossLongValueTemp = value.lossLongValue;
                profitLongValueTemp = value.profitLongValue;
                lossShortValueTemp = value.lossShortValue;
                profitShortValueTemp = value.profitShortValue;
            }
        }

        public QuotesFromSmartCom(string _login, string _pass)
        {
            login = _login;
            password = _pass;
            cookieId = new Random().Next(100000, 900000);
        }

        public void ConnectToDataSource()
        {
            scom = new SmartCOM3Lib.StServerClass();
            //useOrderStreaming=yes;
            scom.ConfigureClient(@"logLevel=4;CalcPlannedPos=no;logFilePath=" + pathLogs);
            scom.ConfigureServer(@"logLevel=4;pingTimeOut=20;logFilePath=" + pathLogs);
            //scom.connect("mx.ittrade.ru", 8443, login, password); workPortfolioName = "BP12800-RF-01";
            scom.connect("mx2.ittrade.ru", 8443, login, password); workPortfolioName = "BP12800-RF-01";
            //scom.connect("mxr.ittrade.ru", 8443, login, password); workPortfolioName = "BP12800-RF-01";
            //scom.connect("st1.ittrade.ru", 8090, login, password); workPortfolioName = "BP12800-RF-01";
            //scom.connect("mxdemo.ittrade.ru", 8443, "JPBABPSD", "3QCCG8");  workPortfolioName = "ST69529-RF-01"; // тестовый доступ
            workSymbol = "RTS-6.15_FT";
            //workSymbol = "SBRF-6.15_FT";
            scom.Connected += scom_Connected;
            scom.Disconnected += scom_Disconnected;
        }

        void scom_Disconnected(string reason)
        {
            MessageBox.Show("Причина отключения: " + reason);
        }

        void scom_Connected()
        {
            scom.AddSymbol += scom_AddSymbol;
            scom.GetSymbols();
            scom.ListenBidAsks(workSymbol);
            scom.UpdateBidAsk += scom_UpdateBidAsk;
            scom.ListenQuotes(workSymbol);
            scom.UpdateQuote += scom_UpdateQuote;
            scom.ListenTicks(workSymbol);
            scom.AddTick += scom_AddTick;

            scom.ListenPortfolio(workPortfolioName);
            scom.SetMyOrder += scom_SetMyOrder;

            scom.AddTrade += scom_AddTrade;
            scom.UpdateOrder += scom_UpdateOrder;
            scom.UpdatePosition += scom_UpdatePosition;

            scom.OrderCancelSucceeded += scom_OrderCancelSucceeded;
            scom.OrderSucceeded += scom_OrderSucceeded;

            //scom.OrderFailed += scom_OrderFailed;
            //scom.GetMyOrders(0, workPortfolioName);
        }

        void scom_AddSymbol(int row, int nrows, string symbol, string short_name, string long_name, string type, int decimals, int lot_size, double punkt, double step, string sec_ext_id, string sec_exch_name, DateTime expiry_date, double days_before_expiry, double strike)
        {
            if (symbol == workSymbol)
            {
                workStep = step;
                glassgraph.StepGlass = step;
            }
            //StreamWriter sw = File.AppendText(@"C:\logssmartcom\!!!symbol.txt");
            //sw.WriteLine("symbol:" + symbol + "           short_name:" + short_name + "                  long_name:" + long_name + "              type:" + type);
            //sw.Close();
        }

        void scom_OrderCancelSucceeded(string orderid)
        {
            //TypeWorkOrder t = allClaims.GetTypeOrderId(orderid);
            //ClaimInfo c = allClaims.dicAllClaims[allClaims.GetCookie(orderid)];
            //switch (t)
            //{
            //    case TypeWorkOrder.none:
            //        break;
            //    case TypeWorkOrder.order:
            //        break;
            //    case TypeWorkOrder.profit:
            //        if (MartinLevel > paramTh.martingValue)
            //        {
            //            priceEnterLong = priceEnterShort = 0;
            //        }
            //        break;
            //    case TypeWorkOrder.loss:
            //        priceEnterLong = priceEnterShort = 0;
            //        break;
            //    default:
            //        break;
            //}
        }

        void scom_SetMyOrder(int row, int nrows, string portfolio, string symbol, StOrder_State state, StOrder_Action action, StOrder_Type type, StOrder_Validity validity
            , double price, double amount, double stop, double filled, DateTime datetime, string id, string no, int cookie)
        {
            allClaims.AddOrderIdAndOrderNo(cookie, amount, action, id, no);
            string messageInf = DateTime.Now.ToString("HH:mm:ss:fff") + " SetMyOrders " + cookie;
            if (OnInformation != null)
                OnInformation(InfoElement.logfile, messageInf);
        }

        void scom_AddTrade(string portfolio, string symbol, string orderno, double price, double amount, DateTime datetime, string tradeno)
        {
            if (!Trading)
                return;
            int cookieTemp = allClaims.GetCookie(orderno);
            if (cookieTemp == 0)
                cookieTemp = allClaims.GetCookie(tradeno);
            if (cookieTemp != 0)
            {
                allClaims.AddTradeNo(cookieTemp, tradeno);
                double realP = allClaims.SetRealPrice(cookieTemp, price, DateTime.Now);
                if (OnInformation != null)
                    OnInformation(InfoElement.logfile, DateTime.Now.ToString("HH:mm:ss:fff") 
                            + " AddTrade  Price: " + allClaims.dicAllClaims[cookieTemp].realPriceEnter + " RealPrice: " + allClaims.dicAllClaims[cookieTemp].realPriceEnter
                            + " cook:" + cookieTemp + " orderId:" + allClaims.dicAllClaims[cookieTemp].orderid + " orderNo:" + orderno + " tradeNo:" + tradeno
                            + " roundTrip: " + (allClaims.dicAllClaims[cookieTemp].dtEnter - allClaims.dicAllClaims[cookieTemp].realDtEnter).TotalMilliseconds.ToString()
                            + " lotCount:" + lotCount.ToString() + "\r\n\r\n"
                        );
                int _cidProfit = 0, _cidLoss = 0;
                TypeWorkOrder tWorkOrder = allClaims.GetTypeCookie(cookieTemp);

                if (tWorkOrder == TypeWorkOrder.none) 
                {
                    string idProfitOrder = allClaims.GetOrderId(cookieTemp, TypeWorkOrder.profit, MartinLevel);
                    scom.CancelOrder(workPortfolioName, workSymbol, idProfitOrder);
                }
                else if (tWorkOrder == TypeWorkOrder.order) // если это вход по индикатору
                {
                    //int pl;
                    //MartinLevel = 0; 
                    lotCountTemp = lotCount;
                    //_cidProfit = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.profit);
                    //_cidLoss = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.loss);
                    allClaims.ActiveCookie = cookieTemp;
                    //if (amount > 0 && allClaims.dicAllClaims[cookieTemp].ProfitLevel == 0) // если уровень профита еще нулевой - значит ставим профит (что бы не задублировался)
                    //{
                    //    pl = /*TradeAtVolume ? 50 : paramTh.profitLongValue */ profitLongValueTemp * (int)workStep;
                    //    allClaims.dicAllClaims[cookieTemp].ProfitLevel = paramTh.profitLongValue;
                    //    allClaims.dicAllClaims[cookieTemp].LossLevel = paramTh.lossLongValue;
                    //    scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                    //        , realP + pl, lotCount, 0, _cidProfit); // 10 000 000

                    //    //scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                    //    //        , realP - paramTh.lossLongValue, lotCount, 0, _cidLoss); // 100 000 000
                    //}
                    //if (amount < 0 && allClaims.dicAllClaims[cookieTemp].ProfitLevel == 0) // если уровень профита еще нулевой - значит ставим профит (что бы не задублировался)
                    //{
                    //    pl = /*TradeAtVolume ? 50 : paramTh.profitShortValue */ profitShortValueTemp * (int)workStep;
                    //    allClaims.dicAllClaims[cookieTemp].ProfitLevel = paramTh.profitShortValue;
                    //    allClaims.dicAllClaims[cookieTemp].LossLevel = paramTh.lossShortValue;
                    //    scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                    //        , realP - pl, lotCount, 0, _cidProfit); // 10 000 000

                    //    //scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                    //    //        , realP + paramTh.lossShortValue, lotCount, 0, _cidLoss); // 100 000 000
                    //}
                //    TradeAtVolume = false;
                }
                else if (tWorkOrder == TypeWorkOrder.profit) // если это сработал profit
                {
                    string idLossOrder = allClaims.GetOrderId(cookieTemp, TypeWorkOrder.loss, MartinLevel);
                    scom.CancelOrder(workPortfolioName, workSymbol, idLossOrder);
                    priceEnterLong = priceEnterShort = 0;
                    lotCount = 1;
                }
                else if (tWorkOrder == TypeWorkOrder.loss) // если это сработал стоп-лосс
                {
                    string idProfitOrder = allClaims.GetOrderId(cookieId, TypeWorkOrder.profit, MartinLevel);
                    if (idProfitOrder != "")
                        scom.CancelOrder(workPortfolioName, workSymbol, idProfitOrder);
                    lotCount *= 2;

                    int countTrade = 0;
                    double averagePrice = allClaims.GetAveragePrice(cookieTemp, MartinLevel, out countTrade);
                    int averageDelta = (int)(Math.Abs(realP - averagePrice) / (countTrade + 1) / (int)workStep) * (int)workStep;
                    int averagePriceRound = (int)((averagePrice + averageDelta) / (int)workStep) * (int)workStep;

                        _cidProfit = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.profit, MartinLevel);
                        _cidLoss = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.loss, MartinLevel);
                    if (amount > 0) 
                    {
                        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                            , averagePriceRound + (int)Math.Round(paramTh.profitLongValue * 0.8), lotCount, 0, _cidProfit); // 10 000 000

                        /*scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                            , averagePriceRound - paramTh.lossLongValue, lotCount, 0, _cidLoss); // 100 000 000*/
                    }
                    else if (amount < 0)
                    {
                        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                            , averagePriceRound - (int)Math.Round(paramTh.profitLongValue * 0.8), lotCount, 0, _cidProfit); // 10 000 000

                        /*scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                            , averagePriceRound + paramTh.lossShortValue, lotCount, 0, _cidLoss); // 100 000 000*/
                    }
                }
            }
            //string messageInf = " AddTrade: (" + orderid + " | " + tradeno + ") amount: " + amount.ToString() + " ";
            //int cookieTemp = allClaims.GetCookie(orderid);
            //if (cookieTemp == 0)
            //    cookieTemp = allClaims.GetCookie(tradeno);
            //if (cookieTemp == 0)
            //{
            //    if (OnInformation != null)
            //        OnInformation("cookietemp = 0:  " + messageInf + " (" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")");
            //    if (MartinLevel == paramTh.martingValue)
            //    {
            //        string idProfOrder = allClaims.GetOrderId(allClaims.ActiveCookie, TypeWorkOrder.profit, MartinLevel);
            //        MartinLevel++;
            //        scom.CancelOrder(workPortfolioName, workSymbol, idProfOrder);
            //    }
            //    return;
            //}
            //allClaims.AddTradeNo(cookieTemp, tradeno);

            //double realP = allClaims.SetRealPrice(cookieTemp, price);
            //int _cidProfit = 0;
            //int _cidLoss = 0;

            //TypeWorkOrder tWorkOrder = allClaims.GetTypeCookie(cookieTemp);

            //if (tWorkOrder == TypeWorkOrder.none) 
            //{
            //    MartinLevel++;
            //    string idProfitOrder = allClaims.GetOrderId(cookieTemp, TypeWorkOrder.profit, MartinLevel);
            //    scom.CancelOrder(workPortfolioName, workSymbol, idProfitOrder);
            //}
            //else if (tWorkOrder == TypeWorkOrder.order) // если это вход по индикатору
            //{
            //    int pl;

            //    MartinLevel = 0; lotCountTemp = lotCount;
            //    _cidProfit = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.profit);
            //    _cidLoss = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.loss);
            //    allClaims.ActiveCookie = cookieTemp;
            //    if (amount > 0)
            //    {
            //        pl = TradeAtVolume ? 50 : paramTh.profitLongValue;
            //        allClaims.dicAllClaims[cookieTemp].ProfitLevel = paramTh.profitLongValue;
            //        allClaims.dicAllClaims[cookieTemp].LossLevel = paramTh.lossLongValue;
            //        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
            //            , realP + pl, lotCount, 0, _cidProfit); // 10 000 000

            //        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
            //                , realP - paramTh.lossLongValue, lotCount, 0, _cidLoss); // 100 000 000
            //    }
            //    if (amount < 0)
            //    {
            //        pl = TradeAtVolume ? 50 : paramTh.profitShortValue;
            //        allClaims.dicAllClaims[cookieTemp].ProfitLevel = paramTh.profitShortValue;
            //        allClaims.dicAllClaims[cookieTemp].LossLevel = paramTh.lossShortValue;
            //        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
            //            , realP - pl, lotCount, 0, _cidProfit); // 10 000 000

            //        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
            //                , realP + paramTh.lossShortValue, lotCount, 0, _cidLoss); // 100 000 000
            //    }
            //    TradeAtVolume = false;
            //}
            //else if (tWorkOrder == TypeWorkOrder.profit) // если это сработал profit
            //{
            //    string idLossOrder = allClaims.GetOrderId(cookieTemp, TypeWorkOrder.loss, MartinLevel);
            //    scom.CancelOrder(workPortfolioName, workSymbol, idLossOrder);
            //}
            //else if (tWorkOrder == TypeWorkOrder.loss) // если это сработал стоп-лосс
            //{
            //    activeAmounts.Clear();
            //    string idProfitOrder = allClaims.GetOrderId(cookieTemp, TypeWorkOrder.profit, MartinLevel);
            //    scom.CancelOrder(workPortfolioName, workSymbol, idProfitOrder);

            //    lotCountTemp += lotCount;
            //    int countTrade = 0;
            //    double averagePrice = allClaims.GetAveragePrice(cookieTemp, MartinLevel, out countTrade);
            //    int averageDelta = (int)(Math.Abs(realP - averagePrice) / (countTrade + 1) / 10) * 10;
            //    int averagePriceRound = (int)((averagePrice + averageDelta) / 10) * 10;

            //    MartinLevel++;
            //    int profitlevel = allClaims.dicAllClaims[cookieTemp].ProfitLevel += 2 * averageDelta;
            //    if (MartinLevel == 1)
            //        profitlevel = (int)(allClaims.dicAllClaims[cookieTemp].ProfitLevel / 2 / 10) * 10;
            //    if (MartinLevel == 2)
            //        profitlevel = (int)(allClaims.dicAllClaims[cookieTemp].ProfitLevel / 3 / 10) * 10;
            //    int losslevel = allClaims.dicAllClaims[cookieTemp].LossLevel += averageDelta;

            //    _cidProfit = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.profit, MartinLevel);
            //    _cidLoss = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.loss, MartinLevel);
            //    messageInf = "realp: " + realP.ToString() + "  averagePrice: " + averagePrice.ToString() + "   averageDelta: " + averageDelta.ToString()
            //        + "   averagePriceRound: " + averagePriceRound.ToString() + "  profitlevel: " + profitlevel.ToString() + "  losslevel: " + losslevel.ToString();
            //    if (amount > 0)
            //    {
            //        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
            //            , averagePriceRound + profitlevel, lotCountTemp, 0, _cidProfit); // 10 000 000
            //        if (MartinLevel < paramTh.martingValue)
            //        {
            //            scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
            //                , averagePriceRound - losslevel, lotCount, 0, _cidLoss); // 100 000 000
            //        }
            //        else
            //        {
            //            scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Stop, StOrder_Validity.StOrder_Validity_Day
            //                , 0, lotCountTemp, averagePriceRound - losslevel, _cidLoss); // 100 000 000
            //        }
            //    }
            //    if (amount < 0)
            //    {
            //        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
            //            , averagePriceRound - profitlevel, lotCountTemp, 0, _cidProfit); // 10 000 000
            //        if (MartinLevel < paramTh.martingValue)
            //        {
            //            scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
            //                , averagePriceRound + losslevel, lotCount, 0, _cidLoss); // 100 000 000
            //        }
            //        else
            //        {
            //            scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Stop, StOrder_Validity.StOrder_Validity_Day
            //                , 0, lotCountTemp, averagePriceRound + losslevel, _cidLoss); // 100 000 000
            //        }
            //    }
            //}

            //if (OnInformation != null)
            //    OnInformation("(" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")" + messageInf);
        }

        void scom_UpdatePosition(string portfolio, string symbol, double avprice, double amount, double planned)
        {
            //string messageInf = " (" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")" 
            //    + "\tUpdatePosition: " + symbol + " avprice: " + avprice.ToString() + " amonunt: " + amount.ToString() + " planned:" + planned.ToString();
            //if (OnInformation != null)
            //    OnInformation(messageInf);
        }

        void scom_UpdateOrder(string portfolio, string symbol
            , StOrder_State state, StOrder_Action action, StOrder_Type type, StOrder_Validity validity
            , double price, double amount, double stop, double filled, DateTime datetime, string orderid, string orderno, int status_mask, int cookie)
        {
            if (!Trading)
                return;
            string messageInf = "";
            //string messageInf = "(" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")" 
            //    + "\tUpdateOrder(" + action.ToString() + "): " + cookie + "(" + orderid + " | " + orderno + ") price: " + price.ToString() + " stop: " + stop.ToString() + " filled: " + filled.ToString() + ": ";
            allClaims.AddOrderIdAndOrderNo(cookie, amount, action, orderid, orderno);
            string roundtrip = "";
            switch (state) 
            {
                case StOrder_State.StOrder_State_Pending:
                    allClaims.dicAllClaims[cookie].dt_State_Pending = DateTime.Now;
                    roundtrip = (allClaims.dicAllClaims[cookie].dtEnter - allClaims.dicAllClaims[cookie].dt_State_Pending).TotalMilliseconds.ToString();
                    messageInf += DateTime.Now.ToString("HH:mm:ss:fff") + " UpdateOrder Размещен у брокера ";
                    break;
                case StOrder_State.StOrder_State_Open:
                    allClaims.dicAllClaims[cookie].dt_State_Open = DateTime.Now;
                    roundtrip = (allClaims.dicAllClaims[cookie].dtEnter - allClaims.dicAllClaims[cookie].dt_State_Open).TotalMilliseconds.ToString();
                    messageInf += DateTime.Now.ToString("HH:mm:ss:fff") + " UpdateOrder Выведен на рынок ";
                    break;
                case StOrder_State.StOrder_State_Cancel:
                    allClaims.dicAllClaims[cookie].dt_State_Cancel = DateTime.Now;
                    roundtrip = (allClaims.dicAllClaims[cookie].dtEnter - allClaims.dicAllClaims[cookie].dt_State_Cancel).TotalMilliseconds.ToString();
                    messageInf += DateTime.Now.ToString("HH:mm:ss:fff") + " UpdateOrder Отменён ";
                    break;
                case StOrder_State.StOrder_State_Filled:
                    allClaims.dicAllClaims[cookie].dt_State_Filled = DateTime.Now;
                    roundtrip = (allClaims.dicAllClaims[cookie].dtEnter - allClaims.dicAllClaims[cookie].dt_State_Filled).TotalMilliseconds.ToString();
                    messageInf += DateTime.Now.ToString("HH:mm:ss:fff") + " UpdateOrder Исполнен ";
                    break;
                case StOrder_State.StOrder_State_Partial:
                    allClaims.dicAllClaims[cookie].dt_State_Partial = DateTime.Now;
                    roundtrip = (allClaims.dicAllClaims[cookie].dtEnter - allClaims.dicAllClaims[cookie].dt_State_Partial).TotalMilliseconds.ToString();
                    messageInf += DateTime.Now.ToString("HH:mm:ss:fff") + " UpdateOrder Исполнен частично amount: " + amount.ToString() + " filled:" + filled.ToString();
                    break;
                default:
                    break;
            }
            if (OnInformation != null)
                OnInformation(InfoElement.logfile, messageInf + " cook:" + cookie + " orderid:" + orderid + " orderNo:" + orderno + " lotCount:" + lotCount.ToString() + " roundTrip:" + roundtrip);
        }

        void scom_OrderSucceeded(int cookie, string orderid) //orderid - id заявки на сервере котировок
        {
            if (!Trading)
                return;
            allClaims.AddOrderId(cookie, orderid);
            string messageInf = DateTime.Now.ToString("HH:mm:ss:fff") + " OrderSucceeded cook:" + cookie + " id:" + orderid + " lotCount:" + lotCount.ToString();
                                                        //+ " roundTrip:" + (allClaims.dicAllClaims[allClaims.GetCookieId(cookie)].dtEnter - DateTime.Now).TotalMilliseconds.ToString();
            if (OnInformation != null)
                OnInformation(InfoElement.logfile, messageInf);
        }

        void scom_OrderFailed(int cookie, string orderid, string reason)
        {
            //throw new NotImplementedException();
        }

        void scom_AddTick(string symbol, DateTime datetime, double price, double volume, string tradeno, StOrder_Action action)
        {
            double v = 0;
            if (action == StOrder_Action.StOrder_Action_Buy)
            {
                v = volume;
                if (OnAddTick != null)
                    OnAddTick(datetime, price, volume, ActionGlassItem.buy);
            }
            else if (action == StOrder_Action.StOrder_Action_Sell)
            {
                v = -volume;
                if (OnAddTick != null)
                    OnAddTick(datetime, price, volume, ActionGlassItem.sell);
            }
            DateTime dttemp = DateTime.Now;
            if (!activeAmounts.ContainsKey(dttemp))
                activeAmounts.Add(dttemp, (int)v);
            else
                activeAmounts[dttemp] += (int)v;

            oldAmounts.Clear();
            int s = 0, s1 = 0;
            foreach (DateTime dt in activeAmounts.Keys)
            {
                if (dttemp.Subtract(dt) > intervalT)
                    oldAmounts.Add(dt);
                else
                {
                    s += Math.Abs(activeAmounts[dt]);
                    s1 += activeAmounts[dt];
                }
            }
            foreach (DateTime dt in oldAmounts)
                activeAmounts.Remove(dt);
            activeTradingVolume = s;
            activeTradingDiraction = s1;
        }

        void scom_UpdateQuote(string symbol, DateTime datetime, double open, double high, double low, double close, double last, double volume, double size, double bid, double ask, double bidsize, double asksize, double open_int, double go_buy, double go_sell, double go_base, double go_base_backed, double high_limit, double low_limit, int trading_status, double volat, double theor_price)
        {
            lastBid = bid; lastAsk = ask;
        }

        void scom_UpdateBidAsk(string symbol, int row, int nrows, double bid, double bidsize, double ask, double asksize)
        {
            if (OnChangeGlass != null)
            {
                OnChangeGlass(DateTime.Now, ask, asksize, row, ActionGlassItem.buy);
                OnChangeGlass(DateTime.Now, bid, bidsize, row, ActionGlassItem.sell);
            }
            if (row == 0)
            {
                lastAsk = ask; lastBid = bid;
            }
            if (!glass.ContainsKey(bid)) { glass.Add(bid, bidsize); return; }
            if (!glass.ContainsKey(ask)) { glass.Add(ask, asksize); return; }

            if (glass[bid] != bidsize || glass[ask] != asksize)
            {
                glass[bid] = bidsize;
                glass[ask] = asksize;
                if (glass.Count > 50 && workStep > 0)
                {
                    int sumGlass = 0;
                    // среднее значение по стакану
                    for (int i = 0; i < paramTh.glassHeight; i++)
                    {
                        sumGlass += glass.ContainsKey(lastAsk + i * workStep) ? (int)glass[lastAsk + i * workStep] : 0;
                        sumGlass += glass.ContainsKey(lastBid - i * workStep) ? (int)glass[lastBid - i * workStep] : 0;
                    }
                    int averageGlass = (int)sumGlass / (paramTh.glassHeight * 2);
                    int sumlong = 0, sumshort = 0;
                    int sumlongAverage = 0, sumshortAverage = 0;
                    tempListForIndicator.Clear();
                    tempListForIndicatorAverage.Clear();
                    // новая версия, более взвешенное значение (как год назад)
                    double lb = lastBid, la = lastAsk;
                    for (int i = 0; i < 50; i++)
                    {
                        if (glass.ContainsKey((int)la + i * workStep))
                            sumlong += (int)glass[(int)la + i * workStep];
                        if (glass.ContainsKey((int)lb - i * workStep))
                            sumshort += (int)glass[(int)lb - i * workStep];
                        sumlongAverage += glass.ContainsKey((int)la + i * workStep)
                            && glass[(int)la + i * workStep] < averageGlass * 1/*paramTh.averageValue*/
                            ? (int)glass[(int)la + i * workStep] : averageGlass * 1/*(int)paramTh.averageValue*/;
                        sumshortAverage += glass.ContainsKey((int)lb - i * workStep)
                            && glass[(int)lb - i * workStep] < averageGlass * 1 /*paramTh.averageValue*/
                            ? (int)glass[(int)lb - i * workStep] : averageGlass * 1 /*(int)paramTh.averageValue*/;
                        if (sumlong + sumshort == 0)
                            continue;
                        tempListForIndicator.Add((int)(sumlong - sumshort) * 100 / (sumlong + sumshort));
                        int tempsumavr = (sumlongAverage + sumshortAverage) == 0 ? 1 : sumlongAverage + sumshortAverage;
                        tempListForIndicatorAverage.Add((int)(sumlongAverage - sumshortAverage) * 100 / (tempsumavr));
                    }

                    //int s = 0;
                    //for (int i = 0; i < Math.Min(50, tempListForIndicator.Count); i++ )
                    //{
                    //    s += tempListForIndicator[i];
                    //}
                    //int indicatorTemp = (int) s / paramTh.glassHeight;
                    //int indicatorTemp = tempListForIndicator[paramTh.glassHeight - 1];
                    //if (indicatorTemp != indicator && OnChangeIndicator != null)
                    //    OnChangeIndicator(indicatorTemp.ToString() + "\tA: " + activeTradingVolume.ToString()
                    //        + "\tV: " + activeTradingDiraction.ToString() 
                    //        + "\tU: " + LongShotCount.ToString() + " D: " + ShortShotCount.ToString());
                    //indicator = indicatorTemp;
                    if (OnChangeVisualIndicator != null)
                        OnChangeVisualIndicator(tempListForIndicator.ToArray(), tempListForIndicatorAverage.ToArray(), sumGlass);

                    //// вход лонг
                    //if (indicator <= -paramTh.indicatorLongValue)
                    ////if ((activeTradingVolume < 500 && indicator <= -paramTh.indicatorLongValue)
                    ////    || (activeTradingVolume > 500 && indicator >= paramTh.indicatorLongValue)) // || activeTradingDiraction > 400)
                    //{
                    //    //if (activeTradingVolume > 400 && !TradeAtVolume)
                    //    //{
                    //    //    TradeAtVolume = true;
                    //    //    activeAmounts.Clear();
                    //    //}
                    //    //else
                    //        LongShotCount++;
                    //    if (priceEnterLong == 0 && priceEnterShort == 0 && Trading)
                    //    {
                    //        LongShotCount = ShortShotCount = 0;
                    //        lossLongValueTemp = paramTh.lossLongValue;
                    //        profitLongValueTemp = paramTh.profitLongValue;
                    //        priceEnterLong = (int)ask;
                    //        lotCount = 1;
                    //        cookieId++;
                    //        MartinLevel = 0;
                    //        int _cid = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.order);
                    //        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Market, StOrder_Validity.StOrder_Validity_Day
                    //            , 0, lotCount, 0, _cid); // 1 000 000
                    //        allClaims.Add(_cid, priceEnterLong, lotCount, StOrder_Action.StOrder_Action_Buy);
                    //    }
                    //}
                    //// вход шорт
                    //else if (indicator >= paramTh.indicatorShortValue)
                    ////else if ((activeTradingVolume < 500 && indicator >= paramTh.indicatorShortValue)
                    ////    || (activeTradingVolume > 500 && indicator <= -paramTh.indicatorShortValue)) // || activeTradingDiraction < -400)
                    //{
                    //    //if (activeTradingVolume < -400 && !TradeAtVolume)
                    //    //{
                    //    //    TradeAtVolume = true;
                    //    //    activeAmounts.Clear();
                    //    //}
                    //    //else
                    //        ShortShotCount++;
                    //    if (priceEnterLong == 0 && priceEnterShort == 0 && Trading)
                    //    {
                    //        ShortShotCount = LongShotCount = 0;
                    //        lossShortValueTemp = paramTh.lossShortValue;
                    //        profitShortValueTemp = paramTh.profitShortValue;
                    //        priceEnterShort = (int)bid;
                    //        lotCount = 1;
                    //        cookieId++;
                    //        MartinLevel = 0;
                    //        int _cid = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.order);
                    //        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Market, StOrder_Validity.StOrder_Validity_Day
                    //            , 0, lotCount, 0, _cid); // 1 000 000
                    //        allClaims.Add(_cid, priceEnterShort, lotCount, StOrder_Action.StOrder_Action_Sell);
                    //    }
                    //}

                }
            }
        }
        public void ThreadInstruments()
        { 

        }

        public void ThreadConnect() 
        { 

        }
        public void GetAllInstruments() 
        {
 
        }
        public void DoTradeLong()
        {
            //LongShotCount++;
            if (priceEnterLong == 0/* && priceEnterShort == 0*/ && Trading)
            {
                //LongShotCount = ShortShotCount = 0;
                lossLongValueTemp = paramTh.lossLongValue;
                string idProfitOrder = allClaims.GetOrderId(cookieId, TypeWorkOrder.profit, MartinLevel);
                string idLossOrder = allClaims.GetOrderId(cookieId, TypeWorkOrder.loss, MartinLevel);
                if (idProfitOrder != "")
                    scom.CancelOrder(workPortfolioName, workSymbol, idProfitOrder);
                if (idLossOrder != "")
                    scom.CancelOrder(workPortfolioName, workSymbol, idLossOrder);
                //MartinLevel = 0;
                cookieId++;
                int _cid = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.order);
                int oldLotCount = priceEnterShort == 0 ? 0 : lotCount;
                lotCount = priceEnterShort == 0 ? 1 : lotCount * 2;
                // увеличиваем профит с каждым лоссом
                profitLongValueTemp = lotCount == 1 ? paramTh.profitLongValue : (int)Math.Round(profitShortValueTemp * 0.8);
                scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Market, StOrder_Validity.StOrder_Validity_Day
                    , 0, lotCount + oldLotCount, 0, _cid); // 1 000 000
                allClaims.Add(_cid, DateTime.Now, (int)lastAsk, lotCount, StOrder_Action.StOrder_Action_Buy);
                allClaims.ActiveCookie = _cid;
                //if (priceEnterShort == 0)
                priceEnterLong = (int)lastAsk;

                // сразу же профит для длинной
                int _cidProfit = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.profit);
                int _cidLoss = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.loss);
                int pl = /*TradeAtVolume ? 50 : paramTh.profitLongValue */ profitLongValueTemp * (int)workStep;
                allClaims.dicAllClaims[_cid].ProfitLevel = paramTh.profitLongValue;
                allClaims.dicAllClaims[_cid].LossLevel = paramTh.lossLongValue;
                scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                    , priceEnterLong + pl, lotCount, 0, _cidProfit); // 10 000 000

                scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                        , priceEnterLong - paramTh.lossLongValue, lotCount, 0, _cidLoss); // 100 000 000

                priceEnterShort = 0;
                string messageInf = DateTime.Now.ToString("HH:mm:ss:fff") + " PlaceOrder LONG " + _cid + " lotCount:" + (lotCount + oldLotCount).ToString();
                if (OnInformation != null)
                    OnInformation(InfoElement.logfile, messageInf);
            }
        }
        public void DoTradeShort()
        {
            //ShortShotCount++;
            if (/*priceEnterLong == 0 &&*/ priceEnterShort == 0 && Trading)
            {
                //ShortShotCount = LongShotCount = 0;
                lossShortValueTemp = paramTh.lossShortValue;
                string idProfitOrder = allClaims.GetOrderId(cookieId, TypeWorkOrder.profit, MartinLevel);
                string idLossOrder = allClaims.GetOrderId(cookieId, TypeWorkOrder.loss, MartinLevel);
                if (idProfitOrder != "")
                    scom.CancelOrder(workPortfolioName, workSymbol, idProfitOrder);
                if (idLossOrder != "")
                    scom.CancelOrder(workPortfolioName, workSymbol, idLossOrder);
                //MartinLevel = 0;
                cookieId++;
                int _cid = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.order);
                int oldLotCount = priceEnterLong == 0 ? 0 : lotCount;
                lotCount = priceEnterLong == 0 ? 1 : lotCount * 2;
                // увеличиваем профит с каждым лоссом
                profitShortValueTemp = lotCount == 1 ? paramTh.profitShortValue : (int) Math.Round(profitLongValueTemp * 0.8);
                scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Market, StOrder_Validity.StOrder_Validity_Day
                    , 0, lotCount + oldLotCount, 0, _cid); // 1 000 000
                allClaims.Add(_cid, DateTime.Now, (int)lastBid, lotCount, StOrder_Action.StOrder_Action_Sell);
                allClaims.ActiveCookie = _cid;
                //if (priceEnterLong == 0)
                priceEnterShort = (int)lastBid;

                //выставляем сразу профит для короткой
                int _cidProfit = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.profit);
                int _cidLoss = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.loss);
                double pl = /*TradeAtVolume ? 50 : paramTh.profitShortValue */ profitShortValueTemp * (int)workStep;
                allClaims.dicAllClaims[_cid].ProfitLevel = profitShortValueTemp;
                allClaims.dicAllClaims[_cid].LossLevel = paramTh.lossShortValue;
                scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                    , priceEnterShort - pl, lotCount, 0, _cidProfit); // 10 000 000

                scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                        , priceEnterShort + paramTh.lossShortValue, lotCount, 0, _cidLoss); // 100 000 000

                priceEnterLong = 0;
                string messageInf = DateTime.Now.ToString("HH:mm:ss:fff") + " PlaceOrder SHORT cook:" + _cid + " lotCount:" + (lotCount + oldLotCount).ToString();
                if (OnInformation != null)
                    OnInformation(InfoElement.logfile, messageInf);
            }
        }
    }
}
