using System;
using System.Threading.Tasks;

namespace CoCoL.Blocks
{
    /// <summary>
    /// The abstract base class for blocks
    /// </summary>
	public abstract class BlockBase : IProcess, IAsyncProcess
	{
        /// <summary>
        /// Run this by invoking <see cref="RunAsync"/>.
        /// </summary>
		public virtual void Run()
		{
			RunAsync().Wait();
		}

        /// <summary>
        /// Runs the process.
        /// </summary>
        /// <returns>An awaitable task.</returns>
		public abstract Task RunAsync();
	}
}

