using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfRobot
{
    class QuotesThread
    {
        private string instrument = "";
        private string login = "";
        private string password = "";
        public QuotesThread(string _instrument, string _login, string _password) 
        {
            instrument = _instrument;
            login = _login;
            password = _password;
            MessageBox.Show("p: " + instrument);

        }
    }
}
