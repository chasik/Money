using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MyMoney
{
    public class QuotesFromSmartCom : IDataSource
    {
        public delegate void ChangeIndicator(string _value);

        private string login;
        private string password;
        public double lastBid = 0;
        public double lastAsk = 0;
        public int indicator = 0;
        public int priceEnterLong = 0, priceEnterShort = 0;
        public int lossLongValueTemp = 0, lossShortValueTemp = 0;
        public int profitLongValueTemp = 0, profitShortValueTemp = 0;
        public int lotCount = 1;

        public int cookie = 0, cookieProfit = 0, cookieLoss = 0;

        public SmartCOM3Lib.StServerClass scom;
        public DataTable dtInstruments { get; set; }

        public event ConnectedHandler OnConnected;

        public event GetInstrumentsHandler OnGetInstruments;

        public event ChangeIndicator OnChangeIndicator;

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
            int z = new Random().Next(1, 100);
            cookie = z * 100000;
            cookieProfit = z * 200000;
            cookieLoss = z * 300000;
        }
        public void ConnectToDataSource()
        {
            scom = new SmartCOM3Lib.StServerClass();
            scom.ConfigureClient("logLevel=1");
            scom.ConfigureServer("logLevel=1;pingTimeOut=5");
            //scom.connect("mx.ittrade.ru", 8443, login, password);
            scom.connect("mxdemo.ittrade.ru", 8443, "C9GAAL6V", "VKTFP3"); // тестовый доступ
            scom.Connected += scom_Connected;
        }

        void scom_Connected()
        {
            MessageBox.Show("Connected!!!");
            scom.ListenBidAsks("RTS-12.14_FT");
            scom.UpdateBidAsk += scom_UpdateBidAsk;
            scom.ListenQuotes("RTS-12.14_FT");
            scom.UpdateQuote += scom_UpdateQuote;
            scom.ListenTicks("RTS-12.14_FT");
            scom.AddTick += scom_AddTick;
            scom.OrderFailed += scom_OrderFailed;
            scom.OrderSucceeded += scom_OrderSucceeded;
            scom.UpdateOrder += scom_UpdateOrder;
        }

        void scom_UpdateOrder(string portfolio, string symbol
            , SmartCOM3Lib.StOrder_State state, SmartCOM3Lib.StOrder_Action action, SmartCOM3Lib.StOrder_Type type
            , SmartCOM3Lib.StOrder_Validity validity
            , double price, double amount, double stop, double filled, DateTime datetime, string orderid, string orderno, int status_mask, int cookie)
        {
            //throw new NotImplementedException();

        }

        void scom_OrderSucceeded(int cookie, string orderid)
        {
            //throw new NotImplementedException();
        }

        void scom_OrderFailed(int cookie, string orderid, string reason)
        {
            //throw new NotImplementedException();
        }

        void scom_AddTick(string symbol, DateTime datetime, double price, double volume, string tradeno, SmartCOM3Lib.StOrder_Action action)
        {
            //throw new NotImplementedException();
        }

        void scom_UpdateQuote(string symbol, DateTime datetime, double open, double high, double low, double close, double last, double volume, double size, double bid, double ask, double bidsize, double asksize, double open_int, double go_buy, double go_sell, double go_base, double go_base_backed, double high_limit, double low_limit, int trading_status, double volat, double theor_price)
        {
            //throw new NotImplementedException();
        }

        void scom_UpdateBidAsk(string symbol, int row, int nrows, double bid, double bidsize, double ask, double asksize)
        {
            if (row == 0){
                lastBid = bid; lastAsk = ask;
            }
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
                    int s = 0;
                    foreach (int i in tempListForIndicator)
                        s += i;
                    int indicatorTemp = (int) s / paramTh.glassHeight;
                    if (indicatorTemp != indicator && OnChangeIndicator != null)
                        OnChangeIndicator(indicatorTemp.ToString());
                    indicator = indicatorTemp;
                    // вход лонг
                    if (indicator >= paramTh.indicatorLongValue && priceEnterLong == 0 && priceEnterShort == 0)
                    {
                        lossLongValueTemp = paramTh.lossLongValue;
                        profitLongValueTemp = paramTh.profitLongValue;
                        priceEnterLong = (int)ask;
                        lotCount = 1;
                        cookie++;
                        scom.PlaceOrder("BP12800-RF-01", "RTS-12.14_FT"
                            , SmartCOM3Lib.StOrder_Action.StOrder_Action_Buy, SmartCOM3Lib.StOrder_Type.StOrder_Type_Market
                            , SmartCOM3Lib.StOrder_Validity.StOrder_Validity_Day, 0, lotCount, 0, cookie);
                    }
                    // вход шорт
                    else if (indicator <= -paramTh.indicatorShortValue && priceEnterLong == 0 && priceEnterShort == 0)
                    {
                        lossShortValueTemp = paramTh.lossShortValue;
                        profitShortValueTemp = paramTh.profitShortValue;
                        priceEnterShort = (int)bid;
                        lotCount = 1;
                        cookie++;
                        scom.PlaceOrder("BP12800-RF-01", "RTS-12.14_FT"
                            , SmartCOM3Lib.StOrder_Action.StOrder_Action_Sell, SmartCOM3Lib.StOrder_Type.StOrder_Type_Market
                            , SmartCOM3Lib.StOrder_Validity.StOrder_Validity_Day, 0, lotCount, 0, cookie);
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
