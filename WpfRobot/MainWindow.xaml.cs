﻿using System;
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
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sc = new SmartCom("mx.ittrade.ru", 8443, LoginBox.Text, PassBox.Password);
            sc.SmartC.Connected     += new SmartCOM3Lib._IStClient_ConnectedEventHandler(this.ShowConected);
            sc.SmartC.AddPortfolio  += new SmartCOM3Lib._IStClient_AddPortfolioEventHandler(this.AddPortfolio);
            sc.SmartC.AddSymbol += new SmartCOM3Lib._IStClient_AddSymbolEventHandler(this.AddSymbol);

            sc.ConnectDataSource();
            
        }

        private void ShowConected() {
            sc.SmartC.GetPrortfolioList();
            sc.SmartC.GetSymbols();
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate() {
                    Memo.Text = "Connected!!!";
                }
            );
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
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate() { 
                    Label2.Content = row.ToString() + " " + nrows.ToString();
                    if (symbol.ToUpper().Contains("-9.14_FT"))
                        ComboBox1.Items.Add(symbol + "\t\t" + short_name + "\t\t" + long_name);
                }
            );
        }
    }
}
