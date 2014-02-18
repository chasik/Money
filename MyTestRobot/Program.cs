using System;
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
            SmartCom sc = new SmartCom("87.118.223.109", 8090, "LSB0005", "4PG0IX");
            Console.WriteLine(sc.Login);
            sc.SmartC.Connected += () => { Console.WriteLine("CONNECTED3!!!!"); sc.SmartC.GetSymbols(); };
            sc.SmartC.AddSymbol += (int row, int nrows, string symbol, string short_name, string long_name, string type, int decimals, int lot_size, double punkt, double step, string sec_ext_id, string sec_exch_name, DateTime expiry_date, double days_before_expiry, double strike) 
            => {
                if (symbol.ToUpper().StartsWith("RTS-3.14_FT"))
                {
                    Console.WriteLine("{0} => {1}", row, symbol);
                    sc.SmartC.AddTick += (string symbol1, DateTime datetime, double price, double volume, string tradeno, StOrder_Action action) => { Console.WriteLine("{0} => {1} => {2}", price, volume, allCount += (int)volume); };
                    sc.SmartC.ListenTicks(symbol);
                }
            };
            sc.ConnectDataSource();
            Console.ReadLine();
        }
    }
}
