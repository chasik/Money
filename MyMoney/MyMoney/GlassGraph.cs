﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
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
            UpBrush = new SolidColorBrush { Color = Color.FromArgb(255, 255, 228, 225) };
            DownBrush = new SolidColorBrush { Color = Color.FromArgb(255, 152, 251, 152) };
            ZeroBrush = new SolidColorBrush { Color = Color.FromArgb(255, 252, 252, 252) };

            GradientBrushForIndicator = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            GradientBrushForIndicator2 = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };

            GradientBrushForIndicatorAverage = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            GradientBrushForIndicatorAverage2 = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };

            _indicatorRect.Fill = GradientBrushForIndicator;
            _indicatorRect2.Fill = GradientBrushForIndicator2;

            _indicatorAverageRect.Fill = GradientBrushForIndicatorAverage;
            _indicatorAverageRect2.Fill = GradientBrushForIndicatorAverage2;

            tickGraphAsk = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(0, 0, 110) }, StrokeThickness = 2, SnapsToDevicePixels = true };
            tickGraphBid = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(110, 0, 0) }, StrokeThickness = 2, SnapsToDevicePixels = true };

            tickGraphCanvas.Children.Add(tickGraphAsk);
            tickGraphCanvas.Children.Add(tickGraphBid);
            Canvas.SetZIndex(tickGraphAsk, 3);
            Canvas.SetZIndex(tickGraphBid, 2);
        }
        public void ChangeValues(double _price, double _volume, int _row, ActionGlassItem _action)
        {
            if (GlassValues.ContainsKey(_price))
            {
                if (GlassValues[_price].volume != _volume || GlassValues[_price].action != _action)
                {
                    ribboncanvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (ThreadStart)delegate()
                        {
                            if (_row == 0) 
                                CalcGlassValue();
                        });
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
                                if (Math.Abs(deltaAsk) > 150)
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
                                            if (gi.line100 != null)
                                            {
                                                double top = Canvas.GetTop(gi.line100);
                                                gi.line100.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 140 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(300)));
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
                    if (_price == lastPriceTick)// && _action == lastActionTick)
                        return;
                    CalcGlassValue();

                    for (int i = 0; i < listGradient.Count - queueRect.Count; i++)
                        listGradient.RemoveRange(0, 1);

                    for (int i = 0; i < listArrayValues.Count - queueRect.Count; i++)
                    {
                        /*int snegative = 0, spositive = 0;
                        double priceentertrade = listTicksPriceAsk[0];
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
                            if (Math.Abs(priceentertrade - listTicksPriceAsk[jj]) > 50)
                            {
                                priceexittrade = listTicksPriceAsk[jj];
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
                        if (Math.Abs(percentDelta) >= 99 && (percentDelta > 0 && lastactiond != ActionGlassItem.buy || percentDelta <= 0 && lastactiond != ActionGlassItem.sell))
                        {
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
                        }*/

                        listArrayValues.RemoveRange(0, 1);
                    }

                    for (int i = 0; i < listTicksPriceAsk.Count - queueRect.Count; i++)
                    {
                        listTicksPriceAsk.RemoveRange(0, 1);
                        listTicksPriceBid.RemoveRange(0, 1);
                    }

                   
                    listGradient.Add(GradientBrushForIndicator.Clone());
                    if (lastPriceAsk == 0) lastPriceAsk = _price;
                    if (lastPriceBid == 0) lastPriceBid = _price;
                    listTicksPriceAsk.Add(lastPriceAsk);
                    listTicksPriceBid.Add(lastPriceBid);
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
                    tickGraphAsk.Points.Clear();
                    tickGraphBid.Points.Clear();
                    Random rr = new Random((int)DateTime.Now.TimeOfDay.TotalMilliseconds);
                    double maxp = listTicksPriceAsk.Max() + 20;
                    double minp = listTicksPriceBid.Min() - 20;
                    double delta = maxp - minp;
                    double onePixelPrice = delta / tickGraphCanvas.ActualHeight;
                    foreach (double p in listTicksPriceAsk)
                    {
                        x++;
                        tickGraphAsk.Points.Add(new Point((double)x + (ribboncanvas.ActualWidth - listTicksPriceAsk.Count - 1), (maxp - p) / onePixelPrice));
                    }
                    x = 0;
                    foreach (double p in listTicksPriceBid)
                    {
                        x++;
                        tickGraphBid.Points.Add(new Point((double)x + (ribboncanvas.ActualWidth - listTicksPriceBid.Count - 1), (maxp - p) / onePixelPrice));
                    }
                    lastPriceTick = _price;
                    if (_action == ActionGlassItem.buy)
                        lastPriceAsk = _price;
                    else if (_action == ActionGlassItem.sell)
                            lastPriceBid = _price;
                    lastActionTick = _action;
                }
            );
        }

        private void CalcGlassValue()
        {
            int snegative = 0, spositive = 0;
            int sumnegative = 0, sumpositive = 0;
            int percentDelta = 1;
            for (int j = 0; j < atemp.Length; j++)
            {
                if (atemp[j] > 0)
                {
                    sumpositive += atemp[j];
                    spositive++;
                }
                else
                {
                    sumnegative += atemp[j];
                    snegative++;
                }
                if (j == 17)
                {
                    percentDelta = spositive + snegative;
                    GlValues25 = (int)(100 * Math.Max(spositive, snegative) / percentDelta) * (spositive > snegative ? 1 : -1);
                    tbGlassValue25.Text += "\r\n" + (sumpositive + sumnegative).ToString();
                }
                else if (j == atemp.Length - 1) // если последняя итерация
                {
                    percentDelta = (spositive + Math.Abs(snegative));
                    GlValues = (int)(100 * Math.Max(spositive, snegative) / percentDelta) * (spositive > snegative ? 1 : -1);
                    tbGlassValue.Text += "\r\n" + (sumpositive + sumnegative).ToString();
                }
            }

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
                        // выводим на градиенте значения индикатора ival
                        if (i % 5 == 0)
                            ChangeTextValue(i, ival);

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
                    canvas.Children.RemoveRange(14, canvas.Children.Count - 14);
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
            Rectangle block = new Rectangle { SnapsToDevicePixels = true, Width = 110, Height = 12 };
            if (i > 0)
                block.Fill = UpBrush;
            else
                block.Fill = DownBrush;

            TextBlock t = new TextBlock { FontSize = 10 };
            TextBlock t1 = new TextBlock { FontSize = 10 };

            Canvas.SetLeft(block, canvas.ActualWidth - 108 - 120);
            Canvas.SetTop(block, centerCanvas - i * 12 + 1);
            Canvas.SetZIndex(block, 0);

            Canvas.SetLeft(t, canvas.ActualWidth - 40 - 120);
            Canvas.SetTop(t, centerCanvas - i * 12);
            Canvas.SetZIndex(t, 10);

            Canvas.SetLeft(t1, canvas.ActualWidth - 105 - 120);
            Canvas.SetTop(t1, centerCanvas - i * 12);
            Canvas.SetZIndex(t1, 10);

            canvas.Children.Add(block);
            canvas.Children.Add(t);
            canvas.Children.Add(t1);

            Line line100 = null;
            if ((_maxBid + i * 10) % 100 == 0)
            {
                line100 = new Line { X2 = canvas.ActualWidth - 111, Stroke = Brushes.Silver, StrokeThickness = 1 };
                t.FontWeight = System.Windows.FontWeights.Black;
                Canvas.SetTop(line100, centerCanvas - i * 12 + 8);
                Canvas.SetZIndex(t, 5);
                canvas.Children.Add(line100);
            }

            if (GlassValues.ContainsKey(_maxBid + i * StepGlass))
            {
                t.Text = (_maxBid + i * StepGlass).ToString("### ###");
                t1.Text = GlassValues[_maxBid + i * StepGlass].volume.ToString();
                lock (objLock)
                {
                    GlassValues[_maxBid + i * StepGlass].rectMain = block;
                    GlassValues[_maxBid + i * StepGlass].tbPrice = t;
                    GlassValues[_maxBid + i * StepGlass].tbVolume = t1;
                    if (line100 != null)
                        GlassValues[_maxBid + i * StepGlass].line100 = line100;
                }
            }
        }
        //
        // Создание и дальнешее изменение текстовых надписей на градиенте индикатора
        //
        private void ChangeTextValue(int i, int v)
        {
            if (!dicTBForIndicators.ContainsKey(i))
            {
                if (dicTBForIndicators.Count == 0)
                    canvas.Children.RemoveRange(4, canvas.Children.Count - 4);
                TextBlock tb = new TextBlock { Text = v.ToString(), FontSize = 11, FontWeight = FontWeights.Bold
                                                , Foreground = Brushes.White, Width = 22, HorizontalAlignment = HorizontalAlignment.Right, SnapsToDevicePixels = true};
                //tb.Effect = new DropShadowEffect { Color = Colors.Black, Direction = 310, ShadowDepth = 2 };
                dicTBForIndicators.Add(i, new IndicatorValuesTextBlock { value = v, textblock = tb });
                Canvas.SetTop(tb, canvas.ActualHeight - 20 - canvas.ActualHeight / 50 * i);
                Canvas.SetLeft(tb, canvas.ActualWidth - 30);
                Canvas.SetZIndex(tb, 100);
                canvas.Children.Add(tb);
            }
            else 
            {
                if (v < 0)
                    dicTBForIndicators[i].textblock.Text = v.ToString();
                else
                    dicTBForIndicators[i].textblock.Text = " " + v.ToString();
            }
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

        public int GlValues
        {
            get{ return glvalues; }
            set{ 
                glvalues = value;
                if (value < 0)
                    tbGlassValue.Foreground = Brushes.Red;
                else
                    tbGlassValue.Foreground = Brushes.Blue;
                if (tbGlassValue != null)
                    tbGlassValue.Text = glvalues.ToString() + "%";
            }
        }
        public int GlValues25
        {
            get { return glvalues25; }
            set { 
                glvalues25 = value;
                if (value < 0)
                    tbGlassValue25.Foreground = Brushes.Red;
                else
                    tbGlassValue25.Foreground = Brushes.Blue;
                if (tbGlassValue25 != null)
                    tbGlassValue25.Text = glvalues25.ToString() + "%";
            }
        }

        public SortedDictionary<double, GlassItem> GlassValues = new SortedDictionary<double, GlassItem>();
        public double StepGlass = 0;
        public Canvas canvas;
        public Canvas ribboncanvas;
        public Canvas tickGraphCanvas;
        public int centerCanvas;
        private double lastMinAsk;
        private double lastMaxBid;
        private double lastPriceTick, lastPriceAsk, lastPriceBid;
        private int glvalues, glvalues25;
        private ActionGlassItem lastActionTick;
        private int[] atemp = { };

        public SolidColorBrush UpBrush;
        public SolidColorBrush DownBrush;
        public SolidColorBrush ZeroBrush;
        public LinearGradientBrush GradientBrushForIndicator;
        public LinearGradientBrush GradientBrushForIndicator2;
        public LinearGradientBrush GradientBrushForIndicatorAverage;
        public LinearGradientBrush GradientBrushForIndicatorAverage2;
        public Polyline tickGraphAsk, tickGraphBid;

        private Dictionary<int, IndicatorValuesTextBlock> dicTBForIndicators = new Dictionary<int, IndicatorValuesTextBlock>();
        public TextBlock tbGlassValue;
        public TextBlock tbGlassValue25;

        public object objLock = new Object();
        public Queue<Rectangle> queueRect = new Queue<Rectangle>();
        public List<LinearGradientBrush> listGradient = new List<LinearGradientBrush>();
        public List<double> listTicksPriceAsk = new List<double>();
        public List<double> listTicksPriceBid = new List<double>();
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
        public Line line100;
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
    public class IndicatorValuesTextBlock
    {
        public double value;
        public TextBlock textblock;
    }
}
