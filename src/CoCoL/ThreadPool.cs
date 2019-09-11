using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#if DISABLE_WAITCALLBACK
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
		public static readonly IThreadPool DEFAULT_THREADPOOL = new SystemThreadPoolWrapper();

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		public static void QueueItem(Action a)
		{
			ExecutionScope.Current.QueueItem(a);
		}

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		/// <param name="item">An optional callback parameter.</param>
		public static void QueueItem(WAITCALLBACK a, object item = null)
		{
			ExecutionScope.Current.QueueItem(a, item);
		}

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		/// <returns>The awaitable task.</returns>
		public static Task QueueTask(Action a)
		{
			return ExecutionScope.Current.QueueTask(a);
		}
	}

	/// <summary>
	/// The default thread pool implementation, which just wraps the .Net Thread Pool
	/// </summary>
	public class SystemThreadPoolWrapper : IThreadPool
	{
		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		public void QueueItem(Action a) 
		{
#if DISABLE_WAITCALLBACK
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
#if DISABLE_WAITCALLBACK
			System.Threading.Tasks.Task.Run(() => a(item));
#else
			System.Threading.ThreadPool.QueueUserWorkItem(a, item);
#endif
		}

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		/// <returns>The awaitable task.</returns>
		public Task QueueTask(Action a)
		{
			var tcs = new TaskCompletionSource<bool>();
			return QueueTask(() =>
				{
					try
					{ 
						a();
						tcs.SetResult(true);
					}
					catch(Exception ex)
					{
						tcs.TrySetException(ex);
					}
				});
		}

		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.SystemThreadPoolWrapper"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CoCoL.SystemThreadPoolWrapper"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="CoCoL.SystemThreadPoolWrapper"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="CoCoL.SystemThreadPoolWrapper"/>
		/// so the garbage collector can reclaim the memory that the <see cref="CoCoL.SystemThreadPoolWrapper"/> was occupying.</remarks>
		public void Dispose()
		{
            // No resources need to be disposed
		}
	}

	/// <summary>
	/// A thread pool that puts a cap on the number of concurrent requests
	/// </summary>
	public class CappedThreadedThreadPool : IFinishAbleThreadPool, ILimitingThreadPool
    {
		/// <summary>
		/// The list of pending work
		/// </summary>
		private readonly Queue<Action> m_work = new Queue<Action>();

		/// <summary>
		/// The locking object
		/// </summary>
		private readonly object m_lock = new object();

		/// <summary>
		/// The number of running instances
		/// </summary>
		private int m_instances = 0;

		/// <summary>
		/// The maximum number of concurrent threads
		/// </summary>
		private readonly int m_maxThreads;

		/// <summary>
		/// True if a shutdown is in progress
		/// </summary>
		private bool m_shutdown = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.CappedThreadedThreadPool"/> class.
		/// </summary>
		/// <param name="max_threads">The maximum number of concurrent threads.</param>
		public CappedThreadedThreadPool(int max_threads)
		{
			if (max_threads < 1)
				throw new ArgumentOutOfRangeException(nameof(max_threads));
			m_maxThreads = max_threads;
		}

		/// <summary>
		/// Performs the execution
		/// </summary>
		/// <param name="method">The the method to execute.</param>
		private void Execute(object method)
		{
			try
			{
				((Action)method)();
			}
			finally
			{
				lock (m_lock)
				{
					if (m_work.Count > 0 && m_instances <= m_maxThreads)
						ThreadPool.DEFAULT_THREADPOOL.QueueItem(Execute, m_work.Dequeue());
					else
						m_instances--;
				}
			}
		}

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		public void QueueItem(Action a) 
		{
			lock (m_lock)
			{
				if (m_shutdown)
					throw new ObjectDisposedException("ThreadPool is in shutdown phase, and does not support new requests");
				
				if (m_instances < m_maxThreads)
				{
					m_instances++;
					ThreadPool.DEFAULT_THREADPOOL.QueueItem(Execute, a);
				}
				else
				{
					m_work.Enqueue(a);
				}
			}
		}

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		/// <param name="item">An optional callback parameter.</param>
		public void QueueItem(WAITCALLBACK a, object item) 
		{
			QueueItem(() => { a(item); });
		}
			
		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		/// <returns>The awaitable task.</returns>
		public Task QueueTask(Action a)
		{
			var tcs = new TaskCompletionSource<bool>();
			QueueItem(() => {
				try
				{ 
					a();
					tcs.SetResult(true);
				}
				catch(Exception ex)
				{
					tcs.TrySetException(ex);
				}
			});

            return tcs.Task;
		}

		/// <summary>
		/// Ensures that all threads are finished
		/// </summary>
		/// <param name="waittime">The time to wait for completion</param>
		public async Task EnsureFinishedAsync(TimeSpan waittime = default(TimeSpan))
		{
			lock (m_lock)
			{
				m_shutdown = true;
				if (m_instances == 0)
					return;
			}

			if (waittime == default(TimeSpan))
				throw new InvalidOperationException(string.Format("Thread pool was not completed, there are {0} instances running", m_instances));

			var endttime = DateTime.Now + waittime;
			while (DateTime.Now < endttime)
			{
                await Task.Delay(100).ConfigureAwait(false);
				lock (m_lock)
					if (m_instances == 0)
						return;
			}

			throw new TimeoutException(string.Format("Failed to shut down execution context, there are still {0} instances running", m_instances));

		}

		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.CappedThreadedThreadPool"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CoCoL.CappedThreadedThreadPool"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="CoCoL.CappedThreadedThreadPool"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="CoCoL.CappedThreadedThreadPool"/>
		/// so the garbage collector can reclaim the memory that the <see cref="CoCoL.CappedThreadedThreadPool"/> was occupying.</remarks>
		public void Dispose()
		{
			lock (m_lock)
			{
				m_shutdown = true;

				//TODO: Should throw, but throwing in Dispose leads to unwanted complexity for the user
				if (m_instances > 0)
					System.Diagnostics.Debug.WriteLine("*Warning*: CappedThreadPool was disposed before all threads had completed!");
			}
		}
	}
}

