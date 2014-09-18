using System;
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
        private ObservableCollection<ResultOneThread> allResults;
        private IDataSource dsource;
        private QuotesFromBD dsourceDB;
        public MainWindow()
        {
            InitializeComponent();
            allResults = new ObservableCollection<ResultOneThread>();
/*            DataGridTextColumn c1 = new DataGridTextColumn();
            c1.Header = "profitFactor"; c1.Binding = new Binding("profitFactor"); c1.Width = 120;
            DataGridTextColumn c2 = new DataGridTextColumn();
            c2.Header = "profit"; c2.Binding = new Binding("profit"); c2.Width = 120;
            DataGridTextColumn c3 = new DataGridTextColumn();
            c3.Header = "loss"; c3.Binding = new Binding("loss"); c3.Width = 120;
            DataGridTextColumn c4 = new DataGridTextColumn();
            c4.Header = "countProfitDeal"; c4.Binding = new Binding("countProfitDeal"); c4.Width = 120;
            DataGridTextColumn c5 = new DataGridTextColumn();
            c5.Header = "countLossDeal"; c5.Binding = new Binding("countLossDeal"); c5.Width = 120;
            dgResult.Columns.Add(c1);
            dgResult.Columns.Add(c2);
            dgResult.Columns.Add(c3);
            dgResult.Columns.Add(c4);
            dgResult.Columns.Add(c5);*/
            dgResult.ItemsSource = allResults;
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

        void MainWindow_OnFinishOneThread(ResultOneThread resTh)
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
            textBoxTable.Clear();
            object item = (sender as ListBox).SelectedItem;
            if (item == null || dsourceDB == null || dsourceDB.dictInstruments == null)
                return;
            dsourceDB.GetAllTables((int) dsourceDB.dictInstruments[item.ToString()]);
            listBox2.Items.Clear();
            foreach (string k in dsourceDB.dictAllTables.Keys)
            {
                string st1 = dsourceDB.dictAllTables[k].dateTable;
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
                    listBox2.Items.Add(st1 + "\t[" + dsourceDB.dictAllTables[k].shortName + "]");
            }
        }

        private void listBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            textBoxTable.Clear();
            object item = (sender as ListBox).SelectedItem;
            if (item == null || dsourceDB == null || dsourceDB.dictInstruments == null)
                return;
            foreach(string k in dsourceDB.dictAllTables.Keys)
            {
                if (item.ToString().Contains(dsourceDB.dictAllTables[k].shortName)){
                    textBoxTable.AppendText(k + "\t" + dsourceDB.GetCountRecrodInTable(k).ToString() + "\r\n");
                }
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

    }
}
