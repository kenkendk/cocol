using System;
using CoCoL;

namespace CommsTimeBlocks
{
	/// <summary>
	/// Basic consumer class that measures time
	/// </summary>
	class Consumer
	{
		private const long WARMUP = 1000;
		private const long ROUNDS = 1000000;

		private IChannel<long> m_channel;
		private System.Threading.ManualResetEventSlim m_evt;

		public Consumer(IChannel<long> channel, System.Threading.ManualResetEventSlim evt)
		{
			m_channel = channel;
			m_evt = evt;
		}

		public async void Run()
		{
			for (var i = 0L; i < WARMUP; i++)
				await m_channel.ReadAsync();

			var start = DateTime.Now;

			for (var i = 0L; i < ROUNDS; i++)
				await m_channel.ReadAsync();	

			var finish = DateTime.Now;

			m_channel.Retire();

			Console.WriteLine("Time per iteration: {0} milliseconds", (finish - start).TotalMilliseconds / ROUNDS);
			m_evt.Set();
		}
	}

	/// <summary>
	/// Entry class
	/// </summary>
	class MainClass
	{
		public static void Main(string[] args)
		{
			// Use event to wait for completion
			var evt = new System.Threading.ManualResetEventSlim(false);

			// Set up channels
			var channel_a = ChannelManager.CreateChannel<long>();
			var channel_b = ChannelManager.CreateChannel<long>();
			var channel_c = ChannelManager.CreateChannel<long>();
			var channel_d = ChannelManager.CreateChannel<long>();

			// Start processes
			new CoCoL.Blocks.Prefix<long>(channel_c, channel_a, 0).Run();
			new CoCoL.Blocks.Delta<long>(channel_a, channel_d, channel_b).Run();
			new CoCoL.Blocks.Successor(channel_b, channel_c).Run();
			new Consumer(channel_d, evt).Run();

			evt.Wait();
		}
	}
}
