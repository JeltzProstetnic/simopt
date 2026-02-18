using System;
using System.Windows;
using MatthiasToolbox.DiningPhilosophers.Model;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Logging.Loggers;
using System.Threading;
using System.IO;

namespace MatthiasToolbox.DiningPhilosophers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MatthiasToolbox.DiningPhilosophers.Model.Simulation sim;

        public MainWindow()
        {
            InitializeComponent();
            checkBoxLog.IsChecked = true;
            Logger.Add(new WPFRichTextBoxLogger(richTextBoxLog));

            this.Log<INFO>("Initializing simulation.");

            sim = new MatthiasToolbox.DiningPhilosophers.Model.Simulation(richTextBoxLog, (bool)checkBoxLog.IsChecked);
            sim.Model.SimulationTerminating += Finished;

            this.Log<INFO>("Initialization finished.");
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (sim.Model.IsPaused)
            {
                (new Thread(new ThreadStart(() => sim.Model.Continue()))).Start();
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;
                btnPause.IsEnabled = true;
                btnSave.IsEnabled = false;
            }
            else if (!sim.Model.IsRunning)
            {
                sim.Model.LogEvents = (bool)checkBoxLog.IsChecked;
                if (!sim.Model.IsReset) sim.Model.Reset();
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;
                btnPause.IsEnabled = true;
                btnSave.IsEnabled = false;
                (new Thread(new ThreadStart(() => sim.Run()))).Start();
            }
        }

        private void Finished() // IModel model, IEventInstance lastEvent)
        {
            this.Log<INFO>("GUI finished.\r");

            // the following line causes the application to wait here, 
            // until the last log message is written. Note though, that
            // the stop button will usually do nothing, because until 
            // you manage to click it, the actual simulation is already 
            // finished since aeons. for the stop button to meet a 
            // model which is still running, you must set a very long 
            // simulation duration. to see the effect, you must comment
            // the following line.
            // Logger.Dispatch(); // in this model the dispatch is called inside the simulation, forcing synchronization.

            btnStart.Dispatcher.Invoke(new Action(() => btnStart.IsEnabled = true));
            btnStop.Dispatcher.Invoke(new Action(() => btnStop.IsEnabled = false));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Shutdown(true); // should be moved to the App.xaml.cs
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            sim.Model.Stop();
            Logger.ClearMessageQueue();
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnPause.IsEnabled = false;
            btnSave.IsEnabled = true;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            sim.Model.Save();
            // sim.Model.Resume();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            sim.Model.Pause();
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnPause.IsEnabled = false;
            btnSave.IsEnabled = true;
        }
    }
}