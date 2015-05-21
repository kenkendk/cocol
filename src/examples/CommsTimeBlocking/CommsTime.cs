using System;
using CoCoL;

namespace CommsTimeBlocking
{
	class TickCollector : IProcess
	{
		public const string TICK_CHANNEL_NAME = "ticks";
		public const string TERM_CHANNEL_NAME = "terminate";

		public const int MEASURE_COUNT = 10;
		public const int TICK_COUNT = 1000000;

		public void Run()
		{
			var tick_chan = ChannelManager.GetChannel<bool>(TICK_CHANNEL_NAME).AsRead();
			var tickcount = 0;
			var rounds = 0;

			//Initialize
			tick_chan.Read();

			var a_second = TimeSpan.FromSeconds(1).Ticks;

			//Warm up
			Console.WriteLine("Warming up ...");
			DateTime m_last = DateTime.Now;
			while (tick_chan.Read())
				if ((DateTime.Now - m_last).Ticks > a_second)
					break;

			//Measuring
			Console.WriteLine("Measuring!");
			var measure_span = TimeSpan.FromSeconds(5).Ticks;
			m_last = DateTime.Now;

			try
			{
				while (tick_chan.Read())
				{
					tickcount++;
					//var duration = DateTime.Now - m_last;
					//if (duration.Ticks >= measure_span)
					if (tickcount >= TICK_COUNT)
					{
						var duration = DateTime.Now - m_last;
						Console.WriteLine("Got {0} ticks in {1} seconds, speed is {2} rounds/s ({3} msec/comm)", tickcount, duration, tickcount / duration.TotalSeconds, duration.TotalMilliseconds / ((tickcount) * CommsTime.PROCESSES));
						Console.WriteLine("Time per iteration: {0} microseconds", (duration.TotalMilliseconds * 1000) / tickcount);
						Console.WriteLine("Time per communication: {0} microseconds", (duration.TotalMilliseconds * 1000) / tickcount / 4);

						tickcount = 0;
						m_last = DateTime.Now;

						// For shutdown, we retire the initial channel
						if (++rounds >= MEASURE_COUNT)
							ChannelManager.GetChannel<bool>("0->1").Retire();
					}
				}
			}
			catch(RetiredException)
			{
				//Console.WriteLine("Retired tick writer");
				ChannelManager.GetChannel<bool>(TERM_CHANNEL_NAME).Retire();
			}

		}
	}

	[Process(count: PROCESSES)]
	class CommsTime : IProcess
	{
		public const int PROCESSES = 3;

		private static int _index = -1;
		private readonly int m_index = System.Threading.Interlocked.Increment(ref _index);

		#region IProcess implementation
		public void Run()
		{
			var next_chan = (m_index + 1) % PROCESSES;
			var prev_chan = m_index == 0 ? PROCESSES - 1 : m_index - 1;

			//Console.WriteLine("Started process {0}", m_index);
			var write_chan_name = string.Format("{0}->{1}", m_index, next_chan);
			var read_chan_name = string.Format("{0}->{1}", prev_chan, m_index);

			var chan_write = ChannelManager.GetChannel<bool>(write_chan_name).AsWrite();
			var chan_read = ChannelManager.GetChannel<bool>(read_chan_name).AsRead();
			var tick_chan = ChannelManager.GetChannel<bool>(TickCollector.TICK_CHANNEL_NAME).AsWrite();
				
			if (m_index == 0)
			{
				//Console.WriteLine("process {0} is writing to {1}", m_index, write_chan_name);
				chan_write.Write(true);
			}

			try
			{
				while (true)
				{
					//Console.WriteLine("process {0} is reading from {1}", m_index, read_chan_name);
					var v = chan_read.Read();

					// Output the tick, should be done in parallel
					if (m_index == 0)
						tick_chan.Write(true);
					
					//Console.WriteLine("process {0} is writing to {1}", m_index, write_chan_name);
					chan_write.Write(v);
						
				}
			}
			catch(RetiredException)
			{
				//Console.WriteLine("Retired process {0}", m_index);
				if (m_index == 0)
					tick_chan.Retire();
				chan_write.Retire();
			}
		}
		#endregion

	}
}

