using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// A helper for ensuring we only offer to a single recipient
	/// </summary>
	public class SingleOffer<T> : ITwoPhaseOffer
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
		/// The completion source to signal on completio
		/// </summary>
		private readonly TaskCompletionSource<T> m_tcs;
		/// <summary>
		/// The time the offer expires
		/// </summary>
		private readonly DateTime m_timeout;
		/// <summary>
		/// A value indicating if the request is the first to set the TaskCompletionSource
		/// </summary>
		private bool m_isFirst = true;

		/// <summary>
		/// Creates a new SingleOffer instance
		/// </summary>
		/// <param name="tcs">The task completion source</param>
		/// <param name="timeout">The timeout value</param>
		public SingleOffer(TaskCompletionSource<T> tcs, DateTime timeout) 
		{
			m_tcs = tcs;
			m_timeout = timeout;
		}

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
			// We can never be un-taken
			if (m_taken || !m_isFirst)
				return false;

			// Atomic access
			Monitor.Enter(m_lock);

			if (m_taken || !m_isFirst)
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

		/// <summary>
		/// Gets a value indicating if this call is the first to set the TaskCompletionSource,
		/// and atomically updates the flag to return false to all subsequent callers
		/// </summary>
		/// <returns><c>true</c>, if the call is the first, <c>false</c> otherwise.</returns>
		public bool AtomicIsFirst()
		{
			lock (m_lock)
			{
				var r = m_isFirst;
				m_isFirst = false;
				return r;
			}
		}

		/// <summary>
		/// Callback method that activates expiration
		/// </summary>
		private void ExpirationCallback()
		{
			if (AtomicIsFirst())
				m_tcs.SetException(new TimeoutException());
		}

		/// <summary>
		/// The caller indicates that the offer is now registered in all places
		/// </summary>
		public void ProbePhaseComplete()
		{
			// If there is no timeout, do nothing
			// If we are already completed, do nothing
			if (m_timeout == Timeout.InfiniteDateTime || IsTaken)
				return;

			// If the timeout has occurred, set the timeout
			else if (m_timeout < DateTime.Now)
				ExpirationCallback();

			// Register the timeout callback
			else
				ExpirationManager.AddExpirationCallback(m_timeout, ExpirationCallback);
		}
	}
}

