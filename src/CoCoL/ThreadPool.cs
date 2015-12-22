using System;
using System.Collections.Generic;

#if PCL_BUILD
using WAITCALLBACK = System.Action<object>;
#else
using WAITCALLBACK = System.Threading.WaitCallback;
#endif

namespace CoCoL
{
	/// <summary>
	/// Thread Pool, responsible for queueing work items
	/// </summary>
	public static class ThreadPool
	{
		/// <summary>
		/// The implementation of a thread pool, for easy replacement
		/// </summary>
		private static readonly ThreadPoolImpl _tr = new ThreadPoolImpl();

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		public static void QueueItem(Action a)
		{
			_tr.QueueItem(a);
		}

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		/// <param name="item">An optional callback parameter.</param>
		public static void QueueItem(WAITCALLBACK a, object item = null)
		{
			_tr.QueueItem(a, item);
		}
	}

	/// <summary>
	/// The thread pool implementation, which just wraps the .Net Thread Pool
	/// </summary>
	internal class ThreadPoolImpl
	{
		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		public void QueueItem(Action a) 
		{
#if PCL_BUILD
			System.Threading.Tasks.Task.Run(a);
#else
			System.Threading.ThreadPool.QueueUserWorkItem((x) => a());
#endif
		}

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		/// <param name="item">An optional callback parameter.</param>
		public void QueueItem(WAITCALLBACK a, object item) 
		{
#if PCL_BUILD
			System.Threading.Tasks.Task.Run(() => a(item));
#else
			System.Threading.ThreadPool.QueueUserWorkItem(a, item);
#endif
		}
	}
}

