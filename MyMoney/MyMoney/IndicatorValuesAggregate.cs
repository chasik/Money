using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMoney
{
    public class IndicatorValuesAggregate
    {
        public IndicatorValuesAggregate()
        {

        }
        public IndicatorValuesAggregate(TimeSpan _ts)
        {
            interval = _ts;
        }
        public void AddIndicatorValueForAggregate(DateTime _dtvalue, float _value)
        {
            //DateTime dt = new DateTime(_dtvalue.Ticks);
            //TimeSpan ts;
            //do
            //{
                //ts = new TimeSpan(dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
            //timeofday
                //dt.AddMilliseconds(1);
            //} while (dicValues.ContainsKey(ts));
            //dicValues.Add(ts, _value);
            if (_dtvalue.Subtract(lastValueDate) < interval)
                return;
            if (_value > 0)
                LongSummValue += (int)_value;
            if (_value < 0)
                ShortSummValue += (int)Math.Abs(_value);
            lastValueDate = _dtvalue;
        }
        public void Reset()
        {
            dicValues.Clear();
            LongCount = 0;
            ShortCount = 0;
            lastValueDate = new DateTime();
        }
        public DateTime lastValueDate;
        public TimeSpan IntervalL
        {
            get { return interval; }
            set { interval = value; }
        }
        public TimeSpan IntervalLastValue
        {
            get { return intervallastvalue; }
            set { intervallastvalue = value; }
        }
        public float ResultAggregate
        {
            get {
                if (longcount == 0) longcount = 1;
                if (shortcount == 0) shortcount = 1;
                resultaggregate = LongSummValue / LongCount - ShortSummValue / ShortCount;
                return resultaggregate; 
            }
        }
        private Int64 LongSummValue 
        {
            get { return longsummvalue; }
            set {
                LongCount++;
                longsummvalue = value; 
            }
        }
        private Int64 ShortSummValue
        {
            get { return shortsummvalue; }
            set {
                ShortCount++;
                shortsummvalue = value; 
            }
        }
        public int LongCount
        {
            get { return longcount; }
            set {
                if (value == 0)
                    LongSummValue = 0;
                longcount = value; 
            }
        }
        public int ShortCount
        {
            get { return shortcount; }
            set {
                if (value == 0)
                    ShortSummValue = 0;
                shortcount = value; 
            }
        }
        public SortedDictionary<TimeSpan, float> dicValues = new SortedDictionary<TimeSpan, float>();

        private TimeSpan intervallastvalue;
        private TimeSpan interval;
        private float resultaggregate;
        private Int64 longsummvalue;
        private Int64 shortsummvalue;
        private int longcount;
        private int shortcount;
    }
}
