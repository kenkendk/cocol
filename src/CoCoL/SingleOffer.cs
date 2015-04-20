using System;
using System.Threading;

namespace CoCoL
{
	/// <summary>
	/// A helper for ensuring we only offer to a single recipient
	/// </summary>
	public class SingleOffer : ITwoPhaseOffer
	{
		/// <summary>
		/// True if this item is already taken, false otherwise
		/// </summary>
		private bool m_taken = false;
		/// <summary>
		/// The lock for providing exclusive access
		/// </summary>
		private object m_lock = new object();
		/// <summary>
		/// An optional callback for reporting if the offer was taken
		/// </summary>
		private Action<object> m_commitCallback = null;

		/// <summary>
		/// Register a callback method, which is invoked if the two-phase sequence is committed
		/// </summary>
		/// <param name="cb">The callback method.</param>
		public void SetCommitCallback(Action<object> cb) 
		{
			m_commitCallback = cb;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is taken.
		/// </summary>
		/// <value><c>true</c> if this instance is taken; otherwise, <c>false</c>.</value>
		public bool IsTaken { get { return m_taken; } }

		/// <summary>
		/// Starts the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		public bool Offer(object caller)
		{
			// We can new be un-taken
			if (m_taken)
				return false;

			// Atomic access
			Monitor.Enter(m_lock);

			if (m_taken)
			{
				// We do not offer, so release the lock
				Monitor.Exit(m_lock);
				return false;
			}
			else
				return true;
		}

		/// <summary>
		/// Commits the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		public void Commit(object caller)
		{
			System.Diagnostics.Debug.Assert(m_taken == false, "Item was taken before commit");

			m_taken = true;
			if (m_commitCallback != null)
				m_commitCallback(caller);

			Monitor.Exit(m_lock);
		}

		/// <summary>
		/// Cancels the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		public void Withdraw(object caller)
		{
			System.Diagnostics.Debug.Assert(m_taken == false, "Item was taken before commit");

			Monitor.Exit(m_lock);
		}
	}
}

