using System;
using CoCoL;
using System.Threading.Tasks;
using T = System.Int32;
using CoCoL.Network;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

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
			try
			{
				while (true)
				{
					var value = await chan_read.ReadAsync();
                    // Parallel delta copy
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
						round_complete = tickcount >= Config.Ticks;
					else
						round_complete = (DateTime.Now - m_last).Ticks >= measure_span;

					if (round_complete)
					{
						var duration = DateTime.Now - m_last;
						Console.WriteLine("Got {0} ticks in {1} seconds, speed is {2} rounds/s ({3} msec/comm)", tickcount, duration, tickcount / duration.TotalSeconds, duration.TotalMilliseconds / ((tickcount) * Config.Processes));
						Console.WriteLine("Time per iteration: {0} microseconds", (duration.TotalMilliseconds * 1000) / tickcount);
						Console.WriteLine("Time per communication: {0} microseconds", (duration.TotalMilliseconds * 1000) / tickcount / 4);

						tickcount = 0;
						m_last = DateTime.Now;

						// For shutdown, we retire the initial channel
						if (++rounds >= Config.MeasureCount)
							stop.Retire();
					}
				}
			}
			catch(RetiredException)
			{
				chan.Retire();
			}
		}

		public class Config
		{
			/// <summary>
			/// The number of measurements to perform in the tick collector before exiting
			/// </summary>
			[CommandlineOption("The number of measure rounds to perform", longname: "rounds")]
			public const int MeasureCount = 10;

			/// <summary>
			/// The number of processes in the ring
			/// </summary>
			[CommandlineOption("The number of processes in the communication ring", longname: "processes")]
			public static int Processes = 3; //10000000;

			/// <summary>
			/// The number of ticks to measure in each round
			/// </summary>
			[CommandlineOption("The number of ticks in each measurement rount", longname: "ticks")]
			public static int Ticks = 1000000;

			/// <summary>
			/// A value indicating if the network should stop after MeasureCount * Ticks
			/// </summary>
			[CommandlineOption("Indicates if the network should stop running after measurements are completed", longname: "autostop")]
			public static bool StopWithTicks = true;

			/// <summary>
			/// A value indicating if the tick channel should be network based
			/// </summary>
			[CommandlineOption("Indicates if the tick channel is network hosted", longname: "ticknetwork")]
			public static bool TickChannelNetworked = false;

			/// <summary>
			/// A value indicating if the delta process should use two-phase offers
			/// </summary>
			[CommandlineOption("Indicates if the delta process uses a WriteToAny call", longname: "deltaalt")]
			public static bool UseAltingForDeltaProcess = false;

			/// <summary>
			/// A value indicating if all channels should be network based
			/// </summary>
			[CommandlineOption("Indicates if all channels are created as network channels", longname: "allnetwork")]
			public static bool AllChannelsNetworked = false;

			/// <summary>
			/// The size of the latency hiding buffer used on network channels
			/// </summary>
			[CommandlineOption("The buffer size for network channels", longname: "buffersize")]
			public static int NetworkChannelLatencyBufferSize = 0;

			/// <summary>
			/// The hostname for the channel server
			/// </summary>
			[CommandlineOption("The hostname for the channel server", longname: "host")]
			public static string ChannelServerHostname = "localhost";

			/// <summary>
			/// The port for the channel server
			/// </summary>
			[CommandlineOption("The port for the channel server", longname: "port")]
			public static int ChannelServerPort = 8888;

			/// <summary>
			/// A value indicating if the channel server is on the local host
			/// </summary>
			[CommandlineOption("Indicates if the process hosts a server itself", longname: "selfhost")]
			public static bool ChannelServerSelfHost = true;

			/// <summary>
			/// Parses the commandline args
			/// </summary>
			/// <param name="args">The commandline arguments.</param>
			public static bool Parse(string[] args)
			{
				return SettingsHelper.Parse<Config>(args.ToList(), null);
			}

			/// <summary>
			/// Returns the config object as a human readable string
			/// </summary>
			/// <returns>The string.</returns>
			public static string AsString()
			{
				return string.Join(", ", typeof(Config).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(x => string.Format("{0}={1}", x.Name, x.GetValue(null))));
			}
		}
			
		/// <summary>
		/// Wraps the channel with a latency hiding instance, if required by config
		/// </summary>
		/// <returns>The buffered write channel.</returns>
		/// <param name="input">The input channel.</param>
		private static IWriteChannel<T> AsBufferedWrite(IWriteChannel<T> input)
		{
			if (Config.NetworkChannelLatencyBufferSize != 0 && input is NetworkChannel<T> && (Config.AllChannelsNetworked || ((INamedItem)input).Name == "tick"))
				return new LatencyHidingWriter<T>(input, Config.NetworkChannelLatencyBufferSize);
			return input;
		}

		/// <summary>
		/// Wraps the channel with a latency hidign instance, if required by config
		/// </summary>
		/// <returns>The buffered read channel.</returns>
		/// <param name="input">The input channel.</param>
		private static IReadChannel<T> AsBufferedRead(IReadChannel<T> input)
		{
			if (Config.NetworkChannelLatencyBufferSize != 0 && input is NetworkChannel<T> && (Config.AllChannelsNetworked || ((INamedItem)input).Name == "tick"))
				return new LatencyHidingReader<T>(input, Config.NetworkChannelLatencyBufferSize);
			return input;
		}

		/// <summary>
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name="args">The command-line arguments.</param>
		public static void Main(string[] args)
		{
			if (!Config.Parse(args))
				return;

			Console.WriteLine("Config is: {0}", Config.AsString());

			var anynetwork = Config.AllChannelsNetworked || Config.TickChannelNetworked;

			var servertoken = new CancellationTokenSource();
			var server = (anynetwork && Config.ChannelServerSelfHost) ? NetworkChannelServer.HostServer(servertoken.Token, Config.ChannelServerHostname, Config.ChannelServerPort) : null;

			if (anynetwork && !Config.ChannelServerSelfHost)
				NetworkConfig.Configure(Config.ChannelServerHostname, Config.ChannelServerPort, true);

			using (anynetwork ? new NetworkChannelScope(n => {

				if (Config.TickChannelNetworked)
					return string.Equals(n, "tick");
				else if (Config.AllChannelsNetworked)
					return true;

				return false;

			}) : null)
			{
				var chan_in = ChannelManager.CreateChannel<T>();
				var chan_tick = ChannelManager.CreateChannel<T>("tick");
				var chan_out = ChannelManager.CreateChannel<T>();

				// Start the delta process
				if (Config.UseAltingForDeltaProcess)
					RunDeltaAlt(AsBufferedRead(chan_in), AsBufferedWrite(chan_out), AsBufferedWrite(chan_tick));
				else
					RunDelta(AsBufferedRead(chan_in), AsBufferedWrite(chan_out), AsBufferedWrite(chan_tick));

				IChannel<T> chan_new = null;

				// Spin up the forwarders
				for (var i = 0; i < Config.Processes - 2; i++)
				{
					//Console.WriteLine("Starting process {0}", i);
					chan_new = ChannelManager.CreateChannel<T>();
					RunIdentity(AsBufferedRead(chan_out), AsBufferedWrite(chan_new));
					chan_out = chan_new;
				}
				
				// Close the ring
				RunIdentity(AsBufferedRead(chan_out), AsBufferedWrite(chan_in));

				// Start the tick collector 
				var t = RunTickCollectorAsync(AsBufferedRead(chan_tick), chan_in, Config.StopWithTicks);

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
