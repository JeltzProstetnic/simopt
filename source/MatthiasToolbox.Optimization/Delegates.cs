using MatthiasToolbox.Optimization.Strategies;

namespace MatthiasToolbox.Optimization
{
    /// <summary>
    /// Handles an event indicating that an IStrategy has found a new best solution.
    /// </summary>
    /// <param name="sender">The currently running IStrategy</param>
    /// <param name="e">The event arguments</param>
    public delegate void BestSolutionChangedHandler(object sender, BestSolutionChangedEventArgs e);
}