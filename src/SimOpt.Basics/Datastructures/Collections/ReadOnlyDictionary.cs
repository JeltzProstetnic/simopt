/*
 * User: Matthias Gruber
 * Date: 2010-06-15
 * Time: 16:27
 * 
 * (C) Matthias Gruber
 */

using System;
using System.Collections.Generic;

namespace SimOpt.Basics.Datastructures.Collections
{
	public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable
	{
		#region over
		
		#region Equals and GetHashCode implementation
		
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked {
				if (_dict != null)
					hashCode += 1000000007 * _dict.GetHashCode();
			}
			return hashCode;
		}

		public override bool Equals(object obj)
		{
			ReadOnlyDictionary<TKey, TValue> other = obj as ReadOnlyDictionary<TKey, TValue>;
			if (other == null) return false;
			return object.Equals(this._dict, other._dict);
		}

		public static bool operator ==(ReadOnlyDictionary<TKey, TValue> lhs, ReadOnlyDictionary<TKey, TValue> rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
				return false;
			return lhs.Equals(rhs);
		}

		public static bool operator !=(ReadOnlyDictionary<TKey, TValue> lhs, ReadOnlyDictionary<TKey, TValue> rhs)
		{
			return !(lhs == rhs);
		}
		
		#endregion
		
		#endregion
		#region cvar
		
		internal IDictionary<TKey, TValue> _dict;
		
		#endregion
		#region prop
		
		public int Count
		{
			get { return _dict.Count; }
		}

		public bool IsReadOnly
		{
			get { return true; }
		}
		
		public ICollection<TKey> Keys
		{
			get { return _dict.Keys; }
		}
		
		public ICollection<TValue> Values
		{
			get { return _dict.Values; }
		}

		public TValue this[TKey key]
		{
			get { return _dict[key]; }
			set { throw new InvalidOperationException(); }
		}
		
		#endregion
		#region ctor

		public ReadOnlyDictionary(IDictionary<TKey, TValue> backingDict)
		{
			_dict = backingDict;
		}

		#endregion
		#region impl

		#region modify

		public void Add(TKey key, TValue value)
		{
			throw new InvalidOperationException();
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			throw new InvalidOperationException();
		}

		public bool Remove(TKey key)
		{
			throw new InvalidOperationException();
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			throw new InvalidOperationException();
		}
		
		public void Clear()
		{
			throw new InvalidOperationException();
		}
		
		#endregion
		#region other

		public bool TryGetValue(TKey key, out TValue value)
		{
			return _dict.TryGetValue(key, out value);
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return _dict.Contains(item);
		}

		public bool ContainsKey(TKey key)
		{
			return _dict.ContainsKey(key);
		}
		
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			_dict.CopyTo(array, arrayIndex);
		}
		
		#endregion
		#region IEnumerable

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return _dict.GetEnumerator();
		}

		System.Collections.IEnumerator
			System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)_dict).GetEnumerator();
		}

		#endregion
		
		#endregion
	}
}