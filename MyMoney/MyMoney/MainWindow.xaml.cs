﻿using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Data;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;


namespace MyMoney
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string dtForLogFileName = "";
        public TradeGraph tradeGraphVisual;
        public Thread threadViewGraphDeal;
        float maxPF = 0, maxMargin = 0;
        int lastSecondReIndicator = 0;
        public ObservableCollection<ResultOneThreadSumm> allResults;
        private ObservableCollection<ResultOneThread> detailResults;
        private ObservableCollection<SubDealInfo> detailAllDeals;
        private IDataSource dsource;
        private QuotesFromBD dsourceDB;
        public GlassGraph GlassVisual;
        
        public MainWindow()
        {
            try
            {
                dtForLogFileName = DateTime.Now.ToString("dd-MMM-yyyy HH-mm-ss");
                CultureInfo ci = new CultureInfo("ru-RU");
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
                InitializeComponent();
                tradeGraphVisual = new TradeGraph(); // график для визуализации сделок
                GlassVisual = new GlassGraph(glassCanvas);
                GlassVisual.tbGlassValue = tbValuesGlass;
                GlassVisual.tbGlassValue25 = tbValuesGlass25;

                GlassVisual.visualAllElements.LevelStartGlass = (int)sliderStartGlassLevel.Value;
                GlassVisual.visualAllElements.LevelHeightGlass = (int)sliderGlassHeightLevel.Value;
                GlassVisual.visualAllElements.LevelIgnoreValue = (int)sliderIndicatorLevel.Value;
                GlassVisual.visualAllElements.LevelRefillingValue = (int)sliderRefillingLevel.Value;

                allResults = new ObservableCollection<ResultOneThreadSumm>();
                detailResults = new ObservableCollection<ResultOneThread>();
                detailAllDeals = new ObservableCollection<SubDealInfo>();

                dgResult.ItemsSource = allResults;
                dgResult.ColumnWidth = DataGridLength.Auto;

                dgResultDetail.ItemsSource = detailResults;
                dgResultDetail.ColumnWidth = DataGridLength.Auto;

                dgResultDeals.ItemsSource = detailAllDeals;
                dgResultDeals.ColumnWidth = DataGridLength.Auto;

                DataGridTextColumn c0 = new DataGridTextColumn();
                c0.Header = "shortName"; c0.Binding = new Binding("shortName");
                DataGridTextColumn c1 = new DataGridTextColumn();
                c1.Header = "profitFac"; c1.Binding = new Binding("profitFac");
                DataGridTextColumn c21 = new DataGridTextColumn();
                c21.Header = "margin"; c21.Binding = new Binding("margin");
                DataGridTextColumn c2 = new DataGridTextColumn();
                c2.Header = "profit"; c2.Binding = new Binding("profit");
                DataGridTextColumn c3 = new DataGridTextColumn();
                c3.Header = "loss"; c3.Binding = new Binding("loss");
                DataGridTextColumn c4 = new DataGridTextColumn();
                c4.Header = "countPDeal"; c4.Binding = new Binding("countPDeal");
                DataGridTextColumn c5 = new DataGridTextColumn();
                c5.Header = "countLDeal"; c5.Binding = new Binding("countLDeal");
                dgResultDetail.Columns.Add(c0);
                dgResultDetail.Columns.Add(c1);
                dgResultDetail.Columns.Add(c21);
                dgResultDetail.Columns.Add(c2);
                dgResultDetail.Columns.Add(c3);
                dgResultDetail.Columns.Add(c4);
                dgResultDetail.Columns.Add(c5);
            }
            catch (Exception e)
            {
                MessageBox.Show("MainWindow Catch: " + e.Message);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                listBox1.Items.Clear();
                if (chbConnectToServer.IsChecked == false)
                {
                    dsource = new QuotesFromBD();
                    (dsource as QuotesFromBD).glassgraph = GlassVisual;
                    dsource.OnConnected += new ConnectedHandler(ConnectedEvent);
                    dsource.OnGetInstruments += new GetInstrumentsHandler(GetInstrumentsEvent);
                    (dsource as QuotesFromBD).OnThreadTesterStart += new ThreadStarted(ThreadTesterStarted);
                    (dsource as QuotesFromBD).OnChangeProgress += MainWindow_OnChangeProgress;
                    (dsource as QuotesFromBD).OnFinishOneThread += MainWindow_OnFinishOneThread;
                    //if (chbVisualisationTest.IsChecked == true)
                    //{
                        (dsource as QuotesFromBD).paramTh = new ParametrsForTest(0, new List<string> { }
                            , int.Parse(tbGlassCurrent.Text), float.Parse(tbAverageCurrent.Text)
                            , int.Parse(tbProfitLongCurrent.Text), int.Parse(tbLossLongCurrent.Text)
                            , int.Parse(tbIndicatorEnterCurrent.Text), int.Parse(tbMartingCurrent.Text)
                            , int.Parse(tbLossShortCurrent.Text), int.Parse(tbProfitShortCurrent.Text)
                            , int.Parse(tbIndicatorExitCurrent.Text), int.Parse(tbDelayCurrent.Text));
                    //}
                }
                else
                {
                    dsource = new QuotesFromSmartCom(textBox1.Text, passBox1.Password);
                    (dsource as QuotesFromSmartCom).glassgraph = GlassVisual;
                    GlassVisual.OnDoTradeLong += (dsource as QuotesFromSmartCom).DoTradeLong;
                    GlassVisual.OnDoTradeShort += (dsource as QuotesFromSmartCom).DoTradeShort;
                    (dsource as QuotesFromSmartCom).Trading = (bool)chbTrading.IsChecked;
                    (dsource as QuotesFromSmartCom).paramTh = new ParametrsForTest(0, new List<string> { }
                        , int.Parse(tbGlassCurrent.Text), float.Parse(tbAverageCurrent.Text)
                        , int.Parse(tbProfitLongCurrent.Text), int.Parse(tbLossLongCurrent.Text)
                        , int.Parse(tbIndicatorEnterCurrent.Text), int.Parse(tbMartingCurrent.Text)
                        , int.Parse(tbLossShortCurrent.Text), int.Parse(tbProfitShortCurrent.Text)
                        , int.Parse(tbIndicatorExitCurrent.Text), int.Parse(tbDelayCurrent.Text));
                    (dsource as QuotesFromSmartCom).OnChangeIndicator += MainWindow_OnChangeIndicator;
                }
                dsource.ConnectToDataSource();
                dsource.OnInformation += dsource_OnInformation;

                dsource.OnChangeGlass += GlassVisual.ChangeValues;
                dsource.OnAddTick += GlassVisual.AddTick;
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        void dsource_OnInformation(InfoElement _element,string _mess)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate() {
                    try
                    {
                        switch (_element)
                        {
                            case InfoElement.logfile:
                                StreamWriter sw = File.AppendText(@"C:\logssmartcom\!!! temp text " + dtForLogFileName + ".txt");
                                sw.WriteLine(_mess);
                                sw.Close();
                                break;
                            case InfoElement.tbInformation:
                                tbInformation.Clear();
                                tbInformation.AppendText(_mess + "\r\n");
                                break;
                            case InfoElement.tbInfo2:
                                tbInfo2.Text = _mess;
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ee)
                    {
                        MessageBox.Show("dsource_OnInformation: " + ee.Message);
                    }
            });
        }

        void MainWindow_OnChangeIndicator(string _value)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate() {
                    DateTime dt = DateTime.Now;
                    int ls = dt.Hour * 60 * 60 * 1000 + dt.Minute * 60 * 1000 + dt.Second * 1000 + dt.Millisecond;
                    //if (ls > lastSecondReIndicator + 300)
                    {
                        progressLabel.Content = _value;
                        //tbInformation.AppendText(_value + " " + (dt.Second * 1000 + dt.Millisecond).ToString() + "\r\n");
                        lastSecondReIndicator = ls;
                    }
            });
        }

        void MainWindow_OnFinishOneThread(ResultOneThreadSumm resTh)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                    {
                        allResults.Add(resTh);
                        if (resTh.profitFac > maxPF || resTh.profit - resTh.loss > maxMargin)
                        {
                            if (resTh.profitFac > maxPF)
                                maxPF = resTh.profitFac;
                            if (resTh.profit - resTh.loss > maxMargin)
                                maxMargin = resTh.profit - resTh.loss;
                        }
                    }
                );
        }

        void MainWindow_OnChangeProgress(int minval, int maxval, int val, string mes = "", bool showProgress = true)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate()
                {
                    pbar2.IsIndeterminate = showProgress;
                    progressLabel.Content = val.ToString() + " / " + maxval.ToString();
                });
        }

        private void ConnectedEvent(string mess) 
        {
            dsource.GetAllInstruments();
            this.Dispatcher.BeginInvoke(DispatcherPriority.Render,
                (ThreadStart)delegate()
                {
                    this.button1.Content = mess;
                    if (dsource is QuotesFromBD)
                    {
                        
                    }
                }
            );
        }

        private void GetInstrumentsEvent()
        {

            if (dsource is QuotesFromBD)
            {
                dsourceDB = dsource as QuotesFromBD;
                foreach (DataRow dr in dsourceDB.dtInstruments.Rows)
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Render,
                        (ThreadStart)delegate()
                        {
                            if (dr["name"].ToString().ToUpper().Contains(filterTextBox.Text.ToUpper()))
                                listBox1.Items.Add(dr["name"]);
                        }
                    );
                }
            }
        }

        private void listBox1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // собираем выделенные инструменты и передаем в GetAllTables
            List<int> selInsLst = new List<int>();
            foreach (string item in (sender as ListBox).SelectedItems)
            {
                selInsLst.Add((int)dsourceDB.dictInstruments[item]);
            }
            if (dsourceDB == null || dsourceDB.dictInstruments == null)
                return;
            listBox2.Items.Clear();
            if (selInsLst.Count == 0)
            {
                dsourceDB.dictAllTables.Clear();
                return;
            }
            dsourceDB.GetAllTables(selInsLst.ToArray());
            foreach (string k in dsourceDB.dictAllTables.Keys)
            {
                string st1 = dsourceDB.dictAllTables[k].shortName;
                bool newDate = true;
                foreach (string s in listBox2.Items)
                {
                    if (s.Contains(st1))
                    {
                        newDate = false;
                        break;
                    }
                }
                if (newDate)
                    listBox2.Items.Add(dsourceDB.dictAllTables[k].dateTable + "\t[" + dsourceDB.dictAllTables[k].shortName + "]");
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (dsource == null || (dsource is QuotesFromBD && dsourceDB == null) ) {
                MessageBox.Show("Не подключена база данных!!!");
                return;
            }
            // если это подключение к бд
            if (dsource is QuotesFromBD)
            {
                dsourceDB.selectedSessionList.Clear();
                foreach (string s in listBox2.SelectedItems)
                {
                    foreach (string k in dsourceDB.dictAllTables.Keys)
                    {
                        if (s.Contains(dsourceDB.dictAllTables[k].shortName))
                        {
                            dsourceDB.selectedSessionList.Add(dsourceDB.dictAllTables[k].shortName);
                            break;
                        }
                    }
                }

                dsourceDB.countThreads = int.Parse(tbThreadCount.Text);
                dsourceDB.DoVisualisation = (bool)chbVisualisationTest.IsChecked;

                dsourceDB.dicDiapasonParams.Clear();
                dsourceDB.dicDiapasonParams.Add("glassHeight", new diapasonTestParam(1, tbGlassStart.Text, tbGlassFinish.Text, tbGlassStep.Text));
                dsourceDB.dicDiapasonParams.Add("averageValue", new diapasonTestParam(2, tbAverageStart.Text, tbAverageFinish.Text, tbAverageStep.Text));

                dsourceDB.dicDiapasonParams.Add("profitLongValue", new diapasonTestParam(3, tbProfitLongStart.Text, tbProfitLongFinish.Text, tbProfitLongStep.Text));
                dsourceDB.dicDiapasonParams.Add("lossLongValue", new diapasonTestParam(4, tbLossLongStart.Text, tbLossLongFinish.Text, tbLossLongStep.Text));
                dsourceDB.dicDiapasonParams.Add("profitShortValue", new diapasonTestParam(5, tbProfitShortStart.Text, tbProfitShortFinish.Text, tbProfitShortStep.Text));
                dsourceDB.dicDiapasonParams.Add("lossShortValue", new diapasonTestParam(6, tbLossShortStart.Text, tbLossShortFinish.Text, tbLossShortStep.Text));

                dsourceDB.dicDiapasonParams.Add("indicatorLongValue", new diapasonTestParam(7, tbIndicatorLongStart.Text, tbIndicatorLongFinish.Text, tbIndicatorLongStep.Text));
                dsourceDB.dicDiapasonParams.Add("indicatorShortValue", new diapasonTestParam(8, tbIndicatorShortStart.Text, tbIndicatorShortFinish.Text, tbIndicatorShortStep.Text));
                dsourceDB.dicDiapasonParams.Add("martingValue", new diapasonTestParam(9, tbMartingStart.Text, tbMartingFinish.Text, tbMartingStep.Text));
                dsourceDB.dicDiapasonParams.Add("delay", new diapasonTestParam(10, tbDelayStart.Text, tbDelayFinish.Text, tbDelayStep.Text));
                dsourceDB.StartTester();
            }
        }
        public void ThreadTesterStarted(string _m)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Send,
               (ThreadStart)delegate()
           {
           });
        }

        private void dgResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResultOneThreadSumm r = (sender as DataGrid).SelectedItem as ResultOneThreadSumm;
            if (r == null)
                return;
            detailResults.Clear();
            foreach (ResultOneThread item in r.lstResults)
            {
                detailResults.Add(item);
            }
        }

        private void dgResultDetail_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResultOneThread r = (sender as DataGrid).SelectedItem as ResultOneThread;
            if (r == null)
                return;
            detailAllDeals.Clear();

            foreach (DealInfo item in r.lstAllDeals)
            {
                item.shortName = r.shortName;
                detailAllDeals.Add(item);
                foreach (SubDealInfo item2 in item.lstSubDeal)
                {
                    item2.parentDeal = item;
                    detailAllDeals.Add(item2);
                }
            }
        }

        private void dgResultDeals_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            SubDealInfo di = (e.Row.Item as SubDealInfo);
            if (di == null)
                return;

            if (di.actiond == ActionDeal.subbuy)
                e.Row.Background = new SolidColorBrush(Color.FromArgb(255, 220, 222, 255));
            if (di.actiond == ActionDeal.subsell)
                e.Row.Background = new SolidColorBrush(Color.FromArgb(255, 255, 205, 219));
            else if (di.actiond == ActionDeal.buy && di.margin >= 0)
                e.Row.Background = new SolidColorBrush(Colors.SkyBlue);
            else if (di.actiond == ActionDeal.buy && di.margin < 0)
                e.Row.Background = new SolidColorBrush(Color.FromArgb(255, 0xFF, 0x1E, 0x00));
            else if (di.actiond == ActionDeal.sell && di.margin >= 0)
                e.Row.Background = new SolidColorBrush(Colors.HotPink);
            else if (di.actiond == ActionDeal.sell && di.margin < 0)
                e.Row.Background = new SolidColorBrush(Color.FromArgb(255, 0xFF, 0x1E, 0x00));
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show((sender as Rectangle)..ToString());

        }

        private void dgResultDeals_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SubDealInfo sd = (sender as DataGrid).SelectedItem as SubDealInfo;
            if (sd == null)
                return;
            // находим родительскую сделку
            if (sd.parentDeal != null)
                sd = sd.parentDeal;
            string shortN = sd.shortName;
            if (threadViewGraphDeal == null || !threadViewGraphDeal.IsAlive)
            {
                threadViewGraphDeal = new Thread(new ParameterizedThreadStart(InitGraph));
                threadViewGraphDeal.Start(sd);
            }
        }

        public void InitGraph(object obj)
        {
            SubDealInfo sd = (obj as SubDealInfo);
            DateTime dt1 = sd.datetimeEnter.Subtract(new TimeSpan(0, 3, 0));
            DateTime dt2 = sd.datetimeExit.Add(new TimeSpan(0, 3, 0));
            tradeGraphVisual.drData = dsourceDB.dicSelectedDataTables[sd.shortName].datatable
                .Select("dtserver > '" + dt1.ToString()
                         + "' AND dtserver <'" + dt2.ToString() + "' AND priceTick IS NOT NULL"
                       );
        }

        private void dgResult_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            ResultOneThreadSumm r = (e.Row.Item as ResultOneThreadSumm);
            if (r == null)
                return;

            if (r.profitFac >= 1)
                e.Row.Background = new SolidColorBrush(Color.FromArgb(255, 0x90, 0xDC, 0xED));
            else if (r.profitFac < 1)
                e.Row.Background = new SolidColorBrush(Color.FromArgb(255, 0xED, 0x99, 0xD5));
        }

        private void speedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            speedLabel.Content = e.NewValue.ToString();
            if (dsource is QuotesFromBD && (dsource as QuotesFromBD).DoVisualisation)
                (dsource as QuotesFromBD).SpeedVisualisation = e.NewValue;
        }

        private void glassCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (dsource != null)
            {
                dsource.glassgraph.visualAllElements.ShowData(true, true);
            }
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            lbLevelIngoreGlass.Content = tbAverageCurrent.Text = sliderStartGlassLevel.Value.ToString();
            lbLevelHeighGlass.Content = tbGlassCurrent.Text = sliderGlassHeightLevel.Value.ToString();
            lbLevelIngoreVal.Content = tbIndicatorEnterCurrent.Text = sliderIndicatorLevel.Value.ToString();
            lbLevelRefillingVal.Content = tbIndicatorExitCurrent.Text = sliderRefillingLevel.Value.ToString();
            //if (GlassVisual != null && GlassVisual.ribboncanvas != null)
            //{
            //}
        }

        private void MyWindow_KeyUp(object sender, KeyEventArgs e)
        {
            //MessageBox.Show(e.Key.ToString());
            if (!(dsource is QuotesFromSmartCom))
                return;
            QuotesFromSmartCom scomt = dsource as QuotesFromSmartCom;
            SmartCOM3Lib.StServerClass sc = scomt.scom;
            if (e.Key == Key.Up)
            {
                MessageBox.Show("UP:" + scomt.workPortfolioName);
                //sc.PlaceOrder(scomt.workPortfolioName);
            }
            else if (e.Key == Key.Down)
            {
                MessageBox.Show("DOWN:" + scomt.workPortfolioName);
                //sc.PlaceOrder(scomt.workPortfolioName);
            }
            else if (e.Key == Key.Space)
            {
                MessageBox.Show("space:" + scomt.workPortfolioName);
                //sc.PlaceOrder(scomt.workPortfolioName);
            }
        }

        private void glassCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            dsource.glassgraph.AnimateGlassToCenter((int)Math.Round((double)e.Delta / 2));
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GlassVisual != null)
                GlassVisual.visualAllElements.LevelIgnoreValue = (int) e.NewValue;
            if (lbLevelIngoreVal != null)
                lbLevelIngoreVal.Content = e.NewValue.ToString();
        }
        private void sliderRefillingLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GlassVisual != null)
                GlassVisual.visualAllElements.LevelRefillingValue = (int)e.NewValue;
            if (lbLevelRefillingVal != null)
                lbLevelRefillingVal.Content = e.NewValue.ToString();
        }
        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GlassVisual != null)
                GlassVisual.visualAllElements.LevelStartGlass = (int) e.NewValue;
            if (lbLevelIngoreGlass != null)
                lbLevelIngoreGlass.Content = e.NewValue.ToString();
        }

        private void Slider_ValueChanged_2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GlassVisual != null)
                GlassVisual.visualAllElements.LevelHeightGlass = (int)e.NewValue;
            if (lbLevelHeighGlass != null)
                lbLevelHeighGlass.Content = e.NewValue.ToString();
        }

        private void chbTrading_Checked(object sender, RoutedEventArgs e)
        {
            if (dsource != null && dsource is QuotesFromSmartCom)
                (dsource as QuotesFromSmartCom).Trading = true;
        }

        private void chbTrading_Unchecked(object sender, RoutedEventArgs e)
        {
            if (dsource != null && dsource is QuotesFromSmartCom)
                (dsource as QuotesFromSmartCom).Trading = false;
        }
        private void MyWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }
    }
}
