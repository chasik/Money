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
        public GlassGraph(Canvas _c, Rectangle _indicatorRect, double _step)
        {
            canvas = _c;
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
            GradientStop gs1 = new GradientStop(Colors.Blue, 0.2);
            GradientStop gs2 = new GradientStop(Colors.Red, 0.6);
            GradientBrushForIndicator.GradientStops.Add(gs1);
            GradientBrushForIndicator.GradientStops.Add(gs2);


            _indicatorRect.Fill = GradientBrushForIndicator;
            indicatorRect = _indicatorRect;
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
                                                gi.rectMain.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 140 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(800)));
                                            }
                                            if (gi.tbVolume != null)
                                            {
                                                double top = Canvas.GetTop(gi.tbVolume);
                                                gi.tbVolume.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 140 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(800)));
                                            }
                                            if (gi.tbPrice != null)
                                            {
                                                double top = Canvas.GetTop(gi.tbPrice);
                                                gi.tbPrice.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(top, top + 140 * Math.Sign(deltaAsk), TimeSpan.FromMilliseconds(800)));
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
                                        for (double j = _price - StepGlass; j >= GetMinAsk(); j = j - StepGlass)
                                        {
                                            GlassValues[j].action = ActionGlassItem.zero;
                                            GlassValues[j].rectMain.Fill = ZeroBrush;
                                            GlassValues[j].tbVolume.Text = "";
                                        }
                                }
                                else if (_action == ActionGlassItem.sell)
                                {
                                    GlassValues[_price].rectMain.Fill = DownBrush;
                                    if (_row == 0)
                                        for (double j = _price + StepGlass; j <= GetMaxBid(); j = j + StepGlass)
                                        {
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

        }
        public void ChangeVisualIndicator(int[] _arrind)
        {
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    GradientBrushForIndicator.GradientStops.Clear();
                    int ival = 0;
                    int s = 0;
                    for (int i = 0; i < _arrind.Length; i++)
                    {
                        s += _arrind[i];
                        ival = s / (i + 1);
                        byte b = Convert.ToByte(Math.Abs(ival + 50));
                        byte b1 = Convert.ToByte(Math.Abs(ival) + 100);
                        GradientBrushForIndicator.GradientStops.Add(new GradientStop(ival > 0 ? Color.FromRgb(0, b1, 255) : Color.FromRgb(255, b, 0), 1 - (double)i / 50));
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
                    canvas.Children.RemoveRange(1, canvas.Children.Count - 1);
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
            block.Width = canvas.ActualWidth / 6 * 3;
            block.Height = 14;
            Canvas.SetLeft(block, 0);
            Canvas.SetTop(block, centerCanvas - i * 14);
            TextBlock t = new TextBlock();
            if (GlassValues.ContainsKey(_maxBid + i * StepGlass))
                t.Text = (_maxBid + i * StepGlass).ToString("### ###");
            t.FontSize = 9;
            Canvas.SetLeft(t, canvas.ActualWidth / 6 * 3 - 38);
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
            Canvas.SetLeft(t1, 5);
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

        public SortedDictionary<double, GlassItem> GlassValues = new SortedDictionary<double, GlassItem>();
        public double StepGlass = 0;
        public Canvas canvas;
        public int centerCanvas;
        private double lastMinAsk;
        private double lastMaxBid;

        public SolidColorBrush UpBrush;
        public SolidColorBrush DownBrush;
        public SolidColorBrush ZeroBrush;
        public LinearGradientBrush GradientBrushForIndicator;
        public Rectangle indicatorRect;

        public object objLock = new Object();
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
}
