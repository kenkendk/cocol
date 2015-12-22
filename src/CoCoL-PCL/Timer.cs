using System;
using System.Threading;
using System.Threading.Tasks;

#if PCL_BUILD
namespace CoCoL.PCL
{
	/// <summary>
	/// Implements a timer using the Task.Delay method
	/// </summary>
	internal sealed class Timer : CancellationTokenSource
	{
		/// <summary>
		/// The currently active task, used to prevent old task from firing
		/// </summary>
		private Task m_runner = null;
		/// <summary>
		/// The user callback method
		/// </summary>
		private Action<object> m_callback;
		/// <summary>
		/// The user callback state object
		/// </summary>
		private object m_state;
		/// <summary>
		/// Set to avoid scheduling the callback, but rather invoke it directly
		/// </summary>
		private bool m_invokeAsync;
		/// <summary>
		/// The number of milliseconds to wait before performing a callback
		/// </summary>
		private long m_millisecondsDueTime;
		/// <summary>
		/// The number of milliseconds to wait between each callback
		/// </summary>
		private long m_millisecondsPeriod;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.PCL.Timer"/> class.
		/// </summary>
		/// <param name="callback">The method to call.</param>
		/// <param name="state">The state to invoke the method with.</param>
		/// <param name="millisecondsDueTime">Number of milliseconds before invoking the timer.</param>
		/// <param name="millisecondsPeriod">Number of milliseconds between repeated timer invocations.</param>
		/// <param name="invokeAsync">If set to <c>true</c>, the callbacks are scheduled rather than invoked directly.</param>
		internal Timer(Action<object> callback, object state, long millisecondsDueTime, long millisecondsPeriod, bool invokeAsync = false)
		{
			m_callback = callback;
			m_state = state;
			m_millisecondsDueTime = millisecondsDueTime;
			m_millisecondsPeriod = millisecondsPeriod;
			m_invokeAsync = invokeAsync;
			RestartTimer(millisecondsDueTime);
		}


		/// <summary>
		/// Callback method that is fired after the delay
		/// </summary>
		/// <param name="task">The task that was completed.</param>
		private void OnTimerTick(Task task)
		{
			if (!IsCancellationRequested && m_runner == task)
			{
				//TODO: We may loose some precision in the interval
				if (!m_invokeAsync)
					m_callback(m_state);
				else
					Task.Run(() => m_callback(m_state));

				if (m_millisecondsPeriod > 0)
					RestartTimer(m_millisecondsPeriod);
			}
		}

		/// <summary>
		/// Restarts the timer
		/// </summary>
		/// <param name="milliseconds">The number of milliseconds to wait before performing a callback.</param>
		private void RestartTimer(long milliseconds)
		{
			if (milliseconds <= 0)
				OnTimerTick(m_runner = null);
			else
				m_runner = 
					Task.Delay(TimeSpan.FromMilliseconds(m_millisecondsDueTime), Token)
					.ContinueWith(OnTimerTick, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
		}

		/// <summary>
		/// Changes the timer to use the new period and interval.
		/// Calling this method cancels any active timers
		/// </summary>
		/// <param name="millisecondsDueTime">Number of milliseconds before invoking the timer.</param>
		/// <param name="millisecondsPeriod">Number of milliseconds between repeated timer invocations.</param>
		public void Change(long millisecondsDueTime, long millisecondsPeriod)
		{
			RestartTimer(millisecondsDueTime);
		}

		/// <summary>
		/// Dispose timer.
		/// </summary>
		/// <param name="disposing">Set to <c>true</c> if disposing.</param>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
				Cancel();

			base.Dispose(disposing);
		}
	}
}
#endif

