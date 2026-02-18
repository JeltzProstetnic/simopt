using System;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

using SimOpt.Simulation.Engine;
using System.Collections.Generic;
using SimOpt.Simulation.Enum;

namespace SimOpt.Simulation.Tools
{
    [Serializable]
    public class AsyncModelRunner
    {
        #region cvar

        private object lockObject = new object();
        private BackgroundWorker worker;

        #region settings
        
        private double preferredSpeed = 1d;
        private double synchronizationIntervalMS = 50;
        private TimeSpan synchronizationInterval = new TimeSpan(0, 0, 0, 0, 50);

        #endregion
        #region statistics

        // total averages
        private double totalTimeTotal = 0;
        private double syncTimeTotal = 0;
        private double simTimeTotal = 0;
        private double speedTotal = 0d;
        private double measurementCount = 1d;

        // windowed averages
        private int statisticsWindowSize = 10;
        private List<double> totalTimeSamples = new List<double>();
        private List<double> syncTimeSamples = new List<double>();
        private List<double> simTimeSamples = new List<double>();
        private List<double> speedSamples = new List<double>();

        #endregion
        #region control

        private bool manualStopRequested = false;
        private Func<bool> BreakCondition;

        #endregion

        #endregion
        #region dele

        public delegate void ModelStepDelegate(AsyncModelRunner sender, IModel model);
        public delegate void ModelFinishDelegate(AsyncModelRunner sender, IModel model, RunWorkerCompletedEventArgs e);

        #endregion
        #region evnt

        public event ModelStepDelegate Synchronize;
        public event ModelFinishDelegate SimulationFinished;

        #endregion
        #region prop

        #region main

        public Model Model { get; set; }

        public bool IsRunning { get; private set; }

        /// <summary>
        /// This may not be equal to Model.CurrentState. If the Model state is TimeElapsed (which
        /// it is after every step) this will still return Running.
        /// </summary>
        public ExecutionState CurrentState 
        { 
            get 
            {
                if (IsRunning) return ExecutionState.Running;
                return Model.CurrentState;
            } 
        }

        public double PreferredSpeed
        {
            get { return preferredSpeed; }
            set
            {
                preferredSpeed = value;
                StepSizeMS = (double)SynchronizationIntervalMS * value;
            }
        }

        #endregion
        #region control

        private bool IsStopRequested
        {
            get
            {
                if (manualStopRequested) return true;
                else return BreakCondition.Invoke();
            }
        }

        /// <summary>
        /// Milliseconds
        /// </summary>
        public double StepSizeMS { get; private set; }

        public TimeSpan StepSize { get { return TimeSpan.FromMilliseconds(StepSizeMS); } }
        
        public double EndingTime { get; private set; }

        public double ActualSpeed { get; set; }

        /// <summary>
        /// In milliseconds.
        /// </summary>
        public double SynchronizationIntervalMS
        {
            get { return synchronizationIntervalMS;  }
            set {
                synchronizationIntervalMS = value;
                synchronizationInterval = TimeSpan.FromMilliseconds(value);
                StepSizeMS = value * preferredSpeed;
            }
        }

        public TimeSpan SynchronizationInterval
        {
            get
            {
                return synchronizationInterval;
            }
            set 
            {
                synchronizationInterval = value;
                synchronizationIntervalMS = value.TotalMilliseconds;
                StepSizeMS = value.TotalMilliseconds * preferredSpeed;
            }
        }

        #endregion
        #region statistics

        #region total

        public double AverageSpeedSinceReset { get { return speedTotal / measurementCount; } } // 

        public double AverageStepSizeSinceReset { get { return totalTimeTotal / measurementCount; } }

        public double AverageSyncTimeSinceReset { get { return syncTimeTotal / measurementCount; } }

        public double AverageSimTimeSinceReset { get { return simTimeTotal / measurementCount; } }

        public double FractionOfTimeForModelSinceReset { get { return AverageSimTimeSinceReset / AverageStepSizeSinceReset; } } // 

        public double FractionOfTimeForSyncSinceReset { get { return AverageSyncTimeSinceReset / AverageStepSizeSinceReset; } } // 

        #endregion
        #region windowed

        /// <summary>
        /// Number of measurements on which to base speed statistics.
        /// </summary>
        public int StatisticsWindowSize
        {
            get { return statisticsWindowSize; }
            set { statisticsWindowSize = value; }
        }

        /// <summary>
        /// 0..oo
        /// </summary>
        public double AverageSpeedInWindow
        {
            get
            {
                return speedSamples.Count > 0 ? speedSamples.Sum() / speedSamples.Count : 0d;
            }
        }

        /// <summary>
        /// milliseconds
        /// </summary>
        public double AverageStepSizeInWindow
        {
            get
            {
                return totalTimeSamples.Count > 0 ? totalTimeSamples.Sum() / totalTimeSamples.Count : 0d;
            }
        }

        /// <summary>
        /// milliseconds
        /// </summary>
        public double AverageSyncTimeInWindow
        {
            get
            {
                return syncTimeSamples.Count > 0 ? syncTimeSamples.Sum() / syncTimeSamples.Count : 0d;
            }
        }

        /// <summary>
        /// milliseconds
        /// </summary>
        public double AverageSimTimeInWindow
        {
            get
            {
                return simTimeSamples.Count > 0 ? simTimeSamples.Sum() / simTimeSamples.Count : 0d;
            }
        }

        /// <summary>
        /// 0..1
        /// </summary>
        public double FractionOfTimeForModelInWindow
        {
            get
            {
                return AverageStepSizeInWindow > 0 ? AverageSimTimeInWindow / AverageStepSizeInWindow : 0d;
            }
        }

        /// <summary>
        /// 0..1
        /// </summary>
        public double FractionOfTimeForSyncInWindow
        {
            get
            {
                return AverageStepSizeInWindow > 0 ? AverageSyncTimeInWindow / AverageStepSizeInWindow : 0d;
            }
        }

        #endregion

        #endregion

        #endregion
        #region ctor

        public AsyncModelRunner()
        {
            BreakCondition = () => false;
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }

        public AsyncModelRunner(Model model) : this()
        {
            this.Model = model;
        }

        #endregion
        #region impl

        public bool CanStart()
        {
            lock (lockObject)
            {
                if (IsRunning) return false;
                else return true;
            }
        }

        public void Stop() { lock (lockObject) Model.Stop(); }

        public void Pause() { lock (lockObject) Model.Pause(); }

        public void Interrupt() { lock (lockObject) Model.Interrupt(); }

        [Obsolete("Use Stop() instead.")]
        public void RequestStop() 
        {
            lock (lockObject)
            {
                manualStopRequested = true;
            }
        }

        private bool PrepareStart()
        {
            lock (lockObject)
            {
                if (IsRunning) return false;
                manualStopRequested = false;
                IsRunning = true;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="synchronizationInterval">Interval for synchronization via events in milliseconds.</param>
        /// <returns></returns>
        public bool StartAsync()
        {
            if (!PrepareStart()) return false;
            DoStart();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endingTime"></param>
        /// <param name="preferredSpeed"></param>
        /// <param name="synchronizationInterval">Interval for synchronization via events in milliseconds.</param>
        /// <returns></returns>
        public bool StartAsync(double endingTime, double preferredSpeed = double.MaxValue, double synchronizationInterval = 50)
        {
            if (!PrepareStart()) return false;

            this.EndingTime = endingTime;
            this.PreferredSpeed = preferredSpeed;
            this.BreakCondition = EndingTimeExitCondition;
            this.SynchronizationIntervalMS = synchronizationInterval;

            DoStart();

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exitCondition"></param>
        /// <param name="preferredSpeed"></param>
        /// <param name="synchronizationInterval">Interval for synchronization via events in milliseconds.</param>
        /// <returns></returns>
        public bool StartAsync(Func<bool> exitCondition, double preferredSpeed = double.MaxValue, double synchronizationInterval = 50)
        {
            if (!PrepareStart()) return false;

            this.PreferredSpeed = preferredSpeed;
            this.BreakCondition = exitCondition;
            this.SynchronizationIntervalMS = synchronizationInterval;

            DoStart();

            return true;
        }

        private void DoStart()
        {
            worker.RunWorkerAsync();
        }

        #endregion
        #region work
        
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            #region init

            Stopwatch stopEach = new Stopwatch();
            Stopwatch stopBoth = new Stopwatch();

            TimeSpan modelExecutionTime;
            TimeSpan synchronizationTime;
            TimeSpan sleepTime;
            TimeSpan totalTime;

            speedTotal = 0d;
            double totalElapsed;
            measurementCount = 1d;

            #endregion

            while (!IsStopRequested)
            {
                stopBoth.Reset(); stopBoth.Start(); stopEach.Reset(); stopEach.Start();
                
                Model.Step(StepSize); // TODO: @andi exact stopping and reactions to changed model state should be ok. If yes, remove todo. thanx
                
                stopEach.Stop();
                modelExecutionTime = stopEach.Elapsed;
                stopEach.Reset(); stopEach.Start();
                
                OnSynchronization(Model);
                
                stopEach.Stop();
                synchronizationTime = stopEach.Elapsed;

                #region update stats I

                if (speedTotal != 0d) measurementCount++;
                syncTimeTotal += synchronizationTime.TotalMilliseconds;
                UpdateStatisticsList(syncTimeSamples, synchronizationTime.TotalMilliseconds);
                simTimeTotal += modelExecutionTime.TotalMilliseconds;
                UpdateStatisticsList(simTimeSamples, modelExecutionTime.TotalMilliseconds);

                #endregion
                #region sleep rest of time

                totalTime = stopBoth.Elapsed;

                if (totalTime < synchronizationInterval)
                {
                    sleepTime = synchronizationInterval - totalTime;
                    Thread.Sleep(sleepTime);
                }
                else
                {
                    sleepTime = TimeSpan.Zero;
                }

                #endregion
                #region update stats II

                stopBoth.Stop();

                totalElapsed = stopBoth.ElapsedMilliseconds;
                totalTimeTotal += totalElapsed;
                UpdateStatisticsList(totalTimeSamples, totalElapsed);

                ActualSpeed = StepSizeMS / totalElapsed;
                speedTotal += ActualSpeed;
                UpdateStatisticsList(speedSamples, ActualSpeed);

                #endregion

                if(Model.CurrentState != ExecutionState.TimeElapsed)
                    break;
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lock (lockObject)
            {
                manualStopRequested = false;
                IsRunning = false;
            }
            OnSimulationFinished(Model,e);
        }

        #endregion
        #region util

        public void ResetSpeedStatistics()
        {
            totalTimeTotal = 0d;
            syncTimeTotal = 0d;
            simTimeTotal = 0d;
            speedTotal = 0d;

            measurementCount = 1d;

            totalTimeSamples.Clear();
            syncTimeSamples.Clear();
            simTimeSamples.Clear();
            speedSamples.Clear();
        }

        private bool EndingTimeExitCondition()
        {
            return Model.CurrentTime >= EndingTime;
        }

        protected virtual void OnSynchronization(IModel model)
        {
            if (Synchronize != null) Synchronize.Invoke(this, model);
        }

        protected virtual void OnSimulationFinished(IModel model, RunWorkerCompletedEventArgs e)
        {
            if (SimulationFinished != null) SimulationFinished.Invoke(this, model,e);
        }

        private void UpdateStatisticsList<T>(List<T> list, T item)
        {
            list.Add(item);
            if (list.Count > statisticsWindowSize) list.RemoveAt(0);
        }

        #endregion
    }
}