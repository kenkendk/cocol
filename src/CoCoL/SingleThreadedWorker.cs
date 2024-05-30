using System;
using System.Threading.Tasks;
using System.Threading;

namespace CoCoL
{
	/// <summary>
	/// Provides exclusive access to a resource,
	/// by ensuring all methods are executed sequentially
	/// </summary>
	public abstract class SingleThreadedWorker : IDisposable
	{
		/// <summary>
		/// The channel that forwards tasks to the worker
		/// </summary>
		protected IChannel<Func<Task>> m_channel;
		/// <summary>
		/// The task that represents the worker
		/// </summary>
		protected readonly Task m_worker;
		/// <summary>
		/// A token for stopping the worker
		/// </summary>
		protected CancellationTokenSource m_workerSource;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.SingleThreadedWorker"/> class.
		/// </summary>
		protected SingleThreadedWorker()
		{
			AutomationExtensions.AutoWireChannels(this, null);
			m_channel = ChannelManager.CreateChannel<Func<Task>>();
			m_workerSource = new CancellationTokenSource();
			m_worker = AutomationExtensions.RunProtected(this, Start);
		}

		/// <summary>
		/// Start the worker.
		/// </summary>
		private async Task Start()
		{
			while (!m_workerSource.IsCancellationRequested)
			{
				// Grab next task
				var nextTask = await m_channel.ReadAsync();

				// Execute it
				await nextTask();
			}
		}

		/// <summary>
		/// Runs the specified function in the worker process
		/// </summary>
		/// <returns>The task used to signal the result.</returns>
		/// <param name="method">The method to invoke in the worker.</param>
		/// <typeparam name="T">The return type.</typeparam>
		protected Task<T> DoRunOnWorker<T>(Func<Task<T>> method)
		{
			var res = new TaskCompletionSource<T>();

			Task.Run(async () =>
				{
					try
					{
						if (m_workerSource.IsCancellationRequested)
						{
							res.TrySetCanceled();
							return;
						}

						await m_channel.WriteAsync(async () =>
							{
								if (m_workerSource.IsCancellationRequested)
								{
									res.TrySetCanceled();
									return;
								}

								try
								{
									var r = await method().ConfigureAwait(false);
									Task.Run(() => res.SetResult(r)).FireAndForget();
								}
								catch (Exception ex)
								{
									if (ex is System.Threading.ThreadAbortException)
										res.TrySetCanceled();
									else
										res.TrySetException(ex);
								}
							}).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						if (ex is System.Threading.ThreadAbortException)
							res.TrySetCanceled();
						else
							res.TrySetException(ex);
					}
				});

			return res.Task;
		}

		/// <summary>
		/// Runs the specified method in the worker
		/// </summary>
		/// <returns>The task signaling worker completion.</returns>
		/// <param name="method">The method to run in the worker.</param>
		protected Task RunOnWorker(Action method)
		{
			return DoRunOnWorker<bool>(() =>
				{
					method();
					return Task.FromResult(true);
				});
		}

		/// <summary>
		/// Runs the specified method in the worker
		/// </summary>
		/// <returns>The task with the worker result.</returns>
		/// <param name="method">The method to run in the worker.</param>
		/// <typeparam name="T">The return type.</typeparam>
		protected Task<T> RunOnWorker<T>(Func<T> method)
		{
			return DoRunOnWorker(() =>
				{
					return Task.FromResult(method());
				});
		}

		/// <summary>
		/// Runs the specified method in the worker
		/// </summary>
		/// <returns>The task signaling worker completion.</returns>
		/// <param name="method">The method to run in the worker.</param>
		protected Task RunOnWorker(Func<Task> method)
		{
			return DoRunOnWorker(async () =>
			{
				await method().ConfigureAwait(false);
				return true;
			});
		}

		/// <summary>
		/// Runs the specified method in the worker
		/// </summary>
		/// <returns>The task with the worker result.</returns>
		/// <param name="method">The method to run in the worker.</param>
		/// <typeparam name="T">The return type.</typeparam>
		protected Task<T> RunOnWorker<T>(Func<Task<T>> method)
		{
			return DoRunOnWorker(method);
		}

		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.SingleThreadedWorker"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose()"/> when you are finished using the <see cref="CoCoL.SingleThreadedWorker"/>. The
		/// <see cref="Dispose()"/> method leaves the <see cref="CoCoL.SingleThreadedWorker"/> in an unusable state. After
		/// calling <see cref="Dispose()"/>, you must release all references to the <see cref="CoCoL.SingleThreadedWorker"/> so
		/// the garbage collector can reclaim the memory that the <see cref="CoCoL.SingleThreadedWorker"/> was occupying.</remarks>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Dispose the current instance
		/// </summary>
		/// <param name="isDisposing"><c>True</c> if disposing, false otherwise.</param>
		protected virtual void Dispose(bool isDisposing)
		{
			m_workerSource.Cancel();

			if (m_channel != null)
				try { m_channel.Retire(); }
				catch { /* Ignore retire errors */ }

			AutomationExtensions.RetireAllChannels(this);
		}
	}
}

