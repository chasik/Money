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
            sc = new SmartCom("mx.ittrade.ru", 8443, LoginBox.Text, PassBox.Password);
            sc.SmartC.Connected += new SmartCOM3Lib._IStClient_ConnectedEventHandler(this.ShowConected);

            sc.ConnectDataSource();

        }

        private void ShowConected() {
            MessageBox.Show(System.Threading.Thread.CurrentThread.ToString());
            MessageBox.Show(Memo.Dispatcher.Thread.ToString());
        }
    }
}
