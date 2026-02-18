using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using MatthiasToolbox.Mathematics;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Mathematics.Test;
using MatthiasToolbox.Semantics;
using MatthiasToolbox.Semantics.Dictionary;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Events;
using MatthiasToolbox.Statistics.Analysis;
using MatthiasToolbox.Utilities;
using MatthiasToolbox.Utilities.IO;
using MatthiasToolbox.Test.Utilities;
using MatthiasToolbox.Basics.Algorithms;

namespace MatthiasToolbox.Test
{
    public partial class MainForm : Form
    {
        #region cvar

        private int it1 = 0;
        private int it2 = 0;
        private int it3 = 0;
        private int it4 = 0;
        private int it5 = 0;

        private RichTextBoxConsoleRedirector crd;

        #endregion
        #region ctor

        public MainForm()
        {
            InitializeComponent();
            SystemTools.Culture = new CultureInfo("DE-de");
        }

        #endregion
        #region impl

        #region simulation

        // Play
        private void button6_Click(object sender, EventArgs e)
        {

        }

        // Pause
        private void button7_Click(object sender, EventArgs e)
        {

        }

        // Reset
        private void button8_Click(object sender, EventArgs e)
        {

        }

        // Load
        private void button10_Click(object sender, EventArgs e)
        {

        }

        // Save
        private void button9_Click(object sender, EventArgs e)
        {

        }

        #endregion
        #region stochastics

        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Testing...");
            crd.Indent++;
            Console.WriteLine("Testing MatthiasToolbox.Mathematics ...");
            crd.Indent++;
            
            Console.WriteLine("Testing MatthiasToolbox.Mathematics.Numerics...");
            crd.Indent++;
            TestNumerics.RunAllTests();
            crd.Indent--;
            Console.WriteLine("MatthiasToolbox.Mathematics.Numerics OK.");

            Console.WriteLine("Testing MatthiasToolbox.Mathematics.Stochastics...");
            crd.Indent++;
            TestStochastics.RunAllTests();
            StochasticsGraphTest();
            crd.Indent--;
            Console.WriteLine("MatthiasToolbox.Mathematics.Stochastics OK.");

            Console.WriteLine("Testing MatthiasToolbox.Mathematics.Geometry...");
            crd.Indent++;
            TestGeometry.RunAllTests();
            crd.Indent--;
            Console.WriteLine("MatthiasToolbox.Mathematics.Geometry OK.");

            Console.WriteLine("Testing MatthiasToolbox.Mathematics.Units...");
            crd.Indent++;
            TestUnits.RunAllTests();
            crd.Indent--;
            Console.WriteLine("MatthiasToolbox.Mathematics.Units OK.");

            crd.Indent--;
            Console.WriteLine("MatthiasToolbox.Mathematics OK.");

            Console.WriteLine("Testing MatthiasToolbox.Delta ...");
            crd.Indent++;

            Debug.Assert(LevenshteinDistance.Get("Tier", "Tor") == 2);
            Debug.Assert(LevenshteinDistance.Get("kitten", "sitting") == 3);

            crd.Indent--;
            Console.WriteLine("MatthiasToolbox.Delta OK.");

            Console.WriteLine("Testing MatthiasToolbox.Utilities ...");
            crd.Indent++;

            string s = "ac93p45bq2)Z§(!(Z$'%:F.,c+*asd";
            foreach (char c in SystemTools.NonLetterList) s = s.Replace(c, '_');

            Debug.Assert(s == "ac__p__bq__Z____Z____F__c__asd");

            crd.Indent--;
            Console.WriteLine("MatthiasToolbox.Utilities OK.");


            Console.WriteLine("Testing MatthiasToolbox.Semantics ...");
            crd.Indent++;

            int big = 0;
            int small = 0;
            Random<double> rndu = new Random<double>(new MockSeedSOurce((new Random()).Next()), new UniformDoubleDistribution(0d, 1d));
            for (int i = 0; i < 1000; i++)
            {
                if (rndu.Next() > 0.5) big++;
                else small++;
            }
            Console.WriteLine(big.ToString() + " ##### " + small.ToString());

            crd.Indent--;
            Console.WriteLine("MatthiasToolbox.Semantics OK.");


            crd.Indent--;
            Console.WriteLine("Testing finished.");
        }

        private void StochasticsGraphTest() 
        {
            chart1.Series.Clear();
            
            Series s1 = new Series();
            s1.LegendText = "Gaussian Distribution";
            s1.ChartType = SeriesChartType.Spline;

            Series s1t = new Series();
            s1t.LegendText = "Gaussian Function";
            s1t.ChartType = SeriesChartType.Spline;

            Series s2 = new Series();
            s2.LegendText = "Triangular";
            // s2.ChartType = SeriesChartType.Spline;
            
            Series s3 = new Series();
            s3.LegendText = "Erlang";
            s3.ChartType = SeriesChartType.Spline;

            DataPoint[] points1 = new DataPoint[101];
            DataPoint[] points1t = new DataPoint[101];
            DataPoint[] points2 = new DataPoint[101];
            DataPoint[] points3 = new DataPoint[101];

            for (int i = 0; i < 101; i++)
            {
                points1[i] = new DataPoint(s1);
                points1t[i] = new DataPoint(s1t);
                points2[i] = new DataPoint(s1);
                points3[i] = new DataPoint(s1);
            }

            ChartArea area = chart1.ChartAreas[0];
            //area.Area3DStyle = new ChartArea3DStyle(area);
            //area.Area3DStyle.Enable3D = true;
            
            area.AxisX.Minimum = -50.5;
            area.AxisY.Minimum = 0;
            area.AxisX.Maximum = 50.5;
            area.AxisY.Maximum = 60;

            int n = -50;
            foreach (DataPoint point in points1)
            {
                point.XValue = n;
                point.YValues[0] = 0;
                s1.Points.Add(point);
                n++;
            }

            n = -50;
            foreach (DataPoint point in points1t)
            {
                point.XValue = n;
                point.YValues[0] = MMath.Gaussian(n, 0, 15 * 15) * 1000;
                s1t.Points.Add(point);
                n++;
            }

            n = -50;
            foreach (DataPoint point in points2)
            {
                point.XValue = n;
                point.YValues[0] = 0;
                s2.Points.Add(point);
                n++;
            }

            n = -50;
            foreach (DataPoint point in points3)
            {
                point.XValue = n;
                point.YValues[0] = 0;
                s3.Points.Add(point);
                n++;
            }

            chart1.Series.Add(s1);
            chart1.Series.Add(s1t);
            chart1.Series.Add(s2); // TODO: this seems to be erroneous
            chart1.Series.Add(s3);
            
            foreach (double d in TestStochastics.TestGaussianWithFeedback())
            {
                int i = (int)Math.Round(d, 0);
                if (i < -50) i = -50;
                if (i > 50) i = 50;
                points1[i + 50].YValues[0] += 1;
            }

            foreach (double d in TestStochastics.TestTriangularWithFeedback())
            {
                int i = (int)Math.Round(d, 0);
                if (i < -50) i = -50;
                if (i > 50) i = 50;
                points2[i + 50].YValues[0] += 1;
            }

            foreach (double d in TestStochastics.TestErlangWithFeedback())
            {
                int i = (int)Math.Round(d, 0);
                if (i < -50) i = -50;
                if (i > 50) i = 50;
                points3[i + 50].YValues[0] += 1;
            }
        }

        #endregion
        #region semantics

        private IEnumerable<Tuple<string, bool>> testData1
        {
            get
            {
                Random rnd = new Random(123);
                int c = 0;
                int i = 0;
                foreach (string word in new German())
                {
                    yield return new Tuple<string, bool>(word, false);
                    i++;
                    c++;
                    // if (c > 500) break;
                }
                Application.DoEvents();
                while (i > 0)
                {
                    string tmp = "";
                    int len = (int)(rnd.NextDouble() * 5 + 3);
                    for (int l = 3; l <= len; l++)
                    {
                        tmp += SystemTools.ValidLetterList[(int)(rnd.NextDouble() * SystemTools.ValidLetterList.Count)];
                    }
                    yield return new Tuple<string, bool>(tmp, true);
                    i--;
                }
                it1++;
                this.Log<INFO>("1 Iteration " + it1.ToString());
                Application.DoEvents();
            }
        }

        private IEnumerable<Tuple<string, bool>> testData2
        {
            get
            {
                Random rnd = new Random(123);
                int c = 0;
                int i = 0;
                foreach (string word in new German())
                {
                    yield return new Tuple<string, bool>(word, false);
                    i++;
                    c++;
                    // if (c > 500) break;
                }
                Application.DoEvents();
                while (i > 0)
                {
                    string tmp = "";
                    int len = (int)(rnd.NextDouble() * 5 + 3);
                    for (int l = 3; l <= len; l++)
                    {
                        tmp += SystemTools.ValidLetterList[(int)(rnd.NextDouble() * SystemTools.ValidLetterList.Count)];
                    }
                    yield return new Tuple<string, bool>(tmp, true);
                    i--;
                }
                it2++;
                this.Log<INFO>("2 Iteration " + it2.ToString());
                Application.DoEvents();
            }
        }

        private IEnumerable<Tuple<string, bool>> testData3
        {
            get
            {
                Random rnd = new Random(123);
                int c = 0;
                int i = 0;
                foreach (string word in new German())
                {
                    yield return new Tuple<string, bool>(word, false);
                    i++;
                    c++;
                    // if (c > 500) break;
                }
                Application.DoEvents();
                while (i > 0)
                {
                    string tmp = "";
                    int len = (int)(rnd.NextDouble() * 5 + 3);
                    for (int l = 3; l <= len; l++)
                    {
                        tmp += SystemTools.ValidLetterList[(int)(rnd.NextDouble() * SystemTools.ValidLetterList.Count)];
                    }
                    yield return new Tuple<string, bool>(tmp, true);
                    i--;
                }
                it3++;
                this.Log<INFO>("3 Iteration " + it3.ToString());
                Application.DoEvents();
            }
        }

        private IEnumerable<Tuple<string, bool>> testData4
        {
            get
            {
                Random rnd = new Random(123);
                int c = 0;
                int i = 0;
                foreach (string word in new German())
                {
                    yield return new Tuple<string, bool>(word, false);
                    i++;
                    c++;
                    // if (c > 500) break;
                }
                Application.DoEvents();
                while (i > 0)
                {
                    string tmp = "";
                    int len = (int)(rnd.NextDouble() * 5 + 3);
                    for (int l = 3; l <= len; l++)
                    {
                        tmp += SystemTools.ValidLetterList[(int)(rnd.NextDouble() * SystemTools.ValidLetterList.Count)];
                    }
                    yield return new Tuple<string, bool>(tmp, true);
                    i--;
                }
                it4++;
                this.Log<INFO>("4 Iteration " + it4.ToString());
                Application.DoEvents();
            }
        }

        private IEnumerable<Tuple<string, bool>> testData5
        {
            get
            {
                Random rnd = new Random(123);
                int c = 0;
                int i = 0;
                foreach (string word in new German())
                {
                    yield return new Tuple<string, bool>(word, false);
                    i++;
                    c++;
                    // if (c > 500) break;
                }
                Application.DoEvents();
                while (i > 0)
                {
                    string tmp = "";
                    int len = (int)(rnd.NextDouble() * 5 + 3);
                    for (int l = 3; l <= len; l++)
                    {
                        tmp += SystemTools.ValidLetterList[(int)(rnd.NextDouble() * SystemTools.ValidLetterList.Count)];
                    }
                    yield return new Tuple<string, bool>(tmp, true);
                    i--;
                }
                it5++;
                this.Log<INFO>("5 Iteration " + it5.ToString());
                Application.DoEvents();
            }
        }

        private Tuple<Series, Series> setupRocGraph(Chart chart)
        {
            chart.AntiAliasing = AntiAliasingStyles.All;
            chart.TextAntiAliasingQuality = TextAntiAliasingQuality.High;

            chart.Series.Clear();

            Series s0 = new Series();
            s0.LegendText = "Random guess";
            s0.ChartType = SeriesChartType.Line;
            chart.Series.Add(s0);
            s0.Points.AddXY(0, 0);
            s0.Points.AddXY(1, 1);
            s0.Color = Color.Red;
            s0.BorderDashStyle = ChartDashStyle.Dash;

            Series s1 = new Series();
            s1.LegendText = "ROC1";
            s1.ChartType = SeriesChartType.StepLine;
            s1.BorderWidth = 2;
            s1.Color = Color.Black;

            Series s2 = new Series();
            s2.LegendText = "ROC2";
            s2.ChartType = SeriesChartType.StepLine;
            s2.BorderDashStyle = ChartDashStyle.Dot;
            s2.BorderWidth = 2;
            s2.Color = Color.Black;

            ChartArea area = chart.ChartAreas[0];
            //area.Area3DStyle = new ChartArea3DStyle(area);
            //area.Area3DStyle.Enable3D = true;

            area.AxisX.Minimum = 0;
            area.AxisY.Minimum = 0;
            area.AxisX.Maximum = 1;
            area.AxisY.Maximum = 1;

            area.AxisX.Title = "False positive rate (1 - specificity)";
            area.AxisY.Title = "True positive rate (sensitivity) ";

            //area.AxisX.MajorGrid.Enabled = false;
            //area.AxisY.MajorGrid.Enabled = false;
            area.AxisX.MajorGrid.Interval = 1;
            area.AxisY.MajorGrid.Interval = 1;

            chart.Series.Add(s1);
            chart.Series.Add(s2);

            return new Tuple<Series, Series>(s1, s2);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Tuple<Series, Series> s = setupRocGraph(chart2);
            Series s1 = s.Item1;
            Series s2 = s.Item2;

            ReceiverOperatingCharacteristic<Tuple<string, bool>> roc1 =
                new ReceiverOperatingCharacteristic<Tuple<string, bool>>(
                    testData1,
                    (v, t) => v.Item1.GarbageProbabilityROC(1d / 3d, 1d / 3d, 1d / 3d) > 1d - t,
                    (v, c) => c == v.Item2);

            ReceiverOperatingCharacteristic<Tuple<string, bool>> roc2 =
                new ReceiverOperatingCharacteristic<Tuple<string, bool>>(
                    testData2,
                    (v, t) => v.Item1.GarbageProbabilityROCPlus(0.46d, 0.43d, 0.11d) > 1d - t,
                    (v, c) => c == v.Item2);

            Task t1 = new Task(() => roc1.Calculate(100));
            t1.Start();

            Task t2 = new Task(() => roc2.Calculate(100));
            t2.Start();
            
            while (!(t1.IsCompleted && t2.IsCompleted)) { Application.DoEvents(); }

            s1.Points.AddXY(0, 0);
            foreach (ConfusionMatrix m in roc1.Points)
                s1.Points.AddXY(m.FalsePositiveRate, m.Sensitivity);
            s1.Points.AddXY(1, 1);

            s2.Points.AddXY(0, 0);
            foreach (ConfusionMatrix m in roc2.Points)
                s2.Points.AddXY(m.FalsePositiveRate, m.Sensitivity);
            s2.Points.AddXY(1, 1);

            //so.Points.AddXY(roc.BestPoint.FalsePositiveRate, roc.BestPoint.Sensitivity);
            this.Log<INFO>("1.1 Best threshold = " + roc1.BestThreshold.ToString());
            this.Log<INFO>("1.1 false positive = " + roc1.BestPoint.FalsePositives.ToString());
            this.Log<INFO>("1.1 false negatives = " + roc1.BestPoint.FalseNegatives.ToString());
            this.Log<INFO>("1.1 Observations: " + roc1.BestPoint.Observations.ToString());

            this.Log<INFO>("1.2 Best threshold = " + roc2.BestThreshold.ToString());
            this.Log<INFO>("1.2 false positive = " + roc2.BestPoint.FalsePositives.ToString());
            this.Log<INFO>("1.2 false negatives = " + roc2.BestPoint.FalseNegatives.ToString());
            this.Log<INFO>("1.2 Observations: " + roc2.BestPoint.Observations.ToString());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Tuple<Series, Series> s = setupRocGraph(chart3);
            Series s1 = s.Item1;
            Series so = s.Item2;

            ReceiverOperatingCharacteristic<Tuple<string, bool>> roc =
                new ReceiverOperatingCharacteristic<Tuple<string, bool>>(
                    testData3,
                    (v, t) => v.Item1.GarbageProbabilityROCPlus((1d - t) / 2d, (1d - t) / 2d, t) > 0.15d,
                    (v, c) => c == v.Item2);

            Task tt = new Task(() => roc.Calculate(100));
            tt.Start();
            while (!tt.IsCompleted) { Application.DoEvents(); }

            s1.Points.AddXY(0, 0);
            foreach (ConfusionMatrix m in roc.Points)
                s1.Points.AddXY(m.FalsePositiveRate, m.Sensitivity);
            s1.Points.AddXY(1, 1);

            //so.Points.AddXY(roc.BestPoint.FalsePositiveRate, roc.BestPoint.Sensitivity);
            this.Log<INFO>("2 Best threshold = " + roc.BestThreshold.ToString());
            this.Log<INFO>("2 false positive = " + roc.BestPoint.FalsePositives.ToString());
            this.Log<INFO>("2 false negatives = " + roc.BestPoint.FalseNegatives.ToString());
            this.Log<INFO>("2 Observations: " + roc.BestPoint.Observations.ToString());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Tuple<Series, Series> s = setupRocGraph(chart4);
            Series s1 = s.Item1;
            Series so = s.Item2;

            ReceiverOperatingCharacteristic<Tuple<string, bool>> roc =
                new ReceiverOperatingCharacteristic<Tuple<string, bool>>(
                    testData4,
                    (v, t) => v.Item1.GarbageProbabilityROCPlus((1d - t) / 2d, t, (1d - t) / 2d) > 0.15d, 
                    (v, c) => c == v.Item2);

            Task tt = new Task(() => roc.Calculate(100));
            tt.Start();
            while (!tt.IsCompleted) { Application.DoEvents(); }
            
            s1.Points.AddXY(1, 1);
            foreach (ConfusionMatrix m in roc.Points)
                s1.Points.AddXY(m.FalsePositiveRate, m.Sensitivity);
            s1.Points.AddXY(0, 0);

            //so.Points.AddXY(roc.BestPoint.FalsePositiveRate, roc.BestPoint.Sensitivity);
            this.Log<INFO>("3 Best threshold = " + roc.BestThreshold.ToString());
            this.Log<INFO>("3 false positive = " + roc.BestPoint.FalsePositives.ToString());
            this.Log<INFO>("3 false negatives = " + roc.BestPoint.FalseNegatives.ToString());
            this.Log<INFO>("3 Observations: " + roc.BestPoint.Observations.ToString());
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Tuple<Series, Series> s = setupRocGraph(chart5);
            Series s1 = s.Item1;
            Series so = s.Item2;

            ReceiverOperatingCharacteristic<Tuple<string, bool>> roc =
                new ReceiverOperatingCharacteristic<Tuple<string, bool>>(
                    testData5,
                    (v, t) => v.Item1.GarbageProbabilityROCPlus(t, (1d - t) / 2d, (1d - t) / 2d) > 0.15d, 
                    (v, c) => c == v.Item2);

            Task tt = new Task(() => roc.Calculate(500));
            tt.Start();
            while (!tt.IsCompleted) { Application.DoEvents(); }
            
            s1.Points.AddXY(1, 1);
            foreach (ConfusionMatrix m in roc.Points)
                s1.Points.AddXY(m.FalsePositiveRate, m.Sensitivity);
            s1.Points.AddXY(0, 0);

            //so.Points.AddXY(roc.BestPoint.FalsePositiveRate, roc.BestPoint.Sensitivity);
            this.Log<INFO>("4 Best threshold = " + roc.BestThreshold.ToString());
            this.Log<INFO>("4 false positive = " + roc.BestPoint.FalsePositives.ToString());
            this.Log<INFO>("4 false negatives = " + roc.BestPoint.FalseNegatives.ToString());
            this.Log<INFO>("4 Observations: " + roc.BestPoint.Observations.ToString());
        }

        #endregion

        #endregion

        private void MainForm_Load(object sender, EventArgs e)
        {
            crd = new RichTextBoxConsoleRedirector(richTextBox1, true);
            Logger.Add(new RichTextBoxLogger(richTextBox2), 1);
            Logger.Add<STATUS>(new StatusBarLogger(toolStripStatusLabel1));
            Logger.AutoDispatch = true;
            this.Log<INFO>("Ready.");
            this.Log<STATUS>("Status: ready.");
        }
    }
}