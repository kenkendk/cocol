using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoCoL
{
    /// <summary>
    /// Utility class, similar to <see cref="System.Threading.ThreadPool"/> but allows a limited number of tasks to run in parallel
    /// </summary>
    public class TaskPool
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
        private readonly Action<Task> m_errorHandler;

        /// <summary>
        /// The list of active tasks
        /// </summary>
        private readonly List<Task> m_active = new List<Task>();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SlimDHT.TaskPool"/> class.
        /// </summary>
        /// <param name="maxtasks">The maximum number of concurrent tasks.</param>
        /// <param name="errorHandler">An optional error handler</param>
        public TaskPool(int maxtasks, Action<Task> errorHandler = null)
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
                    // Remove completed tasks and make room
                    if (m.IsCompleted)
                        m_active.RemoveAt(i);

                    // Handle any errors
                    if (m_errorHandler != null && (m.IsFaulted || m.IsCanceled))
                        m_errorHandler(m);
                }
            }
        }

        /// <summary>
        /// Runs a task on the pool
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="method">The method to invoke.</param>
        public async Task Run(Func<Task> method)
        {
            Task result;
            using (await m_lock.LockAsync())
            {
                // Make room for another task
                await EnsureRunnerSpace();

                // Spin up the new task, and add it to the list
                m_active.Add(result = Task.Run(method));
            }

            // Send the original task results back
            await result;
        }

        /// <summary>
        /// Runs a task on the pool
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="method">The method to invoke.</param>
        /// <typeparam name="T">The result type</typeparam>
        public async Task<T> Run<T>(Func<Task<T>> method)
        {
            Task<T> result;
            using (await m_lock.LockAsync())
            {
                // Make room for another task
                await EnsureRunnerSpace();

                // Spin up the new task, and add it to the list
                m_active.Add(result = Task.Run(method));
            }

            // Send the original task results back
            return await result;
        }

    }
}
