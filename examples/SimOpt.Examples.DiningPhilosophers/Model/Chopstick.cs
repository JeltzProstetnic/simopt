using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Engine;

namespace SimOpt.Examples.DiningPhilosophers.Model
{
    /// <summary>
    /// The chopstick is the resource which has to be shared between philosophers. It is a very simple
    /// resource entity. The only property is an index number, which has to be parametrized in the constructor call.
    /// </summary>
    public class Chopstick : ResourceEntity
    {
        /// <summary>
        /// The index number for this instance.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Create an instance of the chopstick class.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="index"></param>
        public Chopstick(IModel model, int index) : base(model)
        {
            Index = index;
        }
    }
}
