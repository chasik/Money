﻿using System;
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

        public static SolidColorBrush UpBrush = new SolidColorBrush { Color = Color.FromArgb(255, 255, 228, 225) };
        public static SolidColorBrush DownBrush = new SolidColorBrush { Color = Color.FromArgb(255, 152, 251, 152) };
        public static SolidColorBrush UpBrushAsk = new SolidColorBrush { Color = Color.FromArgb(255, 255, 88, 69) };
        public static SolidColorBrush DownBrushBid = new SolidColorBrush { Color = Color.FromArgb(255, 110, 184, 129) };
        public static SolidColorBrush ZeroBrush = new SolidColorBrush { Color = Color.FromArgb(255, 252, 252, 252) };
        public static SolidColorBrush VolumeBrush = new SolidColorBrush { Color = Color.FromArgb(255, 255, 127, 39) };
        public static SolidColorBrush ChangeVolUpBrush = new SolidColorBrush { Color = Color.FromArgb(255, 36, 187, 250) };
        public static SolidColorBrush ChangeVolDownBrush = new SolidColorBrush { Color = Color.FromArgb(255, 250, 36, 36) };
        public GlassGraph()
        {
            visualAllElements = new VisualAllElemnts();
        }
        public GlassGraph(Canvas _c)
        {
            canvas = _c;
            canvas.SizeChanged += canvas_SizeChanged;

            GradientBrushForIndicatorUp = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            GradientBrushForIndicatorDown = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            GradientBrushForIndicatorAll = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };

            indicatorGraphSumm = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(0, 167, 31) }, StrokeThickness = 1, SnapsToDevicePixels = true };
            indicatorRefilling = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(255, 0, 192) }, StrokeThickness = 1, SnapsToDevicePixels = true };
            SMA = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(166, 167, 31) }, StrokeThickness = 1, SnapsToDevicePixels = true };

            canvas.Children.Add(indicatorGraphSumm);
            canvas.Children.Add(indicatorRefilling);
            canvas.Children.Add(SMA);

            Canvas.SetZIndex(indicatorGraphSumm, 3);
            Canvas.SetZIndex(indicatorRefilling, 2);
            Canvas.SetZIndex(SMA, 1);

            visualAllElements = new VisualAllElemnts() {CanvasGraph = canvas, _sma = SMA, _indicatorGraphSumm = indicatorGraphSumm, _indicatorRefilling =  indicatorRefilling };
        }

        void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }
        public void ChangeValues(DateTime _dt, double _price, double _volume, int _row, ActionGlassItem _action)
        {
            if (_row == 0)
            {
                if (_action == ActionGlassItem.sell) lastAsk = _price;
                else if (_action == ActionGlassItem.buy) lastBid = _price;
            }
            visualAllElements.ChangeGlass(_dt, _price, _volume, _row, _action);

            int sumGlass = visualAllElements.GetSummGlass50();

            this.summContractInGlass50 = sumGlass;
            int averageGlass = (int)sumGlass / (50 * 2);

            // новая версия, более взвешенное значение (как год назад)
            int sumlong = 0, sumshort = 0;
            int sumlongAverage = 0, sumshortAverage = 0;
            for (int i = 0; i < 50/*paramTh.glassHeight*/; i++)
            {
                //if (GlassValues.ContainsKey((int)la + i * StepGlass))
                //    sumlong += (int)GlassValues[(int)la + i * StepGlass].volume;
                //if (GlassValues.ContainsKey((int)lb - i * StepGlass))
                //    sumshort += (int)GlassValues[(int)lb - i * StepGlass].volume;
                //int tempsum = (sumlong + sumshort) == 0 ? 1 : sumlong + sumshort;
                //atemp[i] = (int)(sumlong - sumshort) * 100 / (tempsum);

                sumlongAverage += GlassValues.ContainsKey((int)lastAsk + i * GlassItem.stepGlass)
                    && GlassValues[(int)lastAsk + i * GlassItem.stepGlass].volume < averageGlass * 3/*paramTh.averageValue*/
                    ? (int)GlassValues[(int)lastAsk + i * GlassItem.stepGlass].volume : averageGlass * 3/*(int)paramTh.averageValue*/;
                sumshortAverage += GlassValues.ContainsKey((int)lastBid - i * GlassItem.stepGlass)
                    && GlassValues[(int)lastBid - i * GlassItem.stepGlass].volume < averageGlass * 3 /*paramTh.averageValue*/
                    ? (int)GlassValues[(int)lastBid - i * GlassItem.stepGlass].volume : averageGlass * 3 /*(int)paramTh.averageValue*/;
                int tempsumavr = (sumlongAverage + sumshortAverage) == 0 ? 1 : sumlongAverage + sumshortAverage;
                atemp[i] = (int)(sumlongAverage - sumshortAverage) * 100 / (tempsumavr);
            }

            ChangeVisualIndicator();

            if (GlassValues.ContainsKey(_price) && (GlassValues[_price].volume != _volume || GlassValues[_price].action != _action))
            {
            //    if (GlassValues[_price].rectMain != null)
            //        canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
            //            (ThreadStart)delegate()
            //            {
            //                // смотрим, далеко ли "уполз" стакан
            //                double deltaAsk = minAsk - lastMinAsk;
            //                if (Math.Abs(deltaAsk) > doAnimationValue * StepGlass)
            //                {
            //                    AnimateGlassToCenter(doAnimationValue * (int)StepGlass * Math.Sign(deltaAsk));
            //                }
            //            });
            }
            //else if (!GlassValues.ContainsKey(_price))
            //{
            //    lock (objLock)
            //    {
            //        AddGlassItemToGlassValues(_price, new GlassItem(_price, _volume, _action));
            //    }
            //    RebuildGlass(_dt);
            //}
        }

        private void AddGlassItemToGlassValues(double _price, GlassItem glassItem)
        {
            GlassValues.Add(_price, glassItem);
            if (_price > maxpriceindictionary)
                maxpriceindictionary = _price;
            if (_price < minpriceindictionary)
                minpriceindictionary = _price;
        }
        public void AnimateGlassToCenter(int _deltamove)
        {
            visualAllElements.AnimateElemntsToCenter(_deltamove);
            lastMinAsk = GetMinAsk();
            lastMaxBid = GetMaxBid();
        }
        public void AddTick(DateTime _dt, double _price, double _volume, ActionGlassItem _action)
        {
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Render,
                (ThreadStart)delegate()
                {
                    ResultOneTick r = new ResultOneTick();
                    r.resultChangeSpeed.CalcSummChangeSpeedInGlass(GlassValues, lastAsk, lastBid, GlassItem.stepGlass);
                    Tick tmptick = new Tick((DateTime?)_dt, (float?)_price, (float?)_volume, _action);
                    if (visualAllElements.AddTick(tmptick))
                        return;
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

                    // делаем рендеринг раз в минуту (1000ms)
                    DateTime ddd = DateTime.Now;
                    if (ddd.Subtract(lastShowDataCall).TotalMilliseconds > 300)
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
            GetLastVisiblePrices();
            canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate()
                {
                    canvas.Children.Clear();
                    canvas.Children.Add(visualAllElements.movedCanvas2D);
                    canvas.Children.Add(visualAllElements.movedCanvasGlass);
                    visualAllElements.ClearMovedCanvas();
                    visualAllElements.AnimateToZeroLevel();
                    double centerPrice = lastMaxBid;
                    centerCanvas = (int)(canvas.ActualHeight / 2);

                    recGradientUp = new Rectangle { SnapsToDevicePixels = true, Width = 12, Height = GlassItem.heightOneItem * 50 };
                    recGradientDown = new Rectangle { SnapsToDevicePixels = true, Width = 12, Height = GlassItem.heightOneItem * 50 };

                    Canvas.SetLeft(recGradientUp, canvas.ActualWidth - 85);
                    Canvas.SetTop(recGradientUp, centerCanvas - GlassItem.heightOneItem * 50);
                    Canvas.SetZIndex(recGradientUp, 2);
                    recGradientUp.Fill = GradientBrushForIndicatorUp;

                    Canvas.SetLeft(recGradientDown, canvas.ActualWidth - 85);
                    Canvas.SetTop(recGradientDown, centerCanvas);
                    Canvas.SetZIndex(recGradientDown, 2);
                    recGradientDown.Fill = GradientBrushForIndicatorDown;

                    canvas.Children.Add(recGradientUp);
                    canvas.Children.Add(recGradientDown);

                    canvas.Children.Add(indicatorGraphSumm);
                    canvas.Children.Add(indicatorRefilling);
                    canvas.Children.Add(SMA);

                    foreach (FrameworkElement fe in visualAllElements.listFElemnts)
                        visualAllElements.movedCanvas2D.Children.Add(fe);

                    GraphAreaForGlass.ShowAllBars(canvas);
                    visualAllElements.ShowData(true, true);
                });
        }
        private void GetLastVisiblePrices()
        {
            lastVisibleAsk = Math.Round(lastMinAsk + (canvas.ActualHeight - centerCanvas) / GlassItem.heightOneItem * GlassItem.stepGlass);
            lastVisibleBid = Math.Round(lastMaxBid - (canvas.ActualHeight - centerCanvas) / GlassItem.heightOneItem * GlassItem.stepGlass);
        }
        public void ChangeBidAsk(double _ask, double _bid)
        {
        }
        private double GetMinAsk()
        {
            double _minA = 1000000;
            for (double p = minpriceindictionary; p <= maxpriceindictionary; p = p + GlassItem.stepGlass)
            {
                if (GlassValues.ContainsKey(p) && GlassValues[p].action == ActionGlassItem.sell && p < _minA)
                    _minA = p;
            }                
            return _minA;
        }
        private double GetMaxBid()
        {
            double _maxB = -1000000;
            for (double p = minpriceindictionary; p <= maxpriceindictionary; p = p + GlassItem.stepGlass)
            {
                if (GlassValues.ContainsKey(p) && GlassValues[p].action == ActionGlassItem.buy && p > _maxB)
                    _maxB = p;
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
        private double maxpriceindictionary = -10000000, minpriceindictionary = 10000000;
        private int doAnimationValue = 15;
        private double lastMinAsk, lastMaxBid;
        private double lastvisibleask, lastvisiblebid;
        public double lastVisibleAsk { get { return lastvisibleask; } private set { lastvisibleask = value; visualAllElements.lastVisibleAsk = value; } }
        public double lastVisibleBid { get { return lastvisiblebid; } private set { lastvisiblebid = value; visualAllElements.lastVisibleBid = value; } }
        public static double lastBid = 0, lastAsk = 0;

        public int summContractInGlass50 = 0;
        public Canvas canvas;
        public int centerCanvas;
        private int glvalues, glvalues25, glvaluesrefilling;
        private int[] atemp = new int[50];
        private DateTime lastShowDataCall = DateTime.Now;

        public LinearGradientBrush GradientBrushForIndicatorUp, GradientBrushForIndicatorDown, GradientBrushForIndicatorAll;
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
            if (startPriceLevel == 0)
                startPriceLevel = _price;
        }
        public List<ChangeValuesItem> listChangeVal = new List<ChangeValuesItem>();
        public static double heightOneItem = 9;
        public static double stepGlass = 0;
        public static double startPriceLevel = 0;
        public double volume;
        public double price;
        public ActionGlassItem action;
        public Line line100;
        public Rectangle rectMain, rectVolume, rectChangeVolumeOut, rectChangeVolumeIn;
        public TextBlock tbVolume, tbPrice, tbChangeVal;
        internal ResultSpeedChangeGlass speedChangeGlass = new ResultSpeedChangeGlass();
        internal void CalcSpeedChange(Canvas _canvas, DateTime _dt)
        {
            speedChangeGlass.changeCountIn = 0;
            speedChangeGlass.changeCountOut = 0;
            List<ChangeValuesItem> templist = new List<ChangeValuesItem>();
            foreach (ChangeValuesItem v in listChangeVal)
            {
                if ((_dt - v.dt).TotalMilliseconds < 500) // за последние n секунд
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
                _canvas.Dispatcher.BeginInvoke(DispatcherPriority.Normal
                    , (ThreadStart)delegate() {
                        rectChangeVolumeOut.Width = Math.Abs(speedChangeGlass.changeCountOut) * 1;
                        rectChangeVolumeIn.Width = Math.Abs(speedChangeGlass.changeCountIn) * 1;
                        Canvas.SetLeft(rectChangeVolumeIn, _canvas.ActualWidth - rectChangeVolumeIn.Width - 17);
                    }
                );
            }
        }
        internal void DrawItem(Canvas _canvasforglass, DateTime _dt)
        {
            Rectangle block = new Rectangle { SnapsToDevicePixels = true, Width = 100, Height = heightOneItem };
            Rectangle block2 = new Rectangle { SnapsToDevicePixels = true, Height = heightOneItem };
            Rectangle block3 = new Rectangle { SnapsToDevicePixels = true, Height = heightOneItem };
            Rectangle block4 = new Rectangle { SnapsToDevicePixels = true, Height = heightOneItem };

            if (action == ActionGlassItem.sell)
                block.Fill = GlassGraph.UpBrush;
            else if (action == ActionGlassItem.buy)
                block.Fill = GlassGraph.DownBrush;

            TextBlock t = new TextBlock { FontSize = 8 };
            TextBlock t1 = new TextBlock { FontSize = 8 };
            TextBlock t2 = new TextBlock { FontSize = 8 };
            double topCoordinate = (startPriceLevel - price) / stepGlass * heightOneItem;
            Canvas.SetLeft(block, 0);
            Canvas.SetTop(block, topCoordinate);
            Canvas.SetZIndex(block, 0);

            Canvas.SetLeft(block2, 0);
            Canvas.SetTop(block2, topCoordinate);
            Canvas.SetZIndex(block2, 1);
            block2.Fill = GlassGraph.VolumeBrush;

            Canvas.SetLeft(block3, 121);
            Canvas.SetTop(block3, topCoordinate);
            Canvas.SetZIndex(block3, 1);
            block3.Fill = GlassGraph.ChangeVolDownBrush;

            Canvas.SetLeft(block4, 121);
            Canvas.SetTop(block4, topCoordinate);
            Canvas.SetZIndex(block4, 1);
            block4.Fill = GlassGraph.ChangeVolUpBrush;

            Canvas.SetLeft(t, 68);
            Canvas.SetTop(t, topCoordinate - 1);
            Canvas.SetZIndex(t, 9);

            Canvas.SetLeft(t1, 3);
            Canvas.SetTop(t1, topCoordinate - 1);
            Canvas.SetZIndex(t1, 9);

            Canvas.SetLeft(t2, 118);
            Canvas.SetTop(t2, topCoordinate - 1);
            Canvas.SetZIndex(t2, 9);

            _canvasforglass.Children.Add(block);
            _canvasforglass.Children.Add(block2);
            _canvasforglass.Children.Add(block3);
            _canvasforglass.Children.Add(block4);
            _canvasforglass.Children.Add(t);
            _canvasforglass.Children.Add(t1);
            _canvasforglass.Children.Add(t2);

            Line line100 = null;
            //if ((_maxBid + i * stepGlass) % (10 * stepGlass) == 0)
            //{
            //    line100 = new Line { X2 = 117, Stroke = Brushes.Silver, StrokeThickness = 1 };
            //    t.FontWeight = System.Windows.FontWeights.Black;
            //    Canvas.SetTop(line100, centerCanvas - i * heightOneItem + doAnimationValue);
            //    Canvas.SetZIndex(t, 5);
            //    Canvas.SetZIndex(line100, 0);
            //    _canvasforglass.Children.Add(line100);
            //}

            t.Text = price.ToString("### ###");
            double tw = volume / 5;
            block2.Width = tw > 65 ? 65 : tw;
            t1.Text = volume.ToString();

            rectMain = block;
            tbPrice = t;
            tbVolume = t1;
            tbChangeVal = t2;
            rectVolume = block2;
            rectChangeVolumeOut = block3;
            rectChangeVolumeIn = block4;
            if (line100 != null)
                this.line100 = line100;
        }

        internal void ChangeItem(DateTime _dt, int _row, double _price, double _volume, ActionGlassItem _action)
        {
            volume = _volume;
            action = _action;

            if (_volume == 0)
            {
                rectVolume.Width = tbChangeVal.Width = rectChangeVolumeOut.Width = rectChangeVolumeIn.Width = 0;
                listChangeVal.Clear();
            }
            else
            {
                double tw = _volume / 5;
                rectVolume.Width = tw > 65 ? 65 : tw;
            }
            tbVolume.Text = _volume > 0 ? _volume.ToString() : "";
            bool issellaction = _action == ActionGlassItem.sell;
            rectMain.Fill = issellaction ? (_row == 0 ? GlassGraph.UpBrushAsk : GlassGraph.UpBrush) : (_row == 0 ? GlassGraph.DownBrushBid : GlassGraph.DownBrush);
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
        public SortedDictionary<double, GlassItem> GlassValues = new SortedDictionary<double, GlassItem>();
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
        public void ShowData(bool _rebuild = false, bool _resize = false)
        {
            if (visualElementsList.Count < 1 || (visualElementsList.Count - countAddedWithNotShowData < 0))
                return;
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
                if (visualElementsList.Count > 3 * CanvasGraph.ActualWidth && CanvasGraph.ActualWidth > 0)
                    visualElementsList.RemoveRange(0, (int)(visualElementsList.Count - 3 * CanvasGraph.ActualWidth));

                double maxp = GlassItem.startPriceLevel + canvasgraph.ActualHeight / 2 / GlassItem.heightOneItem * GlassItem.stepGlass;
                double minp = GlassItem.startPriceLevel - canvasgraph.ActualHeight / 2 / GlassItem.heightOneItem * GlassItem.stepGlass;
                double delta = maxp - minp;
                double onePixelPrice = delta / CanvasGraph.ActualHeight;
                if (onePixelPrice == 0)
                    onePixelPrice = 10;
                double maxInd = Math.Max(Math.Abs(GetMaxMinVisibleValue("valpreseth").MaxValue), Math.Abs(GetMaxMinVisibleValue("valpreseth").MaxValue));
                if (maxInd == 0)
                    maxInd = Math.Max(maxInd, GetMaxMinVisibleValue("valrefilling").MaxValue);
                double deltaIndicator = 2 * maxInd;
                double onePixelIndicator = deltaIndicator / CanvasGraph.ActualHeight;

                _indicatorGraphSumm.Points.Clear();
                _indicatorRefilling.Points.Clear();
                _sma.Points.Clear();
                int c = visualElementsList.Count;
                GraphAreaForGlass.areasList.Clear();
                for (int i = (int)(c > CanvasGraph.ActualWidth ? c - CanvasGraph.ActualWidth : 0); i < c; i++)
                {
                    ResultOneTick rt = visualElementsList[i].resultOneTick;
                    double yAsk = (maxp - rt.valAsk) / onePixelPrice;
                    double yBid = (maxp - rt.valBid) / onePixelPrice;
                    double xAll = (double)i + (CanvasGraph.ActualWidth - c - widthGlassValues);
                    _sma.Points.Add(new Point(xAll, (maxp - rt.valSMA) / onePixelPrice));
                    _indicatorGraphSumm.Points.Add(new Point(xAll, (maxInd - rt.valPresetHeight) / onePixelIndicator));
                    _indicatorRefilling.Points.Add(new Point(xAll, (maxInd - rt.valRefilling) / onePixelIndicator));
                    GraphAreaForGlass.AddData((IndicatorCommand)Math.Sign(rt.valPresetHeight), xAll, yAsk, yBid);
                }
                GraphAreaForGlass.ShowAllBars(CanvasGraph);
                if (_resize)
                {
                    listFElemnts.Clear();
                    tickGraphAsk.Points.Clear();
                    tickGraphBid.Points.Clear();
                }

                movedCanvas2D.Margin = new Thickness(CanvasGraph.ActualWidth - widthGlassValues - visualElementsList.Count, movedCanvas2D.Margin.Top, 0, 0);

                for (int x = _resize ? 0 : visualElementsList.Count - countAddedWithNotShowData; x < visualElementsList.Count; x++)
                {
                    ResultOneTick rt = visualElementsList[x].resultOneTick;
                    double yAsk = (GlassItem.startPriceLevel - rt.valAsk) / GlassItem.stepGlass *  GlassItem.heightOneItem;
                    double yBid = (GlassItem.startPriceLevel - rt.valBid) / GlassItem.stepGlass *  GlassItem.heightOneItem;
                    tickGraphAsk.Points.Add(new Point(x, yAsk));
                    tickGraphBid.Points.Add(new Point(x, yBid));

                    ShowSpeedChange(movedCanvas2D, rt.resultChangeSpeed, /*CanvasGraph.ActualWidth - visualElementsList.Count - widthGlassValues */ x);
                }
                countAddedWithNotShowData = 0;

            } catch (Exception e)
            {
                MessageBox.Show("метод ShowData:\r\n" + e.Message);
            }
        }
        private void ShowSpeedChange(Canvas _canvas, ResultSummSpeedChangeGlass _resultChangeSpeed, double _xAll)
        {
            double ycenter = 100;
            Line l1 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter, Y2 = ycenter - _resultChangeSpeed.summChangeUp.changeCountIn, Stroke = Brushes.Blue, StrokeThickness = 1, Opacity = 0.5 };
            Line l2 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter + 1, Y2 = ycenter + _resultChangeSpeed.summChangeDown.changeCountIn, Stroke = Brushes.Blue, StrokeThickness = 1, Opacity = 0.5 };
            Line l3 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter, Y2 = ycenter - _resultChangeSpeed.summChangeUp.changeCountOut, Stroke = Brushes.Red, StrokeThickness = 1, Opacity = 0.5 };
            Line l4 = new Line { X1 = _xAll, X2 = _xAll, Y1 = ycenter + 1, Y2 = ycenter + _resultChangeSpeed.summChangeDown.changeCountOut, Stroke = Brushes.Red, StrokeThickness = 1, Opacity = 0.5 };
            Canvas.SetZIndex(l1, 15);
            Canvas.SetZIndex(l2, 15);
            Canvas.SetZIndex(l3, 15);
            Canvas.SetZIndex(l4, 15);
            _canvas.Children.Add(l1);
            _canvas.Children.Add(l2);
            _canvas.Children.Add(l3);
            _canvas.Children.Add(l4);
            listFElemnts.AddRange( new List<Line> { l1, l2, l3, l4 } );
        }
        private MinMaxValue GetMaxMinVisibleValue(string p = "")
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
        private double lastvisibleask, lastvisiblebid;
        public double lastVisibleAsk
        {
            get { return lastvisibleask; }
            set {
                if (lastvisibleask != value)
                lastvisibleask = value; 
            }
        }
        public double lastVisibleBid
        {
            get { return lastvisiblebid; }
            set { lastvisiblebid = value; }
        }

        private double widthGlassValues = 138;
        private ActionGlassItem lastActionTick;
        private int periodsma = 80;
        public int countAddedWithNotShowData = 0;
        
        public List<Line> listHorizontalLine = new List<Line>();
        
        internal List<VisualOneElement> visualElementsList = new List<VisualOneElement>();
        internal List<FrameworkElement> listFElemnts = new List<FrameworkElement>();

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

        private Canvas canvasgraph = null;
        public Canvas CanvasGraph
        {
            get { return canvasgraph; }
            set { 
                canvasgraph = value;
                movedCanvas2D = new Canvas() { Background = Brushes.Green, Opacity = 1, Margin = new Thickness(0, 0, widthGlassValues, 0) };
                movedCanvasGlass = new Canvas() { Background = Brushes.Red, Opacity = 1, Width = widthGlassValues};
                canvasgraph.Children.Add(movedCanvas2D);
                canvasgraph.Children.Add(movedCanvasGlass);
                canvasgraph.SizeChanged += canvasgraph_SizeChanged;

                tickGraphAsk = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(0, 0, 110) }, StrokeThickness = 1, SnapsToDevicePixels = true };
                tickGraphBid = new Polyline { Stroke = new SolidColorBrush { Color = Color.FromRgb(110, 0, 0) }, StrokeThickness = 1, SnapsToDevicePixels = true };
                tickGraphAsk.MouseDown += Polyline_MouseDown;
                tickGraphBid.MouseDown += Polyline_MouseDown;
                Canvas.SetZIndex(tickGraphAsk, 30);
                Canvas.SetZIndex(tickGraphBid, 30);
                movedCanvas2D.Children.Add(tickGraphAsk);
                movedCanvas2D.Children.Add(tickGraphBid); 
            }
        }

        void canvasgraph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            movedCanvas2D.Height = (sender as Canvas).ActualHeight;
            movedCanvas2D.Width = canvasgraph.ActualWidth - widthGlassValues;
            movedCanvas2D.Margin = new Thickness(movedCanvas2D.Margin.Left, (sender as Canvas).ActualHeight / 2, widthGlassValues, 0);

            movedCanvasGlass.Height = (sender as Canvas).ActualHeight;
            movedCanvasGlass.Margin = new Thickness((sender as Canvas).ActualWidth - widthGlassValues, (sender as Canvas).ActualHeight / 2, 0, 0);
        }
        public void Polyline_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Polyline).StrokeThickness == 1)
                (sender as Polyline).StrokeThickness = 2;
            else
                (sender as Polyline).StrokeThickness = 1;
        }
        private Canvas movedcanvas2d = null, movedcanvasglass = null;
        public Canvas movedCanvas2D { get { return movedcanvas2d; } set { movedcanvas2d = value; } }
        public Canvas movedCanvasGlass { get { return movedcanvasglass; } set { movedcanvasglass = value; } }

        private Polyline _tickgraphask, _tickgraphbid;
        public Polyline tickGraphAsk { get { return _tickgraphask; } set { _tickgraphask = value; } }
        public Polyline tickGraphBid { get { return _tickgraphbid; } set { _tickgraphbid = value; } }
        public Polyline _indicatorGraphSumm { get; set; }
        public Polyline _indicatorRefilling { get; set; }
        public Polyline _sma { get; set; }

        internal void ClearMovedCanvas()
        {
            movedCanvas2D.Children.Clear();
            movedCanvas2D.Children.Add(tickGraphBid);
            movedCanvas2D.Children.Add(tickGraphAsk);
            movedCanvasGlass.Children.Clear();
        }

        internal void AnimateElemntsToCenter(int _deltamove)
        {
            movedCanvas2D.Margin = new Thickness(movedCanvas2D.Margin.Left, movedCanvas2D.Margin.Top + _deltamove, 0, 0);
            movedCanvasGlass.Margin = new Thickness(movedCanvasGlass.Margin.Left, movedCanvasGlass.Margin.Top + _deltamove, 0, 0);
        }

        internal void AnimateToZeroLevel()
        {
            movedCanvas2D.Margin = new Thickness(movedCanvas2D.Margin.Left, 0, 0, 0);
            movedCanvasGlass.Margin = new Thickness(movedCanvasGlass.Margin.Left, 0, 0, 0);
        }

        internal void ChangeGlass(DateTime _dt, double _price, double _volume, int _row, ActionGlassItem _action)
        {
            if (GlassValues.ContainsKey(_price))
            {
                // подсчет скорости изменений объема по каждому значению стакана за интервал времени
                foreach (GlassItem gv in GlassValues.Values)
                {
                    gv.CalcSpeedChange(movedCanvasGlass, _dt);
                }
                GlassValues[_price].listChangeVal.Add(new ChangeValuesItem() { dt = _dt, price = _price, oldvalue = GlassValues[_price].volume, newvalue = _volume });
                movedCanvasGlass.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                   (ThreadStart)delegate() {
                       if (_row == 0)
                       {
                           if (GlassValues.ContainsKey(GlassGraph.lastAsk) && GlassValues.ContainsKey(GlassGraph.lastBid) 
                               && GlassValues[GlassGraph.lastAsk].rectMain != null && GlassValues[GlassGraph.lastBid].rectMain != null)
                           {
                               //recGradientUp.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(Canvas.GetTop(recGradientUp), Canvas.GetTop(GlassValues[minAsk].rectMain) - 49 * HeightOneItem, TimeSpan.FromMilliseconds(50)));
                               //recGradientDown.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(Canvas.GetTop(recGradientDown), Canvas.GetTop(GlassValues[maxBid].rectMain), TimeSpan.FromMilliseconds(50)));
                           }
                       }
                       GlassValues[_price].ChangeItem(_dt, _row, _price, _volume, _action); 
                   });
            }
            else
            {
                GlassItem gi = new GlassItem(_price, _volume, _action);
                GlassValues.Add(_price, gi);
                movedCanvasGlass.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                   (ThreadStart)delegate(){ gi.DrawItem(movedCanvasGlass, _dt); });
            }
        }

        internal int GetSummGlass50()
        {
            int result = 0;
            double la = GlassGraph.lastBid, lb = GlassGraph.lastBid;
            for (int i = 0; i < (GlassValues.Count < 50 ? GlassValues.Count : 50); i++)
            {
                result += GlassValues.ContainsKey(la + i * GlassItem.stepGlass) ? (int)GlassValues[la + i * GlassItem.stepGlass].volume : 0;
                result += GlassValues.ContainsKey(lb - i * GlassItem.stepGlass) ? (int)GlassValues[lb - i * GlassItem.stepGlass].volume : 0;
            }
            return result;
        }
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

    internal class ResultSpeedChangeGlass
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

    internal class ResultSummSpeedChangeGlass
    {
        internal ResultSpeedChangeGlass summChangeUp = new ResultSpeedChangeGlass();
        internal ResultSpeedChangeGlass summChangeDown = new ResultSpeedChangeGlass();

        internal void CalcSummChangeSpeedInGlass(SortedDictionary<double, GlassItem> _glassvalues, double _ask, double _bid, double _stepglass)
        {
            summChangeUp.Clear();
            summChangeDown.Clear();

            for (int i = -49; i < 51; i++)
            {
                double _key = i < 1 ? _bid + i * _stepglass : _ask + (i - 1) * _stepglass;
                if (!_glassvalues.ContainsKey(_key))
                    continue;
                ResultSpeedChangeGlass _rchange = _glassvalues[_key].speedChangeGlass;
                //if (_rchange.IsZeroValue())
                //    continue;
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
    }
}
