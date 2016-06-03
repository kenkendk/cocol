using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CoCoL
{
	/// <summary>
	/// A helper for ensuring we only offer to a single recipient
	/// </summary>
	public class SingleOffer<T> : ITwoPhaseOffer
	{
		/// <summary>
		/// Workaround to use System.Thread.Interlocked.Increment,
		/// which does not work on booleans
		/// </summary>
		private const int TRUE = 1;

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
		private int m_isFirst = TRUE;
		/// <summary>
		/// Keeping track of the lock state
		/// </summary>
		private bool m_isLocked = false;
		/// <summary>
		/// The list of offers
		/// </summary>
		private Queue<TaskCompletionSource<bool>> m_offers = new Queue<TaskCompletionSource<bool>>();

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
		/// <returns>The awaitable result.</returns>
		public Task<bool> OfferAsync(object caller)
		{
			if (m_taken || m_isFirst != TRUE)
				return Task.FromResult(false);
			
			lock (m_lock)
				if (m_isLocked)
				{
					var tcs = new TaskCompletionSource<bool>();
					m_offers.Enqueue(tcs);
					return tcs.Task;
				}
				else
				{
					System.Diagnostics.Debug.Assert(m_offers.Count == 0, "Two-Phase instance was unlocked but with pending offers?");

					m_isLocked = true;
					return Task.FromResult(true);
				}
		}

		/// <summary>
		/// Commits the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		public Task CommitAsync(object caller)
		{
			System.Diagnostics.Debug.Assert(m_taken == false, "Item was taken before commit");

			m_taken = true;
			if (m_commitCallback != null)
				m_commitCallback(caller);

			lock (m_lock)
			{
				m_isLocked = false;
				while (m_offers.Count > 0)
				{
					var offer = m_offers.Dequeue();
					ThreadPool.QueueItem(() => offer.TrySetResult(false));
				}
			}

			return Task.FromResult(true);
		}

		/// <summary>
		/// Cancels the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		public Task WithdrawAsync(object caller)
		{
			System.Diagnostics.Debug.Assert(m_taken == false, "Item was taken before withdraw");

			lock (m_lock)
			{
				if (m_offers.Count > 0)
				{
					var offer = m_offers.Dequeue();
					ThreadPool.QueueItem(() => offer.TrySetResult(true));
				}
				else
					m_isLocked = false;
			}
			return Task.FromResult(true);
		}

		/// <summary>
		/// Gets a value indicating if this call is the first to set the TaskCompletionSource,
		/// and atomically updates the flag to return false to all subsequent callers
		/// </summary>
		/// <returns><c>true</c>, if the call is the first, <c>false</c> otherwise.</returns>
		public bool AtomicIsFirst()
		{
			return System.Threading.Interlocked.Exchange(ref m_isFirst, 0) == TRUE;
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

