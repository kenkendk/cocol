using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace CoCoL
{
	/// <summary>
	/// Expiration manager, keeps track of all channels with pending expirations
	/// </summary>
	public static class ExpirationManager
	{
		/// <summary>
		/// The maximum number of ticks we allow the events to be scheduled in advance of the desired expiry time.
		/// Setting this to zero gives more accurate channel timeouts in exchange for a few repeated calls
		/// </summary>
		public static readonly long ALLOWED_ADVANCE_EXPIRE_TICKS = TimeSpan.FromMilliseconds(1).Ticks;
		/// <summary>
		/// The minimum number of ticks we allow time to wait, this reduces repeated re-schedules
		/// if the Task.Delay() calls arrive in advance.
		/// This usually only has an effect when ALLOWED_ADVANCE_EXPIRE_TICKS is less than 10000.
		/// 
		/// If ALLOWED_ADVANCE_EXPIRE_TICKS and MIN_ALLOWED_WAIT are set to zero,
		/// expiration events have less extra delay, but occasionally cause extra CPU load
		/// </summary>
		public static readonly long MIN_ALLOWED_WAIT = TimeSpan.FromMilliseconds(1).Ticks;

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
			/// The token that controls the timer
			/// </summary>
			private CancellationTokenSource m_timerToken;
			/// <summary>
			/// The task that represents the running timer
			/// </summary>
			private Task m_timerTask;

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
					// set the shortest interval, otherwise register the callback
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
						var empty = m_expiryTable.Count == 0;
						m_expiryTable.Add(callback, expires);

						if (expires < m_nextInvoke || empty)
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

					var duration = (m_nextInvoke - DateTime.Now).Ticks;
					if (duration <= 0)
						RunTimer(null);
					else
					{
						if (m_timerToken != null && !m_timerToken.IsCancellationRequested)
							m_timerToken.Cancel();

						(m_timerTask = Task.Delay(TimeSpan.FromTicks(Math.Max(duration, MIN_ALLOWED_WAIT)), (m_timerToken = new CancellationTokenSource()).Token))
							.ContinueWith(RunTimer, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
					}
				}
			}


			/// <summary>
			/// Callback method for handling a timer event
			/// </summary>
			/// <param name="dummy">Unused parameter for matching the callback signature</param>
			private void RunTimer(Task task)
			{
				var expires = new List<Action>();

				lock (m_lock)
				{
					// Advoid races with multiple invokes
					if (m_timerTask != task)
						return;

					m_timerTask = null;
					if (m_timerToken != null)
					{
						m_timerToken.Dispose();
						m_timerToken = null;
					}

					var now = DateTime.Now;

					var sorted = 
						from n in m_expiryTable
						orderby n.Value
						select n;

					var fir = sorted.First().Value;

					var next = new DateTime(0);;

					foreach (var e in sorted)
					{
						// If ALLOWED_ADVANCE_EXPIRE_TICKS == 0, there can be a few
						// extra calls to RunTimer(), as it is repeatedly
						// rescheduled
						if ((e.Value - now).Ticks >= ALLOWED_ADVANCE_EXPIRE_TICKS)
						{
							next = e.Value;
							break;
						}

						m_expiryTable.Remove(e.Key);
						expires.Add(e.Key);
					}

					#if !PCL_BUILD
					if (expires.Count == 0)
						Console.WriteLine("Jitter ticks: {0} - next: {1}, items: {2}", (fir - now).Ticks, (next - now).Ticks, expires.Count);
					//else
					//	Console.WriteLine("Trail ticks: {0}, next: {1}, items: {2}", (fir - now).Ticks, (next - now).Ticks, expires.Count);
					#endif

					if (m_expiryTable.Count != 0 && now.Ticks != 0)
						RescheduleTimer(next);
				}

				foreach (var x in expires)
					ThreadPool.QueueItem(x);
			}
		}
	}
}

