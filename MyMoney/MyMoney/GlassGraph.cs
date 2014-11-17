using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MyMoney
{
    public enum ActionGlassItem
    {
        zero = 0,
        sell = 1,
        buy = 2,
    };
    public class GlassGraph
    {
        public GlassGraph(Canvas _c, Canvas _g, Canvas _ribbon, Rectangle _indicatorRect, Rectangle _indicatorRect2, Rectangle _indicatorAverageRect, Rectangle _indicatorAverageRect2, double _step)
        {
            canvas = _c;
            ribboncanvas = _ribbon;
            tickGraphCanvas = _ribbon;

            StepGlass = _step;
            UpBrush = new SolidColorBrush();
            UpBrush.Color = Color.FromArgb(255, 255, 228, 225);
            DownBrush = new SolidColorBrush();
            DownBrush.Color = Color.FromArgb(255, 152, 251, 152);
            ZeroBrush = new SolidColorBrush();
            ZeroBrush.Color = Color.FromArgb(255, 252, 252, 252);

            GradientBrushForIndicator = new LinearGradientBrush();
            GradientBrushForIndicator.StartPoint = new Point(0, 0);
            GradientBrushForIndicator.EndPoint = new Point(0, 1);

            GradientBrushForIndicator2 = new LinearGradientBrush();
            GradientBrushForIndicator2.StartPoint = new Point(0, 0);
            GradientBrushForIndicator2.EndPoint = new Point(0, 1);

            GradientBrushForIndicatorAverage = new LinearGradientBrush();
            GradientBrushForIndicatorAverage.StartPoint = new Point(0, 0);
            GradientBrushForIndicatorAverage.EndPoint = new Point(0, 1);

            GradientBrushForIndicatorAverage2 = new LinearGradientBrush();
            GradientBrushForIndicatorAverage2.StartPoint = new Point(0, 0);
            GradientBrushForIndicatorAverage2.EndPoint = new Point(0, 1);

            _indicatorRect.Fill = GradientBrushForIndicator;
            _indicatorRect2.Fill = GradientBrushForIndicator2;
            _indicatorAverageRect.Fill = GradientBrushForIndicatorAverage;
            _indicatorAverageRect2.Fill = GradientBrushForIndicatorAverage2;

            tickGraph = new Polyline();
            tickGraph.Stroke = System.Windows.Media.Brushes.Black;
            tickGraph.SnapsToDevicePixels = true;
            tickGraph.StrokeThickness = 2;

            tickGraphCanvas.Children.Add(tickGraph);
            Canvas.SetZIndex(tickGraph, 2);
        }
        public void ChangeValues(double _price, double _volume, int _row, ActionGlassItem _action)
        {
            if (GlassValues.ContainsKey(_price))
            {
                if (GlassValues[_price].volume != _volume || GlassValues[_price].action != _action)
                {
                    GlassValues[_price].volume = _volume;
                    GlassValues[_price].action = _action;
                    if (GlassValues[_price].rectMain != null)
                        canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                            (ThreadStart)delegate()
                            {
                                double minAsk = GetMinAsk();
                                double maxBid = GetMaxBid();
                                // сотрим, далеко ли "уполз" стакан
                                double deltaAsk = minAsk - lastMinAsk;
                                if (Math.Abs(deltaAsk) > 100)
                                {
                                    lock (objLock)
                                    {
                                        foreach (GlassItem gi in GlassValues.Values)
                                        {
                                            if (gi.rectMain != null)
                                            {
                                                double top = Canvas.GetTop(gi.rectMain);
                                                gi.rectMain.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 140 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(300)));
                                            }
                                            if (gi.tbVolume != null)
                                            {
                                                double top = Canvas.GetTop(gi.tbVolume);
                                                gi.tbVolume.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 140 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(300)));
                                            }
                                            if (gi.tbPrice != null)
                                            {
                                                double top = Canvas.GetTop(gi.tbPrice);
                                                gi.tbPrice.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 140 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(300)));
                                            }
                                        }
                                    }
                                    lastMinAsk = minAsk;
                                    lastMaxBid = maxBid;
                                }
                                GlassValues[_price].tbVolume.Text = _volume.ToString();
                                if (_action == ActionGlassItem.buy)
                                {
                                    GlassValues[_price].rectMain.Fill = UpBrush;
                                    if (_row == 0)
                                        for (double j = _price - StepGlass; j >= minAsk; j = j - StepGlass)
                                        {
                                            if (!GlassValues.ContainsKey(j) || GlassValues[j].rectMain == null)
                                                continue;
                                            GlassValues[j].action = ActionGlassItem.zero;
                                            GlassValues[j].rectMain.Fill = ZeroBrush;
                                            GlassValues[j].tbVolume.Text = "";
                                        }
                                }
                                else if (_action == ActionGlassItem.sell)
                                {
                                    GlassValues[_price].rectMain.Fill = DownBrush;
                                    if (_row == 0)
                                        for (double j = _price + StepGlass; j <= maxBid; j = j + StepGlass)
                                        {
                                            if (!GlassValues.ContainsKey(j) || GlassValues[j].rectMain == null)
                                                continue;
                                            GlassValues[j].action = ActionGlassItem.zero;
                                            GlassValues[j].rectMain.Fill = ZeroBrush;
                                            GlassValues[j].tbVolume.Text = "";
                                        }
                                }
                                //GlassValues[_price].tbVolume.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(5, 50, TimeSpan.FromMilliseconds(10000)));
                                });
                }
            }
            else
            {
                lock (objLock)
                {
                    GlassValues.Add(_price, new GlassItem(_volume, _action));
                }
                RebuildGlass();
            }
        }
        public void AddTick(double _price, double _volume, ActionGlassItem _action)
        {
            ribboncanvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    if (_price == lastPriceTick)
                        return;
                    
                    for (int i = 0; i < listGradient.Count - queueRect.Count; i++)
                        listGradient.RemoveRange(0, 1);

                    for (int i = 0; i < listArrayValues.Count - queueRect.Count; i++)
                    {
                        int snegative = 0, spositive = 0;
                        double priceentertrade = listTicksPrice[0];
                        double priceexittrade = 0;
                        for (int j = 0; j < 25 && j < listArrayValues[0].Length; j++)
                        {
                            if (listArrayValues[0][j] > 0)
                                spositive += listArrayValues[0][j];
                            else
                                snegative += listArrayValues[0][j];
                        }
                        bool endtrade = false;
                        int jj = 0;
                        while (!endtrade)
                        { 
                            jj++;
                            if (Math.Abs(priceentertrade - listTicksPrice[jj]) > 50)
                            {
                                priceexittrade = listTicksPrice[jj];
                                endtrade = true;
                            }
                        }
                        // Добавляем значения в resultsribbon для анализа
                        int percentDelta = (spositive + Math.Abs(snegative));
                        if (percentDelta != 0)
                        {
                            if (spositive > Math.Abs(snegative))
                                percentDelta = 100 * spositive / percentDelta;
                            else
                                percentDelta = 100 * snegative / percentDelta;
                        }
                        int spp = 0, sll = 0;
                        /*if (Math.Abs(percentDelta) >= 99 && (percentDelta > 0 && lastactiond != ActionGlassItem.buy || percentDelta <= 0 && lastactiond != ActionGlassItem.sell))
                        {*/
                            if (!resultsribbon.ContainsKey(percentDelta))
                                resultsribbon.Add(percentDelta, new ResTestLocal(percentDelta, priceentertrade - priceexittrade));
                            else
                                resultsribbon[percentDelta].AddValues(percentDelta, priceentertrade - priceexittrade);
                            if (percentDelta > 0)
                                lastactiond = ActionGlassItem.buy;
                            else
                                lastactiond = ActionGlassItem.sell;
                            foreach (int rr3 in resultsribbon.Keys)
                            {
                                spp += resultsribbon[rr3].profitCount;
                                sll += resultsribbon[rr3].lossCount;
                            }
                        /*}*/

                         listArrayValues.RemoveRange(0, 1);
                    }

                    for (int i = 0; i < listTicksPrice.Count - queueRect.Count; i++)
                        listTicksPrice.RemoveRange(0, 1);

                   
                    listGradient.Add(GradientBrushForIndicator.Clone());
                    listTicksPrice.Add(_price);
                    int[] _glassvaluesarray = { };
                    Array.Resize(ref _glassvaluesarray, atemp.Length);
                    atemp.CopyTo(_glassvaluesarray, 0);
                    listArrayValues.Add(_glassvaluesarray);

                    int x = 0;
                    foreach (Rectangle r in queueRect)
                    {
                        x++;
                        if (x > queueRect.Count - listGradient.Count)
                            r.Fill = listGradient[listGradient.Count - queueRect.Count + x - 1];
                    }
                    x = 0;
                    tickGraph.Points.Clear();
                    Random rr = new Random((int)DateTime.Now.TimeOfDay.TotalMilliseconds);
                    double maxp = listTicksPrice.Max();
                    double minp = listTicksPrice.Min();
                    double delta = maxp - minp;
                    double onePixelPrice = delta / tickGraphCanvas.ActualHeight;
                    foreach (double p in listTicksPrice)
                    {
                        x++;
                        tickGraph.Points.Add(new Point((double)x + (ribboncanvas.ActualWidth - listTicksPrice.Count - 1), (maxp - p) / onePixelPrice));
                    }
                    lastPriceTick = _price;
                }
            );
        }
        public void ChangeVisualIndicator(int[] _arrind, int[] _arrindAverage)
        {
            Array.Resize(ref atemp, _arrind.Length);
            _arrind.CopyTo(atemp, 0);
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    GradientBrushForIndicator.GradientStops.Clear();
                    GradientBrushForIndicator2.GradientStops.Clear();
                    GradientBrushForIndicatorAverage.GradientStops.Clear();
                    GradientBrushForIndicatorAverage2.GradientStops.Clear();
                    int ival = 0, ival2 = 0, ivalAvr = 0, ivalAvr2 = 0;
                    int s = 0, sa = 0;
                    for (int i = 0; i < _arrind.Length; i++)
                    {
                        s += _arrind[i];
                        sa += _arrindAverage[i];
                        //ival = s / (i + 1);
                        ival = _arrind[i];
                        ival2 = s / (i + 1);
                        ivalAvr = _arrindAverage[i];
                        ivalAvr2 = sa / (i + 1);

                        byte b = Convert.ToByte(Math.Abs(Math.Abs(3 * ival) > 255 ? 255 : 3 * ival) + 0);
                        byte b1 = Convert.ToByte(Math.Abs(Math.Abs(3 * ival) > 255 ? 255 : 3 * ival) + 0);
                        byte b2 = Convert.ToByte(Math.Abs(Math.Abs(3 * ival2) > 255 ? 255 : 3 * ival2) + 0);
                        byte b22 = Convert.ToByte(Math.Abs(Math.Abs(3 * ival2) > 255 ? 255 : 3 * ival2) + 0);
                        byte ba = Convert.ToByte(Math.Abs(Math.Abs(3 * ivalAvr) > 255 ? 255 : 3 * ivalAvr) + 0);
                        byte ba1 = Convert.ToByte(Math.Abs(Math.Abs(3 * ivalAvr) > 255 ? 255 : 3 * ivalAvr) + 0);
                        byte ba2 = Convert.ToByte(Math.Abs(Math.Abs(3 * ivalAvr2) > 255 ? 255 : 3 * ivalAvr2) + 0);
                        byte ba22 = Convert.ToByte(Math.Abs(Math.Abs(3 * ivalAvr2) > 255 ? 255 : 3 * ivalAvr2) + 0);

                        GradientBrushForIndicator.GradientStops.Add(new GradientStop(ival > 0 ? Color.FromRgb(0, b1, 255) : Color.FromRgb(255, b, 0), 1 - (double)i / 50));
                        GradientBrushForIndicator2.GradientStops.Add(new GradientStop(ival2 > 0 ? Color.FromRgb(0, b22, 255) : Color.FromRgb(255, b2, 0), 1 - (double)i / 50));

                        GradientBrushForIndicatorAverage.GradientStops.Add(new GradientStop(ivalAvr > 0 ? Color.FromRgb(0, ba1, 255) : Color.FromRgb(255, ba, 0), 1 - (double)i / 50));
                        GradientBrushForIndicatorAverage2.GradientStops.Add(new GradientStop(ivalAvr2 > 0 ? Color.FromRgb(0, ba22, 255) : Color.FromRgb(255, ba2, 0), 1 - (double)i / 50));
                    }
                });
        }
        public void RebuildGlass()
        {
            lastMinAsk = GetMinAsk();
            lastMaxBid = GetMaxBid();
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    canvas.Children.RemoveRange(4, canvas.Children.Count - 42);
                    double centerPrice = lastMaxBid;
                    double st;
                    centerCanvas = (int)(canvas.ActualHeight / 2);
                    for (int i = 0; i <= (int)(GlassValues.Count / 2); i++)
                    {
                        st = centerPrice + i * StepGlass;
                        DrawItem(i, lastMinAsk, lastMaxBid);
                        if (i != 0)
                            DrawItem(-i, lastMinAsk, lastMaxBid);
                    }
                });
        }
        public void DrawItem(int i, double _minAsk, double _maxBid)
        {
            Rectangle block = new Rectangle();
            block.StrokeThickness = 0.3;
            block.Stroke = Brushes.Black;
            if (i > 0)
                block.Fill = UpBrush;
            else
                block.Fill = DownBrush;
            block.SnapsToDevicePixels = true;
            block.Width = 110;
            block.Height = 14;
            Canvas.SetLeft(block, canvas.ActualWidth - 110 - 120);
            Canvas.SetTop(block, centerCanvas - i * 14);
            TextBlock t = new TextBlock();
            if (GlassValues.ContainsKey(_maxBid + i * StepGlass))
                t.Text = (_maxBid + i * StepGlass).ToString("### ###");
            t.FontSize = 9;
            Canvas.SetLeft(t, canvas.ActualWidth - 40 - 120);
            Canvas.SetTop(t, centerCanvas - i * 14 + 1);
            TextBlock t1 = new TextBlock();
            if (GlassValues.ContainsKey(_maxBid + i * StepGlass))
            {
                t1.Text = GlassValues[_maxBid + i * StepGlass].volume.ToString();
                lock (objLock)
                {
                    GlassValues[_maxBid + i * StepGlass].rectMain = block;
                    GlassValues[_maxBid + i * StepGlass].tbPrice = t;
                    GlassValues[_maxBid + i * StepGlass].tbVolume = t1;
                }
            }

            t1.FontSize = 9;
            Canvas.SetLeft(t1, canvas.ActualWidth - 105 - 120);
            Canvas.SetTop(t1, centerCanvas - i * 14 + 1);
            canvas.Children.Add(block);
            canvas.Children.Add(t);
            canvas.Children.Add(t1);
        }
        public void ChangeBidAsk(double _ask, double _bid)
        {

        }
        private double GetMinAsk()
        {
            double _minA = 1000000;
            lock (objLock)
            {
                foreach (double p in GlassValues.Keys)
                {
                    if (GlassValues[p].action == ActionGlassItem.buy && p < _minA)
                        _minA = p;
                }                
            }

            return _minA;
        }
        private double GetMaxBid()
        {
            double _maxB = -1000000;
            lock (objLock)
            {
                foreach (double p in GlassValues.Keys)
                {
                    if (GlassValues[p].action == ActionGlassItem.sell && p > _maxB)
                        _maxB = p;
                }
            }
            return _maxB;
        }
        public void CreateQueueForRibbon()
        {
            ribboncanvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    for (int x = 0; x < ribboncanvas.ActualWidth; x++)
                    {
                        Rectangle r = new Rectangle();
                        r.Fill = DownBrush;
                        //r.SnapsToDevicePixels = true;
                        r.Width = 1;
                        r.Height = ribboncanvas.ActualHeight;
                        Canvas.SetLeft(r, x);
                        Canvas.SetTop(r, 0);
                        ribboncanvas.Children.Add(r);
                        queueRect.Enqueue(r);
                    }
                }
            );
        }

        public SortedDictionary<double, GlassItem> GlassValues = new SortedDictionary<double, GlassItem>();
        public double StepGlass = 0;
        public Canvas canvas;
        public Canvas ribboncanvas;
        public Canvas tickGraphCanvas;
        public int centerCanvas;
        private double lastMinAsk;
        private double lastMaxBid;
        private double lastPriceTick;
        private int[] atemp = { };
        private ActionGlassItem lastactiond = ActionGlassItem.zero;

        public SolidColorBrush UpBrush;
        public SolidColorBrush DownBrush;
        public SolidColorBrush ZeroBrush;
        public LinearGradientBrush GradientBrushForIndicator;
        public LinearGradientBrush GradientBrushForIndicator2;
        public LinearGradientBrush GradientBrushForIndicatorAverage;
        public LinearGradientBrush GradientBrushForIndicatorAverage2;
        public Polyline tickGraph;

        public object objLock = new Object();
        public Queue<Rectangle> queueRect = new Queue<Rectangle>();
        public List<LinearGradientBrush> listGradient = new List<LinearGradientBrush>();
        public List<double> listTicksPrice = new List<double>();
        public List<int[]> listArrayValues = new List<int[]>();


        public SortedDictionary<int, ResTestLocal> resultsribbon = new SortedDictionary<int, ResTestLocal>();
    }

    public class GlassItem
    {
        public GlassItem(double _volume, ActionGlassItem _action)
        {
            volume = _volume;
            action = _action;
        }
        public double volume;
        public ActionGlassItem action;
        public Rectangle rectMain;
        public TextBlock tbVolume;
        public TextBlock tbPrice;
    }
    public class ResTestLocal
    {
        public ResTestLocal(int _key, double _deltaprice) 
        {
            if ((_key < 0 && _deltaprice < 0) || (_key > 0 && _deltaprice > 0))
                profitCount++;
            else 
                lossCount++;
        }
        public void AddValues(int _key, double _deltaprice)
        {
            if ((_key < 0 && _deltaprice < 0) || (_key > 0 && _deltaprice > 0))
                profitCount++;
            else
                lossCount++;
        }
        public int profitCount = 0;
        public int lossCount = 0;
    }
}
