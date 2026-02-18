using System;

namespace MatthiasToolbox.EmergencyDepartment.Interfaces
{
	/// <summary>
	/// Description of ITreatmentRoom.
	/// </summary>
	public interface ITreatmentRoom<T>
	{
		void TakeItem(T t);
		
		ITreatmentQueue<T> Queue {get;}
	}
}
