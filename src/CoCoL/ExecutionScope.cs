using System;

#if PCL_BUILD
using WAITCALLBACK = System.Action<object>;
#else
using WAITCALLBACK = System.Threading.WaitCallback;
#endif

namespace CoCoL
{
	public class ExecutionScope : IDisposable, IFinishAbleThreadPool
	{
		/// <summary>
		/// The root scope, where all other scopes descend from
		/// </summary>
		public static readonly ExecutionScope Root;

		/// <summary>
		/// The lock object
		/// </summary>
		protected static readonly object __lock;

		/// <summary>
		/// The key used to assign the current scope into the current call-context
		/// </summary>
		protected const string LOGICAL_CONTEXT_KEY = "CoCoL:ExecutionScope";

		/// <summary>
		/// The thread pool used to execute requests
		/// </summary>
		protected IThreadPool m_threadPool;

		/// <summary>
		/// True if this instance is disposed, false otherwise
		/// </summary>
		protected bool m_isDisposed = false;

		/// <summary>
		/// The parent scope, or null if this is the root scope
		/// </summary>
		/// <value>The parent scope.</value>
		public ExecutionScope ParentScope { get; private set; }

		/// <summary>
		/// Static initializer to control the creation order
		/// </summary>
		static ExecutionScope()
		{
			__lock = new object();
			Root = new ExecutionScope(null, ThreadPool.DEFAULT_THREADPOOL);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ExecutionScope"/> class.
		/// </summary>
		/// <param name="threadPool">The thread pool to use.</param>
		public ExecutionScope(IThreadPool threadPool)
			: this(Current, threadPool)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ExecutionScope"/> class.
		/// </summary>
		/// <param name="parent">The parent scope.</param>
		/// <param name="threadPool">The thread pool implementation.</param>
		private ExecutionScope(ExecutionScope parent, IThreadPool threadPool)
		{
			if (threadPool == null)
				throw new ArgumentNullException("threadPool");

			ParentScope = parent;
			m_threadPool = threadPool;
			Current = this;
		}


		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		public void QueueItem(Action a)
		{
			m_threadPool.QueueItem(a);
		}

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		/// <param name="item">An optional callback parameter.</param>
		public void QueueItem(WAITCALLBACK a, object item = null)
		{
			m_threadPool.QueueItem(a, item);
		}

		/// <summary>
		/// Ensures that the threadpool is finished or throws an exception.
		/// If the underlying threadpool does not support finishing, this call does nothing
		/// </summary>
		/// <param name="waittime">The maximum time to wait for completion.</param>
		public void EnsureFinished(TimeSpan waittime = default(TimeSpan))
		{
			if (m_threadPool is IFinishAbleThreadPool)
				((IFinishAbleThreadPool)m_threadPool).EnsureFinished(waittime);
		}


		#region IDisposable implementation

		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.ChannelScope"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CoCoL.ChannelScope"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="CoCoL.ChannelScope"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="CoCoL.ChannelScope"/> so the garbage
		/// collector can reclaim the memory that the <see cref="CoCoL.ChannelScope"/> was occupying.</remarks>
		public void Dispose()
		{
			lock (__lock)
			{
				if (this == Root)
					throw new InvalidOperationException("Cannot dispose the root scope");

				if (Current == this)
				{
					Current = this.ParentScope;

					// Disposal can be non-deterministic, so we walk the chain
					while (Current.m_isDisposed)
						Current = Current.ParentScope;
				}
				m_isDisposed = true;
				m_threadPool = null;
			}
		}

		#endregion

#if PCL_BUILD
private static bool __IsFirstUsage = true;
private static ExecutionScope __Current = null;
#endif
		/// <summary>
		/// Gets the current execution scope
		/// </summary>
		public static ExecutionScope Current
		{
			get 
			{
				lock (__lock)
				{
#if PCL_BUILD
					// TODO: Use AsyncLocal if targeting 4.6
					//var cur = new System.Threading.AsyncLocal<ChannelScope>();
					if (__IsFirstUsage)
					{
						__IsFirstUsage = false;
						System.Diagnostics.Debug.WriteLine("*Warning*: PCL does not provide a call context, so channel scoping does not work correctly for multithreaded use!");
					}

					var cur = __Current;
#else
					var cur = System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(LOGICAL_CONTEXT_KEY) as ExecutionScope;
#endif
					if (cur == null)
						return Current = Root;
					else
						return cur;
				}
			}
			private set
			{
				lock (__lock)
				{
#if PCL_BUILD				
					__Current = value;
#else
					System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(LOGICAL_CONTEXT_KEY, value);
#endif
				}
			}
		}
	}
}

