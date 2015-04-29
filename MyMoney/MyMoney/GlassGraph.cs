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
        public int abssummchangeval;
        public GlassGraph()
        {
            visualAllElements = new VisualAllElemnts();
        }
        public GlassGraph(Canvas _c, Canvas _g, Canvas _ribbon, Rectangle _indicatorRect, Rectangle _indicatorRect2, Rectangle _indicatorAverageRect, Rectangle _indicatorAverageRect2)
        {
            canvas = _c;
            ribboncanvas = _ribbon;
            tickGraphCanvas = _ribbon;

            UpBrush = new SolidColorBrush { Color = Color.FromArgb(255, 255, 228, 225) };
            DownBrush = new SolidColorBrush { Color = Color.FromArgb(255, 152, 251, 152) };

            UpBrushAsk = new SolidColorBrush { Color = Color.FromArgb(255, 255, 185, 177) };
            DownBrushBid = new SolidColorBrush { Color = Color.FromArgb(255, 125, 209, 125) };

            ZeroBrush = new SolidColorBrush { Color = Color.FromArgb(255, 252, 252, 252) };
            VolumeBrush = new SolidColorBrush { Color = Color.FromArgb(255, 255, 127, 39) };

            ChangeVolUpBrush = new SolidColorBrush { Color = Color.FromArgb(255, 36, 187, 250) };
            ChangeVolDownBrush = new SolidColorBrush { Color = Color.FromArgb(255, 250, 36, 36) };

            GradientBrushForIndicator = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            GradientBrushForIndicator2 = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };

            //GradientBrushForIndicatorAverage = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            //GradientBrushForIndicatorAverage2 = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };

            _indicatorRect.Fill = GradientBrushForIndicator;
            _indicatorRect2.Fill = GradientBrushForIndicator2;

            //_indicatorAverageRect.Fill = GradientBrushForIndicatorAverage;
            //_indicatorAverageRect2.Fill = GradientBrushForIndicatorAverage2;

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

            tickGraphCanvas.Children.Add(tickGraphAsk);
            tickGraphCanvas.Children.Add(tickGraphBid);
            tickGraphCanvas.Children.Add(indicatorGraphSumm);
            tickGraphCanvas.Children.Add(indicatorRefilling);
            tickGraphCanvas.Children.Add(SMA);
            Canvas.SetZIndex(tickGraphAsk, 3);
            Canvas.SetZIndex(tickGraphBid, 2);
            Canvas.SetZIndex(indicatorGraphSumm, 6);
            Canvas.SetZIndex(indicatorRefilling, 5);
            Canvas.SetZIndex(SMA, 4);

            visualAllElements = new VisualAllElemnts() { _sma = SMA, _tickGraphAsk = tickGraphAsk, _tickGraphBid = tickGraphBid
                                                            , _indicatorGraphSumm = indicatorGraphSumm, _indicatorRefilling =  indicatorRefilling, CanvasGraph = tickGraphCanvas };
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
            if (GlassValues.ContainsKey(_price))
            {
                if (GlassValues[_price].volume != _volume || GlassValues[_price].action != _action)
                {
                    //ribboncanvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    //    (ThreadStart)delegate()
                    //    {
                    //        if (_row == 0) 
                    //        {}
                    //    });
                    lock (objLock)
                    {
                        GlassValues[_price].listChangeVal.Add(new ChangeValuesItem() { dt = _dt, price = _price, oldvalue = GlassValues[_price].volume, newvalue = _volume });
                    }
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
                                if (Math.Abs(deltaAsk) > 12 * StepGlass)
                                {
                                    lock (objLock)
                                    {
                                        foreach (GlassItem gi in GlassValues.Values)
                                        {
                                            if (gi.rectMain != null)
                                            {
                                                double top = Canvas.GetTop(gi.rectMain);
                                                gi.rectMain.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 120 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(1000)));
                                            }
                                            if (gi.tbVolume != null)
                                            {
                                                double top = Canvas.GetTop(gi.tbVolume);
                                                gi.tbVolume.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 120 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(1000)));
                                            }
                                            if (gi.tbPrice != null)
                                            {
                                                double top = Canvas.GetTop(gi.tbPrice);
                                                gi.tbPrice.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 120 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(1000)));
                                            }
                                            if (gi.tbChangeVal != null)
                                            {
                                                double top = Canvas.GetTop(gi.tbChangeVal);
                                                gi.tbChangeVal.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 120 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(1000)));
                                            }
                                            if (gi.rectVolume != null)
                                            {
                                                double top = Canvas.GetTop(gi.rectVolume);
                                                gi.rectVolume.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 120 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(1000)));
                                            }
                                            if (gi.rectChangeVolume != null)
                                            {
                                                double top = Canvas.GetTop(gi.rectChangeVolume);
                                                gi.rectChangeVolume.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 120 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(1000)));
                                            }
                                            if (gi.line100 != null)
                                            {
                                                double top = Canvas.GetTop(gi.line100);
                                                gi.line100.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 120 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(1000)));
                                            }
                                        }
                                    }
                                    lastMinAsk = minAsk;
                                    lastMaxBid = maxBid;
                                }

                                lock (objLock)
                                {
                                    //abssummchangeval = 0;
                                    foreach(GlassItem gv in GlassValues.Values){
                                        //if (gv.price == _price && _row == 0) // если это уровень bid и ask - то эти изменения не учитываем
                                        //    continue;
                                        int summchangeval = 0;
                                        List<ChangeValuesItem> templist = new List<ChangeValuesItem>();
                                        foreach (ChangeValuesItem v in gv.listChangeVal)
                                        {
                                            if ((_dt - v.dt).TotalSeconds < 1)
                                            {
                                                summchangeval += (int)v.newvalue - (int)v.oldvalue > 0 ? 1 : -1;
                                                //abssummchangeval++;
                                            }
                                            else
                                                templist.Add(v);
                                        }
                                        foreach (ChangeValuesItem o in templist)
                                        {
                                            if (gv.listChangeVal.Contains(o))
                                                gv.listChangeVal.Remove(o);
                                        }
                                        templist.Clear();
                                        if (gv.tbChangeVal != null)
                                        {
                                            //gv.tbChangeVal.Text = Math.Abs(summchangeval) > 0 ? "" : ""; //summchangeval.ToString() : "";
                                            gv.rectChangeVolume.Width = Math.Abs(summchangeval) * 5;
                                            gv.rectChangeVolume.Fill = summchangeval < 0 ? ChangeVolDownBrush : ChangeVolUpBrush;
                                        }
                                    }
                                }
                                double tw = _volume / 5;
                                GlassValues[_price].rectVolume.Width = tw > 65 ? 65 : tw;
                                GlassValues[_price].tbVolume.Text = _volume.ToString();
                                if (_action == ActionGlassItem.buy)
                                {
                                    if (_row == 0)
                                    {
                                        GlassValues[_price].rectMain.Fill = UpBrushAsk;
                                        if (GlassValues.ContainsKey(_price + StepGlass) && GlassValues[_price + StepGlass].rectMain != null)
                                            GlassValues[_price + StepGlass].rectMain.Fill = UpBrush;
                                        for (double j = _price - StepGlass; j >= minAsk; j = j - StepGlass)
                                        {
                                            if (!GlassValues.ContainsKey(j) || GlassValues[j].rectMain == null)
                                                continue;
                                            GlassValues[j].action = ActionGlassItem.zero;
                                            GlassValues[j].rectMain.Fill = ZeroBrush;
                                            GlassValues[j].tbVolume.Text = "";
                                        }
                                    }
                                    else
                                        GlassValues[_price].rectMain.Fill = UpBrush;
                                }
                                else if (_action == ActionGlassItem.sell)
                                {

                                    if (_row == 0)
                                    {
                                        GlassValues[_price].rectMain.Fill = DownBrushBid;
                                        if (GlassValues.ContainsKey(_price - StepGlass) && GlassValues[_price - StepGlass].rectMain != null)
                                            GlassValues[_price - StepGlass].rectMain.Fill = DownBrush;
                                        for (double j = _price + StepGlass; j <= maxBid; j = j + StepGlass)
                                        {
                                            if (!GlassValues.ContainsKey(j) || GlassValues[j].rectMain == null)
                                                continue;
                                            GlassValues[j].action = ActionGlassItem.zero;
                                            GlassValues[j].rectMain.Fill = ZeroBrush;
                                            GlassValues[j].tbVolume.Text = "";
                                        }
                                    }
                                    else
                                        GlassValues[_price].rectMain.Fill = DownBrush;
                                }
                                //GlassValues[_price].tbVolume.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(5, 50, TimeSpan.FromMilliseconds(10000)));
                                });
                }
            }
            else
            {
                lock (objLock)
                {
                    GlassValues.Add(_price, new GlassItem(_price, _volume, _action));
                }
                RebuildGlass(_dt);
            }
        }
        public void AddTick(DateTime _dt, double _price, double _volume, ActionGlassItem _action)
        {
            ribboncanvas.Dispatcher.BeginInvoke(DispatcherPriority.Render,
                (ThreadStart)delegate()
                {
                    // вход
                    if (GlValues25 > 50 && OnDoTradeLong != null)
                        OnDoTradeLong();
                    else if (GlValues25 < -50 && OnDoTradeShort != null)
                        OnDoTradeShort();
                    Tick tmptick = new Tick((DateTime?)_dt, (float?) _price, (float?)_volume, _action);
                    if (visualAllElements.AddTick(tmptick))
                        return;

                    ResultOneTick r = new ResultOneTick();
                    visualAllElements.AddData( atemp, r, tmptick );

                    GlValues25 = r.valPresetHeight;
                    GlValuesRefilling = r.valRefilling;
                    GlValues = r.valMaxHeight;
                    tbGlassValue25.Text += "\r\n" + r.sumPresetHeight.ToString();
                    tbGlassValue.Text += "\r\nOpIn:" + summContractInGlass50.ToString();

                    //visualAllElements.listGradient.Add(GradientBrushForIndicator.Clone());
                    //visualAllElements.listGradient2.Add(GradientBrushForIndicator2.Clone());

                    visualAllElements.ShowData();
                }
            );
        }

        public void ChangeVisualIndicator(int[] _arrind, int[] _arrindAverage, int summContractInGlass50)
        {
            Array.Resize(ref atemp, _arrindAverage.Length);
            _arrindAverage.CopyTo(atemp, 0);
            this.summContractInGlass50 = summContractInGlass50;
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    GradientBrushForIndicator.GradientStops.Clear();
                    GradientBrushForIndicator2.GradientStops.Clear();
                    //GradientBrushForIndicatorAverage.GradientStops.Clear();
                    //GradientBrushForIndicatorAverage2.GradientStops.Clear();
                    int ival = 0, ival2 = 0;
                    //int ivalAvr = 0, ivalAvr2 = 0;
                    int s = 0, sa = 0;
                    for (int i = 0; i < _arrindAverage.Length; i++)
                    {
                        s += _arrind[i];
                        sa += _arrindAverage[i];
                        //ival = s / (i + 1);
                        //ival2 = _arrind[i];
                        //ival = _arrind[i];
                        //ival2 = s / (i + 1);
                        ival = _arrindAverage[i];
                        ival2 = sa / (i + 1);

                        //ivalAvr = _arrindAverage[i];
                        //ivalAvr2 = sa / (i + 1);
                        // выводим на градиенте значения индикатора ival
                        if (i % 5 == 0)
                            ChangeTextValue(i, ival);

                        byte b = Convert.ToByte(Math.Abs(Math.Abs(3 * ival) > 255 ? 255 : 3 * ival) + 0);
                        byte b1 = Convert.ToByte(Math.Abs(Math.Abs(3 * ival) > 255 ? 255 : 3 * ival) + 0);
                        byte b2 = Convert.ToByte(Math.Abs(Math.Abs(3 * ival2) > 255 ? 255 : 3 * ival2) + 0);
                        byte b22 = Convert.ToByte(Math.Abs(Math.Abs(3 * ival2) > 255 ? 255 : 3 * ival2) + 0);
                        //byte ba = Convert.ToByte(Math.Abs(Math.Abs(3 * ivalAvr) > 255 ? 255 : 3 * ivalAvr) + 0);
                        //byte ba1 = Convert.ToByte(Math.Abs(Math.Abs(3 * ivalAvr) > 255 ? 255 : 3 * ivalAvr) + 0);
                        //byte ba2 = Convert.ToByte(Math.Abs(Math.Abs(3 * ivalAvr2) > 255 ? 255 : 3 * ivalAvr2) + 0);
                        //byte ba22 = Convert.ToByte(Math.Abs(Math.Abs(3 * ivalAvr2) > 255 ? 255 : 3 * ivalAvr2) + 0);

                        //if (Math.Abs(abssummchangeval) > 500)
                            GradientBrushForIndicator.GradientStops.Add(new GradientStop(ival > 0 ? Color.FromRgb(0, b1, 255) : Color.FromRgb(255, b, 0), 1 - (double)i / 50));
                        //else
                        //    GradientBrushForIndicator.GradientStops.Add(new GradientStop(ival > 0 ? Color.FromRgb(0, 0, 0) : Color.FromRgb(0, 0, 0), 1 - (double)i / 50));
                        GradientBrushForIndicator2.GradientStops.Add(new GradientStop(ival2 > 0 ? Color.FromRgb(0, b22, 255) : Color.FromRgb(255, b2, 0), 1 - (double)i / 50));

                        //GradientBrushForIndicatorAverage.GradientStops.Add(new GradientStop(ivalAvr > 0 ? Color.FromRgb(0, ba1, 255) : Color.FromRgb(255, ba, 0), 1 - (double)i / 50));
                        //GradientBrushForIndicatorAverage2.GradientStops.Add(new GradientStop(ivalAvr2 > 0 ? Color.FromRgb(0, ba22, 255) : Color.FromRgb(255, ba2, 0), 1 - (double)i / 50));
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
                    canvas.Children.RemoveRange(14, canvas.Children.Count - 14);
                    double centerPrice = lastMaxBid;
                    double st;
                    centerCanvas = (int)(canvas.ActualHeight / 2);
                    for (int i = 0; i <= (int)(GlassValues.Count / 2); i++)
                    {
                        st = centerPrice + i * StepGlass;
                        DrawItem(_dt, i, lastMinAsk, lastMaxBid);
                        if (i != 0)
                            DrawItem(_dt, -i, lastMinAsk, lastMaxBid);
                    }
                });
        }
        public void DrawItem(DateTime _dt, int i, double _minAsk, double _maxBid)
        {
            Rectangle block = new Rectangle { SnapsToDevicePixels = true, Width = 110, Height = 10 };
            Rectangle block2 = new Rectangle { SnapsToDevicePixels = true, Height = 10 };
            Rectangle block3 = new Rectangle { SnapsToDevicePixels = true, Height = 10 };

            if (i > 0)
                block.Fill = UpBrush;
            else if (i < 0)
                block.Fill = DownBrush;

            TextBlock t = new TextBlock { FontSize = 10 };
            TextBlock t1 = new TextBlock { FontSize = 10 };
            TextBlock t2 = new TextBlock { FontSize = 10 };

            Canvas.SetLeft(block, canvas.ActualWidth - 108 - 120);
            Canvas.SetTop(block, centerCanvas - i * 10 + 1);
            Canvas.SetZIndex(block, 0);

            Canvas.SetLeft(block2, canvas.ActualWidth - 108 - 120);
            Canvas.SetTop(block2, centerCanvas - i * 10 + 1);
            Canvas.SetZIndex(block2, 1);
            block2.Fill = VolumeBrush;

            Canvas.SetLeft(block3, canvas.ActualWidth - 110);
            Canvas.SetTop(block3, centerCanvas - i * 10 + 1);
            Canvas.SetZIndex(block2, 1);
            block2.Fill = VolumeBrush;

            Canvas.SetLeft(t, canvas.ActualWidth - 40 - 120);
            Canvas.SetTop(t, centerCanvas - 1 - i * 10);
            Canvas.SetZIndex(t, 9);

            Canvas.SetLeft(t1, canvas.ActualWidth - 105 - 120);
            Canvas.SetTop(t1, centerCanvas - 1 - i * 10);
            Canvas.SetZIndex(t1, 9);

            Canvas.SetLeft(t2, canvas.ActualWidth - 110);
            Canvas.SetTop(t2, centerCanvas - 1 - i * 10);
            Canvas.SetZIndex(t2, 9);

            canvas.Children.Add(block);
            canvas.Children.Add(block2);
            canvas.Children.Add(block3);
            canvas.Children.Add(t);
            canvas.Children.Add(t1);
            canvas.Children.Add(t2);

            Line line100 = null;
            if ((_maxBid + i * StepGlass) % (10 * StepGlass) == 0)
            {
                line100 = new Line { X2 = canvas.ActualWidth - 111, Stroke = Brushes.Silver, StrokeThickness = 1 };
                t.FontWeight = System.Windows.FontWeights.Black;
                Canvas.SetTop(line100, centerCanvas - i * 10 + 6);
                Canvas.SetZIndex(t, 5);
                canvas.Children.Add(line100);
            }

            if (GlassValues.ContainsKey(_maxBid + i * StepGlass))
            {
                t.Text = (_maxBid + i * StepGlass).ToString("### ###");
                double tw = GlassValues[_maxBid + i * StepGlass].volume / 5;
                block2.Width = tw > 65 ? 65 : tw;
                t1.Text = GlassValues[_maxBid + i * StepGlass].volume.ToString();
                int summchangeval = 0;
                lock (objLock)
                {
                    foreach (ChangeValuesItem v in GlassValues[_maxBid + i * StepGlass].listChangeVal)
                        if ((_dt - v.dt).TotalSeconds < 1)
                            summchangeval += (int) v.newvalue - (int) v.oldvalue > 0 ? 1 : -1;
                    t2.Text = Math.Abs(summchangeval) > 0 ? "" : ""; //summchangeval.ToString() : "";
                    block3.Width = Math.Abs(summchangeval) * 5;
                    block3.Fill = summchangeval < 0 ? ChangeVolDownBrush : ChangeVolUpBrush;
                }
                lock (objLock)
                {
                    GlassValues[_maxBid + i * StepGlass].rectMain = block;
                    GlassValues[_maxBid + i * StepGlass].tbPrice = t;
                    GlassValues[_maxBid + i * StepGlass].tbVolume = t1;
                    GlassValues[_maxBid + i * StepGlass].tbChangeVal = t2;
                    GlassValues[_maxBid + i * StepGlass].rectVolume = block2;
                    GlassValues[_maxBid + i * StepGlass].rectChangeVolume = block3;
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
        private double stepglass = 0;
        public double StepGlass { get { return stepglass; } set { stepglass = value;  visualAllElements.StepGlass = value; } }
        public int summContractInGlass50 = 0;
        public Canvas canvas, ribboncanvas, tickGraphCanvas;
        public int centerCanvas;
        private double lastMinAsk, lastMaxBid;
        private int glvalues, glvalues25, glvaluesrefilling;
        private int[] atemp = { };

        public SolidColorBrush UpBrush, UpBrushAsk, DownBrush, DownBrushBid;
        public SolidColorBrush ZeroBrush, VolumeBrush;
        public SolidColorBrush ChangeVolUpBrush, ChangeVolDownBrush;
        public LinearGradientBrush GradientBrushForIndicator;
        public LinearGradientBrush GradientBrushForIndicator2;
        //public LinearGradientBrush GradientBrushForIndicatorAverage;
        //public LinearGradientBrush GradientBrushForIndicatorAverage2;
        public Polyline tickGraphAsk, tickGraphBid;
        public Polyline indicatorGraphSumm, indicatorRefilling, SMA;

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
        public List<ChangeValuesItem> listChangeVal = new List<ChangeValuesItem>();
        public double volume;
        public double price;
        public ActionGlassItem action;
        public Rectangle rectMain;
        public Rectangle rectVolume;
        public Rectangle rectChangeVolume;
        public TextBlock tbVolume;
        public TextBlock tbPrice;
        public TextBlock tbChangeVal;
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
    public class VisualOneElement
    {
        public int[] atempValues = { };
        public ResultOneTick resultOneTick;
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
                    //_summiddle = 100 - Math.Min(sumpositive, Math.Abs(sumnegative)) / Math.Max(sumpositive, Math.Abs(sumnegative)) * 100;
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
            _r.valSMA = (int)Math.Round(sumaskatperiod / periodsma);

            // добавление ask и bid
            if (_tick.Action == ActionGlassItem.buy)
                lastPriceAsk = _tick.Price;
            else if (_tick.Action == ActionGlassItem.sell)
                lastPriceBid = _tick.Price;

            _r.valAsk = lastPriceAsk;
            _r.valBid = lastPriceBid;

            VisualOneElement tmpvis = new VisualOneElement();
            tmpvis.resultOneTick = _r;
            Array.Resize(ref tmpvis.atempValues, _atemp.Length); // будем хранить значения индикатора для возможного дальнейшего пересчета
            _atemp.CopyTo(tmpvis.atempValues, 0);
            visualElementsList.Add(tmpvis);
        }
        public bool AddTick(Tick _tick)
        {
            if (lastPriceAsk == 0) lastPriceAsk = _tick.Price;
            if (lastPriceBid == 0) lastPriceBid = _tick.Price;

            //bool result = false;
            bool result = _tick.Price == lastPriceTick;  // && _action == lastActionTick);
            lastPriceTick = _tick.Price;
            lastActionTick = _tick.Action;

            return result;
        }
        public void ShowData(bool _rebuild = false)
        {
            List<Shape> tempshapes = new List<Shape>();
            foreach(Shape s in CanvasGraph.Children)
            {
                if (s is Rectangle && s.Width == 1)
                {
                    if (Canvas.GetLeft(s) - 1 < 0 || _rebuild)
                        tempshapes.Add(s);
                    else
                        Canvas.SetLeft(s, Canvas.GetLeft(s) - 1);
                }
            }
            foreach(Shape s in tempshapes)
            {
                CanvasGraph.Children.Remove(s);
            }
            tempshapes.Clear();
            int x = 0;
            if (listGradient.Count > 0)
                for (x = _rebuild ? 0 : listGradient.Count - 1; x < listGradient.Count; x++) // LinearGradientBrush brushg in  listGradient)
                {
                    Rectangle r = new Rectangle() { Fill = listGradient[x], Width = 1, Height = CanvasGraph.ActualHeight, Opacity = 1 };
                    Canvas.SetLeft(r, CanvasGraph.ActualWidth - listGradient.Count + x);
                    Canvas.SetTop(r, 0);
                    CanvasGraph.Children.Add(r);
                }

            if (_rebuild)
            {
                foreach (VisualOneElement ve in visualElementsList)
                    CalcSummIndicatorValue( ve.atempValues, ve.resultOneTick );
            }
            if (visualElementsList.Count > 2 * CanvasGraph.ActualWidth)
            {
                visualElementsList.RemoveRange(0, (int)(visualElementsList.Count - 2 * CanvasGraph.ActualWidth));
            }
            double maxp = visualElementsList.Count > 0 ? GetMaxMinVisibleValue("ask").MaxValue + StepGlass : StepGlass;
            double minp = visualElementsList.Count > 0 ? GetMaxMinVisibleValue("bid").MinValue - StepGlass : StepGlass;
            double delta = maxp - minp;
            double onePixelPrice = delta / CanvasGraph.ActualHeight;
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

            for (int i = visualElementsList.Count - 1; i > (visualElementsList.Count > CanvasGraph.ActualWidth ? visualElementsList.Count - CanvasGraph.ActualWidth : 0); i--)
            {
                _sma.Points.Add(new Point((double)i + (CanvasGraph.ActualWidth - visualElementsList.Count - 0), (maxp - visualElementsList[i].resultOneTick.valSMA) / onePixelPrice));
                _tickGraphAsk.Points.Add(new Point((double)i + (CanvasGraph.ActualWidth - visualElementsList.Count - 0), (maxp - visualElementsList[i].resultOneTick.valAsk) / onePixelPrice));
                _tickGraphBid.Points.Add(new Point((double)i + (CanvasGraph.ActualWidth - visualElementsList.Count - 0), (maxp - visualElementsList[i].resultOneTick.valBid) / onePixelPrice));
                _indicatorGraphSumm.Points.Add(new Point((double)i + (CanvasGraph.ActualWidth - visualElementsList.Count - 0), (maxInd - visualElementsList[i].resultOneTick.valPresetHeight) / onePixelIndicator));
                _indicatorRefilling.Points.Add(new Point((double)i + (CanvasGraph.ActualWidth - visualElementsList.Count - 0), (maxInd - visualElementsList[i].resultOneTick.valRefilling) / onePixelIndicator));
            }

            // разметка цены
            if (maxp != lastmaxpricegraph || minp != lastminpricegraph)
            {
                foreach (Line l in listHorizontalLine)
                {
                    CanvasGraph.Children.Remove(l);
                }
                listHorizontalLine.Clear();
                for (double yy = minp; yy < maxp; yy += StepGlass)
                {
                    if (yy % (10 * StepGlass) == 0 || yy % (5 * StepGlass) == 0)
                    {
                        Line linehorizontal = null;
                        if (yy % (10 * StepGlass) == 0)
                            linehorizontal = new Line { X2 = CanvasGraph.ActualWidth, Stroke = Brushes.Silver, StrokeThickness = 1 };
                        else
                            linehorizontal = new Line { X2 = CanvasGraph.ActualWidth, Stroke = Brushes.Silver, StrokeThickness = 1, StrokeDashArray = { 3, 5 } };
                        Canvas.SetTop(linehorizontal, (maxp - yy) / onePixelPrice);
                        Canvas.SetZIndex(linehorizontal, 5);
                        CanvasGraph.Children.Add(linehorizontal);
                        listHorizontalLine.Add(linehorizontal);
                    }
                }
                lastmaxpricegraph = maxp; lastminpricegraph = minp;
            }
        }

        private MinMaxValue GetMaxMinVisibleValue(string p)
        {
            MinMaxValue result = new MinMaxValue(1000000000, -1000000000);
            for (int i = visualElementsList.Count - 1; i > (visualElementsList.Count > CanvasGraph.ActualWidth ? visualElementsList.Count - CanvasGraph.ActualWidth : 0); i--)
            {
                switch (p)
                {
                    case "ask": 
                        result.MaxValue = visualElementsList[i].resultOneTick.valAsk > result.MaxValue ? (float)visualElementsList[i].resultOneTick.valAsk : result.MaxValue;
                        result.MinValue = visualElementsList[i].resultOneTick.valAsk < result.MinValue ? (float)visualElementsList[i].resultOneTick.valAsk : result.MinValue;
                        break;
                    case "bid":
                        result.MaxValue = visualElementsList[i].resultOneTick.valBid > result.MaxValue ? (float)visualElementsList[i].resultOneTick.valBid : result.MaxValue;
                        result.MinValue = visualElementsList[i].resultOneTick.valBid < result.MinValue ? (float)visualElementsList[i].resultOneTick.valBid : result.MinValue;
                        break;
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
        private double lastmaxpricegraph, lastminpricegraph;
        private ActionGlassItem lastActionTick;
        private int periodsma = 20;
        public double StepGlass = 0;
        
        public List<Line> listHorizontalLine = new List<Line>();
        
        public List<VisualOneElement> visualElementsList = new List<VisualOneElement>();

        public List<LinearGradientBrush> listGradient = new List<LinearGradientBrush>();
        public List<LinearGradientBrush> listGradient2 = new List<LinearGradientBrush>();

        public int levelignoreval, levelheightglass, levelstartglass, levelrefilling;
        private Canvas canvasgraph = null;
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

        //private int valpresetheight = 0, valmaxheight = 0, sumpresetheight = 0, summaxheight = 0, valrefilling = 0;
        public double valAsk { get; set; }
        public double valBid { get; set; }
        public int valPresetHeight{ get; set; }
        public int valMaxHeight{ get; set; }
        public int sumPresetHeight{ get; set; }
        public int sumMaxHeight{ get; set; }
        public int valRefilling{ get; set; }
        public int valSMA { get; set; }

    }
}
