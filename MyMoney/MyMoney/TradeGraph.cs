using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Shapes;
using System.Windows.Media;

namespace MyMoney
{
    public struct MinMaxValue
	{
        public MinMaxValue(float _min, float _max)
        {
            MinValue = _min;
            MaxValue = _max;
        }
		public float MinValue;
        public float MaxValue;
	}
    public class TradeGraph
    {
        private DataRow[] _dr;
        public SortedDictionary<DateTime, Bar> Bars = new SortedDictionary<DateTime, Bar>();
        private TypeBar _typeBarGraph = TypeBar.TimeMinuteBar;
        private int ValueBar = 1;
        public Canvas graphC;
        public Canvas graphI;
        public DataRow[] drData{
            get { return _dr;}
            set {
                _dr = value;
                this.CalcBars();
            }
        }
        public TypeBar TypeBarGraph
        {
            get { return _typeBarGraph; }
            set
            {
                _typeBarGraph = value;
            }
        }
        public TradeGraph(Canvas _graph = null, Canvas _indic = null)
        {
            graphC = _graph;
            graphI = _indic;
        }
        //
        // расчет баров по тикам
        //
        public void CalcBars()
        {
            Bars.Clear();
            if (drData == null || drData.Length < 1)
                return;
            Bar b = new Bar(TypeBarGraph, ValueBar);
            Bar btemp = b;
            foreach (DataRow dr in drData)
            {
                DateTime dt1 = (DateTime)dr.Field<DateTime?>("dtserver");
                float pt = (float)dr.Field<float?>("priceTick");
                float pv = (float)dr.Field<float?>("volumeTick");
                btemp = b.AddTick(new Tick(dt1, pt, pv));
                if (btemp != b)
                {
                    Bars.Add(b.openTick.dtTick, b);
                    b = btemp;
                }
            }
            if (graphC != null)
                DrawGraph();
        }
        //
        // прорисовка графика со свечами
        //
        public void DrawGraph() 
        {
            graphC.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    graphC.Children.Clear();

                    SolidColorBrush barBrushUp = new SolidColorBrush();
                    barBrushUp.Color = Color.FromArgb(255, 90, 20, 255);
                    SolidColorBrush barBrushDown = new SolidColorBrush();
                    barBrushDown.Color = Color.FromArgb(255, 255, 53, 50);

                    MinMaxValue mm = this.GetMinMaxValues();
                    
                    double widthBar = graphC.ActualWidth / Bars.Count / 1.2;
                    double pixelInPunkt = (mm.MaxValue - mm.MinValue) / graphC.ActualHeight;
                    int i = -1;
                    foreach (Bar b in Bars.Values)
                    {
                        i++;
                        double topB = (mm.MaxValue - b.hiTick.Price) / pixelInPunkt;
                        double leftB = i * widthBar * 1.2;
                        double heightB = (b.hiTick.Price - b.lowTick.Price) / pixelInPunkt;

                        Line currentShadow = new Line();
                        currentShadow.X1 = leftB + widthBar / 2;
                        currentShadow.X2 = leftB + widthBar / 2;
                        currentShadow.Y1 = topB;
                        currentShadow.Y2 = topB + heightB;
                        currentShadow.Stroke = Brushes.Black;
                        currentShadow.StrokeThickness = 1;

                        Rectangle currentBar = new Rectangle();
                        if (b.closeTick.Price >= b.openTick.Price)
                            currentBar.Fill = barBrushUp;
                        else
                            currentBar.Fill = barBrushDown;

                        currentBar.StrokeThickness = 1;
                        currentBar.Stroke = Brushes.Black;

                        currentBar.Width = widthBar;
                        currentBar.Height = Math.Abs((b.openTick.Price - b.closeTick.Price) / pixelInPunkt);
                        Canvas.SetTop(currentBar, (mm.MaxValue - Math.Max(b.openTick.Price, b.closeTick.Price)) / pixelInPunkt);
                        Canvas.SetLeft(currentBar, leftB);

                        graphC.Children.Add(currentShadow);
                        graphC.Children.Add(currentBar);
                    }
                });
        }

        public MinMaxValue GetMinMaxValues()
        {
            MinMaxValue _minmaxv = new MinMaxValue(1000000, -1000000);
            foreach (Bar b in Bars.Values)
            {
                if (b.hiTick.Price > _minmaxv.MaxValue)
                    _minmaxv.MaxValue = b.hiTick.Price;
                if (b.lowTick.Price < _minmaxv.MinValue)
                    _minmaxv.MinValue = b.lowTick.Price;
            }
            return _minmaxv;
        }
    }

    public enum TypeBar
    {
        VolumeBar = 0,
        TimeSecondBar = 1,
        TimeMinuteBar = 2,
        TimeHourBar = 3
    }

    public class Tick
    {
        public DateTime dtTick;
        public float Price;
        public float Volume;
        public Tick(DateTime? _dtserver, float? _price, float? _volume)
        {
            dtTick = (DateTime)_dtserver;
            Price = (float)_price;
            Volume = (float)_volume;
        }
    }
    public class Bar
    {
        private Tick _lasttick = null;
        private Tick _openTick;
        private Tick _closeTick;
        private Tick _hiTick;
        private Tick _lowTick;
        public SortedDictionary<DateTime, Tick> Ticks = new SortedDictionary<DateTime, Tick>();

        public Bar(TypeBar _typebar, int _valuebar)
        {
            typebar = _typebar;
            valuebar = _valuebar;
        }
        public TypeBar typebar;
        public int valuebar;
        public Tick openTick { 
            get { return _openTick; }
            set { _openTick = value; } 
        }
        public Tick closeTick {
            get { return _closeTick; }
            set { _closeTick = value; } 
        }
        public Tick hiTick
        {
            get { return _hiTick; }
            set { _hiTick = value; }
        }
        public Tick lowTick
        {
            get { return _lowTick; }
            set { _lowTick = value; }
        }

        public Bar AddTick(Tick _tick)
        {
            TimeSpan ts;
            double deltaT = 0;
            if (_lasttick != null)
                ts = _tick.dtTick.Subtract(this.openTick.dtTick);
            else
            {
                openTick = _tick;
                ts = new TimeSpan();
            }

            switch (typebar)
            {
                case TypeBar.VolumeBar:
                    break;
                case TypeBar.TimeSecondBar:
                    deltaT = ts.TotalSeconds;
                    break;
                case TypeBar.TimeMinuteBar:
                    deltaT = ts.TotalMinutes;
                    break;
                case TypeBar.TimeHourBar:
                    deltaT = ts.TotalHours;
                    break;
                default:
                    break;
            }
            _lasttick = _tick; // запоминаем последний добавленный тик
            if (hiTick == null || _tick.Price > hiTick.Price)
                hiTick = _tick;
            if (lowTick == null || _tick.Price < lowTick.Price)
                lowTick = _tick;
            while (Ticks.ContainsKey(_tick.dtTick))
            {
                _tick.dtTick = _tick.dtTick.AddMilliseconds(1);
            }
            Ticks.Add(_tick.dtTick, _tick);
            if (deltaT >= (float)valuebar)
            {
                closeTick = _tick;
                return new Bar(typebar, valuebar);
            }
            return this;
        }
    }
}
