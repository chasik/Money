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


namespace MyMoney
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        float maxPF = 0, maxMargin = 0;
        private ObservableCollection<ResultOneThreadSumm> allResults;
        private ObservableCollection<ResultOneThread> detailResults;
        private ObservableCollection<SubDealInfo> detailAllDeals;
        private IDataSource dsource;
        private QuotesFromBD dsourceDB;
        private Dictionary<int, ResultOneThreadSumm> dicAllResults = new Dictionary<int, ResultOneThreadSumm>();
        public MainWindow()
        {
            InitializeComponent();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            listBox1.Items.Clear();
            if (checkBox1.IsChecked == false)
            {
                dsource = new QuotesFromBD();
                dsource.OnConnected += new ConnectedHandler(ConnectedEvent);
                dsource.OnGetInstruments += new GetInstrumentsHandler(GetInstrumentsEvent);
                (dsource as QuotesFromBD).OnThreadTesterStart += new ThreadStarted(ThreadTesterStarted);
                (dsource as QuotesFromBD).OnChangeProgress += MainWindow_OnChangeProgress;
                (dsource as QuotesFromBD).OnFinishOneThread += MainWindow_OnFinishOneThread;
            }
            else
            {
                dsource = new QuotesFromSmartCom(textBox1.Text, passBox1.Password);
            }
            dsource.ConnectToDataSource();
        }

        void MainWindow_OnFinishOneThread(ResultOneThreadSumm resTh)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                    {
                        dicAllResults.Add(resTh.idParam, resTh);
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
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    pbar1.Minimum = minval;
                    pbar1.Maximum = maxval;
                    pbar1.Value = val;
                    pbar2.IsIndeterminate = showProgress;
                    progressLabel.Content = val.ToString() + " / " + maxval.ToString();
                });
        }

        private void ConnectedEvent(string mess) 
        {
            dsource.GetAllInstruments();
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
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
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (ThreadStart)delegate()
                        {
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
                dsourceDB.dicDiapasonParams.Clear();
                dsourceDB.dicDiapasonParams.Add("glassHeight", new diapasonTestParam(tbGlassStart.Text, tbGlassFinish.Text, tbGlassStep.Text));
                dsourceDB.dicDiapasonParams.Add("averageValue", new diapasonTestParam(tbAverageStart.Text, tbAverageFinish.Text, tbAverageStep.Text));
                dsourceDB.dicDiapasonParams.Add("profitValue", new diapasonTestParam(tbProfitStart.Text, tbProfitFinish.Text, tbProfitStep.Text));
                dsourceDB.dicDiapasonParams.Add("lossValue", new diapasonTestParam(tbLossStart.Text, tbLossFinish.Text, tbLossStep.Text));
                dsourceDB.dicDiapasonParams.Add("indicatorValue", new diapasonTestParam(tbIndicatorStart.Text, tbIndicatorFinish.Text, tbIndicatorStep.Text));
                dsourceDB.dicDiapasonParams.Add("martingValue", new diapasonTestParam(tbMartingStart.Text, tbMartingFinish.Text, tbMartingStep.Text));
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
                detailAllDeals.Add(item);
                foreach (SubDealInfo item2 in item.lstSubDeal)
                {
                    detailAllDeals.Add(item2);
                }
            }
        }

    }
}
