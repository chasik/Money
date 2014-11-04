using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SmartCOM3Lib;
using DataSources;
using SmartComClass;

namespace WpfRobot
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        SmartCom sc = null;
        private List<QuotesThread> _quotesThreads = new List<QuotesThread>();

        public MainWindow()
        {
            InitializeComponent();

            //InstrumentsList.Items.Add("MICEX");
            InstrumentsList.Items.Add("RTS-12.14_FT");
            //InstrumentsList.Items.Add("Si-12.14_FT");
            //InstrumentsList.Items.Add("GOLD-12.14_FT");
            //InstrumentsList.Items.Add("SBRF-12.14_FT");
            //InstrumentsList.Items.Add("GAZR-12.14_FT");
            //InstrumentsList.Items.Add("LKOH-12.14_FT");
        }
        public void StartThreadsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (string s in InstrumentsList.Items)
            {
                var q = new QuotesThread(this, s, "BP12800", "8GVZ7Z");
                q.OnBeforeStart += q_OnBeforeStart;
                _quotesThreads.Add(q);
                //break;
            }
        }

        void q_OnBeforeStart(TimeSpan t)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {   
                    Label1.Content = t.ToString(@"hh\:mm\:ss");
                }
            );
        }


        public void ShowConnected() {
            sc.SmartC.GetPrortfolioList();
            sc.SmartC.GetSymbols();
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart) delegate() {
                    Label1.Content = "Connected!!!";
                }
            );
        }

        private void ShowDisconnected(string _reason) {
            MessageBox.Show("Отключение: " + _reason);

        }

        private void AddPortfolio(int row, int nrows, string portfolioName, string portfolioExch, StPortfolioStatus portfolioStatus)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate() {
                    Label1.Content = "Инструмент: " + portfolioName;
                }
            );
        }

        private void AddSymbol(int row, int nrows, string symbol, string short_name, string long_name, string type, int decimals, int lot_size, double punkt, double step, string sec_ext_id, string sec_exch_name, DateTime expiry_date, double days_before_expiry, double strike)
        {
            if (symbol.ToUpper().Contains("RTS-12.14_FT")) 
            {
                sc.SmartC.ListenBidAsks(symbol);
                sc.SmartC.UpdateBidAsk += SmartC_UpdateBidAsk;
            }
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate() { 
                    Label2.Content = row.ToString() + " " + nrows.ToString();
                    if (symbol.ToLower().Contains("12.14_ft"))
                        ComboBox1.Items.Add(symbol + "\t\t" + short_name + "\t\t" + long_name);
                }
            );
        }

        void SmartC_UpdateBidAsk(string symbol, int row, int nrows, double bid, double bidsize, double ask, double asksize)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Send,
                (ThreadStart)delegate()
            {
                if (symbol.ToLower().Contains("rts"))
                    Label2.Content = symbol + " -- bid: " + bid.ToString() + " -- bidSize: " + bidsize.ToString() + " -- ask: " + ask.ToString() + " -- askSize: " + asksize.ToString();
            }
            );

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            foreach(QuotesThread qt in _quotesThreads)
            {
                textBox1.AppendText("-- " + qt.Instrument);
            }
        }

    }
}
