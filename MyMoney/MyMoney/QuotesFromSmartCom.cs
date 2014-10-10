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
        public int lotCountTemp = 1;

        public int cookieId = 0;
        public AllClaimsInfo allClaims = new AllClaimsInfo();
        public SmartCOM3Lib.StServerClass scom;
        public DataTable dtInstruments { get; set; }
        public int MartinLevel { get { return _martinlevel; } set { _martinlevel = value; } }

        public event ConnectedHandler OnConnected;

        public event GetInstrumentsHandler OnGetInstruments;

        public event ChangeIndicator OnChangeIndicator;

        public event GetInformation OnInformation;

        SortedDictionary<double, double> glass = new SortedDictionary<double, double>();
        List<int> tempListForIndicator = new List<int>();

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
            scom.ConfigureClient("logLevel=5;CalcPlannedPos=no;logFilePath=D:");
            scom.ConfigureServer("logLevel=5;pingTimeOut=5;logFilePath=D:");
            //scom.connect("mx.ittrade.ru", 8443, login, password); workPortfolioName = "BP12800-RF-01";
            scom.connect("st1.ittrade.ru", 8090, login, password); workPortfolioName = "BP12800-RF-01";
            //scom.connect("mxdemo.ittrade.ru", 8443, "C9GAAL6V", "VKTFP3");  workPortfolioName = "ST59164-RF-01"; // тестовый доступ
            workSymbol = "RTS-12.14_FT";
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
            
            string messageInf = "AddTrade: " + orderid + " | " + tradeno + ": ";
            int cookieTemp = allClaims.GetCookie(orderid);
            if (cookieTemp == 0)
                cookieTemp = allClaims.GetCookie(tradeno);
            if (cookieTemp == 0)
            {
                if (OnInformation != null)
                    OnInformation("cookietemp = 0:  " + messageInf + " (" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")");
                return;
            }
            allClaims.AddTradeNo(cookieTemp, tradeno);

            double realP = allClaims.SetRealPrice(cookieTemp, price);
            int _cidProfit = 0;
            int _cidLoss = 0;

            TypeWorkOrder tWorkOrder = allClaims.GetTypeCookie(cookieTemp);

            if (tWorkOrder == TypeWorkOrder.order) // если это вход по индикатору
            {
                MartinLevel = 0; lotCountTemp = 1;
                _cidProfit = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.profit);
                _cidLoss = allClaims.GetCookieIdFromWorkType(cookieId, TypeWorkOrder.loss);
                allClaims.ActiveCookie = cookieTemp;
                if (amount > 0)
                {
                    allClaims.dicAllClaims[cookieTemp].ProfitLevel = paramTh.profitLongValue;
                    allClaims.dicAllClaims[cookieTemp].LossLevel = paramTh.lossLongValue;
                    scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                        , realP + paramTh.profitLongValue, lotCount, 0, _cidProfit); // 10 000 000
                    if (MartinLevel < paramTh.martingValue)
                    {
                        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                            , realP - paramTh.lossLongValue, lotCount, 0, _cidLoss); // 100 000 000
                    }
                    else
                    {

                    }
                }
                if (amount < 0)
                {
                    allClaims.dicAllClaims[cookieTemp].ProfitLevel = paramTh.profitShortValue;
                    allClaims.dicAllClaims[cookieTemp].LossLevel = paramTh.lossShortValue;
                    scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                        , realP - paramTh.profitShortValue, lotCount, 0, _cidProfit); // 10 000 000
                    if (MartinLevel < paramTh.martingValue)
                    {
                        scom.PlaceOrder(workPortfolioName, workSymbol, StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day
                            , realP + paramTh.lossShortValue, lotCount, 0, _cidLoss); // 100 000 000
                    }
                    else
                    {

                    }
                }
            }
            else if (tWorkOrder == TypeWorkOrder.profit) // если это сработал profit
            {
                string idLossOrder = allClaims.GetOrderId(cookieTemp, TypeWorkOrder.loss, MartinLevel);
                scom.CancelOrder(workPortfolioName, workSymbol, idLossOrder);
            }
            else if (tWorkOrder == TypeWorkOrder.loss) // если это сработал стоп-лосс
            {
                string idProfitOrder = allClaims.GetOrderId(cookieTemp, TypeWorkOrder.profit, MartinLevel);
                scom.CancelOrder(workPortfolioName, workSymbol, idProfitOrder);

                lotCountTemp += lotCount;
                int countTrade = 0;
                double averagePrice = allClaims.GetAveragePrice(cookieTemp, MartinLevel, out countTrade);
                int averageDelta = (int)(Math.Abs(realP - averagePrice) / (countTrade + 1) / 10) * 10;
                int averagePriceRound = (int)((averagePrice + averageDelta) / 10) * 10;
                int profitlevel = allClaims.dicAllClaims[cookieTemp].ProfitLevel += 2 * averageDelta;
                int losslevel = allClaims.dicAllClaims[cookieTemp].LossLevel += averageDelta;
                MartinLevel++;
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
                OnInformation(messageInf + " (" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")");
        }

        void scom_UpdatePosition(string portfolio, string symbol, double avprice, double amount, double planned)
        {
            string messageInf = "\tUpdatePosition: " + symbol + " avprice: " + avprice.ToString() + " amonunt: " + amount.ToString() + " planned:" + planned.ToString()
                + " (" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")";
            if (OnInformation != null)
                OnInformation(messageInf);
        }

        void scom_UpdateOrder(string portfolio, string symbol
            , StOrder_State state, StOrder_Action action, StOrder_Type type, StOrder_Validity validity
            , double price, double amount, double stop, double filled, DateTime datetime, string orderid, string orderno, int status_mask, int cookie)
        {
            string messageInf = "UpdateOrder: " + cookie + "(" + orderid + " | " + orderno + ")price: " + price.ToString() + " stop: " + stop.ToString() + " filled: " + filled.ToString() + ": ";
            allClaims.AddOrderIdAndOrderNo(cookie, amount, action, orderid, orderno);
            switch (state) 
            {
                case StOrder_State.StOrder_State_Pending:
                    messageInf += "Размещен у брокера (" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.CreateSpecificCulture("ru-RU")) + ")";
                    break;
                case StOrder_State.StOrder_State_Open:
                    messageInf += "Выведен на рынок (" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")";
                    break;
                case StOrder_State.StOrder_State_Cancel:
                    messageInf += "Отменён (" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")";
                    break;
                case StOrder_State.StOrder_State_Filled:
                    messageInf += "Исполнен (" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")";
                    break;
                case StOrder_State.StOrder_State_Partial:
                    messageInf += "Исполнен частично (" + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + ")";
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

        }

        void scom_UpdateQuote(string symbol, DateTime datetime, double open, double high, double low, double close, double last, double volume, double size, double bid, double ask, double bidsize, double asksize, double open_int, double go_buy, double go_sell, double go_base, double go_base_backed, double high_limit, double low_limit, int trading_status, double volat, double theor_price)
        {
            lastBid = bid; lastAsk = ask;
        }

        void scom_UpdateBidAsk(string symbol, int row, int nrows, double bid, double bidsize, double ask, double asksize)
        {
            if (!glass.ContainsKey(bid) || !glass.ContainsKey(ask))
            {
                if (!glass.ContainsKey(bid))
                    glass.Add(bid, bidsize);
                if (!glass.ContainsKey(ask))
                    glass.Add(ask, asksize);
            }
            else if (glass[bid] != bidsize || glass[ask] != asksize)
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

                    tempListForIndicator.Clear();
                    // новая версия, более взвешенное значение (как год назад)
                    for (int i = 0; i < paramTh.glassHeight; i++)
                    {
                        sumlong += glass.ContainsKey((int)lastAsk + i * 10)
                            && glass[(int)lastAsk + i * 10] < averageGlass * paramTh.averageValue
                            ? (int)glass[(int)lastAsk + i * 10] : averageGlass;
                        sumshort += glass.ContainsKey((int)lastBid - i * 10)
                            && glass[(int)lastBid - i * 10] < averageGlass * paramTh.averageValue
                            ? (int)glass[(int)lastBid - i * 10] : averageGlass;
                        if (sumlong + sumshort == 0)
                            continue;
                        tempListForIndicator.Add((int)(sumlong - sumshort) * 100 / (sumlong + sumshort));
                    }
                    int s = 0, s10 = 0, s20 = 0;
                    int ii = 0;
                    foreach (int ivalue in tempListForIndicator)
                    {
                        ii++;
                        if (ii < 11) s10 += ivalue;
                        if (ii < 21) s20 += ivalue;
                        s += ivalue;
                    }
                    int indicatorTemp = (int) s / paramTh.glassHeight;
                    int indicatorTemp10 = (int)s10 / 10;
                    int indicatorTemp20 = (int)s20 / 20;
                    if (indicatorTemp != indicator && OnChangeIndicator != null)
                        //OnChangeIndicator(indicatorTemp10.ToString() + " " + indicatorTemp20.ToString() + " " + indicatorTemp.ToString());
                        OnChangeIndicator(indicatorTemp.ToString());
                    indicator = indicatorTemp;
                    // вход лонг
                    if (indicator >= paramTh.indicatorLongValue && priceEnterLong == 0 && priceEnterShort == 0 && Trading)
                    {
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
                    // вход шорт
                    else if (indicator <= -paramTh.indicatorShortValue && priceEnterLong == 0 && priceEnterShort == 0 && Trading)
                    {
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
