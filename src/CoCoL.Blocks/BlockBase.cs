using System;
using System.Threading.Tasks;

namespace CoCoL.Blocks
{
	public abstract class BlockBase : IProcess, IAsyncProcess
	{
		public virtual void Run()
		{
			RunAsync().Wait();
		}

		public abstract Task RunAsync();
	}
}

