using System;
using CoCoL;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Linq;
using CoCoL.Network;
using System.Threading;
using System.Collections.Generic;

using Pixel = System.Tuple<int, int, int>;

namespace MandelbrotDynamic
{
	/*
	/// <summary>
	/// The pixel output
	/// </summary>
	struct Pixel
	{
		/// <summary>
		/// The pixel x coordinate
		/// </summary>
		public int x;
		/// <summary>
		/// The pixel y coordinate
		/// </summary>
		public int y;
		/// <summary>
		/// The iteration count
		/// </summary>
		public int value;
	}*/

	/// <summary>
	/// The render process is responsible for spawning the workers
	/// </summary>
	class Render : IAsyncProcess
	{
		private readonly int m_top;
		private readonly int m_left;
		private readonly int m_width;
		private readonly int m_height;
		private readonly int m_iterations;

		public Render(int width, int height, int iterations)
			: this(height / -2, width / -2, width, height, iterations)
		{
		}

		public Render(int top, int left, int width, int height, int iterations)
		{
			m_top = top;
			m_left = left;
			m_width = width;
			m_height = height;
			m_iterations = iterations;
		}

		public void Run()
		{
			RunAsync().WaitForTaskOrThrow();
		}

		public async Task RunAsync()
		{
			var starttime = DateTime.Now;

			// Prepare the result channel
			var worker_channel = ChannelManager.CreateChannel<Pixel>();

			// Start all the workers width*height without waiting
			var workers = CoCoL.Loader.StartAsync(
				from x in Enumerable.Range(0, m_width)
				from y in Enumerable.Range(0, m_height)
				select new Worker(
					worker_channel,
					m_left + x, 
					m_top + y, 
					m_iterations
				)
			);

			var result_channel = 
				Config.NetworkedChannels && Config.NetworkChannelLatencyBufferSize > 0
				? new LatencyHidingReader<Pixel>(worker_channel, Config.NetworkChannelLatencyBufferSize)
				: worker_channel.AsReadOnly();

			// Set up an image buffer
			var pixels = m_width * m_height;
			using(var img = Config.DisableImages ? null : new Bitmap(m_width, m_height))
			{
				// Collect all pixels
				for(var i = 0; i < pixels; i++)
				{
					var px = await result_channel.ReadAsync();
					if (img != null)
						img.SetPixel(px.Item1 - m_left, px.Item2 - m_top, ColorMap(px.Item3, m_iterations));
				}

				if (img != null)
					img.Save(string.Format("{0}-{1}x{2}-{3}.png", DateTime.Now.Ticks, m_width, m_height, m_iterations), ImageFormat.Png);
			}

			Console.WriteLine("Rendered a {0}x{1}:{2} image in {3}", m_width, m_height, m_iterations, DateTime.Now - starttime);

			// Not required, but removes compiler warnings
			await workers;
		}

		public static Color ColorMap(int value, int max)
		{
			var v = Math.Max(0, Math.Min(255, (int)(255.0 / max * value)));
			return Color.FromArgb(255, v, v, v);			
		}
	}

	/// <summary>
	/// The worker processes are responsible for rendering each pixel
	/// </summary>
	class Worker : IProcess
	{
		private IChannel<Pixel> m_channel;
		private int m_x;
		private int m_y;
		private int m_iterations;

		public Worker(IChannel<Pixel> channel, int x, int y, int iterations)
		{
			m_channel = channel;
			m_x = x;
			m_y = y;
			m_iterations = iterations;
		}

		public void Run()
		{
			// Compute the value
			var n = Compute(m_x / 100.0, m_y / 100.0, m_iterations);
			if (n == m_iterations)
				n = 0;

			// Write the result, and terminate before completion
			m_channel.WriteAsync(new Pixel( m_x, m_y, n ));
		}

		private const double RADIUS_SQUARED = 4.0;

		private static int Compute(double a, double b, int maxIterations)
		{
			int n = 0;
			var x = a;
			var y = b;
			var xSquared = x * x;
			var ySquared = y * y;
			while ((n < maxIterations) && ((xSquared + ySquared) < RADIUS_SQUARED)) {
				double tmp = (xSquared - ySquared) + a;
				y = ((2 * x) * y) + b;
				x = tmp;
				xSquared = x * x;
				ySquared = y * y;
				n++;
			}

			return n;
		}
	}

	public class Config
	{
		/// <summary>
		/// The width of the image
		/// </summary>
		[CommandlineOption("The width of the image", "width", "w")]
		public static int Width = 500;

		/// <summary>
		/// The height of the image
		/// </summary>
		[CommandlineOption("The height of the image", "height", "h")]
		public static int Height = 500;

		/// <summary>
		/// The number of iterations on each pixel
		/// </summary>
		[CommandlineOption("The number of iterations on each pixel", "iterations", "i")]
		public static int Iterations = 100;

		/// <summary>
		/// The number of iterations on each pixel
		/// </summary>
		[CommandlineOption("The number of repeated runs", "repeats", "r")]
		public static int Repeats = 1;

		/// <summary>
		/// A value indicating if the channels should be network based
		/// </summary>
		[CommandlineOption("Indicates if the channels are network hosted", longname: "network")]
		public static bool NetworkedChannels = false;

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
		/// Disables all image operations
		/// </summary>
		[CommandlineOption("Disable writing images, prevents loading GDK+", longname: "noimages")]
		public static bool DisableImages = false;

		/// <summary>
		/// Parses the commandline args
		/// </summary>
		/// <param name="args">The commandline arguments.</param>
		public static bool Parse(List<string> args)
		{
			return SettingsHelper.Parse<Config>(args, null);
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
	/// The main class provides the driver for starting jobs
	/// </summary>
	class MainClass
	{
		public static void Main(string[] _args)
		{
			var args = new List<string>(_args);
			// Send jobs into the network
			if (!Config.Parse(args))
				return;

			Console.WriteLine("Config is: {0}", Config.AsString());

			var servertoken = new CancellationTokenSource();
			var server = (Config.NetworkedChannels && Config.ChannelServerSelfHost) ? NetworkChannelServer.HostServer(servertoken.Token, Config.ChannelServerHostname, Config.ChannelServerPort) : null;

			if (Config.NetworkedChannels && !Config.ChannelServerSelfHost)
				NetworkConfig.Configure(Config.ChannelServerHostname, Config.ChannelServerPort, true);

			using (Config.NetworkedChannels ? new NetworkChannelScope(redirectunnamed: true) : null)
				foreach(var job in Enumerable.Range(0, Config.Repeats).Select(x => new Render(Config.Width, Config.Height, Config.Iterations)))
					job.Run();

			servertoken.Cancel();
			if (server != null)
				server.WaitForTaskOrThrow();

		}
	}


}
