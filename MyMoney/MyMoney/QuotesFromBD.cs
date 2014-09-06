using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Data;

namespace MyMoney
{
    public class QuotesFromBD : IDataSource
    {
        private object lockObj = new Object();
        private SqlConnection sqlconn;
        private SqlCommand sqlcommand = new SqlCommand();
        public DataTable dtInstruments { get; set; }
        public DataTable dtAllTables { get; set; }
        public Dictionary<string, int> dictInstruments;
        public Dictionary<string, tableInfo> dictAllTables;
        public string connectionstr = "user id=sa;password=WaNo11998811mssql;server=localhost;database=smartcom;MultipleActiveResultSets=true";
        public QuotesFromBD() 
        {
            dtInstruments = new DataTable();
            dtAllTables = new DataTable();
            dictInstruments = new Dictionary<string, int>();
            dictAllTables = new Dictionary<string, tableInfo>();
        }

        public void ConnectToDataSource()
        {
            new Thread(new ThreadStart(ThreadConnect)).Start();
            return;
        }

        public void ThreadConnect() {
            sqlconn = new SqlConnection(connectionstr);
            sqlconn.Open();
            if (OnConnected != null)
                OnConnected("Соединение с базой данных установлено");
            Thread.Sleep(100000);
        }

        public event ConnectedHandler OnConnected;

        public event GetInstrumentsHandler OnGetInstruments;
        public void GetAllInstruments(){
            new Thread(new ThreadStart(ThreadInstruments)).Start();
        }

        public void ThreadInstruments()
        {
            lock (lockObj)
            {
                sqlcommand.Connection = sqlconn;
                sqlcommand.CommandText = "SELECT * FROM instruments";
                dtInstruments.Load(sqlcommand.ExecuteReader());
                foreach (DataRow item in dtInstruments.Rows)
                {
                    dictInstruments.Add(item["name"].ToString(), int.Parse(item["idinstrument"].ToString()));
                }
                if (OnGetInstruments != null)
                    OnGetInstruments();
            }
        }

        public void GetAllTables(int _idinst)
        {
            lock (lockObj)
            {
                dictAllTables.Clear();
                sqlcommand.CommandText = @"
                    select at.idtable, at.idinstrument, i.name, tt.name as typetname, convert(date, at.datecre) as shortdate, at.datecre from alltables at 
	                    join instruments i on at.idinstrument = i.idinstrument
	                    join typetable tt on at.idtypetable = tt.idtypetable
                    where at.idinstrument = " + _idinst.ToString();
                dtAllTables.Clear();
                dtAllTables.Load(sqlcommand.ExecuteReader());

                foreach (DataRow item in dtAllTables.Rows)
                {
                    Regex r = new Regex(@"((\d+)\.(\d+)\.(\d+))");
                    MatchCollection mc = r.Matches(item["shortdate"].ToString().Remove(10));
                    string sname = item["name"].ToString() + "_" + mc[0].Groups[4] + "-" + mc[0].Groups[3] + "-" + mc[0].Groups[2];
                    string lname = sname + "_" + item["typetname"].ToString();
                    if (!dictAllTables.ContainsKey(lname))
                    {
                        tableInfo ti = new tableInfo();
                        ti.tableType = item["typetname"].ToString();
                        ti.shortName = sname;
                        ti.fullName = lname;
                        ti.dateTable = item["shortdate"].ToString().Remove(10);
                        ti.dayNum = int.Parse(mc[0].Groups[4].ToString());
                        ti.monthNum = int.Parse(mc[0].Groups[3].ToString());
                        ti.yearNum = int.Parse(mc[0].Groups[2].ToString());
                        ti.isntrumentName = item["name"].ToString();
                        dictAllTables.Add(lname, ti);
                    }
                }
            }
        }

        public int GetCountRecrodInTable(string _nameTable)
        {
            sqlcommand.CommandText = "SELECT count(*) FROM [" + _nameTable + "];";

            return (int)sqlcommand.ExecuteScalar();
        }
    }
}
