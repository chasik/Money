﻿using System;
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

        private string login;
        private string password;
        public double lastBid = 0;
        public double lastAsk = 0;
        public int indicator = 0;
        public int priceEnterLong = 0, priceEnterShort = 0;
        public int lossLongValueTemp = 0, lossShortValueTemp = 0;
        public int profitLongValueTemp = 0, profitShortValueTemp = 0;
        public int lotCount = 1;

        public int idCookieValue { get; set; }
        public int cookie = 0, cookieProfit = 0, cookieLoss = 0;

        public SmartCOM3Lib.StServerClass scom;
        public DataTable dtInstruments { get; set; }

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
            idCookieValue = new Random().Next(10, 99);
            cookie = idCookieValue * 100000;
            cookieProfit = idCookieValue * 200000;
            cookieLoss = idCookieValue * 300000;
        }
        public void ConnectToDataSource()
        {
            scom = new SmartCOM3Lib.StServerClass();
            scom.ConfigureClient("logLevel=1;CalcPlannedPos=no");
            scom.ConfigureServer("logLevel=1;pingTimeOut=5");
            //scom.connect("mx.ittrade.ru", 8443, login, password);
            scom.connect("mxdemo.ittrade.ru", 8443, "C9GAAL6V", "VKTFP3"); // тестовый доступ
            scom.Connected += scom_Connected;
        }

        void scom_Connected()
        {
            scom.ListenBidAsks("RTS-12.14_FT");
            scom.UpdateBidAsk += scom_UpdateBidAsk;
            scom.ListenQuotes("RTS-12.14_FT");
            scom.UpdateQuote += scom_UpdateQuote;
            scom.ListenTicks("RTS-12.14_FT");
            scom.AddTick += scom_AddTick;

            //scom.ListenPortfolio("BP12800-RF-01");
            scom.ListenPortfolio("ST59164-RF-01");
            scom.UpdateOrder += scom_UpdateOrder;
            scom.UpdatePosition += scom_UpdatePosition;
            scom.OrderFailed += scom_OrderFailed;
            scom.OrderSucceeded += scom_OrderSucceeded;
            scom.AddTrade += scom_AddTrade;

        }

        void scom_AddTrade(string portfolio, string symbol, string orderid, double price, double amount, DateTime datetime, string tradeno)
        {
            if (OnInformation != null)
                OnInformation("scom_AddTrade " + price.ToString() + " " + amount.ToString() + " " + orderid + " " + tradeno);
        }

        void scom_UpdatePosition(string portfolio, string symbol, double avprice, double amount, double planned)
        {
            if (OnInformation != null)
                OnInformation("scom_UpdatePosition " + avprice.ToString() + " " + amount.ToString() + " " + planned.ToString());
        }

        void scom_UpdateOrder(string portfolio, string symbol
            , StOrder_State state, StOrder_Action action, StOrder_Type type
            , StOrder_Validity validity
            , double price, double amount, double stop, double filled, DateTime datetime, string orderid, string orderno, int status_mask, int cookie)
        {
            string messageInf = cookie + ": ";
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
                    switch (action)
                    {
                        case StOrder_Action.StOrder_Action_Buy:
                            OnInformation("priceEnterLong: " + priceEnterLong.ToString() + " new: " + price.ToString());
                            priceEnterLong = (int)price;
                            //scom.PlaceOrder("BP12800-RF-01", "RTS-12.14_FT"
                            //scom.PlaceOrder("ST59164-RF-01", "RTS-12.14_FT"
                            //    , StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Limit, StOrder_Validity.StOrder_Validity_Day, priceEnterLong + paramTh.profitLongValue, lotCount, 0, cookieProfit);
                            break;
                        case StOrder_Action.StOrder_Action_Sell:
                            OnInformation("priceEnterShort: " + priceEnterShort.ToString() + " new: " + price.ToString());
                            priceEnterShort = (int)price;
                            //scom.PlaceOrder("BP12800-RF-01", "RTS-12.14_FT"
                            //scom.PlaceOrder("ST59164-RF-01", "RTS-12.14_FT"
                            //    , StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Market, StOrder_Validity.StOrder_Validity_Day, 0, lotCount, 0, cookie);
                            break;
                    }
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
                        //scom.PlaceOrder("BP12800-RF-01", "RTS-12.14_FT"
                        scom.PlaceOrder("ST59164-RF-01", "RTS-12.14_FT"
                            , StOrder_Action.StOrder_Action_Buy, StOrder_Type.StOrder_Type_Market
                            , StOrder_Validity.StOrder_Validity_Day, 0, lotCount, 0, cookie);
                    }
                    // вход шорт
                    else if (indicator <= -paramTh.indicatorShortValue && priceEnterLong == 0 && priceEnterShort == 0)
                    {
                        lossShortValueTemp = paramTh.lossShortValue;
                        profitShortValueTemp = paramTh.profitShortValue;
                        priceEnterShort = (int)bid;
                        lotCount = 1;
                        cookie++;
                        //scom.PlaceOrder("BP12800-RF-01", "RTS-12.14_FT"
                        scom.PlaceOrder("ST59164-RF-01", "RTS-12.14_FT"
                            , StOrder_Action.StOrder_Action_Sell, StOrder_Type.StOrder_Type_Market
                            , StOrder_Validity.StOrder_Validity_Day, 0, lotCount, 0, cookie);
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
