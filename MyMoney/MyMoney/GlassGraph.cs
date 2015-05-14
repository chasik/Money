using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    public struct ChangeValuesItem
    {
        public DateTime dt;
        public double price;
        public double oldvalue;
        public double newvalue;
    }
    public class GlassGraph
    {
        public delegate void DoTradeLong();
        public delegate void DoTradeShort();
        public event DoTradeLong OnDoTradeLong;
        public event DoTradeShort OnDoTradeShort;
        public GlassGraph()
        {
            visualAllElements = new VisualAllElemnts();
        }
        public GlassGraph(Canvas _c)
        {
            canvas = _c;
            canvas.SizeChanged += canvas_SizeChanged;

            UpBrush = new SolidColorBrush { Color = Color.FromArgb(255, 255, 228, 225) };
            DownBrush = new SolidColorBrush { Color = Color.FromArgb(255, 152, 251, 152) };

            UpBrushAsk = new SolidColorBrush { Color = Color.FromArgb(255, 255, 88, 69) };
            DownBrushBid = new SolidColorBrush { Color = Color.FromArgb(255, 110, 184, 129) };

            ZeroBrush = new SolidColorBrush { Color = Color.FromArgb(255, 252, 252, 252) };
            VolumeBrush = new SolidColorBrush { Color = Color.FromArgb(255, 255, 127, 39) };

            ChangeVolUpBrush = new SolidColorBrush { Color = Color.FromArgb(255, 36, 187, 250) };
            ChangeVolDownBrush = new SolidColorBrush { Color = Color.FromArgb(255, 250, 36, 36) };

            GradientBrushForIndicatorUp = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            GradientBrushForIndicatorDown = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            GradientBrushForIndicatorAll = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };

            tickGraphAsk = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(0, 0, 110) }, StrokeThickness = 1, SnapsToDevicePixels = true };
            tickGraphBid = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(110, 0, 0) }, StrokeThickness = 1, SnapsToDevicePixels = true };
            indicatorGraphSumm = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(0, 167, 31) }, StrokeThickness = 1, SnapsToDevicePixels = true };
            indicatorRefilling = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(255, 0, 192) }, StrokeThickness = 1, SnapsToDevicePixels = true };
            SMA = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(166, 167, 31) }, StrokeThickness = 1, SnapsToDevicePixels = true };
            tickGraphAsk.MouseDown += Polyline_MouseDown;
            tickGraphBid.MouseDown += Polyline_MouseDown;
            indicatorGraphSumm.MouseDown += Polyline_MouseDown;
            indicatorRefilling.MouseDown += Polyline_MouseDown;
            SMA.MouseDown += Polyline_MouseDown;

            canvas.Children.Add(tickGraphAsk);
            canvas.Children.Add(tickGraphBid);
            canvas.Children.Add(indicatorGraphSumm);
            canvas.Children.Add(indicatorRefilling);
            canvas.Children.Add(SMA);
            Canvas.SetZIndex(tickGraphAsk, 30);
            Canvas.SetZIndex(tickGraphBid, 30);
            Canvas.SetZIndex(indicatorGraphSumm, 3);
            Canvas.SetZIndex(indicatorRefilling, 2);
            Canvas.SetZIndex(SMA, 1);

            visualAllElements = new VisualAllElemnts() {CanvasGraph = canvas, _sma = SMA, _tickGraphAsk = tickGraphAsk, _tickGraphBid = tickGraphBid
                                                            , _indicatorGraphSumm = indicatorGraphSumm, _indicatorRefilling =  indicatorRefilling };
        }

        void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (GlassValues.Count > 20)
                RebuildGlass(DateTime.Now);
        }
        public void Polyline_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Polyline).StrokeThickness == 1)
                (sender as Polyline).StrokeThickness = 2;
            else
                (sender as Polyline).StrokeThickness = 1;
        }
        public void ChangeValues(DateTime _dt, double _price, double _volume, int _row, ActionGlassItem _action)
        {
            if (_row == 0)
            {
                if (_action == ActionGlassItem.sell) lastAsk = _price;
                else if (_action == ActionGlassItem.buy) lastBid = _price;
            }

            if (GlassValues.ContainsKey(_price) && (GlassValues[_price].volume != _volume || GlassValues[_price].action != _action))
            {
                lock (objLock)
                {
                    GlassValues[_price].listChangeVal.Add(new ChangeValuesItem() { dt = _dt, price = _price, oldvalue = GlassValues[_price].volume, newvalue = _volume });
                }
                GlassValues[_price].volume = _volume;
                GlassValues[_price].action = _action;

                int sumGlass = 0;
                double la = lastAsk, lb = lastBid;
                // среднее значение по всему доступному стакану
                for (int i = 0; i < (GlassValues.Count < 50 ? GlassValues.Count : 50); i++)
                {
                    sumGlass += GlassValues.ContainsKey(la + i * StepGlass) ? (int)GlassValues[la + i * StepGlass].volume : 0;
                    sumGlass += GlassValues.ContainsKey(lb - i * StepGlass) ? (int)GlassValues[lb - i * StepGlass].volume : 0; 
                }
                this.summContractInGlass50 = sumGlass;
                int averageGlass = (int)sumGlass / (50 * 2);
                int sumlong = 0, sumshort = 0;

                int sumlongAverage = 0, sumshortAverage = 0;

                // новая версия, более взвешенное значение (как год назад)
                for (int i = 0; i < 50/*paramTh.glassHeight*/; i++)
                {
                    //if (GlassValues.ContainsKey((int)la + i * StepGlass))
                    //    sumlong += (int)GlassValues[(int)la + i * StepGlass].volume;
                    //if (GlassValues.ContainsKey((int)lb - i * StepGlass))
                    //    sumshort += (int)GlassValues[(int)lb - i * StepGlass].volume;
                    //int tempsum = (sumlong + sumshort) == 0 ? 1 : sumlong + sumshort;
                    //atemp[i] = (int)(sumlong - sumshort) * 100 / (tempsum);

                    sumlongAverage += GlassValues.ContainsKey((int)la + i * StepGlass)
                        && GlassValues[(int)la + i * StepGlass].volume < averageGlass * 3/*paramTh.averageValue*/
                        ? (int)GlassValues[(int)la + i * StepGlass].volume : averageGlass * 3/*(int)paramTh.averageValue*/;
                    sumshortAverage += GlassValues.ContainsKey((int)lb - i * StepGlass)
                        && GlassValues[(int)lb - i * StepGlass].volume < averageGlass * 3 /*paramTh.averageValue*/
                        ? (int)GlassValues[(int)lb - i * StepGlass].volume : averageGlass * 3 /*(int)paramTh.averageValue*/;
                    int tempsumavr = (sumlongAverage + sumshortAverage) == 0 ? 1 : sumlongAverage + sumshortAverage;
                    atemp[i] = (int)(sumlongAverage - sumshortAverage) * 100 / (tempsumavr);
                }
                ChangeVisualIndicator();

                if (GlassValues[_price].rectMain != null)
                    canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (ThreadStart)delegate()
                        {
                            double minAsk = GetMinAsk();
                            double maxBid = GetMaxBid();
                            // смотрим, далеко ли "уполз" стакан
                            double deltaAsk = minAsk - lastMinAsk;
                            if (Math.Abs(deltaAsk) > doAnimationValue * StepGlass)
                            {
                                AnimateGlassToCenter(doAnimationValue * (int)StepGlass * Math.Sign(deltaAsk));
                            }

                            lock (objLock)
                            {
                                // подсчет скорости изменений объема по каждому значению стакана за интервал времени
                                foreach(GlassItem gv in GlassValues.Values)
                                {
                                    gv.CalcSpeedChange(canvas.ActualWidth, _dt);
                                }
                            }
                            double tw = _volume / 5;
                            GlassValues[_price].rectVolume.Width = tw > 65 ? 65 : tw;
                            GlassValues[_price].tbVolume.Text = _volume.ToString();

                            bool issellaction = _action == ActionGlassItem.sell;
                            GlassValues[_price].rectMain.Fill = issellaction ? (_row == 0 ? UpBrushAsk : UpBrush) : (_row == 0 ? DownBrushBid : DownBrush);

                            // пространство спреда белым цветом
                            int signdiraction = issellaction ? -1 : 1;
                            if (_row == 0)
                            {
                                for (double j = _price + StepGlass * signdiraction; issellaction ? j >= minAsk : j <= maxBid; j = j + StepGlass * signdiraction)
                                {
                                    if (!GlassValues.ContainsKey(j) || GlassValues[j].rectMain == null)
                                        continue;
                                    GlassValues[j].action = ActionGlassItem.zero;
                                    GlassValues[j].rectMain.Fill = ZeroBrush;
                                    GlassValues[j].tbVolume.Text = "";
                                    GlassValues[j].rectVolume.Width = GlassValues[j].tbChangeVal.Width = GlassValues[j].rectChangeVolumeOut.Width = GlassValues[j].rectChangeVolumeIn.Width = 0;
                                    GlassValues[j].listChangeVal.Clear();
                                }
                                if (GlassValues[minAsk].rectMain != null && GlassValues[maxBid].rectMain != null)
                                {
                                    recGradientUp.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(Canvas.GetTop(recGradientUp), Canvas.GetTop(GlassValues[minAsk].rectMain) - 49 * HeightOneItem, TimeSpan.FromMilliseconds(50)));
                                    recGradientDown.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(Canvas.GetTop(recGradientDown), Canvas.GetTop(GlassValues[maxBid].rectMain), TimeSpan.FromMilliseconds(50)));
                                }
                            }
                        });
                GetLastVisiblePrices();
            }
            else if (!GlassValues.ContainsKey(_price))
            {
                lock (objLock)
                {
                    GlassValues.Add(_price, new GlassItem(_price, _volume, _action));
                }
                RebuildGlass(_dt);
            }
        }

        public void AnimateGlassToCenter(int _deltamove)
        {
            lock (objLock)
            {
                foreach (GlassItem gi in GlassValues.Values)
                {
                    if (gi.AnimatedShapesList == null) continue;
                    foreach (object o in gi.AnimatedShapesList)
                    {
                        if (o == null) continue;
                        double top = 0;
                        DoubleAnimation danimation = new DoubleAnimation();
                        danimation.By = _deltamove;
                        danimation.Duration = TimeSpan.FromMilliseconds(200);
                        if (o is FrameworkElement)
                        {
                            top = Canvas.GetTop((o as FrameworkElement));
                            (o as FrameworkElement).BeginAnimation(Canvas.TopProperty, danimation);
                        }
                    }
                }
                lastMinAsk = GetMinAsk();
                lastMaxBid = GetMaxBid();
                GetLastVisiblePrices();
            }
        }
        public void AddTick(DateTime _dt, double _price, double _volume, ActionGlassItem _action)
        {
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Render,
                (ThreadStart)delegate()
                {
                    Tick tmptick = new Tick((DateTime?)_dt, (float?) _price, (float?)_volume, _action);
                    if (visualAllElements.AddTick(tmptick))
                        return;

                    ResultOneTick r = new ResultOneTick();
                    r.resultChangeSpeed.CalcSummChangeSpeedInGlass(GlassValues, lastAsk, lastBid);
                    visualAllElements.AddData(atemp, r, tmptick);
                    
                    GlValues25 = r.valPresetHeight;
                    GlValuesRefilling = r.valRefilling;
                    GlValues = r.valMaxHeight;

                    // вход
                    if (GlValues25 > 20 && OnDoTradeLong != null)
                        OnDoTradeLong();
                    else if (GlValues25 < -20 && OnDoTradeShort != null)
                        OnDoTradeShort();

                    tbGlassValue25.Text += "\r\n" + r.sumPresetHeight.ToString();
                    tbGlassValue.Text += "\r\nOpIn:" + summContractInGlass50.ToString();

                    //visualAllElements.listGradient.Add(GradientBrushForIndicatorAll.Clone());
                    // делаем рендеринг раз в минуту (1000ms)
                    DateTime ddd = DateTime.Now;
                    if (ddd.Subtract(lastShowDataCall).TotalMilliseconds > 200)
                    {
                        visualAllElements.ShowData();
                        lastShowDataCall = ddd;
                    }
                }
            );
        }

        public void ChangeVisualIndicator()
        {
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    GradientBrushForIndicatorUp.GradientStops.Clear();
                    GradientBrushForIndicatorDown.GradientStops.Clear();
                    GradientBrushForIndicatorAll.GradientStops.Clear();
                    int s = 0;
                    int ival = 0;
                    //int ivalAvr = 0, ivalAvr2 = 0;
                    for (int i = 0; i < atemp.Length; i++)
                    {
                        s += atemp[i];
                        ival = atemp[i];
                        //sa += _arrindAverage[i];
                        //ival = s / (i + 1);
                        //ival2 = _arrind[i];
                        //ival = _arrind[i];
                        //ival2 = s / (i + 1);
                        //ival2 = sa / (i + 1);

                        //ivalAvr = _arrindAverage[i];
                        //ivalAvr2 = sa / (i + 1);

                        byte b = Convert.ToByte(Math.Abs(Math.Abs(3 * ival) > 255 ? 255 : 3 * ival) + 0);

                        GradientBrushForIndicatorUp.GradientStops.Add(new GradientStop(ival > 0 ? Color.FromRgb(0, b, 255) : UpBrush.Color, 1 - (double)i / 50));
                        GradientBrushForIndicatorDown.GradientStops.Add(new GradientStop(ival > 0 ? DownBrush.Color : Color.FromRgb(255, b, 0), (double)i / 50));
                        GradientBrushForIndicatorAll.GradientStops.Add(new GradientStop(ival > 0 ? Color.FromRgb(0, b, 255) : Color.FromRgb(255, b, 0), (double)i / 50));
                    }
                });
        }
        public void RebuildGlass(DateTime _dt)
        {
            lastMinAsk = GetMinAsk();
            lastMaxBid = GetMaxBid();
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    canvas.Children.Clear();
                    double centerPrice = lastMaxBid;
                    double st;
                    centerCanvas = (int)(canvas.ActualHeight / 2);
                    GetLastVisiblePrices();
                    for (int i = 0; i <= (int)(GlassValues.Count / 2); i++)
                    {
                        st = centerPrice + i * StepGlass;
                        DrawItem(_dt, i, lastMinAsk, lastMaxBid);
                        if (i != 0)
                            DrawItem(_dt, -i, lastMinAsk, lastMaxBid);
                    }
                    recGradientUp = new Rectangle { SnapsToDevicePixels = true, Width = 12, Height = HeightOneItem * 50 };
                    recGradientDown = new Rectangle { SnapsToDevicePixels = true, Width = 12, Height = HeightOneItem * 50 };

                    Canvas.SetLeft(recGradientUp, canvas.ActualWidth - 85);
                    Canvas.SetTop(recGradientUp, centerCanvas - HeightOneItem * 50);
                    Canvas.SetZIndex(recGradientUp, 2);
                    recGradientUp.Fill = GradientBrushForIndicatorUp;

                    Canvas.SetLeft(recGradientDown, canvas.ActualWidth - 85);
                    Canvas.SetTop(recGradientDown, centerCanvas);
                    Canvas.SetZIndex(recGradientDown, 2);
                    recGradientDown.Fill = GradientBrushForIndicatorDown;

                    canvas.Children.Add(recGradientUp);
                    canvas.Children.Add(recGradientDown);

                            canvas.Children.Add(tickGraphAsk);
                            canvas.Children.Add(tickGraphBid);
                            canvas.Children.Add(indicatorGraphSumm);
                            canvas.Children.Add(indicatorRefilling);
                            canvas.Children.Add(SMA);
                });
        }

        private void GetLastVisiblePrices()
        {
            lastVisibleAsk = Math.Round(lastMinAsk + (canvas.ActualHeight - centerCanvas) / heightoneitem * stepglass);
            lastVisibleBid = Math.Round(lastMaxBid - (canvas.ActualHeight - centerCanvas) / heightoneitem * stepglass);
        }
        public void DrawItem(DateTime _dt, int i, double _minAsk, double _maxBid)
        {
            Rectangle block = new Rectangle { SnapsToDevicePixels = true, Width = 100, Height = HeightOneItem };
            Rectangle block2 = new Rectangle { SnapsToDevicePixels = true, Height = HeightOneItem };
            Rectangle block3 = new Rectangle { SnapsToDevicePixels = true, Height = HeightOneItem };
            Rectangle block4 = new Rectangle { SnapsToDevicePixels = true, Height = HeightOneItem };

            if (i > 0)
                block.Fill = UpBrush;
            else if (i < 0)
                block.Fill = DownBrush;

            TextBlock t = new TextBlock { FontSize = 8 };
            TextBlock t1 = new TextBlock { FontSize = 8 };
            TextBlock t2 = new TextBlock { FontSize = 8 };

            Canvas.SetLeft(block, canvas.ActualWidth - 138);
            Canvas.SetTop(block, centerCanvas - i * HeightOneItem);
            Canvas.SetZIndex(block, 0);

            Canvas.SetLeft(block2, canvas.ActualWidth - 138);
            Canvas.SetTop(block2, centerCanvas - i * HeightOneItem);
            Canvas.SetZIndex(block2, 1);
            block2.Fill = VolumeBrush;

            Canvas.SetLeft(block3, canvas.ActualWidth - 17);
            Canvas.SetTop(block3, centerCanvas - i * HeightOneItem);
            Canvas.SetZIndex(block3, 1);
            block3.Fill = ChangeVolDownBrush;

            Canvas.SetLeft(block4, canvas.ActualWidth - 17);
            Canvas.SetTop(block4, centerCanvas - i * HeightOneItem);
            Canvas.SetZIndex(block4, 1);
            block4.Fill = ChangeVolUpBrush;

            Canvas.SetLeft(t, canvas.ActualWidth - 70);
            Canvas.SetTop(t, centerCanvas - 1 - i * HeightOneItem);
            Canvas.SetZIndex(t, 9);

            Canvas.SetLeft(t1, canvas.ActualWidth - 135);
            Canvas.SetTop(t1, centerCanvas - 1 - i * HeightOneItem);
            Canvas.SetZIndex(t1, 9);

            Canvas.SetLeft(t2, canvas.ActualWidth - 20);
            Canvas.SetTop(t2, centerCanvas - 1 - i * HeightOneItem);
            Canvas.SetZIndex(t2, 9);

            canvas.Children.Add(block);
            canvas.Children.Add(block2);
            canvas.Children.Add(block3);
            canvas.Children.Add(block4);
            canvas.Children.Add(t);
            canvas.Children.Add(t1);
            canvas.Children.Add(t2);

            Line line100 = null;
            if ((_maxBid + i * StepGlass) % (10 * StepGlass) == 0)
            {
                line100 = new Line { X2 = canvas.ActualWidth - 21, Stroke = Brushes.Silver, StrokeThickness = 1 };
                t.FontWeight = System.Windows.FontWeights.Black;
                Canvas.SetTop(line100, centerCanvas - i * HeightOneItem + doAnimationValue);
                Canvas.SetZIndex(t, 5);
                Canvas.SetZIndex(line100, 0);
                canvas.Children.Add(line100);
            }

            if (!GlassValues.ContainsKey(_maxBid + i * StepGlass))
            {
                GlassValues.Add(_maxBid + i * StepGlass, new GlassItem(_maxBid + i * StepGlass, 0, ActionGlassItem.zero));
            }
            t.Text = (_maxBid + i * StepGlass).ToString("### ###");
            double tw = GlassValues[_maxBid + i * StepGlass].volume / 5;
            block2.Width = tw > 65 ? 65 : tw;
            t1.Text = GlassValues[_maxBid + i * StepGlass].volume.ToString();
            //int summchangeval = 0;
            //lock (objLock)
            //{
                //foreach (ChangeValuesItem v in GlassValues[_maxBid + i * StepGlass].listChangeVal)
                //    if ((_dt - v.dt).TotalSeconds < 1)
                //        summchangeval += (int) v.newvalue - (int) v.oldvalue > 0 ? 1 : -1;
                //t2.Text = Math.Abs(summchangeval) > 0 ? "" : ""; //summchangeval.ToString() : "";
                //block3.Width = Math.Abs(summchangeval) * 5;
                //block3.Fill = summchangeval < 0 ? ChangeVolDownBrush : ChangeVolUpBrush;
            //}
            lock (objLock)
            {
                GlassItem gi = GlassValues[_maxBid + i * StepGlass];
                gi.rectMain = block;
                gi.tbPrice = t;
                gi.tbVolume = t1;
                gi.tbChangeVal = t2;
                gi.rectVolume = block2;
                gi.rectChangeVolumeOut = block3;
                gi.rectChangeVolumeIn = block4;
                if (line100 != null)
                    gi.line100 = line100;
                gi.AnimatedShapesList = new List<object>() { gi.rectMain, gi.rectVolume, gi.rectChangeVolumeOut, gi.rectChangeVolumeIn, gi.line100, gi.tbVolume, gi.tbPrice, gi.tbChangeVal };
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
                    if (GlassValues[p].action == ActionGlassItem.sell && p < _minA)
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
                    if (GlassValues[p].action == ActionGlassItem.buy && p > _maxB)
                        _maxB = p;
                }
            }
            return _maxB;
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
        public int GlValuesRefilling
        {
            get { return glvaluesrefilling;  }
            set
            {
                glvaluesrefilling = value;
            }
        }

        public SortedDictionary<double, GlassItem> GlassValues = new SortedDictionary<double, GlassItem>();
        private int doAnimationValue = 15;
        private double lastMinAsk, lastMaxBid;
        private double lastvisibleask, lastvisiblebid;
        public double lastVisibleAsk { get { return lastvisibleask; } private set { lastvisibleask = value; visualAllElements.lastvisibleask = value; } }
        public double lastVisibleBid { get { return lastvisiblebid; } private set { lastvisiblebid = value; visualAllElements.lastvisiblebid = value; } }
        public double lastBid = 0, lastAsk = 0;

        private double stepglass = 0;
        public double StepGlass { get { return stepglass; } set { stepglass = value;  visualAllElements.StepGlass = value; } }

        private double heightoneitem = 9;
        public double HeightOneItem { get { return heightoneitem; } private set { heightoneitem = 9; } }

        public int summContractInGlass50 = 0;
        public Canvas canvas;
        public int centerCanvas;
        private int glvalues, glvalues25, glvaluesrefilling;
        private int[] atemp = new int[50];
        private DateTime lastShowDataCall = DateTime.Now;

        public SolidColorBrush UpBrush, UpBrushAsk, DownBrush, DownBrushBid;
        public SolidColorBrush ZeroBrush, VolumeBrush;
        public SolidColorBrush ChangeVolUpBrush, ChangeVolDownBrush;
        public LinearGradientBrush GradientBrushForIndicatorUp, GradientBrushForIndicatorDown, GradientBrushForIndicatorAll;
        public Polyline tickGraphAsk, tickGraphBid;
        public Polyline indicatorGraphSumm, indicatorRefilling, SMA;

        public Rectangle recGradientUp, recGradientDown;

        private Dictionary<int, IndicatorValuesTextBlock> dicTBForIndicators = new Dictionary<int, IndicatorValuesTextBlock>();
        public TextBlock tbGlassValue;
        public TextBlock tbGlassValue25;

        public object objLock = new Object();
        public List<int[]> listArrayValues = new List<int[]>();

        public SortedDictionary<int, ResTestLocal> resultsribbon = new SortedDictionary<int, ResTestLocal>();

        public VisualAllElemnts visualAllElements;
        public AllTradesAtGraph allTradesAtGraph = new AllTradesAtGraph();
    }

    public class GlassItem
    {
        public GlassItem(double _price, double _volume, ActionGlassItem _action)
        {
            price = _price;
            volume = _volume;
            action = _action;
        }
        public List<object> AnimatedShapesList;
        public List<ChangeValuesItem> listChangeVal = new List<ChangeValuesItem>();
        public double volume;
        public double price;
        public ActionGlassItem action;
        public Line line100;
        public Rectangle rectMain, rectVolume, rectChangeVolumeOut, rectChangeVolumeIn;
        public TextBlock tbVolume, tbPrice, tbChangeVal;

        internal ResultSpeedChangeGlass speedChangeGlass = new ResultSpeedChangeGlass();

        internal void CalcSpeedChange(double _canvasWidth, DateTime _dt)
        {
            speedChangeGlass.changeCountIn = 0;
            speedChangeGlass.changeCountOut = 0;
            List<ChangeValuesItem> templist = new List<ChangeValuesItem>();
            foreach (ChangeValuesItem v in listChangeVal)
            {
                if ((_dt - v.dt).TotalSeconds < 5) // за последние n секунд
                {
                    double deltaval = v.newvalue - v.oldvalue;
                    if (deltaval < 0)
                        speedChangeGlass.changeCountOut += 1;
                    else if (deltaval > 0)
                        speedChangeGlass.changeCountIn += 1;
                    speedChangeGlass.changeVolume += Math.Abs(deltaval);
                }
                else
                    templist.Add(v);
            }
            foreach (ChangeValuesItem o in templist)
            {
                if (listChangeVal.Contains(o))
                    listChangeVal.Remove(o);
            }
            templist.Clear();
            if (tbChangeVal != null)
            {
                rectChangeVolumeOut.Width = Math.Abs(speedChangeGlass.changeCountOut) * 1;
                rectChangeVolumeIn.Width = Math.Abs(speedChangeGlass.changeCountIn) * 1;
                Canvas.SetLeft(rectChangeVolumeIn, _canvasWidth - rectChangeVolumeIn.Width - 17);
            }
        }
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
    public class VisualOneElement
    {
        internal int[] atempValues = {};
        internal ResultOneTick resultOneTick;
    }
    public class VisualAllElemnts
    {
        public VisualAllElemnts()
        {
        }
        public void CalcSummIndicatorValue(int[] _arrval, ResultOneTick _resonetick)
        {
            int sumnegative = 0, sumpositive = 0, cnegative = 0, cpositive = 0;
            for (int j = LevelStartGlass; j < _arrval.Length; j++)
            {
                if (_arrval[j] > 0)
                {
                    sumpositive += _arrval[j];
                    cpositive++;
                }
                else
                {
                    sumnegative += _arrval[j];
                    cnegative++;
                }
                if (j == LevelHeightGlass && sumnegative + sumpositive != 0)
                {
                    _resonetick.valPresetHeight = (int)(100 * Math.Max(sumpositive, Math.Abs(sumnegative)) / (sumpositive + Math.Abs(sumnegative))) * (sumpositive > Math.Abs(sumnegative) ? 1 : -1);
                    _resonetick.sumPresetHeight = sumpositive + sumnegative;
                }
                else if (j == _arrval.Length - 1 && sumnegative + sumpositive != 0) // если последняя итерация
                {
                    _resonetick.valMaxHeight = (int)(100 * Math.Max(sumpositive, Math.Abs(sumnegative)) / (sumpositive + Math.Abs(sumnegative))) * (sumpositive > Math.Abs(sumnegative) ? 1 : -1);
                    _resonetick.sumMaxHeight = sumpositive + Math.Abs(sumnegative);
                }
            }
            _resonetick.valRefilling = _resonetick.valPresetHeight;
            if (Math.Abs(_resonetick.sumPresetHeight) < LevelIgnoreValue)
                _resonetick.valPresetHeight = 0;
            if (Math.Abs(_resonetick.sumPresetHeight) < LevelRefillingValue)
                _resonetick.valRefilling = 0;
        }
        public void AddData(int[] _atemp, ResultOneTick _r, Tick _tick)
        {
            // расчет индикатора по стакану
            CalcSummIndicatorValue( _atemp, _r );
            
            // расчет простой скользящей средней
            double sumaskatperiod = 0;
            for (int j = visualElementsList.Count - 1; j >= visualElementsList.Count - periodsma; j--)
            {
                if (j < 0)
                    break;
                //sumaskatperiod += visualElementsList[j].resultOneTick.valAsk;
                sumaskatperiod += (visualElementsList[j].resultOneTick.valAsk + visualElementsList[j].resultOneTick.valBid) / 2;
            }
            _r.valSMA = sumaskatperiod / periodsma;

            // добавление ask и bid
            if (_tick.Action == ActionGlassItem.sell)
                lastPriceAsk = _tick.Price;
            else if (_tick.Action == ActionGlassItem.buy)
                lastPriceBid = _tick.Price;

            _r.valAsk = lastPriceAsk;
            _r.valBid = lastPriceBid;

            VisualOneElement tmpvis = new VisualOneElement();
            tmpvis.resultOneTick = _r;
            Array.Resize(ref tmpvis.atempValues, _atemp.Length); // будем хранить значения индикатора для возможного дальнейшего пересчета
            _atemp.CopyTo(tmpvis.atempValues, 0);
            visualElementsList.Add(tmpvis);
            countAddedWithNotShowData++;
        }
        public bool AddTick(Tick _tick)
        {
            if (lastPriceAsk == 0) lastPriceAsk = _tick.Price;
            if (lastPriceBid == 0) lastPriceBid = _tick.Price;

            //bool result = false;
            bool result = _tick.Price == lastPriceTick;// && _tick.Action == lastActionTick;
            lastPriceTick = _tick.Price;
            lastActionTick = _tick.Action;

            return result;
        }
        public void ShowData(bool _rebuild = false)
        {
            try
            {
                // смещение градиента -------------------------------------------------------------------------------
                //List<Shape> tempshapes = new List<Shape>();
                //foreach (FrameworkElement s in CanvasGraph.Children)
                //{
                //    if (s is Rectangle && s.Width == 1)
                //    {
                //        if (Canvas.GetLeft(s) - 1 < 0 || _rebuild)
                //            tempshapes.Add(s as Shape);
                //        else
                //            Canvas.SetLeft(s, Canvas.GetLeft(s) - 1);
                //    }
                //}
                //foreach (Shape s in tempshapes)
                //{
                //    CanvasGraph.Children.Remove(s);
                //}
                //tempshapes.Clear();
                // --------------------------------------------------------------------------------------------------


                //int x = 0;
                //if (listGradient.Count > 0)
                //    for (x = _rebuild ? 0 : listGradient.Count - countAddedWithNotShowData; x < listGradient.Count; x++) // LinearGradientBrush brushg in  listGradient)
                //    {
                //        Rectangle r = new Rectangle() { Fill = listGradient[x], Width = 1, Height = CanvasGraph.ActualHeight, Opacity = 1 };
                //        Canvas.SetLeft(r, CanvasGraph.ActualWidth - listGradient.Count + x);
                //        Canvas.SetTop(r, 0);
                //        CanvasGraph.Children.Add(r);
                //    }

                // пересчет значений индикатора
                if (_rebuild)
                {
                    foreach (VisualOneElement ve in visualElementsList)
                        CalcSummIndicatorValue(ve.atempValues, ve.resultOneTick);
                }
                // удаляем все, что за пределами видимости
                if (visualElementsList.Count > 2 * CanvasGraph.ActualWidth)
                    visualElementsList.RemoveRange(0, (int)(visualElementsList.Count - 2 * CanvasGraph.ActualWidth));

                double maxp = visualElementsList.Count > 0 ? lastvisibleask : StepGlass;
                double minp = visualElementsList.Count > 0 ? lastvisiblebid : StepGlass;
                double delta = maxp - minp;
                double onePixelPrice = delta / CanvasGraph.ActualHeight;
                if (onePixelPrice == 0)
                    onePixelPrice = 10;
                double maxInd = Math.Max(Math.Abs(GetMaxMinVisibleValue("valpreseth").MaxValue), Math.Abs(GetMaxMinVisibleValue("valpreseth").MaxValue));
                if (maxInd == 0)
                    maxInd = Math.Max(maxInd, GetMaxMinVisibleValue("valrefilling").MaxValue);
                double deltaIndicator = 2 * maxInd;
                double onePixelIndicator = deltaIndicator / CanvasGraph.ActualHeight;

                _tickGraphAsk.Points.Clear();
                _tickGraphBid.Points.Clear();
                _indicatorGraphSumm.Points.Clear();
                _indicatorRefilling.Points.Clear();
                _sma.Points.Clear();
                int c = visualElementsList.Count;
                GraphAreaForGlass.areasList.Clear();
                foreach (FrameworkElement fe in listFElemnts)
                {
                    CanvasGraph.Children.Remove(fe);
                }
                for (int i = (int)(c > CanvasGraph.ActualWidth ? c - CanvasGraph.ActualWidth : 0); i < c; i++)
                {
                    ResultOneTick rt = visualElementsList[i].resultOneTick;
                    double yAsk = (maxp - rt.valAsk) / onePixelPrice;
                    double yBid = (maxp - rt.valBid) / onePixelPrice;
                    double xAll = (double)i + (CanvasGraph.ActualWidth - c - widthGlassValues);
                    _sma.Points.Add(new Point(xAll, (maxp - rt.valSMA) / onePixelPrice));
                    _tickGraphAsk.Points.Add(new Point(xAll, yAsk));
                    _tickGraphBid.Points.Add(new Point(xAll, yBid));
                    _indicatorGraphSumm.Points.Add(new Point(xAll, (maxInd - rt.valPresetHeight) / onePixelIndicator));
                    _indicatorRefilling.Points.Add(new Point(xAll, (maxInd - rt.valRefilling) / onePixelIndicator));
                    GraphAreaForGlass.AddData((IndicatorCommand) Math.Sign(rt.valPresetHeight), xAll, yAsk, yBid);
                    ShowSpeedChange(CanvasGraph, rt, xAll);
                }
                GraphAreaForGlass.ShowAllBars(CanvasGraph);

                // разметка цены
                //if (_rebuild || maxp != lastmaxpricegraph || minp != lastminpricegraph)
                //{
                //    foreach (Line l in listHorizontalLine)
                //    {
                //        CanvasGraph.Children.Remove(l);
                //    }
                //    listHorizontalLine.Clear();
                //    for (double yy = minp; yy < maxp; yy += StepGlass)
                //    {
                //        if (yy % (10 * StepGlass) == 0 || yy % (5 * StepGlass) == 0)
                //        {
                //            Line linehorizontal = null;
                //            if (yy % (10 * StepGlass) == 0)
                //                linehorizontal = new Line { X2 = CanvasGraph.ActualWidth, Stroke = Brushes.DarkGray, StrokeThickness = 1 };
                //            else
                //                linehorizontal = new Line { X2 = CanvasGraph.ActualWidth, Stroke = Brushes.DarkGray, StrokeThickness = 1, StrokeDashArray = { 3, 5 } };
                //            Canvas.SetTop(linehorizontal, (maxp - yy) / onePixelPrice);
                //            Canvas.SetZIndex(linehorizontal, 1);
                //            CanvasGraph.Children.Add(linehorizontal);
                //            listHorizontalLine.Add(linehorizontal);
                //        }
                //    }
                //    lastmaxpricegraph = maxp; lastminpricegraph = minp;
                //}
                countAddedWithNotShowData = 0;
            } catch (Exception e )
            {
                MessageBox.Show(e.Message + " " + e.Source);
            }
        }

        private void ShowSpeedChange(Canvas CanvasGraph, ResultOneTick _rt, double _xAll)
        {
            double ycenter = 120;
            Line l1 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter, Y2 = ycenter - _rt.resultChangeSpeed.summChangeUp.changeCountIn, Stroke = Brushes.Blue, StrokeThickness = 1, Opacity = 0.2 };
            Line l2 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter + 1, Y2 = ycenter + _rt.resultChangeSpeed.summChangeDown.changeCountIn, Stroke = Brushes.Blue, StrokeThickness = 1, Opacity = 0.2 };
            Line l3 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter, Y2 = ycenter - _rt.resultChangeSpeed.summChangeUp.changeCountOut, Stroke = Brushes.Red, StrokeThickness = 1, Opacity = 0.2 };
            Line l4 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter + 1, Y2 = ycenter + _rt.resultChangeSpeed.summChangeDown.changeCountOut, Stroke = Brushes.Red, StrokeThickness = 1, Opacity = 0.2 };
            //Canvas.SetTop(l, centerCanvas - i * HeightOneItem + doAnimationValue);
            Canvas.SetZIndex(l1, 5);
            Canvas.SetZIndex(l2, 5);
            Canvas.SetZIndex(l3, 5);
            Canvas.SetZIndex(l4, 5);
            CanvasGraph.Children.Add(l1);
            CanvasGraph.Children.Add(l2);
            CanvasGraph.Children.Add(l3);
            CanvasGraph.Children.Add(l4);
            listFElemnts.AddRange( new List<Line> { l1, l2, l3, l4 } );
        }

        private MinMaxValue GetMaxMinVisibleValue(string p)
        {
            
            MinMaxValue result = new MinMaxValue(1000000000, -1000000000);
            for (int i = visualElementsList.Count - 1; i > (visualElementsList.Count > CanvasGraph.ActualWidth ? visualElementsList.Count - CanvasGraph.ActualWidth : 0); i--)
            {
                switch (p)
                {
                    case "valpreseth":
                        result.MaxValue = visualElementsList[i].resultOneTick.valPresetHeight > result.MaxValue ? (float)visualElementsList[i].resultOneTick.valPresetHeight : result.MaxValue;
                        result.MinValue = visualElementsList[i].resultOneTick.valPresetHeight < result.MinValue ? (float)visualElementsList[i].resultOneTick.valPresetHeight : result.MinValue;
                        break;
                    case "valrefilling":
                        result.MaxValue = visualElementsList[i].resultOneTick.valRefilling > result.MaxValue ? (float)visualElementsList[i].resultOneTick.valRefilling : result.MaxValue;
                        result.MinValue = visualElementsList[i].resultOneTick.valRefilling < result.MinValue ? (float)visualElementsList[i].resultOneTick.valRefilling : result.MinValue;
                        break;
                    default: result.MaxValue = 0; result.MinValue = 0; break;
                }
            }
            return result;
        }

        public double maxp, maxInd, onePixelPrice, onePixelIndicator;
        private double lastPriceTick, lastPriceAsk, lastPriceBid;
        public double lastvisibleask, lastvisiblebid;
        private double widthGlassValues = 138;
        private ActionGlassItem lastActionTick;
        private int periodsma = 80;
        public double StepGlass = 0;
        public int countAddedWithNotShowData = 0;
        
        public List<Line> listHorizontalLine = new List<Line>();
        
        internal List<VisualOneElement> visualElementsList = new List<VisualOneElement>();
        internal List<FrameworkElement> listFElemnts = new List<FrameworkElement>();

        public int levelignoreval, levelheightglass, levelstartglass, levelrefilling;
        private Canvas canvasgraph = null;
        public int LevelHeightGlass
        {
            get { return levelheightglass; }
            set { levelheightglass = value; countAddedWithNotShowData++; ShowData(true); }
        }
        public int LevelStartGlass
        {
            get { return levelstartglass; }
            set { levelstartglass = value; countAddedWithNotShowData++; ShowData(true); }
        }
        public int LevelIgnoreValue
        {
            get { return levelignoreval; }
            set { levelignoreval = value; countAddedWithNotShowData++; ShowData(true); }
        }
        public int LevelRefillingValue
        {
            get { return levelrefilling; }
            set { levelrefilling = value; countAddedWithNotShowData++; ShowData(true); }
        }
        public Canvas CanvasGraph
        {
            get { return canvasgraph; }
            set { canvasgraph = value; }
        }
        public Polyline _tickGraphAsk { get; set; }
        public Polyline _tickGraphBid { get; set; }
        public Polyline _indicatorGraphSumm { get; set; }
        public Polyline _indicatorRefilling { get; set; }
        public Polyline _sma { get; set; }
    }
    public class AllTradesAtGraph
    {
        public void SignalIn(double _lastminask, double _lastmaxbid, double _indvalue)
        {
            //int signtrade = Math.Sign(_indvalue);
            //if (!this.ExistActiveTrade())
            //{
            //    dicAllClaims.Add(dicAllClaims.Count, new ClaimInfo(DateTime.Now, signtrade < 0 ? _lastminask : _lastmaxbid, 1, signtrade < 0 ? SmartCOM3Lib.StOrder_Action.StOrder_Action_Sell : SmartCOM3Lib.StOrder_Action.StOrder_Action_Buy));
            //}
        }
        public void SignalOut()
        {

        }
        public bool ExistActiveTrade()
        {
            bool result = false;
            foreach (ClaimInfo ci in dicAllClaims.Values)
            {
                if (ci.priceExit == 0)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
        public Dictionary<int, ClaimInfo> dicAllClaims = new Dictionary<int, ClaimInfo>();
    }
    public class ResultOneTick
    {
        public ResultOneTick() { }

        internal ResultSummSpeedChangeGlass resultChangeSpeed = new ResultSummSpeedChangeGlass();
        public double valAsk { get; set; }
        public double valBid { get; set; }
        public int valPresetHeight{ get; set; }
        public int valMaxHeight{ get; set; }
        public int sumPresetHeight{ get; set; }
        public int sumMaxHeight{ get; set; }
        public int valRefilling{ get; set; }
        public double valSMA { get; set; }
    }

    internal struct ResultSpeedChangeGlass
    {
        internal double changeCountIn, changeCountOut;
        internal double changeVolume;
        internal void Clear()
        {
            changeCountIn = 0;
            changeCountOut = 0;
            changeVolume = 0;
        }

        internal bool IsZeroValue()
        {
            if (changeCountIn == 0 && changeCountOut == 0)
                return true;
            else
                return false;
        }
    }

    internal struct ResultSummSpeedChangeGlass
    {
        internal ResultSpeedChangeGlass summChangeUp;
        internal ResultSpeedChangeGlass summChangeDown;

        internal void CalcSummChangeSpeedInGlass(SortedDictionary<double, GlassItem> _glassvalues, double _ask, double _bid)
        {
            summChangeUp.Clear();
            summChangeDown.Clear();
            try
            {
                foreach (double _key in _glassvalues.Keys)
                {
                    ResultSpeedChangeGlass _rchange = _glassvalues[_key].speedChangeGlass;
                    if (_rchange.IsZeroValue())
                        continue;
                    if (_key >= _ask)
                    {
                        summChangeUp.changeCountIn += _rchange.changeCountIn;
                        summChangeUp.changeCountOut += _rchange.changeCountOut;
                    }
                    else if (_key <= _bid)
                    {
                        summChangeDown.changeCountIn += _rchange.changeCountIn;
                        summChangeDown.changeCountOut += _rchange.changeCountOut;
                    }
                }
            } 
            catch(Exception e)
            {
                MessageBox.Show("ffff" + e.Message);
            }
        }
    }
}
