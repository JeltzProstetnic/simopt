using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.ParticleSwarm
{
    public class SwarmingAlgorithm : IStrategy
    {
        #region IStrategy Member

        public event BestSolutionChangedHandler BestSolutionChanged;

        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        public string ProcessingStatus
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsInitialized
        {
            get { throw new NotImplementedException(); }
        }

        public bool Initialize(IConfiguration parameters)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public bool Tune(IEnumerable<IProblem> representatives, IStrategy tuningStrategy)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ISolution> Solve(IProblem problem)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        #endregion

        protected virtual void OnBestSolutionChanged(BestSolutionChangedEventArgs e)
        {
            if (BestSolutionChanged != null) BestSolutionChanged.Invoke(this, e);
        }
    }
}
