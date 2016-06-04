using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MyMoney
{
    public class GlassGraph
    {
        public delegate void DoTradeLong();
        public delegate void DoTradeShort();
        public event DoTradeLong OnDoTradeLong;
        public event DoTradeShort OnDoTradeShort;
        public GlassGraph()
        {
            VisualAllElements = new VisualAllElemnts();
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

            indicatorGraphSumm = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(0, 167, 31) }, StrokeThickness = 1, SnapsToDevicePixels = true };
            indicatorRefilling = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(255, 0, 192) }, StrokeThickness = 1, SnapsToDevicePixels = true };
            SMA = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(166, 167, 31) }, StrokeThickness = 1, SnapsToDevicePixels = true };

            canvas.Children.Add(indicatorGraphSumm);
            canvas.Children.Add(indicatorRefilling);
            canvas.Children.Add(SMA);

            Panel.SetZIndex(indicatorGraphSumm, 3);
            Panel.SetZIndex(indicatorRefilling, 2);
            Panel.SetZIndex(SMA, 1);

            VisualAllElements = new VisualAllElemnts {CanvasGraph = canvas, _sma = SMA/*, _indicatorGraphSumm = indicatorGraphSumm, _indicatorRefilling =  indicatorRefilling*/ };
        }

        private void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (GlassValues.Count > 20)
                RebuildGlass(DateTime.Now);
        }
        public void ChangeValues(DateTime _dt, double _price, double _volume, int _row, ActionGlassItem _action)
        {
            var price = (int) _price;
            var volume = (int) _volume;
            if (_row == 0)
            {
                if (_action == ActionGlassItem.Sell) lastAsk = price;
                else if (_action == ActionGlassItem.Buy) lastBid = price;
            }

            if (GlassValues.ContainsKey(price) && (GlassValues[price].Volume != volume || GlassValues[price].Action != _action))
            {
                lock (ObjectForLock)
                {
                    GlassValues[price].ChangeValuesItems.Add(new ChangeValuesItem { DateTime = _dt, Price = price, OldValue = GlassValues[price].Volume, NewValue = volume });
                    GlassValues[price].Volume = volume;
                    GlassValues[price].Action = _action;
                }

                var sumGlass = 0;
                int la = lastAsk, lb = lastBid;
                // среднее значение по всему доступному стакану
                for (var i = 0; i < (GlassValues.Count < 50 ? GlassValues.Count : 50); i++)
                {
                    sumGlass += GlassValues.ContainsKey(la + i * StepGlass) ? GlassValues[la + i * StepGlass].Volume : 0;
                    sumGlass += GlassValues.ContainsKey(lb - i * StepGlass) ? GlassValues[lb - i * StepGlass].Volume : 0; 
                }
                summContractInGlass50 = sumGlass;
                var averageGlass = sumGlass / (50 * 2);
                var sumlong = 0;
                var sumshort = 0;

                int sumlongAverage = 0, sumshortAverage = 0;

                // новая версия, более взвешенное значение (как год назад)
                for (var i = 0; i < 50/*ParamForTest.glassHeight*/; i++)
                {
                    //if (GlassValues.ContainsKey((int)la + i * StepGlass))
                    //    sumlong += (int)GlassValues[(int)la + i * StepGlass].volume;
                    //if (GlassValues.ContainsKey((int)lb - i * StepGlass))
                    //    sumshort += (int)GlassValues[(int)lb - i * StepGlass].volume;
                    //int tempsum = (sumlong + sumshort) == 0 ? 1 : sumlong + sumshort;
                    //tempArray[i] = (int)(sumlong - sumshort) * 100 / (tempsum);

                    sumlongAverage += GlassValues.ContainsKey(la + i * StepGlass) && GlassValues[la + i * StepGlass].Volume < averageGlass * 3/*ParamForTest.averageValue*/ ? GlassValues[la + i * StepGlass].Volume : averageGlass * 3/*(int)ParamForTest.averageValue*/;
                    sumshortAverage += GlassValues.ContainsKey(lb - i * StepGlass) && GlassValues[lb - i * StepGlass].Volume < averageGlass * 3 /*ParamForTest.averageValue*/ ? GlassValues[lb - i * StepGlass].Volume : averageGlass * 3 /*(int)ParamForTest.averageValue*/;
                    var tempsumavr = (sumlongAverage + sumshortAverage) == 0 ? 1 : sumlongAverage + sumshortAverage;
                    atemp[i] = (sumlongAverage - sumshortAverage) * 100 / tempsumavr;
                }
                ChangeVisualIndicator();

                var minAsk = GetMinAsk();
                var maxBid = GetMaxBid();
                GetLastVisiblePrices();
                if (GlassValues[price].RectangleMain != null)
                    canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (ThreadStart)delegate
                        {
                            // смотрим, далеко ли "уполз" стакан
                            var deltaAsk = minAsk - lastMinAsk;
                            if (Math.Abs(deltaAsk) > _doAnimationValue * StepGlass)
                            {
                                AnimateGlassToCenter(_doAnimationValue * (int)StepGlass * Math.Sign(deltaAsk));
                            }

                            lock (ObjectForLock)
                            {
                                // подсчет скорости изменений объема по каждому значению стакана за интервал времени
                                foreach(var gv in GlassValues.Values)
                                {
                                    gv.CalcSpeedChange(canvas.ActualWidth, _dt);
                                }

                                var tw = _volume / 5;
                                GlassValues[price].RectangleVolume.Width = tw > 65 ? 65 : tw;
                                GlassValues[price].VolumeTextBlock.Text = _volume.ToString();

                                var issellaction = _action == ActionGlassItem.Sell;
                                GlassValues[price].RectangleMain.Fill = issellaction ? (_row == 0 ? UpBrushAsk : UpBrush) : (_row == 0 ? DownBrushBid : DownBrush);

                                // пространство спреда белым цветом
                                var signdiraction = issellaction ? -1 : 1;
                                if (_row == 0)
                                {
                                    for (var j = price + StepGlass * signdiraction; issellaction ? j >= minAsk : j <= maxBid; j = j + StepGlass * signdiraction)
                                    {
                                        if (!GlassValues.ContainsKey(j) || GlassValues[j].RectangleMain == null)
                                            continue;
                                        GlassValues[j].Action = ActionGlassItem.Zero;
                                        GlassValues[j].RectangleMain.Fill = ZeroBrush;
                                        GlassValues[j].VolumeTextBlock.Text = "";
                                        GlassValues[j].RectangleVolume.Width = GlassValues[j].ChangeValuesTextBlock.Width = GlassValues[j].RectangleChangeVolumeOut.Width = GlassValues[j].RectangleChangeVolumeIn.Width = 0;
                                        GlassValues[j].ChangeValuesItems.Clear();
                                    }
                                    try
                                    {
                                        if (GlassValues[minAsk].RectangleMain != null && GlassValues[maxBid].RectangleMain != null)
                                        {
                                            RecGradientUp.BeginAnimation(Canvas.TopProperty,
                                                new DoubleAnimation(Canvas.GetTop(RecGradientUp),
                                                    Canvas.GetTop(GlassValues[minAsk].RectangleMain) - 49*HeightOneItem,
                                                    TimeSpan.FromMilliseconds(50)));
                                            RecGradientDown.BeginAnimation(Canvas.TopProperty,
                                                new DoubleAnimation(Canvas.GetTop(RecGradientDown),
                                                    Canvas.GetTop(GlassValues[maxBid].RectangleMain),
                                                    TimeSpan.FromMilliseconds(50)));
                                        }
                                    }
                                    catch { }
                                }
                            }
                        });
                GetLastVisiblePrices();
            }
            else if (!GlassValues.ContainsKey(price))
            {
                lock (ObjectForLock)
                {
                    GlassValues.Add(price, new GlassItem(price, volume, _action));
                }
                RebuildGlass(_dt);
            }
        }
        public void AnimateGlassToCenter(int _deltamove)
        {
            lock (ObjectForLock)
            {
                foreach (var gi in GlassValues.Values)
                {
                    var danimation = new DoubleAnimation
                    {
                        By = _deltamove,
                        Duration = TimeSpan.FromMilliseconds(200)
                    };
                    danimation.Completed += delegate(object sender, EventArgs e) { VisualAllElements.AnimateElemntsToCenter(_deltamove); };
                    if (gi.AnimatedShapesList == null) continue;
                    foreach (var o in gi.AnimatedShapesList)
                    {
                        if (o == null) continue;

                        var element = o as FrameworkElement;
                        element?.BeginAnimation(Canvas.TopProperty, danimation);
                    }
                }
                lastMinAsk = GetMinAsk();
                lastMaxBid = GetMaxBid();
            }
        }
        public void AddTick(DateTime _dt, double _price, double _volume, ActionGlassItem _action)
        {
            var price = (int)_price;
            var volume = (int) _volume;
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Render,
                (ThreadStart)delegate()
                {
                    var r = new ResultOneTick();
                    r.ResultChangeSpeed.CalcSummChangeSpeedInGlass(GlassValues, lastAsk, lastBid, StepGlass);
                    var tmptick = new Tick(_dt, price, volume, _action);
                    if (VisualAllElements.AddTick(tmptick))
                        return;
                    VisualAllElements.AddData(atemp, r, tmptick);

                    GlValues25 = r.PresetHeight;
                    GlValuesRefilling = r.Refilling;
                    GlValues = r.MaxHeight;

                    // вход
                    if (GlValues25 > 20)
                        OnDoTradeLong?.Invoke();
                    else if (GlValues25 < -20)
                        OnDoTradeShort?.Invoke();

                    GlassValue25TextBlock.Text += "\r\n" + r.SumPresetHeight;
                    GlassValueTextBlock.Text += "\r\nOpIn:" + summContractInGlass50;

                    // делаем рендеринг раз в минуту (1000ms)
                    var ddd = DateTime.Now;
                    if (ddd.Subtract(lastShowDataCall).TotalMilliseconds > 300)
                    {
                        VisualAllElements.ShowData();
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
                    var s = 0;
                    var ival = 0;
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

                        var b = Convert.ToByte(Math.Abs(Math.Abs(3 * ival) > 255 ? 255 : 3 * ival) + 0);

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
            GetLastVisiblePrices();
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    canvas.Children.Clear();
                    canvas.Children.Add(VisualAllElements.MovedCanvas2D);
                    canvas.Children.Add(VisualAllElements.MovedCanvasGlass);
                    VisualAllElements.ClearMovedCanvas();
                    var centerPrice = lastMaxBid;
                    double st;
                    centerCanvas = (int)(canvas.ActualHeight / 2);

                    for (var i = 0; i <= (int)(GlassValues.Count / 2); i++)
                    {
                        st = centerPrice + i * StepGlass;
                        DrawItem(VisualAllElements.MovedCanvasGlass, _dt, i, lastMinAsk, lastMaxBid);
                        if (i != 0)
                            DrawItem(VisualAllElements.MovedCanvasGlass, _dt, -i, lastMinAsk, lastMaxBid);
                    }
                    RecGradientUp = new Rectangle { SnapsToDevicePixels = true, Width = 12, Height = HeightOneItem * 50 };
                    RecGradientDown = new Rectangle { SnapsToDevicePixels = true, Width = 12, Height = HeightOneItem * 50 };

                    Canvas.SetLeft(RecGradientUp, canvas.ActualWidth - 85);
                    Canvas.SetTop(RecGradientUp, centerCanvas - HeightOneItem * 50);
                    Canvas.SetZIndex(RecGradientUp, 2);
                    RecGradientUp.Fill = GradientBrushForIndicatorUp;

                    Canvas.SetLeft(RecGradientDown, canvas.ActualWidth - 85);
                    Canvas.SetTop(RecGradientDown, centerCanvas);
                    Canvas.SetZIndex(RecGradientDown, 2);
                    RecGradientDown.Fill = GradientBrushForIndicatorDown;

                    canvas.Children.Add(RecGradientUp);
                    canvas.Children.Add(RecGradientDown);

                    canvas.Children.Add(indicatorGraphSumm);
                    canvas.Children.Add(indicatorRefilling);
                    canvas.Children.Add(SMA);

                    foreach (var fe in VisualAllElements.ListFElemnts)
                        VisualAllElements.MovedCanvas2D.Children.Add(fe);

                    GraphAreaForGlass.ShowAllBars(canvas);
                    VisualAllElements.ShowData(true, true);
                });
        }
        private void GetLastVisiblePrices()
        {
            LastVisibleAsk = (int)(lastMinAsk + (canvas.ActualHeight - centerCanvas) / heightoneitem * _stepGlass);
            LastVisibleBid = (int)(lastMaxBid - (canvas.ActualHeight - centerCanvas) / heightoneitem * _stepGlass);
        }
        public void DrawItem(Canvas canvasForGlass, DateTime dateTime, int i, int minAsk, int maxBid)
        {
            var block = new Rectangle { SnapsToDevicePixels = true, Width = 100, Height = HeightOneItem };
            var block2 = new Rectangle { SnapsToDevicePixels = true, Height = HeightOneItem };
            var block3 = new Rectangle { SnapsToDevicePixels = true, Height = HeightOneItem };
            var block4 = new Rectangle { SnapsToDevicePixels = true, Height = HeightOneItem };

            if (i > 0)
                block.Fill = UpBrush;
            else if (i < 0)
                block.Fill = DownBrush;

            var t = new TextBlock { FontSize = 8 };
            var t1 = new TextBlock { FontSize = 8 };
            var t2 = new TextBlock { FontSize = 8 };

            Canvas.SetLeft(block, 0);
            Canvas.SetTop(block, centerCanvas - i * HeightOneItem);
            Panel.SetZIndex(block, 0);

            Canvas.SetLeft(block2, 0);
            Canvas.SetTop(block2, centerCanvas - i * HeightOneItem);
            Panel.SetZIndex(block2, 1);
            block2.Fill = VolumeBrush;

            Canvas.SetLeft(block3, 121);
            Canvas.SetTop(block3, centerCanvas - i * HeightOneItem);
            Panel.SetZIndex(block3, 1);
            block3.Fill = ChangeVolDownBrush;

            Canvas.SetLeft(block4, 121);
            Canvas.SetTop(block4, centerCanvas - i * HeightOneItem);
            Panel.SetZIndex(block4, 1);
            block4.Fill = ChangeVolUpBrush;

            Canvas.SetLeft(t, 68);
            Canvas.SetTop(t, centerCanvas - 1 - i * HeightOneItem);
            Panel.SetZIndex(t, 9);

            Canvas.SetLeft(t1, 3);
            Canvas.SetTop(t1, centerCanvas - 1 - i * HeightOneItem);
            Panel.SetZIndex(t1, 9);

            Canvas.SetLeft(t2, 118);
            Canvas.SetTop(t2, centerCanvas - 1 - i * HeightOneItem);
            Panel.SetZIndex(t2, 9);

            canvasForGlass.Children.Add(block);
            canvasForGlass.Children.Add(block2);
            canvasForGlass.Children.Add(block3);
            canvasForGlass.Children.Add(block4);
            canvasForGlass.Children.Add(t);
            canvasForGlass.Children.Add(t1);
            canvasForGlass.Children.Add(t2);

            Line line100 = null;
            if ((maxBid + i * StepGlass) % (10 * StepGlass) == 0)
            {
                line100 = new Line { X2 = 117, Stroke = Brushes.Silver, StrokeThickness = 1 };
                t.FontWeight = System.Windows.FontWeights.Black;
                Canvas.SetTop(line100, centerCanvas - i * HeightOneItem + _doAnimationValue);
                Panel.SetZIndex(t, 5);
                Panel.SetZIndex(line100, 0);
                canvasForGlass.Children.Add(line100);
            }

            if (!GlassValues.ContainsKey(maxBid + i * StepGlass))
            {
                GlassValues.Add(maxBid + i * StepGlass, new GlassItem(maxBid + i * StepGlass, 0, ActionGlassItem.Zero));
            }
            t.Text = (maxBid + i * StepGlass).ToString("### ###");
            double tw = GlassValues[maxBid + i * StepGlass].Volume / 5;
            block2.Width = tw > 65 ? 65 : tw;
            t1.Text = GlassValues[maxBid + i * StepGlass].Volume.ToString();
            //int summchangeval = 0;
            //lock (objLock)
            //{
                //foreach (ChangeValuesItem v in GlassValues[_maxBid + i * StepGlass].ChangeValuesItems)
                //    if ((dateTime - v.dateTime).TotalSeconds < 1)
                //        summchangeval += (int) v.NewValue - (int) v.OldValue > 0 ? 1 : -1;
                //t2.Text = Math.Abs(summchangeval) > 0 ? "" : ""; //summchangeval.ToString() : "";
                //block3.Width = Math.Abs(summchangeval) * 5;
                //block3.Fill = summchangeval < 0 ? ChangeVolDownBrush : ChangeVolUpBrush;
            //}
            var gi = GlassValues[maxBid + i * StepGlass];
            gi.RectangleMain = block;
            gi.PriceTextBlock = t;
            gi.VolumeTextBlock = t1;
            gi.ChangeValuesTextBlock = t2;
            gi.RectangleVolume = block2;
            gi.RectangleChangeVolumeOut = block3;
            gi.RectangleChangeVolumeIn = block4;
            if (line100 != null)
                gi.Line100 = line100;
            gi.AnimatedShapesList = new List<object>() { gi.RectangleMain, gi.RectangleVolume, gi.RectangleChangeVolumeOut, gi.RectangleChangeVolumeIn, gi.Line100, gi.VolumeTextBlock, gi.PriceTextBlock, gi.ChangeValuesTextBlock };

        }
        public void ChangeBidAsk(double ask, double bid)
        {

        }
        private int GetMinAsk()
        {
            var minA = 1000000;
            lock (ObjectForLock)
            {
                try
                {
                    foreach (var p in GlassValues.Keys.Where(p => GlassValues[p].Action == ActionGlassItem.Sell && p < minA))
                    {
                        minA = p;
                    }
                }
                catch
                {
   
                }
            }
            return minA;
        }
        private int GetMaxBid()
        {
            var maxB = -1000000;
            lock (ObjectForLock)
            {
                try
                {
                    foreach (var p in GlassValues.Keys.Where(p => GlassValues[p].Action == ActionGlassItem.Buy && p > maxB))
                    {
                        maxB = p;
                    }
                }
                catch
                {
                }
            }
            return maxB;
        }
        public int GlValues
        {
            get{ return glvalues; }
            set{ 
                glvalues = value;
                if (value < 0)
                    GlassValueTextBlock.Foreground = Brushes.Red;
                else
                    GlassValueTextBlock.Foreground = Brushes.Blue;
                if (GlassValueTextBlock != null)
                    GlassValueTextBlock.Text = glvalues.ToString() + "%";
            }
        }
        public int GlValues25
        {
            get { return glvalues25; }
            set { 
                glvalues25 = value;
                if (value < 0)
                    GlassValue25TextBlock.Foreground = Brushes.Red;
                else
                    GlassValue25TextBlock.Foreground = Brushes.Blue;
                if (GlassValue25TextBlock != null)
                    GlassValue25TextBlock.Text = glvalues25 + "%";
            }
        }
        public int GlValuesRefilling { get; set; }

        public SortedDictionary<int, GlassItem> GlassValues = new SortedDictionary<int, GlassItem>();
        private int _doAnimationValue = 15;
        private int lastMinAsk, lastMaxBid;
        private int lastvisibleask, lastvisiblebid;

        public int LastVisibleAsk
        {
            get { return lastvisibleask; }
            private set
            {
                lastvisibleask = value;
                VisualAllElements.LastVisibleAsk = value;
            }
        }

        public int LastVisibleBid
        {
            get { return lastvisiblebid; }
            private set
            {
                lastvisiblebid = value;
                VisualAllElements.LastVisibleBid = value;
            }
        }
        public int lastBid, lastAsk;

        private byte _stepGlass;

        public byte StepGlass
        {
            get { return _stepGlass; }
            set
            {
                _stepGlass = value;
                VisualAllElements.StepGlass = value;
            }
        }

        private double heightoneitem = 9;
        public double HeightOneItem { get { return heightoneitem; } private set { heightoneitem = 9; } }

        public int summContractInGlass50 = 0;
        public Canvas canvas;
        public int centerCanvas;
        private int glvalues, glvalues25;
        private int[] atemp = new int[50];
        private DateTime lastShowDataCall = DateTime.Now;

        public SolidColorBrush UpBrush, UpBrushAsk, DownBrush, DownBrushBid;
        public SolidColorBrush ZeroBrush, VolumeBrush;
        public SolidColorBrush ChangeVolUpBrush, ChangeVolDownBrush;
        public LinearGradientBrush GradientBrushForIndicatorUp, GradientBrushForIndicatorDown, GradientBrushForIndicatorAll;
        public Polyline indicatorGraphSumm, indicatorRefilling, SMA;

        public Rectangle RecGradientUp, RecGradientDown;

        private Dictionary<int, IndicatorValuesTextBlock> dicTBForIndicators = new Dictionary<int, IndicatorValuesTextBlock>();
        public TextBlock GlassValueTextBlock;
        public TextBlock GlassValue25TextBlock;

        public object ObjectForLock = new object();

        public SortedDictionary<int, ResTestLocal> resultsribbon = new SortedDictionary<int, ResTestLocal>();

        public VisualAllElemnts VisualAllElements;
        public AllTradesAtGraph AllTradesAtGraph = new AllTradesAtGraph();
    }

    public class GlassItem
    {
        public int Volume;
        public int Price;
        public GlassItem(int price, int volume, ActionGlassItem action)
        {
            Price = price;
            Volume = volume;
            Action = action;
        }
        public List<object> AnimatedShapesList;
        public List<ChangeValuesItem> ChangeValuesItems = new List<ChangeValuesItem>();
        public ActionGlassItem Action;
        public Line Line100;
        public Rectangle RectangleMain, RectangleVolume, RectangleChangeVolumeOut, RectangleChangeVolumeIn;
        public TextBlock VolumeTextBlock, PriceTextBlock, ChangeValuesTextBlock;

        internal ResultSpeedChangeGlass ResultSpeedChangeGlass = new ResultSpeedChangeGlass();

        internal void CalcSpeedChange(double canvasWidth, DateTime dateTime)
        {
            ResultSpeedChangeGlass.ChangeCountIn = 0;
            ResultSpeedChangeGlass.ChangeCountOut = 0;
            var tempChangeValuesItems = new List<ChangeValuesItem>();
            foreach (var v in ChangeValuesItems)
            {
                if ((dateTime - v.DateTime).TotalMilliseconds < 500) // за последние n секунд
                {
                    var deltaval = v.NewValue - v.OldValue;
                    if (deltaval < 0)
                        ResultSpeedChangeGlass.ChangeCountOut += 1;
                    else if (deltaval > 0)
                        ResultSpeedChangeGlass.ChangeCountIn += 1;
                    ResultSpeedChangeGlass.ChangeVolume += Math.Abs(deltaval);
                }
                else
                    tempChangeValuesItems.Add(v);
            }
            foreach (var o in tempChangeValuesItems.Where(o => ChangeValuesItems.Contains(o)))
            {
                ChangeValuesItems.Remove(o);
            }

            if (ChangeValuesTextBlock == null)
                return;

            RectangleChangeVolumeOut.Width = Math.Abs(ResultSpeedChangeGlass.ChangeCountOut) * 1;
            RectangleChangeVolumeIn.Width = Math.Abs(ResultSpeedChangeGlass.ChangeCountIn) * 1;
            Canvas.SetLeft(RectangleChangeVolumeIn, canvasWidth - RectangleChangeVolumeIn.Width - 17);
        }
    }
    public class ResTestLocal
    {
        public int ProfitCount;
        public int LossCount;

        public ResTestLocal(int key, double deltaPrice) 
        {
            if ((key < 0 && deltaPrice < 0) || (key > 0 && deltaPrice > 0))
                ProfitCount++;
            else 
                LossCount++;
        }

        public void AddValues(int key, double deltaPrice)
        {
            if ((key < 0 && deltaPrice < 0) || (key > 0 && deltaPrice > 0))
                ProfitCount++;
            else
                LossCount++;
        }
    }

    public class IndicatorValuesTextBlock
    {
        public double Value;
        public TextBlock Textblock;
    }

    public class VisualOneElement
    {
        internal int[] AtempValues = {};
        internal ResultOneTick ResultOneTick;
    }
    public class VisualAllElemnts
    {
        public void CalcSummIndicatorValue(int[] arrVal, ResultOneTick resultOneTick)
        {
            var sumnegative = 0;
            var sumpositive = 0;
            for (var j = LevelStartGlass; j < arrVal.Length; j++)
            {
                if (arrVal[j] > 0)
                    sumpositive += arrVal[j];
                else
                    sumnegative += arrVal[j];

                if (j == LevelHeightGlass && sumnegative + sumpositive != 0)
                {
                    resultOneTick.PresetHeight = 100*Math.Max(sumpositive, Math.Abs(sumnegative))/
                                                 (sumpositive + Math.Abs(sumnegative))*
                                                 (sumpositive > Math.Abs(sumnegative) ? 1 : -1);
                    resultOneTick.SumPresetHeight = sumpositive + sumnegative;
                }
                else if (j == arrVal.Length - 1 && sumnegative + sumpositive != 0) // если последняя итерация
                {
                    resultOneTick.MaxHeight = 100*Math.Max(sumpositive, Math.Abs(sumnegative))/
                                              (sumpositive + Math.Abs(sumnegative))*
                                              (sumpositive > Math.Abs(sumnegative) ? 1 : -1);
                    resultOneTick.SumMaxHeight = sumpositive + Math.Abs(sumnegative);
                }
            }
            resultOneTick.Refilling = resultOneTick.PresetHeight;
            if (Math.Abs(resultOneTick.SumPresetHeight) < LevelIgnoreValue)
                resultOneTick.PresetHeight = 0;
            if (Math.Abs(resultOneTick.SumPresetHeight) < LevelRefillingValue)
                resultOneTick.Refilling = 0;
        }

        public void AddData(int[] tempArray, ResultOneTick _r, Tick _tick)
        {
            // расчет индикатора по стакану
            CalcSummIndicatorValue( tempArray, _r );
            
            // расчет простой скользящей средней
            double sumaskatperiod = 0;
            for (var j = VisualElementsList.Count - 1; j >= VisualElementsList.Count - _periodsma; j--)
            {
                if (j < 0)
                    break;
                //sumaskatperiod += visualElementsList[j].resultOneTick.valAsk;
                sumaskatperiod += (VisualElementsList[j].ResultOneTick.Ask + VisualElementsList[j].ResultOneTick.Bid) / 2;
            }
            _r.Sma = sumaskatperiod / _periodsma;

            // добавление ask и bid
            switch (_tick.Action)
            {
                case ActionGlassItem.Sell:
                    lastPriceAsk = _tick.Price;
                    break;
                case ActionGlassItem.Buy:
                    lastPriceBid = _tick.Price;
                    break;
            }

            _r.Ask = lastPriceAsk;
            _r.Bid = lastPriceBid;

            var tmpvis = new VisualOneElement {ResultOneTick = _r};
            Array.Resize(ref tmpvis.AtempValues, tempArray.Length); // будем хранить значения индикатора для возможного дальнейшего пересчета
            tempArray.CopyTo(tmpvis.AtempValues, 0);
            VisualElementsList.Add(tmpvis);
            CountAddedWithNotShowData++;
        }
        public bool AddTick(Tick tick)
        {
            if (Math.Abs(lastPriceAsk) < 0.0000001) lastPriceAsk = tick.Price;
            if (Math.Abs(lastPriceBid) < 0.0000001) lastPriceBid = tick.Price;

            var result = Math.Abs(tick.Price - lastPriceTick) < 0.000001; // && tick.Action == lastActionTick;

            lastPriceTick = tick.Price;
            lastActionTick = tick.Action;

            return result;
            //return false;
        }
        public void ShowData(bool rebuild = false, bool resize = false)
        {
            if (VisualElementsList.Count < 1 || (VisualElementsList.Count - CountAddedWithNotShowData < 0))
                return;
            try
            {
                // пересчет значений индикатора
                if (rebuild)
                {
                    VisualElementsList.ForEach(visualElement =>
                    {
                        Task.Run(() =>
                        {
                            CalcSummIndicatorValue(visualElement.AtempValues, visualElement.ResultOneTick);
                        });
                    });
                }

                // удаляем все, что за пределами видимости
                if (VisualElementsList.Count > 5 * CanvasGraph.ActualWidth && CanvasGraph.ActualWidth > 0)
                    VisualElementsList.RemoveRange(0, (int)(VisualElementsList.Count - 5 * CanvasGraph.ActualWidth));

                var maxPrice = VisualElementsList.Count > 0 ? LastVisibleAsk : StepGlass;
                var minPrice = VisualElementsList.Count > 0 ? LastVisibleBid : StepGlass;
                var delta = maxPrice - minPrice;
                var pixelPrice = delta / CanvasGraph.ActualHeight;
                if (pixelPrice == 0)
                    pixelPrice = 10;
                double maxInd = Math.Max(Math.Abs(GetMaxMinVisibleValue("valpreseth").MaxValue), Math.Abs(GetMaxMinVisibleValue("valpreseth").MaxValue));
                if (maxInd == 0)
                    maxInd = Math.Max(maxInd, GetMaxMinVisibleValue("valrefilling").MaxValue);
                var deltaIndicator = 2 * maxInd;
                //var onePixelIndicator = deltaIndicator / CanvasGraph.ActualHeight;

                //_indicatorGraphSumm.Points.Clear();
                //_indicatorRefilling.Points.Clear();
                _sma.Points.Clear();
                var c = VisualElementsList.Count;
                GraphAreaForGlass.areasList.Clear();
                for (var i = (int)(c > CanvasGraph.ActualWidth ? c - CanvasGraph.ActualWidth : 0); i < c; i++)
                {
                    var resultOneTick = VisualElementsList[i].ResultOneTick;
                    var yAsk = (maxPrice - resultOneTick.Ask) / pixelPrice;
                    var yBid = (maxPrice - resultOneTick.Bid) / pixelPrice;
                    var xAll = (double)i + (CanvasGraph.ActualWidth - c - _widthGlassValues);
                    _sma.Points.Add(new Point(xAll, (maxPrice - resultOneTick.Sma) / pixelPrice));
                    //_indicatorGraphSumm.Points.Add(new Point(xAll, (maxInd - rt.valPresetHeight) / onePixelIndicator));
                    //_indicatorRefilling.Points.Add(new Point(xAll, (maxInd - rt.valRefilling) / onePixelIndicator));
                    GraphAreaForGlass.AddData((IndicatorCommand)Math.Sign(resultOneTick.PresetHeight), xAll, yAsk, yBid);
                }
                GraphAreaForGlass.ShowAllBars(CanvasGraph);
                if (resize)
                {
                    ListFElemnts.Clear();
                    TickGraphAsk.Points.Clear();
                    TickGraphBid.Points.Clear();
                }

                MovedCanvas2D.Margin = new Thickness(CanvasGraph.ActualWidth - _widthGlassValues - VisualElementsList.Count, MovedCanvas2D.Margin.Top, 0, 0);

                for (var x = resize ? 0 : VisualElementsList.Count - CountAddedWithNotShowData; x < VisualElementsList.Count; x++)
                {
                    var rt = VisualElementsList[x].ResultOneTick;
                    var yAsk = (maxPrice - rt.Ask) / pixelPrice;
                    var yBid = (maxPrice - rt.Bid) / pixelPrice;
                    TickGraphAsk.Points.Add(new Point(x, yAsk));
                    TickGraphBid.Points.Add(new Point(x, yBid));

                    ShowSpeedChange(MovedCanvas2D, rt.ResultChangeSpeed, /*CanvasGraph.ActualWidth - visualElementsList.Count - widthGlassValues */ x);
                }
                CountAddedWithNotShowData = 0;

            } catch (Exception e)
            {
                MessageBox.Show("метод ShowData:\r\n" + e.Message);
            }
        }
        private void ShowSpeedChange(Canvas canvas, ResultSummSpeedChangeGlass resultChangeSpeed, double _xAll)
        {
            double ycenter = 100;
            var l1 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter, Y2 = ycenter - resultChangeSpeed.summChangeUp.ChangeCountIn, Stroke = Brushes.Blue, StrokeThickness = 1, Opacity = 0.5 };
            var l2 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter + 1, Y2 = ycenter + resultChangeSpeed.summChangeDown.ChangeCountIn, Stroke = Brushes.Blue, StrokeThickness = 1, Opacity = 0.5 };
            var l3 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter, Y2 = ycenter - resultChangeSpeed.summChangeUp.ChangeCountOut, Stroke = Brushes.Red, StrokeThickness = 1, Opacity = 0.5 };
            var l4 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter + 1, Y2 = ycenter + resultChangeSpeed.summChangeDown.ChangeCountOut, Stroke = Brushes.Red, StrokeThickness = 1, Opacity = 0.5 };
            Panel.SetZIndex(l1, 15);
            Panel.SetZIndex(l2, 15);
            Panel.SetZIndex(l3, 15);
            Panel.SetZIndex(l4, 15);
            canvas.Children.Add(l1);
            canvas.Children.Add(l2);
            canvas.Children.Add(l3);
            canvas.Children.Add(l4);
            ListFElemnts.AddRange(new List<Line> {l1, l2, l3, l4});
        }
        private MinMaxValue GetMaxMinVisibleValue(string p = "")
        {
            
            var result = new MinMaxValue(1000000000, -1000000000);
            for (var i = VisualElementsList.Count - 1; i > (VisualElementsList.Count > CanvasGraph.ActualWidth ? VisualElementsList.Count - CanvasGraph.ActualWidth : 0); i--)
            {
                switch (p)
                {
                    case "valpreseth":
                        result.MaxValue = VisualElementsList[i].ResultOneTick.PresetHeight > result.MaxValue ? (float)VisualElementsList[i].ResultOneTick.PresetHeight : result.MaxValue;
                        result.MinValue = VisualElementsList[i].ResultOneTick.PresetHeight < result.MinValue ? (float)VisualElementsList[i].ResultOneTick.PresetHeight : result.MinValue;
                        break;
                    case "valrefilling":
                        result.MaxValue = VisualElementsList[i].ResultOneTick.Refilling > result.MaxValue ? (float)VisualElementsList[i].ResultOneTick.Refilling : result.MaxValue;
                        result.MinValue = VisualElementsList[i].ResultOneTick.Refilling < result.MinValue ? (float)VisualElementsList[i].ResultOneTick.Refilling : result.MinValue;
                        break;
                    default: result.MaxValue = 0; result.MinValue = 0; break;
                }
            }
            return result;
        }

        public double maxp, maxInd, onePixelPrice, onePixelIndicator;
        private int lastPriceTick, lastPriceAsk, lastPriceBid;
        private int lastvisibleask;
        public int LastVisibleAsk
        {
            get { return lastvisibleask; }
            set {
                if (lastvisibleask != value)
                    lastvisibleask = value; 
            }
        }
        public int LastVisibleBid { get; set; }

        private readonly double _widthGlassValues = 138;
        private readonly int _periodsma = 20;
        private ActionGlassItem lastActionTick;

        public byte StepGlass;
        public int CountAddedWithNotShowData;
        
        internal List<Line> ListHorizontalLine = new List<Line>();
        internal List<VisualOneElement> VisualElementsList = new List<VisualOneElement>();
        internal List<FrameworkElement> ListFElemnts = new List<FrameworkElement>();

        public int levelignoreval, levelheightglass, levelstartglass, levelrefilling;
        public int LevelHeightGlass
        {
            get { return levelheightglass; }
            set { levelheightglass = value; ShowData(true); }
        }
        public int LevelStartGlass
        {
            get { return levelstartglass; }
            set { levelstartglass = value; ShowData(true); }
        }
        public int LevelIgnoreValue
        {
            get { return levelignoreval; }
            set { levelignoreval = value; ShowData(true); }
        }
        public int LevelRefillingValue
        {
            get { return levelrefilling; }
            set { levelrefilling = value; ShowData(true); }
        }

        private Canvas _canvasgraph;
        public Canvas CanvasGraph
        {
            get { return _canvasgraph; }
            set { 
                _canvasgraph = value;
                MovedCanvas2D = new Canvas() { /*Background = Brushes.Green,*/ Opacity = 1, Margin = new Thickness(0, 0, _widthGlassValues, 0) };
                MovedCanvasGlass = new Canvas() { /*Background = Brushes.Red,*/ Opacity = 1, Width = _widthGlassValues};
                _canvasgraph.Children.Add(MovedCanvas2D);
                _canvasgraph.Children.Add(MovedCanvasGlass);
                _canvasgraph.SizeChanged += canvasgraph_SizeChanged;

                TickGraphAsk = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(0, 0, 110) }, StrokeThickness = 1, SnapsToDevicePixels = true };
                TickGraphBid = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(110, 0, 0) }, StrokeThickness = 1, SnapsToDevicePixels = true };
                TickGraphAsk.MouseDown += Polyline_MouseDown;
                TickGraphBid.MouseDown += Polyline_MouseDown;
                Panel.SetZIndex(TickGraphAsk, 30);
                Panel.SetZIndex(TickGraphBid, 30);
                MovedCanvas2D.Children.Add(TickGraphAsk);
                MovedCanvas2D.Children.Add(TickGraphBid); 
            }
        }

        public void canvasgraph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MovedCanvas2D.Height = ((Canvas) sender).ActualHeight;
            MovedCanvas2D.Width = _canvasgraph.ActualWidth - _widthGlassValues;
            MovedCanvasGlass.Height = ((Canvas) sender).ActualHeight;
            MovedCanvasGlass.Margin = new Thickness(((Canvas) sender).ActualWidth - _widthGlassValues, 0, 0, 0);
        }
        public void Polyline_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Polyline).StrokeThickness == 1)
                (sender as Polyline).StrokeThickness = 2;
            else
                (sender as Polyline).StrokeThickness = 1;
        }

        public Canvas MovedCanvas2D { get; set; }
        public Canvas MovedCanvasGlass { get; set; }

        public Polyline TickGraphAsk { get; set; }
        public Polyline TickGraphBid { get; set; }

        //public Polyline _indicatorGraphSumm { get; set; }
        //public Polyline _indicatorRefilling { get; set; }
        public Polyline _sma { get; set; }

        internal void ClearMovedCanvas()
        {
            MovedCanvas2D.Children.Clear();
            MovedCanvas2D.Children.Add(TickGraphBid);
            MovedCanvas2D.Children.Add(TickGraphAsk);
            MovedCanvasGlass.Children.Clear();
        }

        internal void AnimateElemntsToCenter(int _deltamove)
        {
            //double top = 0;
            //DoubleAnimation danimation = new DoubleAnimation();
            //danimation.By = _deltamove;
            //danimation.Duration = TimeSpan.FromMilliseconds(200);
            //top = Canvas.GetTop(movedCanvas);
            //movedCanvas.BeginAnimation(Canvas.TopProperty, danimation);
            //GetMaxMinVisibleValue();
            //ShowData(false, true);
            //for (int i = 0; i < tickGraphAsk.Points.Count; i++)
            //{
            //    Point p = tickGraphAsk.Points[i];
            //    tickGraphAsk.Points[i] = new Point(p.X, p.Y + _deltamove);
            //    p = tickGraphBid.Points[i];
            //    tickGraphBid.Points[i] = new Point(p.X, p.Y + _deltamove);
            //}
        }
    }
    public class AllTradesAtGraph
    {
        public Dictionary<int, ClaimInfo> dicAllClaims = new Dictionary<int, ClaimInfo>();
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
            return dicAllClaims.Values.Any(ci => ci.priceExit == 0);
        }
    }
    public class ResultOneTick
    {
        internal ResultSummSpeedChangeGlass ResultChangeSpeed = new ResultSummSpeedChangeGlass();
        public double Ask { get; set; }
        public double Bid { get; set; }
        public int PresetHeight{ get; set; }
        public int MaxHeight{ get; set; }
        public int SumPresetHeight{ get; set; }
        public int SumMaxHeight{ get; set; }
        public int Refilling{ get; set; }
        public double Sma { get; set; }
    }

    internal class ResultSpeedChangeGlass
    {
        internal double ChangeCountIn;
        internal double ChangeCountOut;
        internal double ChangeVolume;
        internal void Clear()
        {
            ChangeCountIn = 0;
            ChangeCountOut = 0;
            ChangeVolume = 0;
        }

        internal bool IsZeroValue()
        {
            return ChangeCountIn == 0 && ChangeCountOut == 0;
        }
    }

    internal class ResultSummSpeedChangeGlass
    {
        internal ResultSpeedChangeGlass summChangeUp = new ResultSpeedChangeGlass();
        internal ResultSpeedChangeGlass summChangeDown = new ResultSpeedChangeGlass();

        internal void CalcSummChangeSpeedInGlass(SortedDictionary<int, GlassItem> glassValues, int ask, int bid, int stepGlass)
        {
            summChangeUp.Clear();
            summChangeDown.Clear();

            for (var i = -49; i < 51; i++)
            {
                var key = i < 1 ? bid + i * stepGlass : ask + (i - 1) * stepGlass;
                if (!glassValues.ContainsKey(key))
                    continue;
                var rchange = glassValues[key].ResultSpeedChangeGlass;
                //if (_rchange.IsZeroValue())
                //    continue;
                if (key >= ask)
                {
                    summChangeUp.ChangeCountIn += rchange.ChangeCountIn;
                    summChangeUp.ChangeCountOut += rchange.ChangeCountOut;
                }
                else if (key <= bid)
                {
                    summChangeDown.ChangeCountIn += rchange.ChangeCountIn;
                    summChangeDown.ChangeCountOut += rchange.ChangeCountOut;
                }
            }
        }
    }

    public enum ActionGlassItem
    {
        Zero = 0,
        Sell = 1,
        Buy = 2
    }
    public struct ChangeValuesItem
    {
        public DateTime DateTime;
        public int Price;
        public int OldValue;
        public int NewValue;
    }
}
