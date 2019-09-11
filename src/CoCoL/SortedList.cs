using System;
using System.Collections.Generic;

namespace CoCoL
{
	/// <summary>
	/// Simple implementation of a sorted list with operations in log(n) time
	/// </summary>
	internal class SortedList<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
		where TKey : IComparable<TKey>
	{
		/// <summary>
		/// The internal list, which is maintained sorted
		/// </summary>
		private readonly List<KeyValuePair<TKey, TValue>> m_list = new List<KeyValuePair<TKey, TValue>>();

		/// <summary>
		/// Find the index of the specified key, or bitwise inverted value if not found.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns>The index of the key in the list, or the bitwise inverse of the closest match</returns>
		private int Find(TKey key)
		{
			// TKey can be a struct, in which case this statement does nothing.
			// But we need it for reference types to avoid a null-reference
			// exception when comparing keys
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			if (m_list.Count == 0)
				return -1;

			// Initial range is the full list
			var ub = m_list.Count - 1;
			var lb = 0;

			while(true)
			{
				// Take the element in the middle of the range
				var ix = (((ub + 1) - lb) / 2) + lb;

				// Grab the list key for the middle element
				var el = m_list[ix].Key;
				var rel = key.CompareTo(el);
				if (rel == 0)
					return ix;

				// If there are no more elements to check, exit
				if (ub - lb <= 0)
				{
					if (rel > 0)
						return ~(ix + 1);
					else
						return ~(ix);
				}

				// Make range half, minus the currently tested element
				if (rel > 0)
					lb = Math.Min(m_list.Count - 1, ix + 1);
				else
					ub = Math.Max(0, ix - 1);
			}
		}

		/// <summary>
		/// Adds the specified key and value to the list
		/// </summary>
		/// <param name="key">The key to add.</param>
		/// <param name="value">The value to add.</param>
		public void Add(TKey key, TValue value)
		{
			var ix = Find(key);
			if (ix >= 0)
				throw new ArgumentException(string.Format("Duplicate key: {0}", key));

			m_list.Insert(~ix, new KeyValuePair<TKey, TValue>(key, value));
		}

		/// <summary>
		/// Removes the item with the given key
		/// </summary>
		/// <param name="key">The key to remove.</param>
		/// <returns><c>True</c> if the entry with the given key was removed, false otherwise</returns>
		public bool Remove(TKey key)
		{
			var ix = Find(key);
			if (ix < 0)
				return false;

			m_list.RemoveAt(ix);
			return true;
		}

		/// <summary>
		/// Gets the value for the specified key, if it exists
		/// </summary>
		/// <returns><c>true</c>, if get value was found, <c>false</c> otherwise.</returns>
		/// <param name="key">The key to look for.</param>
		/// <param name="value">The matching value.</param>
		public bool TryGetValue(TKey key, out TValue value)
		{
			var ix = Find(key);
			if (ix < 0)
			{
				value = default(TValue);
				return false;
			}
			else
			{
				value = m_list[ix].Value;
				return true;
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="CoCoL.SortedList&lt;Tkey, TValue&gt;"/> at the specified index.
		/// </summary>
		/// <param name="index">Index.</param>
		public TValue this[TKey index]
		{
			get
			{
				var ix = Find(index);
				if (ix < 0)
					throw new KeyNotFoundException(string.Format("Key not found: {0}", index));

				return m_list[ix].Value;
			}
			set
			{
				var ix = Find(index);
				if (ix >= 0)
					m_list[ix] = new KeyValuePair<TKey, TValue>(index, value);
				else
					m_list.Insert(~ix, new KeyValuePair<TKey, TValue>(index, value));
			}
		}

		/// <summary>
		/// Gets the first entry in the list.
		/// </summary>
		public KeyValuePair<TKey, TValue> First { get { return m_list[0]; } }

		/// <summary>
		/// Removes the item at the specified index
		/// </summary>
		/// <param name="index">Index.</param>
		public void RemoveAt(int index)
		{
			m_list.RemoveAt(index);
		}

		/// <summary>
		/// Gets the number of elements in the list.
		/// </summary>
		public int Count { get { return m_list.Count; } }

		#region IEnumerable implementation

		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return m_list.GetEnumerator();
		}

		#endregion

		#region IEnumerable implementation

		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		/// <returns>The enumerator.</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		#endregion
	}
}

