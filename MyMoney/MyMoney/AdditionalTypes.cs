using System;
using System.Collections.Generic;
using System.Data;
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
    public struct diapasonTestParam
    {
        public diapasonTestParam(string _start, string _finish, string _step)
        {
            start = int.Parse( _start);
            finish = int.Parse(_finish);
            step = int.Parse(_step);
        }
        public int start;
        public int finish;
        public int step;

    }
    public struct ParametrsForTest
    {
        public ParametrsForTest(string _shortName, int _i0, float _i1, int _i2, int _i3, int _i4, int _i5)
        {
            glassHeight = _i0;
            shortName = _shortName;
            averageValue = _i1;
            profitValue = _i2;
            lossValue = _i3;
            indicatorValue = _i4;
            martingValue = _i5;
        }
        public string shortName;
        public int glassHeight;
        public float averageValue; // отношение среднего по стакану к каждой позиции (если более averageValue - в рассчет не берется)
        public int profitValue; // значение профита
        public int lossValue; // значение убытка
        public int indicatorValue; // значение индикатора для входа
        public int martingValue; // допустимое количество раз усреднения
    }

    public class ParametrsForTestObj
    {
        public ParametrsForTestObj(ParametrsForTest _p, DataTable _dt)
        {
            paramS = _p;
            dataTableCopy = _dt;
        }
        public ParametrsForTest paramS;
        public DataTable dataTableCopy;
    }

    public class ResultOneThread
    {
        public ResultOneThread()
        {
            countLossDeal = 0;
            countProfitDeal = 0;
            profit = 0;
            loss = 0;
        }
        public int countLossDeal;
        public int countProfitDeal;
        public int profit;
        public int loss;
        public float profitFactor;
    }
}
