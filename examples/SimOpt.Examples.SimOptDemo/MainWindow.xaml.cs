using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;
using MatthiasToolbox.Optimization.Strategies.SimulatedAnnealing;
using MatthiasToolbox.Optimization.Strategies.Evolutionary;
using MatthiasToolbox.SimOptExample.Optimizer;
using MatthiasToolbox.Presentation;
using MatthiasToolbox.Simulation;
using MatthiasToolbox.Optimization.Interfaces;
using System.Windows.Threading;

namespace MatthiasToolbox.SimOptExample
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region cvar

        // simulation
        private MatthiasToolbox.SimOptExample.Model.Simulation sim;

        // optimization
        private bool strategyEA = true;
        private Problem Problem;
        private AnnealingAlgorithm AnnealingAlgorithm;
        private EvolutionaryAlgorithm EvolutionaryAlgorithm;
        private SimulatedAnnealingConfiguration sac;
        private EvolutionaryAlgorithmConfiguration eac;

        // gui
        private Chart optimizerChart;
        private ChartArea optimizerChartArea1;
        private Series optimizerSeries;
        private WPFRichTextBoxLogger simLogger;
        private WPFRichTextBoxLogger optLogger;

        #endregion
        #region ctor

        public MainWindow()
        {
            InitializeComponent();

            checkBoxLog.IsChecked = true;
            
            simLogger = new WPFRichTextBoxLogger(richTextBoxLog);
            optLogger = new WPFRichTextBoxLogger(richTextBoxOpt);

            AnnealingAlgorithm = new AnnealingAlgorithm();
            EvolutionaryAlgorithm = new EvolutionaryAlgorithm();
        }

        #endregion
        #region hand

        #region sim

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (sim == null)
            {
                sim = new MatthiasToolbox.SimOptExample.Model.Simulation(richTextBoxLog, (bool)checkBoxLog.IsChecked);
                sim.Model.SimulationTerminating += Finished;
            }

            if (sim.Model.IsPaused)
            {
                (new Thread(new ThreadStart(() => sim.Model.Continue()))).Start();
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;
                btnPause.IsEnabled = true;
            }
            else if (!sim.Model.IsRunning)
            {
                sim.Model.LogEvents = checkBoxLog.IsChecked.Value;
                sim.Sink.Logging = checkBoxLog.IsChecked.Value;

                if (!sim.Model.IsReset)
                {
                    sim.Model.Reset();
                    sim.FillQueue();
                }
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;
                btnPause.IsEnabled = true;
                (new Thread(new ThreadStart(() => sim.Run()))).Start();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            sim.Model.Stop();
            Logging.Logger.ClearMessageQueue();
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnPause.IsEnabled = false;
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            sim.Model.Pause();
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnPause.IsEnabled = false;
        }

        private void Finished() // IModel model, IEventInstance lastEvent)
        {
            this.Log<INFO>("GUI finished.\r");

            // Logger.Dispatch(); // in this model the dispatch is called inside the simulation, forcing synchronization.

            btnStart.Dispatcher.Invoke(new Action(() => btnStart.IsEnabled = true));
            btnStop.Dispatcher.Invoke(new Action(() => btnStop.IsEnabled = false));
        }

        #endregion
        #region opt

        private void btnStartOpt_Click(object sender, RoutedEventArgs e)
        {
            btnStartOpt.IsEnabled = false;

            int generations;
            if (!int.TryParse(textBoxGenerations.Text, out generations)) generations = 100;

            int queueSize;
            if (!int.TryParse(textBoxQueueSize.Text, out queueSize)) queueSize = 100;

            InitChart(generations);

            sim = new MatthiasToolbox.SimOptExample.Model.Simulation(logEvents: false, seed: 123, queueSize: queueSize);

            sac = new SimulatedAnnealingConfiguration();
            eac = new EvolutionaryAlgorithmConfiguration(123, generations, 10, 20);
            
            AnnealingAlgorithm.Initialize(sac);
            EvolutionaryAlgorithm.Initialize(eac);

            Problem = new Problem(sim);

            if (strategyEA)
            {
                EvolutionaryAlgorithm.BestSolutionChanged += new Optimization.BestSolutionChangedHandler(EvolutionaryAlgorithm_BestSolutionChanged);
                EvolutionaryAlgorithm.GenerationFinished += new Optimization.Strategies.Evolutionary.EvolutionaryAlgorithm.GenerationFinishedHandler(EvolutionaryAlgorithm_GenerationFinished);

                (new Thread(new ThreadStart(() => EvolutionaryAlgorithm.Solve(Problem)))).Start();
            }
            else
            {
                AnnealingAlgorithm.BestSolutionChanged += new Optimization.BestSolutionChangedHandler(AnnealingAlgorithm_BestSolutionChanged);

                (new Thread(new ThreadStart(() => AnnealingAlgorithm.Solve(Problem)))).Start();
            }
        }

        void EvolutionaryAlgorithm_GenerationFinished(object sender, GenerationFinishedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(UpdateChart));
            this.DoEvents();
        }


        void EvolutionaryAlgorithm_BestSolutionChanged(object sender, Optimization.Strategies.BestSolutionChangedEventArgs e)
        {
            Logging.Logger.Log<INFO>("There is a new hero in generation " + EvolutionaryAlgorithm.ProcessedGenerations);
            Logging.Logger.Log<INFO>("Machining Time: " + (-EvolutionaryAlgorithm.BestSolution.Fitness).ToTimeSpan().ToString());
            Logging.Logger.Log<INFO>("Configuration: " + e.NewValue.ToString());
            this.DoEvents();
        }

        void AnnealingAlgorithm_BestSolutionChanged(object sender, Optimization.Strategies.BestSolutionChangedEventArgs e)
        {
            Logging.Logger.Log<INFO>("There is a new hero at temperature " + AnnealingAlgorithm.CurrentTemperature);
            Logging.Logger.Log<INFO>("Machining Time: " + (-AnnealingAlgorithm.BestCandidate.Fitness).ToTimeSpan().ToString());
            Logging.Logger.Log<INFO>("Configuration: " + e.NewValue.ToString());
            this.DoEvents();
        }

        #endregion
        #region tab

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabControlMain.SelectedItem == tabItemSim)
            {
                Logging.Logger.Add(simLogger);
                Logging.Logger.Remove(optLogger);
            }
            else if (tabControlMain.SelectedItem == tabItemOpt)
            {
                Logging.Logger.Add(optLogger);
                Logging.Logger.Remove(simLogger);
            }
        }

        #endregion
        
        #endregion
        #region impl

        #region charting

        private void UpdateChart() 
        {
            double x = EvolutionaryAlgorithm.ProcessedGenerations;
            double y = (-EvolutionaryAlgorithm.BestSolution.Fitness).ToTimeSpan().TotalMinutes;
            optimizerSeries.Points.Add(new DataPoint(x, y));
            optimizerChart.Invalidate();
        }

        private void InitChart(int generations)
        {
            optimizerChart = new Chart();

            optimizerChartArea1 = new ChartArea("Machining Time");
            optimizerChart.ChartAreas.Add(optimizerChartArea1);

            optimizerChart.Legends.Add(new Legend("Generations"));
            optimizerChart.Legends[0].Docking = Docking.Top;
            optimizerChart.AntiAliasing = AntiAliasingStyles.All;
            optimizerChart.TextAntiAliasingQuality = TextAntiAliasingQuality.High;

            optimizerChartArea1.AxisX.Minimum = 0;
            optimizerChartArea1.AxisX.Maximum = generations;
            optimizerChartArea1.AxisY.Minimum = 0;
            optimizerChartArea1.AxisY.Maximum = 100;
            optimizerChartArea1.AxisY.Interval = 5;

            optimizerChartArea1.Area3DStyle.Enable3D = true;

            optimizerChartArea1.Area3DStyle.Rotation = 30;
            optimizerChartArea1.Area3DStyle.Inclination = 10;
            optimizerChartArea1.Area3DStyle.Perspective = 30;
            optimizerChartArea1.Area3DStyle.LightStyle = LightStyle.Realistic;

            optimizerChartArea1.Area3DStyle.IsClustered = false;
            optimizerChartArea1.Area3DStyle.IsRightAngleAxes = false;
            optimizerChartArea1.Area3DStyle.PointGapDepth = 100;
            optimizerChartArea1.Area3DStyle.PointDepth = 100;
            optimizerChartArea1.BackColor = System.Drawing.Color.White;

            optimizerSeries = new Series();
            optimizerChart.Series.Add(optimizerSeries);

            windowsFormsHost1.Child = optimizerChart;
        }

        #endregion

        #endregion
    }
}