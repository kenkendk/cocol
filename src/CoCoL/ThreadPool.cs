using System;
using System.Collections.Generic;

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
		public static void QueueItem(System.Threading.WaitCallback a, object item = null)
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
			System.Threading.ThreadPool.QueueUserWorkItem((x) => a());
		}

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		/// <param name="item">An optional callback parameter.</param>
		public void QueueItem(System.Threading.WaitCallback a, object item) 
		{
			System.Threading.ThreadPool.QueueUserWorkItem(a, item);
		}
	}
}

