using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoCoL
{
    /// <summary>
    /// Utility class, similar to <see cref="T:System.Threading.ThreadPool"/> but allows a limited number of tasks to run in parallel,
    /// handling errors via a callback method
    /// </summary>
    public class TaskPool : IDisposable
    {
        /// <summary>
        /// The internal typed task pool
        /// </summary>
        private readonly TaskPool<bool> m_taskpool;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SlimDHT.TaskPool&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="maxtasks">The maximum number of concurrent tasks.</param>
        /// <param name="errorHandler">An error handler</param>
        public TaskPool(int maxtasks, Action<Exception> errorHandler)
            : this(maxtasks, (ex) => Task.Run(() => errorHandler(ex)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SlimDHT.TaskPool"/> class.
        /// </summary>
        /// <param name="maxtasks">The maximum number of concurrent tasks.</param>
        /// <param name="errorHandler">An optional error handler</param>
        public TaskPool(int maxtasks, Func<Exception, Task> errorHandler = null)
        {
            Func<bool, Exception, Task> handler = null;
            if (errorHandler != null)
                handler = (_, ex) => errorHandler(ex);
            
            m_taskpool = new TaskPool<bool>(maxtasks, handler);
        }

        /// <summary>
        /// Releases all resource used by the <see cref="T:CoCoL.TaskPool"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:CoCoL.TaskPool"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="T:CoCoL.TaskPool"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the <see cref="T:CoCoL.TaskPool"/> so the garbage
        /// collector can reclaim the memory that the <see cref="T:CoCoL.TaskPool"/> was occupying.</remarks>
        public void Dispose()
        {
            m_taskpool.Dispose();
        }

        /// <summary>
        /// Runs a task on the pool
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="method">The method to invoke.</param>
        public Task Run(Func<Task> method)
        {
            return m_taskpool.Run(true, method);
        }

        /// <summary>
        /// Shuts down the task pool and returns a task that can be awaited for completion
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public Task ShutdownAsync()
        {
            return m_taskpool.ShutdownAsync();
        }

        /// <summary>
        /// Runs the given tasks in parallel, observing the maximum number of active parallel tasks
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="tasks">The tasks to run.</param>
        /// <param name="handler">The method to invoke for each item</param>
        /// <param name="maxparallel">The maximum parallelism to use.</param>
        /// <param name="errorHandler">The error handler</param>
        /// <param name="token">An optional cancellation token</param>
        /// <typeparam name="T">The type of data elements to handle</typeparam>
        public static Task RunParallelAsync<T>(IEnumerable<T> tasks, Func<T, Task> handler, Action<T, Exception> errorHandler, System.Threading.CancellationToken token = default(System.Threading.CancellationToken), int maxparallel = 10)
        {
            if (tasks == null)
                throw new ArgumentNullException(nameof(tasks));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (errorHandler == null)
                throw new ArgumentNullException(nameof(errorHandler));

            return RunParallelAsync(tasks, handler, (t, ex) => Task.Run(() => errorHandler(t, ex)), token, maxparallel);
        }

        /// <summary>
        /// Runs the given tasks in parallel, observing the maximum number of active parallel tasks
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="tasks">The tasks to run.</param>
        /// <param name="handler">The method to invoke for each item</param>
        /// <param name="maxparallel">The maximum parallelism to use.</param>
        /// <param name="errorHandler">The error handler</param>
        /// <param name="token">An optional cancellation token</param>
        /// <typeparam name="T">The type of data elements to handle</typeparam>
        public static async Task RunParallelAsync<T>(IEnumerable<T> tasks, Func<T, Task> handler, Func<T, Exception, Task> errorHandler = null, System.Threading.CancellationToken token = default(System.Threading.CancellationToken), int maxparallel = 10)
        {
            if (tasks == null)
                throw new ArgumentNullException(nameof(tasks));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            using (var tp = new TaskPool<T>(maxparallel, errorHandler))
            {
                try
                {
                    foreach (var t in tasks)
                    {
                        if (token.IsCancellationRequested)
                            throw new TaskCanceledException();
                        await tp.Run(t, handler);
                    }
                }
                finally
                {
                    await tp.ShutdownAsync();
                }
            }
        }

        /// <summary>
        /// Runs a repeated parallel operation 
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="source">The channel where requests are read from</param>
        /// <param name="handler">The method to invoke for each item</param>
        /// <param name="token">Token.</param>
        /// <param name="maxparallel">The maximum parallelism to use.</param>
        /// <param name="errorHandler">The error handler</param>
        /// <typeparam name="T">The type of data elements to handle</typeparam>
        public static Task RunParallelAsync<T>(IReadChannel<T> source, Func<T, Task> handler, System.Threading.CancellationToken token = default(System.Threading.CancellationToken), int maxparallel = 10, Func<T, Exception, Task> errorHandler = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (maxparallel <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxparallel), maxparallel, "The size of the queue must be greater than zero");

            return AutomationExtensions.RunTask(
                new
                {
                    Requests = source
                },
                async self =>
                {
                    using (var tp = new TaskPool<T>(maxparallel, errorHandler))
                        while (true)
                        {
                            if (token.IsCancellationRequested)
                                throw new TaskCanceledException();
                            await tp.Run(await source.ReadAsync(), handler);
                        }
                });
        }    
    }

    /// <summary>
    /// Utility class, similar to <see cref="System.Threading.ThreadPool"/> but allows a limited number of tasks to run in parallel,
    /// handling errors via a callback method
    /// </summary>
    /// <typeparam name="T">The data type the threadpool uses</typeparam>
    public class TaskPool<T> : IDisposable
    {
        /// <summary>
        /// The maximum number of tasks to allow running
        /// </summary>
        private readonly int m_max_tasks;

        /// <summary>
        /// The lock ensuring a maximum parallel execution fan-out
        /// </summary>
        private readonly AsyncLock m_lock = new AsyncLock();

        /// <summary>
        /// An optional error handler
        /// </summary>
        private readonly Func<T, Exception, Task> m_errorHandler;

        /// <summary>
        /// The list of active tasks
        /// </summary>
        private readonly List<Task> m_active = new List<Task>();

        /// <summary>
        /// The list of arguments for the active tasks
        /// </summary>
        private readonly List<T> m_arguments = new List<T>();

        /// <summary>
        /// The termination handler
        /// </summary>
        private TaskCompletionSource<bool> m_terminate;

        /// <summary>
        /// Gets the number of active elements in the queue
        /// </summary>
        /// <value>The active count.</value>
        public int ActiveCount
        {
            get
            {
                var total = 0;
                for (var i = 0; i < m_active.Count; i++)
                    if (!m_active[i].IsCompleted)
                        total++;

                return total;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SlimDHT.TaskPool&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="maxtasks">The maximum number of concurrent tasks.</param>
        /// <param name="errorHandler">An error handler</param>
        public TaskPool(int maxtasks, Action<T, Exception> errorHandler)
            : this(maxtasks, (t, ex) => Task.Run(() => errorHandler(t, ex)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SlimDHT.TaskPool&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="maxtasks">The maximum number of concurrent tasks.</param>
        /// <param name="errorHandler">An optional error handler</param>
        public TaskPool(int maxtasks, Func<T, Exception, Task> errorHandler = null)
        {
            m_max_tasks = maxtasks;
            m_errorHandler = errorHandler;
        }

        /// <summary>
        /// Helper method to ensure there is enough space in the active queue.
        /// Must be called with the lock being taken
        /// </summary>
        /// <returns>An awaitable task.</returns>
        private async Task EnsureRunnerSpace()
        {            
            // Check if we have room for another task
            if (m_active.Count >= m_max_tasks)
            {
                // Wait until a task has completed
                await Task.WhenAny(m_active);
                for (var i = m_active.Count - 1; i >= 0; i--)
                {
                    var m = m_active[i];
                    var t = m_arguments[i];

                    // Remove completed tasks and make room
                    if (m.IsCompleted)
                    {
                        m_active.RemoveAt(i);
                        m_arguments.RemoveAt(i);
                    }

                    // Handle any errors
                    if (m_errorHandler != null && (m.IsFaulted || m.IsCanceled))
                        Task.Run(() => m_errorHandler(t, m.IsFaulted ? m.Exception : (Exception)new TaskCanceledException()))
                            .FireAndForget();
                }
            }
        }

        /// <summary>
        /// Runs a task on the pool
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="value">The value to operate on</param>
        /// <param name="method">The method to invoke.</param>
        public Task Run(T value, Func<T, Task> method)
        {
            return Run(value, () => method(value));
        }

        /// <summary>
        /// Runs a task on the pool, waiting until it has been queued
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="value">The value to operate on</param>
        /// <param name="method">The method to invoke.</param>
        public async Task Run(T value, Func<Task> method)
        {
            // If the pool is terminated, stop now
            if (m_terminate != null)
                throw new ObjectDisposedException(nameof(TaskPool));
            
            using (await m_lock.LockAsync())
            {
                // Make room for another task
                await EnsureRunnerSpace();

                // Spin up the new task, and add it to the list
                m_arguments.Add(value);
                m_active.Add(Task.Run(method));
            }
        }

        /// <summary>
        /// Shuts down the task pool and returns a task that can be awaited for completion
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public Task ShutdownAsync()
        {
            Dispose();
            return m_terminate.Task;
        }

        /// <summary>
        /// Waits until all currently running or queued tasks are completed
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task FinishedAsync()
        {
            // If we are done, just use that signal
            if (m_terminate != null)
            {
                await m_terminate.Task;
                return;
            }

            // We make a copy to avoid races
            List<Task> active;

            // Make sure we are the next in queue
            using (await m_lock.LockAsync())
                active = new List<Task>(m_active);

            // Now wait for all to complete
            await Task.WhenAll(active);
        }

        /// <summary>
        /// Releases all resource used by the <see cref="T:CoCoL.TaskPool`1"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:CoCoL.TaskPool`1"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="T:CoCoL.TaskPool`1"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the <see cref="T:CoCoL.TaskPool`1"/> so the
        /// garbage collector can reclaim the memory that the <see cref="T:CoCoL.TaskPool`1"/> was occupying.</remarks>
        public void Dispose()
        {
            if (m_terminate == null)
            {
                m_terminate = new TaskCompletionSource<bool>();

                // Grab the lock, preventing new entries
                m_lock.LockAsync().ContinueWith(lck =>
                {
                    // Wait for all active to complete
                    Task.WhenAll(m_active).ContinueWith(x =>
                    {
                        // Signal that we are now done
                        m_terminate.SetResult(true);
                        // Release the lock, in case some weirdness would otherwise have it locked
                        lck.Result.Dispose();
                    });
                });
            }
        }
    }
}
