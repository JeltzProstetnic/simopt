using System;
using System.Collections.Generic;

namespace SimOpt.Examples.EmergencyDepartment.Interfaces
{
    /// <summary>
    /// Description of IPatient.
    /// </summary>
    public interface IPatient : IComparable
    {
        DateTime Birth { get; set; }

        int Type { get; set; }

        int Cw { get; set; }

        int Idx { get; }

        List<string> Dest { set; }

        void SendToNextDestination();

        string Name { get; }
    }
}
