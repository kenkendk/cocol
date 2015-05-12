using System;
using CoCoL;
using System.Threading.Tasks;

namespace CommsTimeBlocks
{
	/// <summary>
	/// Basic consumer class that measures time
	/// </summary>
	class Consumer
	{
		private const long WARMUP = 1000;
		private const long ROUNDS = 1000000;

		private IReadChannel<long> m_channel;

		public Consumer(IReadChannel<long> channel)
		{
			m_channel = channel;
		}

		public async Task RunAsync()
		{
			// Do warmup rounds
			for (var i = 0L; i < WARMUP; i++)
				await m_channel.ReadAsync();

			Console.WriteLine("Warmup complete, measuring");

			// Measure the run
			var start = DateTime.Now;
			for (var i = 0L; i < ROUNDS; i++)
				await m_channel.ReadAsync();	
			var finish = DateTime.Now;

			// Cleanup and report
			m_channel.Retire();
			Console.WriteLine("Time per iteration: {0} milliseconds", (finish - start).TotalMilliseconds / ROUNDS);
		}
	}

	/// <summary>
	/// Entry class
	/// </summary>
	class MainClass
	{
		public static void Main(string[] args)
		{
			// Set up channels
			var channel_a = ChannelManager.CreateChannel<long>();
			var channel_b = ChannelManager.CreateChannel<long>();
			var channel_c = ChannelManager.CreateChannel<long>();
			var channel_d = ChannelManager.CreateChannel<long>();

			// Start processes in parallel and wait for shutdown
			Task.WhenAll(
				new CoCoL.Blocks.Prefix<long>(channel_c, channel_a, 0).RunAsync(),
				new CoCoL.Blocks.Delta<long>(channel_a, channel_d, channel_b).RunAsync(),
				new CoCoL.Blocks.Successor(channel_b, channel_c).RunAsync(),
				new Consumer(channel_d).RunAsync()
			);

		}
	}
}
