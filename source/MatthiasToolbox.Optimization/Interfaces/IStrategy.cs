using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Optimization.Interfaces
{
	/// <summary>
	/// The strategy interface prescribes a "Name" property, a boolean "IsInitialized" property and a string "ProcessingStatus".
	/// Furthermore an event "BestSolutionChanged" is prescribed to notify the controlling application of the current progress.
	/// Methods for initialization and resetting of the strategy instance are also prescribed. If the configuration class for a
	/// strategy actually implements the ISolution interface, the "Tune" method can be used to tune the strategy with a selected
	/// set of candidates, using any other strategy including the strategy to be tuned itself. 
	/// Finally, the "Solve" method is prescribed. It takes a problem instance as parameter and returns a number of solutions 
	/// when the stopping criteria are met or the "Stop" method is called.
	/// </summary>
	public interface IStrategy
	{
		event BestSolutionChangedHandler BestSolutionChanged;

		string Name { get; }
		string ProcessingStatus { get; }
		bool IsInitialized { get; }

		bool Initialize(IConfiguration parameters);
		void Reset();

		bool Tune(IEnumerable<IProblem> representatives, IStrategy tuningStrategy);
		IEnumerable<ISolution> Solve(IProblem problem);
		void Stop();
	}
}
