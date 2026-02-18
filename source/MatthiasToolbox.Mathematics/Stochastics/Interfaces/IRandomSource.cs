using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MatthiasToolbox.Mathematics.Stochastics.Interfaces
{
    /// <summary>
    /// a source for uniform distributed random numbers
    /// </summary>
    public interface IRandomSource// : ISerializableGrubi
    {
        bool Antithetic { get; }
        int Seed { get; }
        string Name { get; }
        bool Initialized { get; }
        
        /// <summary>
        /// Generate a random number on the interval [0,int.MaxValue)
        /// </summary>
        /// <returns>A random number on the interval [0,int.MaxValue)</returns>
        int NextInteger();

        /// <summary>
        /// Generate a random number on the interval [0,1)
        /// </summary>
        /// <returns>A random number on the interval [0,1)</returns>
        double NextDouble();
        
        void Initialize();
        void Initialize(int seed);
        void Initialize(int seed, bool antithetic);
        void Initialize(bool antithetic);
        
        void Reset();
        void Reset(int seed);
        void Reset(int seed, bool antithetic);
        void Reset(bool antithetic);
    }
}
