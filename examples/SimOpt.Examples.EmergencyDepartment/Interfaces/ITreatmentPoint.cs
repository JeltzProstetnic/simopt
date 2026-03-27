using SimOpt.Simulation.Engine;

namespace SimOpt.Examples.EmergencyDepartment.Interfaces
{
    /// <summary>
    /// Description of ITreatmentPoint.
    /// </summary>
    public interface ITreatmentPoint<T> : IEntity
    {
        bool Busy { get; }

        void StartTreatment(T t);

        void StartTreatment();

        ITreatmentRoom<T> Room { set; }

        void PatientTreatmentHandler(ITreatmentPoint<T> sender, T t);

        void SetOldRoom(ITreatmentRoom<T> room);
    }
}
