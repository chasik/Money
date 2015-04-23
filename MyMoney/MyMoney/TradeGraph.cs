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
        private float _maxV;
        private float _minV;
        public MinMaxValue(float _min, float _max)
        {
            _maxV = _max;
            _minV = _min;
            DeltaValue = Math.Max(_maxV, _minV) - Math.Min(_maxV, _minV);
        }
		public float MinValue {
            get{ return _minV; }
            set{ _minV = value; DeltaValue = Math.Max(_maxV, value) - Math.Min(_maxV, value); }
        }
        public float MaxValue {
            get{ return _maxV; }
            set{ _maxV = value; DeltaValue = Math.Max(value, _minV) - Math.Min(value, _minV); }
        }
        public float DeltaValue;
	}
    public class TradeGraph
    {
        private DataRow[] _dr;
        private int heightXArea = 15, widthYArea = 70;
        public SortedDictionary<DateTime, Bar> Bars = new SortedDictionary<DateTime, Bar>();
        //private TypeBar _typeBarGraph = TypeBar.TimeMinuteBar;
        private TypeBar _typeBarGraph = TypeBar.VolumeBar;
        private int ValueBar = 10000;
        public Canvas graphC;
        public Canvas graphI;
        private Bar b;
        private Bar btemp;
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
            set { _typeBarGraph = value; }
        }
        public TradeGraph(Canvas _graph = null, Canvas _indic = null)
        {
            graphC = _graph;
            graphI = _indic;
            b = new Bar(TypeBarGraph, ValueBar);
            btemp = b;
        }
        //
        // расчет баров по тикам
        //
        public void CalcBars()
        {
            Bars.Clear();
            if (drData == null || drData.Length < 1)
                return;
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
                    SolidColorBrush barBrushUp = new SolidColorBrush();
                    barBrushUp.Color = Color.FromArgb(255, 90, 20, 255);
                    SolidColorBrush barBrushDown = new SolidColorBrush();
                    barBrushDown.Color = Color.FromArgb(255, 255, 53, 50);

                    MinMaxValue mm = this.GetMinMaxValues();
                    double widthBar = (graphC.ActualWidth - widthYArea) / Bars.Count / 1.3;
                    double pixelInPunkt = (mm.MaxValue - mm.MinValue) / (graphC.ActualHeight - heightXArea);
                    if (widthBar > 5)
                        widthBar = 5;

                    ClearWorkAreaGraph(pixelInPunkt);

                    int i = -1;
                    foreach (Bar b in Bars.Values)
                    {
                        i++;
                        double topB = (mm.MaxValue - b.hiTick.Price) / pixelInPunkt;
                        double leftB = i * widthBar * 1.3 + widthYArea;
                        double heightB = (b.hiTick.Price - b.lowTick.Price) / pixelInPunkt;

                        Line currentShadow = new Line();
                        currentShadow.X1 = leftB + widthBar / 2;
                        currentShadow.X2 = leftB + widthBar / 2;
                        currentShadow.Y1 = topB;
                        currentShadow.Y2 = topB + heightB;
                        currentShadow.Stroke = Brushes.Black;
                        currentShadow.StrokeThickness = 1;
                        currentShadow.SnapsToDevicePixels = true;

                        Rectangle currentBar = new Rectangle();
                        if (b.closeTick.Price >= b.openTick.Price)
                            currentBar.Fill = barBrushUp;
                        else
                            currentBar.Fill = barBrushDown;

                        currentBar.StrokeThickness = 1;
                        currentBar.Stroke = Brushes.Black;
                        currentBar.SnapsToDevicePixels = true;

                        currentBar.Width = widthBar;
                        currentBar.Height = Math.Abs((b.openTick.Price - b.closeTick.Price) / pixelInPunkt);
                        Canvas.SetTop(currentBar, (mm.MaxValue - Math.Max(b.openTick.Price, b.closeTick.Price)) / pixelInPunkt);
                        Canvas.SetLeft(currentBar, leftB);

                        graphC.Children.Add(currentShadow);
                        graphC.Children.Add(currentBar);
                    }
                });
        }

        private MinMaxValue GetMinMaxValues()
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

        private void ClearWorkAreaGraph(double _pixelInPunkt)
        {
            graphC.Children.Clear();
            Rectangle gback = new Rectangle();
            gback.StrokeThickness = 1;
            gback.Stroke = Brushes.Black;
            gback.Fill = Brushes.LightGray;
            gback.SnapsToDevicePixels = true;
            gback.Width = graphC.ActualWidth - widthYArea;
            gback.Height = graphC.ActualHeight - heightXArea;
            Canvas.SetLeft(gback, widthYArea);
            Canvas.SetTop(gback, 0);
            graphC.Children.Add(gback);

            MinMaxValue mm = this.GetMinMaxValues();
            float stepY = mm.DeltaValue > 1600 ? 500 : 100;
            float remainderY = mm.MaxValue % stepY;
            for (float y = mm.MaxValue - remainderY; y > mm.MinValue; y -= stepY)
            {
                Line horizontLine = new Line();
                horizontLine.X1 = widthYArea;
                horizontLine.X2 = graphC.ActualWidth;// -widthYArea;
                horizontLine.Y1 = horizontLine.Y2 = (mm.MaxValue - y) * graphC.ActualHeight / mm.DeltaValue;
                horizontLine.Stroke = Brushes.Black;
                horizontLine.StrokeThickness = 1;
                horizontLine.SnapsToDevicePixels = true;
                graphC.Children.Add(horizontLine);
                TextBlock t = new TextBlock();
                t.Text = y.ToString("### ###");
                t.FontSize = 9;
                graphC.Children.Add(t);
                Canvas.SetLeft(t, 25);
                Canvas.SetTop(t, (mm.MaxValue - y) * graphC.ActualHeight / mm.DeltaValue - 5);
            }
        }

        public void AddTick(DateTime _dt, double _price, double _volume, ActionGlassItem _action)
        {
            graphC.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {

                    btemp = b.AddTick(new Tick(DateTime.Now, (float) _price, (float) _volume));
                    if (btemp != b)
                    {
                        while (Bars.ContainsKey(b.openTick.dtTick))
                        {
                            b.openTick.dtTick = b.openTick.dtTick.AddMilliseconds(1);
                        }
                        Bars.Add(b.openTick.dtTick, b);
                        b = btemp;
                    }
                    if (graphC != null)
                        DrawGraph();
                });
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
        public ActionGlassItem Action;
        public Tick(DateTime? _dtserver, float? _price, float? _volume, ActionGlassItem _action = ActionGlassItem.zero)
        {
            dtTick = (DateTime)_dtserver;
            Price = (float)_price;
            Volume = (float)_volume;
            Action = _action;
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
        public float SummVolume = 0;

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
            SummVolume += _tick.Volume;
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
            if ((typebar != TypeBar.VolumeBar && deltaT >= (float)valuebar) || (typebar == TypeBar.VolumeBar && SummVolume > (float) valuebar))
            {
                closeTick = _tick;
                return new Bar(typebar, valuebar);
            }
            return this;
        }
    }
}
