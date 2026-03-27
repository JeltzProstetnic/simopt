using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.ParticleSwarm
{
    /// <summary>
    /// Placeholder for a swarming-based optimization algorithm. Not yet implemented.
    /// </summary>
    [Obsolete("Not implemented. PSO support is planned for a future release.")]
    public class SwarmingAlgorithm : IStrategy
    {
        #region IStrategy Member

        /// <inheritdoc />
        public event BestSolutionChangedHandler BestSolutionChanged = null!;

        /// <inheritdoc />
        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc />
        public string ProcessingStatus
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc />
        public bool IsInitialized
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc />
        public bool Initialize(IConfiguration parameters)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Reset()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Tune(IEnumerable<IProblem> representatives, IStrategy tuningStrategy)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<ISolution> Solve(IProblem problem)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
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
