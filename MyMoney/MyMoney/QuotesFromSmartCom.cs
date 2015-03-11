using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SmartCOM3Lib;

namespace MyMoney
{
    public class QuotesFromSmartCom : IDataSource
    {
        public delegate void ChangeIndicator(string _value);

        public Boolean Trading = false;
        private int _martinlevel = 0;
        private string login;
        private string password;
        private string workPortfolioName = "";
        private string workSymbol = "";
        public double lastBid = 0;
        public double lastAsk = 0;
        public int indicator = 0;
        public int priceEnterLong = 0, priceEnterShort = 0;
        public int lossLongValueTemp = 0, lossShortValueTemp = 0;
        public int profitLongValueTemp = 0, profitShortValueTemp = 0;
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
            scom.ConfigureClient("logLevel=4;CalcPlannedPos=no;logFilePath=D:");
            scom.ConfigureServer("logLevel=4;pingTimeOut=20;logFilePath=D:");
            scom.connect("mxr.ittrade.ru", 8443, login, password); workPortfolioName = "BP12800-RF-01";
            //scom.connect("st1.ittrade.ru", 8090, login, password); workPortfolioName = "BP12800-RF-01";
            //scom.connect("mxdemo.ittrade.ru", 8443, "C9GAAL6V", "VKTFP3");  workPortfolioName = "ST59164-RF-01"; // тестовый доступ
            workSymbol = "RTS-3.15_FT";
            scom.Connected += scom_Connected;
            scom.Disconnected += scom_Disconnected;
        }

        void scom_Disconnected(string reason)
        {
            MessageBox.Show("Причина отключения: " + reason);
        }

        void scom_Connected()
        {
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

            //scom.OrderFailed += scom_OrderFailed;
            //scom.OrderSucceeded += scom_OrderSucceeded;
            //scom.GetMyOrders(0, workPortfolioName);
        }

        void scom_OrderCancelSucceeded(string orderid)
        {
            TypeWorkOrder t = allClaims.GetTypeOrderId(orderid);
            ClaimInfo c = allClaims.dicAllClaims[allClaims.GetCookie(orderid)];
            switch (t)
            {
                case TypeWorkOrder.none:
                    break;
                case TypeWorkOrder.order:
                    break;
                case TypeWorkOrder.profit:
                    if (MartinLevel > paramTh.martingValue)
                    {
                        priceEnterLong = priceEnterShort = 0;
                    }
                    break;
                case TypeWorkOrder.loss:
                    priceEnterLong = priceEnterShort = 0;
                    break;
                default:
                    break;
            }
        }

        void scom_SetMyOrder(int row, int nrows, string portfolio, string symbol, StOrder_State state, StOrder_Action action, StOrder_Type type, StOrder_Validity validity
            , double price, double amount, double stop, double filled, DateTime datetime, string id, string no, int cookie)
        {
            //if (cookie != 0)
            //    allClaims.AddOrderIdAndOrderNo(cookie, action, id, no);
        }

        void scom_AddTrade(string portfolio, string symbol, string orderid, double price, double amount, DateTime datetime, string tradeno)
        {
            
            string messageInf = " AddTrade: (" + orderid + " | " + tradeno + ") amount: " + amount.ToString() + " ";
            int cookieTemp = allClaims.GetCookie(orderid);
            if (cookieTemp == 0)
                cookieTemp = allClaims.GetCookie(tradeno);
            if (cookieTemp == 0)
            {
                if (OnInformation != null)
                    OnInformation("cookietemp = 0:  " + messageInf + " (" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")");
                if (MartinLevel == paramTh.martingValue)
                {
                    string idProfOrder = allClaims.GetOrderId(allClaims.ActiveCookie, TypeWorkOrder.profit, MartinLevel);
                    MartinLevel++;
                    scom.CancelOrder(workPortfolioName, workSymbol, idProfOrder);
                }
                return;
            }
            allClaims.AddTradeNo(cookieTemp, tradeno);

            double realP = allClaims.SetRealPrice(cookieTemp, price);
            int _cidProfit = 0;
            int _cidLoss = 0;

            TypeWorkOrder tWorkOrder = allClaims.GetTypeCookie(cookieTemp);

            if (tWorkOrder == TypeWorkOrder.none) 
            {
                MartinLevel++;
                string idProfitOrder = allClaims.GetOrderId(cookieTemp, TypeWorkOrder.profit, MartinLevel);
                scom.CancelOrder(workPortfolioName, workSymbol, idProfitOrder);
            }
            else if (tWorkOrder == TypeWorkOrder.order) // если это вход по индикатору
            {
                int pl;

                MartinLevel = 0; lotCountTemp = lotCount;
                _cidProfit = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.profit);
                _cidLoss = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.loss);
                allClaims.ActiveCookie = cookieTemp;
                if (amount > 0)
                {
                    pl = TradeAtVolume ? 50 : paramTh.profitLongValue;
                    allClaims.dicAllClaims[cookieTemp].ProfitLevel = paramTh.profitLongValue;
                    allClaims.dicAllClaims[cookieTemp].LossLevel = paramTh.lossLongValue;
                    scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                        , realP + pl, lotCount, 0, _cidProfit); // 10 000 000

                    scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                            , realP - paramTh.lossLongValue, lotCount, 0, _cidLoss); // 100 000 000
                }
                if (amount < 0)
                {
                    pl = TradeAtVolume ? 50 : paramTh.profitShortValue;
                    allClaims.dicAllClaims[cookieTemp].ProfitLevel = paramTh.profitShortValue;
                    allClaims.dicAllClaims[cookieTemp].LossLevel = paramTh.lossShortValue;
                    scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                        , realP - pl, lotCount, 0, _cidProfit); // 10 000 000

                    scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                            , realP + paramTh.lossShortValue, lotCount, 0, _cidLoss); // 100 000 000
                }
                TradeAtVolume = false;
            }
            else if (tWorkOrder == TypeWorkOrder.profit) // если это сработал profit
            {
                string idLossOrder = allClaims.GetOrderId(cookieTemp, TypeWorkOrder.loss, MartinLevel);
                scom.CancelOrder(workPortfolioName, workSymbol, idLossOrder);
            }
            else if (tWorkOrder == TypeWorkOrder.loss) // если это сработал стоп-лосс
            {
                activeAmounts.Clear();
                string idProfitOrder = allClaims.GetOrderId(cookieTemp, TypeWorkOrder.profit, MartinLevel);
                scom.CancelOrder(workPortfolioName, workSymbol, idProfitOrder);

                lotCountTemp += lotCount;
                int countTrade = 0;
                double averagePrice = allClaims.GetAveragePrice(cookieTemp, MartinLevel, out countTrade);
                int averageDelta = (int)(Math.Abs(realP - averagePrice) / (countTrade + 1) / 10) * 10;
                int averagePriceRound = (int)((averagePrice + averageDelta) / 10) * 10;

                MartinLevel++;
                int profitlevel = allClaims.dicAllClaims[cookieTemp].ProfitLevel += 2 * averageDelta;
                if (MartinLevel == 1)
                    profitlevel = (int)(allClaims.dicAllClaims[cookieTemp].ProfitLevel / 2 / 10) * 10;
                if (MartinLevel == 2)
                    profitlevel = (int)(allClaims.dicAllClaims[cookieTemp].ProfitLevel / 3 / 10) * 10;
                int losslevel = allClaims.dicAllClaims[cookieTemp].LossLevel += averageDelta;

                _cidProfit = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.profit, MartinLevel);
                _cidLoss = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.loss, MartinLevel);
                messageInf = "realp: " + realP.ToString() + "  averagePrice: " + averagePrice.ToString() + "   averageDelta: " + averageDelta.ToString()
                    + "   averagePriceRound: " + averagePriceRound.ToString() + "  profitlevel: " + profitlevel.ToString() + "  losslevel: " + losslevel.ToString();
                if (amount > 0)
                {
                    scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                        , averagePriceRound + profitlevel, lotCountTemp, 0, _cidProfit); // 10 000 000
                    if (MartinLevel < paramTh.martingValue)
                    {
                        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                            , averagePriceRound - losslevel, lotCount, 0, _cidLoss); // 100 000 000
                    }
                    else
                    {
                        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Stop, StOrder_Validity.StOrder_Validity_Day
                            , 0, lotCountTemp, averagePriceRound - losslevel, _cidLoss); // 100 000 000
                    }
                }
                if (amount < 0)
                {
                    scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                        , averagePriceRound - profitlevel, lotCountTemp, 0, _cidProfit); // 10 000 000
                    if (MartinLevel < paramTh.martingValue)
                    {
                        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                            , averagePriceRound + losslevel, lotCount, 0, _cidLoss); // 100 000 000
                    }
                    else
                    {
                        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Stop, StOrder_Validity.StOrder_Validity_Day
                            , 0, lotCountTemp, averagePriceRound + losslevel, _cidLoss); // 100 000 000
                    }
                }
            }

            if (OnInformation != null)
                OnInformation("(" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")" + messageInf);
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
            string messageInf = "(" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")" 
                + "\tUpdateOrder(" + action.ToString() + "): " + cookie + "(" + orderid + " | " + orderno + ") price: " + price.ToString() + " stop: " + stop.ToString() + " filled: " + filled.ToString() + ": ";
            allClaims.AddOrderIdAndOrderNo(cookie, amount, action, orderid, orderno);
            switch (state) 
            {
                case StOrder_State.StOrder_State_Pending:
                    messageInf += "Размещен у брокера";
                    break;
                case StOrder_State.StOrder_State_Open:
                    messageInf += "Выведен на рынок";
                    break;
                case StOrder_State.StOrder_State_Cancel:
                    messageInf += "Отменён";
                    break;
                case StOrder_State.StOrder_State_Filled:
                    messageInf += "Исполнен";
                    break;
                case StOrder_State.StOrder_State_Partial:
                    messageInf += "Исполнен частично";
                    break;
                default:
                    break;
            }
            if (OnInformation != null)
                OnInformation(messageInf);
        }

        void scom_OrderSucceeded(int cookie, string orderid)
        {
            //throw new NotImplementedException();
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
                    OnAddTick(price, volume, ActionGlassItem.buy);
            }
            else if (action == StOrder_Action.StOrder_Action_Sell)
            {
                v = -volume;
                if (OnAddTick != null)
                    OnAddTick(price, volume, ActionGlassItem.sell);
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
                OnChangeGlass(ask, asksize, row, ActionGlassItem.buy);
                OnChangeGlass(bid, bidsize, row, ActionGlassItem.sell);
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
                if (glass.Count > 40)
                {
                    int sumGlass = 0;
                    // среднее значение по стакану
                    for (int i = 0; i < paramTh.glassHeight; i++)
                    {
                        sumGlass += glass.ContainsKey(lastAsk + i * 10) ? (int) glass[lastAsk + i * 10] : 0;
                        sumGlass += glass.ContainsKey(lastBid - i * 10) ? (int) glass[lastBid - i * 10] : 0;
                    }
                    int averageGlass = (int)sumGlass / (paramTh.glassHeight * 2);
                    int sumlong = 0, sumshort = 0;
                    int sumlongAverage = 0, sumshortAverage = 0;
                    tempListForIndicator.Clear();
                    tempListForIndicatorAverage.Clear();
                    // новая версия, более взвешенное значение (как год назад)
                    for (int i = 0; i < paramTh.glassHeight; i++)
                    {
                        if (glass.ContainsKey((int)lastAsk + i * 10))
                            sumlong += (int)glass[(int)lastAsk + i * 10];
                        if (glass.ContainsKey((int)lastBid - i * 10))
                            sumshort += (int)glass[(int)lastBid - i * 10]; 
                        sumlongAverage += glass.ContainsKey((int)lastAsk + i * 10)
                            && glass[(int)lastAsk + i * 10] < averageGlass * paramTh.averageValue
                            ? (int)glass[(int)lastAsk + i * 10] : averageGlass * (int)paramTh.averageValue;
                        sumshortAverage += glass.ContainsKey((int)lastBid - i * 10)
                            && glass[(int)lastBid - i * 10] < averageGlass * paramTh.averageValue
                            ? (int)glass[(int)lastBid - i * 10] : averageGlass * (int)paramTh.averageValue;
                        if (sumlong + sumshort == 0)
                            continue;
                        tempListForIndicator.Add((int)(sumlong - sumshort) * 100 / (sumlong + sumshort));
                        tempListForIndicatorAverage.Add((int)(sumlongAverage - sumshortAverage) * 100 / (sumlongAverage + sumshortAverage));
                    }

                    if (OnChangeVisualIndicator != null)
                        OnChangeVisualIndicator(tempListForIndicator.ToArray(), tempListForIndicatorAverage.ToArray());

                    int s = 0;
                    for (int i = 0; i < paramTh.glassHeight; i++ )
                    {
                        s += tempListForIndicator[i];
                    }
                    //int indicatorTemp = (int) s / paramTh.glassHeight;
                    int indicatorTemp = tempListForIndicator[paramTh.glassHeight - 1];
                    if (indicatorTemp != indicator && OnChangeIndicator != null)
                        OnChangeIndicator(indicatorTemp.ToString() + "\tA: " + activeTradingVolume.ToString()
                            + "\tV: " + activeTradingDiraction.ToString() 
                            + "\tU: " + LongShotCount.ToString() + " D: " + ShortShotCount.ToString());
                    indicator = indicatorTemp;
                    // вход лонг
                    if (indicator <= -paramTh.indicatorLongValue)
                    //if ((activeTradingVolume < 500 && indicator <= -paramTh.indicatorLongValue)
                    //    || (activeTradingVolume > 500 && indicator >= paramTh.indicatorLongValue)) // || activeTradingDiraction > 400)
                    {
                        //if (activeTradingVolume > 400 && !TradeAtVolume)
                        //{
                        //    TradeAtVolume = true;
                        //    activeAmounts.Clear();
                        //}
                        //else
                            LongShotCount++;
                        if (priceEnterLong == 0 && priceEnterShort == 0 && Trading)
                        {
                            LongShotCount = ShortShotCount = 0;
                            lossLongValueTemp = paramTh.lossLongValue;
                            profitLongValueTemp = paramTh.profitLongValue;
                            priceEnterLong = (int)ask;
                            lotCount = 1;
                            cookieId++;
                            MartinLevel = 0;
                            int _cid = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.order);
                            scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Market, StOrder_Validity.StOrder_Validity_Day
                                , 0, lotCount, 0, _cid); // 1 000 000
                            allClaims.Add(_cid, priceEnterLong, lotCount, StOrder_Action.StOrder_Action_Buy);
                        }
                    }
                    // вход шорт
                    else if (indicator >= paramTh.indicatorShortValue)
                    //else if ((activeTradingVolume < 500 && indicator >= paramTh.indicatorShortValue)
                    //    || (activeTradingVolume > 500 && indicator <= -paramTh.indicatorShortValue)) // || activeTradingDiraction < -400)
                    {
                        //if (activeTradingVolume < -400 && !TradeAtVolume)
                        //{
                        //    TradeAtVolume = true;
                        //    activeAmounts.Clear();
                        //}
                        //else
                            ShortShotCount++;
                        if (priceEnterLong == 0 && priceEnterShort == 0 && Trading)
                        {
                            ShortShotCount = LongShotCount = 0;
                            lossShortValueTemp = paramTh.lossShortValue;
                            profitShortValueTemp = paramTh.profitShortValue;
                            priceEnterShort = (int)bid;
                            lotCount = 1;
                            cookieId++;
                            MartinLevel = 0;
                            int _cid = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.order);
                            scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Market, StOrder_Validity.StOrder_Validity_Day
                                , 0, lotCount, 0, _cid); // 1 000 000
                            allClaims.Add(_cid, priceEnterShort, lotCount, StOrder_Action.StOrder_Action_Sell);
                        }
                    }
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
    }
}
