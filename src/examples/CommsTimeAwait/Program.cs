using System;
using CoCoL;
using System.Threading.Tasks;
using T = System.Int32;

namespace CommsTimeAwait
{
	class MainClass
	{
		/// <summary>
		/// Runs the identity process, which simply forwards a value.
		/// </summary>
		/// <param name="chan_read">The channel to read from</param>
		/// <param name="chan_write">The channel to write to</param>
		private static async void RunIdentity(IReadChannel<T> chan_read, IWriteChannel<T> chan_write)
		{
			try
			{
				while (true)
				{
					await chan_write.WriteAsync(await chan_read.ReadAsync());
				}
			}
			catch (RetiredException)
			{
				chan_read.Retire();
				chan_write.Retire();
			}
		}

		/// <summary>
		/// Runs the delta process, which copies the value it reads onto two different channels
		/// </summary>
		/// <param name="chan_read">The channel to read from</param>
		/// <param name="chan_a">The channel to write to</param>
		/// <param name="chan_b">The channel to write to</param>
		private static async void RunDelta(IReadChannel<T> chan_read, IWriteChannel<T> chan_a, IWriteChannel<T> chan_b)
		{
			try
			{
				while (true)
				{
					var value = await chan_read.ReadAsync();
					await Task.WhenAll(
						chan_a.WriteAsync(value),
						chan_b.WriteAsync(value)
					);
				}
			}
			catch(RetiredException)
			{
				chan_read.Retire();
				chan_a.Retire();
				chan_b.Retire();
			}
		}

		/// <summary>
		/// Runs the tick collector process which measures the network performance
		/// </summary>
		/// <returns>The awaitable tick collector task.</returns>
		/// <param name="chan">The tick channel.</param>
		/// <param name="stop">The channel used to shut down the network.</param>
		private static async Task RunTickCollectorAsync(IReadChannel<T> chan, IChannel<T> stop)
		{
			var tickcount = 0;
			var rounds = 0;

			//Initialize
			await chan.ReadAsync();

			var a_second = TimeSpan.FromSeconds(1).Ticks;

			//Warm up
			Console.WriteLine("Warming up ...");
			DateTime m_last = DateTime.Now;
			while (await chan.ReadAsync() != 0)
				if ((DateTime.Now - m_last).Ticks > a_second)
					break;

			//Measuring
			Console.WriteLine("Measuring!");
			var measure_span = TimeSpan.FromSeconds(5).Ticks;
			m_last = DateTime.Now;

			try
			{
				while (await chan.ReadAsync() != 0)
				{
					tickcount++;
					//var duration = DateTime.Now - m_last;
					//if (duration.Ticks >= measure_span)
					if (tickcount >= TICKS)
					{
						var duration = DateTime.Now - m_last;
						Console.WriteLine("Got {0} ticks in {1} seconds, speed is {2} rounds/s ({3} msec/comm)", tickcount, duration, tickcount / duration.TotalSeconds, duration.TotalMilliseconds / ((tickcount) * PROCESSES));
						Console.WriteLine("Time per iteration: {0} microseconds", (duration.TotalMilliseconds * 1000) / tickcount);
						Console.WriteLine("Time per communication: {0} microseconds", (duration.TotalMilliseconds * 1000) / tickcount / 4);

						tickcount = 0;
						m_last = DateTime.Now;

						// For shutdown, we retire the initial channel
						if (++rounds >= MEASURE_COUNT)
							stop.Retire();
					}
				}
			}
			catch(RetiredException)
			{
				chan.Retire();
			}
		}

		/// <summary>
		/// The number of measurements to perform in the tick collector before exiting
		/// </summary>
		public const int MEASURE_COUNT = 10;

		/// <summary>
		/// The number of processes in the ring
		/// </summary>
		public const int PROCESSES = 3; //10000000;

		/// <summary>
		/// The number of ticks to measure in each round
		/// </summary>
		public const int TICKS = 1000000;


		public static void Main(string[] args)
		{
			var chan_in = ChannelManager.CreateChannel<T>();
			var chan_tick = ChannelManager.CreateChannel<T>();
			var chan_out = ChannelManager.CreateChannel<T>();

			// Start the delta process
			RunDelta(chan_in, chan_out, chan_tick);

			IChannel<T> chan_new = null;

			// Spin up the forwarders
			for (var i = 0; i < PROCESSES - 2; i++)
			{
				//Console.WriteLine("Starting process {0}", i);
				chan_new = ChannelManager.CreateChannel<T>();
				RunIdentity(chan_out, chan_new);
				chan_out = chan_new;
			}
				
			// Close the ring
			RunIdentity(chan_out, chan_in);

			// Start the tick collector 
			var t = RunTickCollectorAsync(chan_tick, chan_in);

			// Inject a value into the ring
			chan_in.WriteNoWait(1);

			Console.WriteLine("Running, press CTRL+C to stop");

			// Wait for the tick collector to finish measuring
			t.Wait();

		}
	}
}
