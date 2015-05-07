using System;
using CoCoL;

namespace CommsTimeAwait
{
	class TickCollector : IProcess
	{
		public const string TICK_CHANNEL_NAME = "ticks";

		public async void Run()
		{
			var tick_chan = ChannelManager.GetChannel<bool>(TICK_CHANNEL_NAME);
			var tickcount = 0;

			//Initialize
			await tick_chan.ReadAsync();

			var a_second = TimeSpan.FromSeconds(1).Ticks;

			//Warm up
			Console.WriteLine("Warming up ...");
			DateTime m_last = DateTime.Now;
			while (await tick_chan.ReadAsync())
				if ((DateTime.Now - m_last).Ticks > a_second)
					break;

			//Measuring
			Console.WriteLine("Measuring!");
			var measure_span = TimeSpan.FromSeconds(5).Ticks;
			m_last = DateTime.Now;

			while (await tick_chan.ReadAsync())
			{
				tickcount++;
				var duration = DateTime.Now - m_last;
				if (duration.Ticks >= measure_span)
				{
					Console.WriteLine("Got {0} ticks in {1} seconds, speed is {2} rounds/s ({3} msec/comm)", tickcount, duration, tickcount / duration.TotalSeconds, duration.TotalMilliseconds / ((tickcount) * CommsTime.PROCESSES));

					tickcount = 0;
					m_last = DateTime.Now;
				}
			}
		}
	}

	[Process(count: PROCESSES)]
	class CommsTime : IProcess
	{
		public const int PROCESSES = 4;

		private static int _index = -1;
		private readonly int m_index = System.Threading.Interlocked.Increment(ref _index);

		#region IProcess implementation
		public async void Run()
		{
			var next_chan = (m_index + 1) % PROCESSES;
			var prev_chan = m_index == 0 ? PROCESSES - 1 : m_index - 1;

			Console.WriteLine("Started process {0}", m_index);
			var write_chan_name = string.Format("{0}->{1}", m_index, next_chan);
			var read_chan_name = string.Format("{0}->{1}", prev_chan, m_index);

			var chan_write = ChannelManager.GetChannel<bool>(write_chan_name);
			var chan_read = ChannelManager.GetChannel<bool>(read_chan_name);
			var tick_chan = ChannelManager.GetChannel<bool>(TickCollector.TICK_CHANNEL_NAME);
				
			if (m_index == 0)
			{
				//Console.WriteLine("process {0} is writing to {1}", m_index, write_chan_name);
				chan_write.WriteAsync(true);
			}

			while (true)
			{
				//Console.WriteLine("process {0} is reading from {1}", m_index, read_chan_name);
				var v = await chan_read.ReadAsync();

				// Output the tick, should be done in parallel
				if (m_index == 0)
					tick_chan.WriteAsync(true);
				
				//Console.WriteLine("process {0} is writing to {1}", m_index, write_chan_name);
				chan_write.WriteAsync(v);
					
			}
		}
		#endregion

	}
}

