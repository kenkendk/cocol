using System;
using CoCoL;
using System.Threading.Tasks;
using T = System.Int32;
using CoCoL.Network;
using System.Threading;

namespace CommsTimeAwait
{
	public class MainClass
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
		/// Runs the delta process, which copies the value it reads onto two different channels
		/// </summary>
		/// <param name="chan_read">The channel to read from</param>
		/// <param name="chan_a">The channel to write to</param>
		/// <param name="chan_b">The channel to write to</param>
		private static async void RunDeltaAlt(IReadChannel<T> chan_read, IWriteChannel<T> chan_a, IWriteChannel<T> chan_b)
		{
			var tgchans = new [] { chan_b };
			try
			{
				while (true)
				{
					var value = await chan_read.ReadAsync();

					await Task.WhenAll(
						chan_a.WriteAsync(value),
						MultiChannelAccess.WriteToAnyAsync(value, tgchans)
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
		private static async Task RunTickCollectorAsync(IReadChannel<T> chan, IChannel<T> stop, bool stop_after_tickcount)
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

					bool round_complete;
					if (stop_after_tickcount)
						round_complete = tickcount >= TICKS;
					else
						round_complete = (DateTime.Now - m_last).Ticks >= measure_span;

					if (round_complete)
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
		public static int PROCESSES = 3; //10000000;

		/// <summary>
		/// The number of ticks to measure in each round
		/// </summary>
		public static int TICKS = 1000000;

		public static void Main(string[] args)
		{
			var stop_with_ticks = true;
			var networktype = 0;
			var tickbuffer = 0;
			var otherbuff = 0;
			var usedeltaalt = 0;

			if (args.Length != 0)
			{
				PROCESSES = int.Parse(args[0]);
				stop_with_ticks = false;

				if (args.Length >= 2)
				{
					TICKS = int.Parse(args[1]);
					stop_with_ticks = true;
				}

				if (args.Length >= 3)
					networktype = int.Parse(args[2]);
				if (args.Length >= 4)
					tickbuffer = int.Parse(args[3]);
				if (args.Length >= 5)
					otherbuff = int.Parse(args[4]);
				if (args.Length >= 6)
					usedeltaalt = int.Parse(args[6]);
			}

			Console.WriteLine("Config is {0} processes, for {1} ticks, network {2}, tickbuffer={3} and other buffer={4}", PROCESSES, TICKS, networktype, tickbuffer, otherbuff);

			var servertoken = new CancellationTokenSource();
			var server = networktype == 0 ? null : NetworkChannelServer.HostServer(servertoken.Token);
			using (networktype == 0 ? null : new NetworkChannelScope(n => {

				if (networktype == 1)
					return string.Equals(n, "tick");
				if (networktype == 2)
					return true;

				return false;

			}))
			{
				var chan_in = ChannelManager.CreateChannel<T>(buffersize: otherbuff);
				var chan_tick = ChannelManager.CreateChannel<T>("tick", buffersize: tickbuffer);
				var chan_out = ChannelManager.CreateChannel<T>(buffersize: otherbuff);

				// Start the delta process
				if (usedeltaalt != 0)
					RunDeltaAlt(chan_in, chan_out, chan_tick);
				else
					RunDelta(chan_in, chan_out, chan_tick);

				IChannel<T> chan_new = null;

				// Spin up the forwarders
				for (var i = 0; i < PROCESSES - 2; i++)
				{
					//Console.WriteLine("Starting process {0}", i);
					chan_new = ChannelManager.CreateChannel<T>(buffersize: otherbuff);
					RunIdentity(chan_out, chan_new);
					chan_out = chan_new;
				}
				
				// Close the ring
				RunIdentity(chan_out, chan_in);

				// Start the tick collector 
				var t = RunTickCollectorAsync(chan_tick, chan_in, stop_with_ticks);

				// Inject a value into the ring
				chan_in.WriteNoWait(1);

				Console.WriteLine("Running, press CTRL+C to stop");

				// Wait for the tick collector to finish measuring
				t.WaitForTaskOrThrow();

				// Shut down the server if it is running
				if (server != null)
				{
					servertoken.Cancel();
					server.WaitForTaskOrThrow();
				}
			}
		}
	}
}
