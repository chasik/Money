using System;
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
            int allCount = 0;
            sc = new SmartCom("mx.ittrade.ru", 8443, LoginBox.Text, PassBox.Password);
            sc.SmartC.Connected += () => { sc.SmartC.GetSymbols(); };
            sc.SmartC.AddSymbol += (int row, int nrows, string symbol, string short_name, string long_name, string type, int decimals, int lot_size, double punkt, double step, string sec_ext_id, string sec_exch_name, DateTime expiry_date, double days_before_expiry, double strike)
            =>
            {
                if (symbol.ToUpper().StartsWith("RTS-9.14_FT"))
                {
                    Label1.SetValue(symbol.ToUpper());
                    sc.SmartC.AddTick += (string symbol1, DateTime datetime, double price, double volume, string tradeno, StOrder_Action action)
                        =>
                        {
                            DateTime dt = DateTime.Now;
                            string nowTime = dt.Hour.ToString() + ":" + dt.Minute.ToString() + ":" + dt.Second.ToString() + "." + dt.Millisecond.ToString();
                            allCount += (int)volume;
                            Memo.Inlines.Add(price + " -> " + volume + " -> " + allCount + " -> " + nowTime);
                        };
                    sc.SmartC.ListenTicks(symbol);
                }
            };
            sc.ConnectDataSource();

        }
    }
}
