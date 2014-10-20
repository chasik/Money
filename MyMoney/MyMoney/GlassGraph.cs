using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
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
        public GlassGraph(Canvas _c, double _step)
        {
            canvas = _c;
            StepGlass = _step;
            UpBrush = new SolidColorBrush();
            UpBrush.Color = Color.FromArgb(255, 255, 228, 225);
            DownBrush = new SolidColorBrush();
            DownBrush.Color = Color.FromArgb(255, 152, 251, 152);
        }
        public void ChangeValues(double _price, double _volume, ActionGlassItem _action)
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
                                GlassValues[_price].tbVolume.Text = _volume.ToString();
                                if (_action == ActionGlassItem.buy)
                                    GlassValues[_price].rectMain.Fill = UpBrush;
                                else if (_action == ActionGlassItem.buy)
                                    GlassValues[_price].rectMain.Fill = DownBrush;
                            });
                }
            }
            else
            {
                GlassValues.Add(_price, new GlassItem(_volume, _action));
                RebuildGlass();
            }
        }
        public void RebuildGlass()
        {
            double minAsk = GetMinAsk();
            double maxBid = GetMaxBid();
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    canvas.Children.Clear();
                    double centerPrice = maxBid;
                    double st;
                    centerCanvas = (int)(canvas.ActualHeight / 2);
                    for (int i = 0; i <= centerCanvas / 15; i++)
                    {
                        st = centerPrice + i * StepGlass;
                        DrawItem(i, minAsk, maxBid);
                        if (i != 0) 
                            DrawItem(-i, minAsk, maxBid);
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
            block.Height = 15;
            Canvas.SetLeft(block, 0);
            Canvas.SetTop(block, centerCanvas - i * 15);
            TextBlock t = new TextBlock();
            if (GlassValues.ContainsKey(_maxBid + i * StepGlass))
                t.Text = (_maxBid + i * StepGlass).ToString("### ###");
            t.FontSize = 9;
            Canvas.SetLeft(t, canvas.ActualWidth / 6 * 3 - 38);
            Canvas.SetTop(t, centerCanvas - i * 15 + 1);
            TextBlock t1 = new TextBlock();
            if (GlassValues.ContainsKey(_maxBid + i * StepGlass))
            {
                t1.Text = GlassValues[_maxBid + i * StepGlass].volume.ToString();
                GlassValues[_maxBid + i * StepGlass].rectMain = block;
                GlassValues[_maxBid + i * StepGlass].tbPrice = t;
                GlassValues[_maxBid + i * StepGlass].tbVolume = t1;
            }

            t1.FontSize = 9;
            Canvas.SetLeft(t1, 5);
            Canvas.SetTop(t1, centerCanvas - i * 15 + 1);
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
            foreach (double p in GlassValues.Keys)
            {
                if (GlassValues[p].action == ActionGlassItem.buy && p < _minA)
                    _minA = p;
            }
            return _minA;
        }
        private double GetMaxBid()
        {
            double _maxB = -1000000;
            foreach (double p in GlassValues.Keys)
            {
                if (GlassValues[p].action == ActionGlassItem.sell && p > _maxB)
                    _maxB = p;
            }
            return _maxB;
        }

        public SortedDictionary<double, GlassItem> GlassValues = new SortedDictionary<double, GlassItem>();
        public double StepGlass = 0;
        public Canvas canvas;
        public int centerCanvas;

        public SolidColorBrush UpBrush;
        public SolidColorBrush DownBrush;
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
