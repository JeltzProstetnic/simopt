using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Exceptions;

namespace SimOpt.Basics.Datastructures.StateMachine
{
	/// <summary>
	/// A straight forward implementation of IState
	/// </summary>
	/// <remarks>release</remarks>
    [Serializable]
	public class State : IState
	{
		#region over
		
		#region Equals and GetHashCode implementation
		
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked {
				hashCode += 1000000007 * id.GetHashCode();
				hashCode += 1000000009 * idSet.GetHashCode();
				if (name != null)
					hashCode += 1000000021 * name.GetHashCode();
				hashCode += 1000000033 * nameSet.GetHashCode();
				if (targetStates != null)
					hashCode += 1000000087 * targetStates.GetHashCode();
			}
			return hashCode;
		}

		public override bool Equals(object obj)
		{
			State other = obj as State;
			if (other == null) return false;
			return this.id == other.id && this.idSet == other.idSet && this.name == other.name && this.nameSet == other.nameSet && object.Equals(this.targetStates, other.targetStates);
		}

		public static bool operator ==(State lhs, State rhs)
		{
			if (ReferenceEquals(lhs, rhs))
				return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
				return false;
			return lhs.Equals(rhs);
		}

		public static bool operator !=(State lhs, State rhs)
		{
			return !(lhs == rhs);
		}
		
		#endregion
		
		#endregion
		#region cvar
		
		private int id;
		private bool idSet = false;
		private string name;
		private bool nameSet = false;
		private List<IState> targetStates;
		
		#endregion
		#region prop
		
		/// <summary>
		/// unique name of this state
		/// </summary>
		public string Name {
			get { return name; }
			set
			{
				if (nameSet) throw new ValueAlreadySetException("Name");
				name = value;
				nameSet = true;
			}
		}
		
		/// <summary>
		/// unique id of this state
		/// </summary>
		public int ID {
			get { return id; }
			set
			{
				if (idSet) throw new ValueAlreadySetException("ID");
				id = value;
				idSet = true;
			}
		}
		
		#endregion
		#region ctor

		/// <summary>
		/// Creates a new instance without id or name. Caution:
		/// BOTH(!) have to be set before the state can be used!
		/// </summary>
		public State() { targetStates = new List<IState>(); }

		/// <summary>
		/// Provide a unique id for every state. The name
		/// of this instance will be "State n" where n is the id.
		/// </summary>
		/// <param name="id"></param>
		public State(int id) : this("State "  + id.ToString(), id) { }

		/// <summary>
		/// Provide a unique name and id for every state
		/// </summary>
		/// <param name="name"></param>
		public State(String name, int id) {
			this.id = id;
			this.idSet = true;
			this.name = name;
			this.nameSet = true;
			targetStates = new List<IState>();
		}
		
		#endregion
		#region impl
		
		/// <summary>
		/// Find out if a certain transition is allowed.
		/// </summary>
		/// <param name="toState"></param>
		/// <returns>true if a transition to toState is allowed</returns>
		public bool TransitionAllowed(IState toState) {
			return targetStates.Contains(toState);
		}
		
		/// <summary>
		/// add target state to the allowed targets list
		/// </summary>
		/// <param name="target"></param>
		/// <returns>true if the state was new</returns>
		public bool AllowTransition(IState toState) {
			if(targetStates.Contains(toState)) return false;
			targetStates.Add(toState);
			return true;
		}
		
		/// <summary>
		/// remove target state from the allowed targets list
		/// </summary>
		/// <param name="target"></param>
		/// <returns>true if the state was existent</returns>
		public bool ForbidTransition(IState toState) {
			return targetStates.Remove(toState);
		}

		/// <summary>
		/// Compares two states according to their ID and Name.
		/// States with the same name and id will be considered
		/// equal, even though they might be from different state
		/// machines and contain different transitions!
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(IState other)
		{
			return id.Equals(other.ID) && name.Equals(other.Name);
		}

		#endregion
	}
}