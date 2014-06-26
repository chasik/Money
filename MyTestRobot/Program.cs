using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataSources;
using SmartComClass;
using SmartCOM3Lib;

namespace MyTestRobot
{
    class Program
    {
        private static int allCount { get; set; }
        static void Main(string[] args)
        {
            SqlConnection connection = new SqlConnection("Data Source=WIN-K69N1L1NJDB;Initial Catalog=smartcom;User ID=sa;Password=WaNo11998811mssql;Asynchronous Processing=True;ConnectRetryInterval=5");
            connection.Open();
            SmartCom sc = new SmartCom("mx.ittrade.ru", 8443, "BP12800", "8GVZ7Z");
            Console.WriteLine(sc.Login);
            sc.SmartC.Connected += () => { Console.WriteLine("CONNECTED3!!!!"); sc.SmartC.GetSymbols(); };
            sc.SmartC.AddSymbol += (int row, int nrows, string symbol, string short_name, string long_name, string type, int decimals, int lot_size, double punkt, double step, string sec_ext_id, string sec_exch_name, DateTime expiry_date, double days_before_expiry, double strike) 
            => {
                if (symbol.ToUpper().StartsWith("RTS-9.14_FT"))
                {
                    Console.WriteLine("{0} => {1}", row, symbol);
                    sc.SmartC.AddTick += (string symbol1, DateTime datetime, double price, double volume, string tradeno, StOrder_Action action) 
                        => {
                            DateTime dt = DateTime.Now;
                            string nowTime = dt.Hour.ToString() + ":" + dt.Minute.ToString() + ":" + dt.Second.ToString() + "." + dt.Millisecond.ToString();
                            Console.WriteLine("{0} => {1} => {2} => {3}", price, volume, allCount += (int)volume, nowTime);
                    };
                    sc.SmartC.ListenTicks(symbol);
                }
            };
            sc.ConnectDataSource();
            Console.ReadLine();
        }
    }
}
