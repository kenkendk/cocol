using System;
using System.Linq;
using System.Collections.Generic;

namespace CoCoL
{
	/// <summary>
	/// Expiration manager, keeps track of all channels with pending expirations
	/// </summary>
	public static class ExpirationManager
	{
		/// <summary>
		/// The implementation of an expiration manager, for easy replacement
		/// </summary>
		private static ExpirationManagerImpl _ex = new ExpirationManagerImpl();

		/// <summary>
		/// Registers a method for callback
		/// </summary>
		/// <param name="expires">The time when the expiration occurs</param>
		/// <param name="callback">The method to call on expiration</param>
		public static void AddExpirationCallback(DateTime expires, Action callback) 
		{ 
			_ex.AddExpirationCallback(expires, callback); 
		}

		/// <summary>
		/// Implementation of an expiration manager
		/// </summary>
		private class ExpirationManagerImpl
		{
			/// <summary>
			/// The lock that provides exclusive access to the lookup table
			/// </summary>
			private object m_lock = new object();
			/// <summary>
			/// The time at which the timer should trigger next
			/// </summary>
			private DateTime m_nextInvoke = DateTime.Now + TimeSpan.FromDays(30);
			/// <summary>
			/// The table of registered callbacks for timeouts
			/// </summary>
			private Dictionary<Action, DateTime> m_expiryTable = new Dictionary<Action, DateTime>();
			/// <summary>
			/// The timer that performs the signalling
			/// </summary>
			private System.Threading.Timer m_timer;

			/// <summary>
			/// Registers a method for callback
			/// </summary>
			/// <param name="expires">The time when the expiration occurs</param>
			/// <param name="callback">The method to call on expiration</param>
			public void AddExpirationCallback(DateTime expires, Action callback)
			{
				lock (m_lock)
				{
					// If the callback is already registered,
					// set the shortest interval, otherwise register th callback
					DateTime prev;
					if (m_expiryTable.TryGetValue(callback, out prev))
					{
						if (expires < prev)
						{
							m_expiryTable[callback] = expires;
							if (expires < m_nextInvoke)
								RescheduleTimer(expires);
						}
					}
					else
					{
						m_expiryTable.Add(callback, expires);
						if (expires < m_nextInvoke)
							RescheduleTimer(expires);
					}
				}
			}

			/// <summary>
			/// Reschedules the timer.
			/// </summary>
			/// <param name="next">The time when the next tick should occur</param>
			private void RescheduleTimer(DateTime next)
			{
				lock (m_lock)
				{
					m_nextInvoke = next;

					var duration = DateTime.Now - m_nextInvoke;
					if (duration.Ticks <= 0)
						RunTimer(null);
					else
					{
						if (m_timer == null)
							m_timer = new System.Threading.Timer(RunTimer, null, duration.Ticks / TimeSpan.TicksPerMillisecond, System.Threading.Timeout.Infinite);
						else
							m_timer.Change(duration.Ticks / TimeSpan.TicksPerMillisecond, System.Threading.Timeout.Infinite);
					}
				}
			}


			/// <summary>
			/// Callback method for handling a timer event
			/// </summary>
			/// <param name="dummy">Unused parameter for matching the callback signature</param>
			private void RunTimer(object dummy)
			{
				KeyValuePair<Action, DateTime>[] expires;
				lock (m_lock)
				{
					var now = DateTime.Now;
					expires = (from n in m_expiryTable
					              where n.Value < now
					              orderby n.Value
								  select n).ToArray();

					foreach (var x in expires)
						m_expiryTable.Remove(x.Key);

					// TODO: Can we dispose the timer in a callback?
					if (m_expiryTable.Count == 0)
						m_timer.Dispose();
					else
						RescheduleTimer(m_expiryTable.OrderBy(x => x.Value).First().Value);
				}

				foreach (var x in expires)
					ThreadPool.QueueItem(x.Key);
			}
		}
	}
}

