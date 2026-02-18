using System;

namespace MatthiasToolbox.EmergencyDepartment.Interfaces
{
	/// <summary>
	/// Description of ITreatmentQueue.
	/// </summary>
	public interface ITreatmentQueue<T>
	{
		T TakeItem();
		
		void AddItem(T t);
		
		bool Empty {get;}
		
		void OnReset();
		
		void Clear();
		
		int Count();
		
		void Sort();
	}
}
