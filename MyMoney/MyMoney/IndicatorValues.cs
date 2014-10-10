using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMoney
{
    public enum ModeIndicator
    {
        glassInterest = 0
    }
    public class DataTableWithCalcValues
    {
        public DataTableWithCalcValues(DataTable _dt)
        {
            datatable = _dt;
        }
        public DataTable datatable;
        public List<IndicatorValues> indicatorvalues = new List<IndicatorValues>();
    }

    public class IndicatorValues
    {
        public IndicatorValues(ParametrsForTest _pt, ModeIndicator _mi)
        {
            switch (_mi)
            {
                case ModeIndicator.glassInterest:
                    parametrs.Add("glassHeight", _pt.glassHeight);
                    parametrs.Add("averageValue", _pt.averageValue);
                    break;
                default:
                    break;
            }
        }

        public bool CalculatedFinish = false;
        public Dictionary<string, float> parametrs = new Dictionary<string, float>();
        public Dictionary<DateTime, float> values = new Dictionary<DateTime, float>();

        public bool Compare(IndicatorValues p1, IndicatorValues p2)
        {
            bool r = true;
            foreach (string k in p1.parametrs.Keys)
            {
                if (!p2.parametrs.ContainsKey(k) || p2.parametrs[k] != p1.parametrs[k])
                {
                    r = false;
                    break;
                }
            }
            return r;
        }
    }
}
