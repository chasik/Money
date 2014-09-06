using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMoney
{
    public enum ResultConnectToDataSource
    {
    }

    public struct tableInfo
    {
        public string fullName;
        public string shortName;
        public string isntrumentName;
        public string tableType;
        public string dateTable;
        public int dayNum;
        public int monthNum;
        public int yearNum;
    }
}
