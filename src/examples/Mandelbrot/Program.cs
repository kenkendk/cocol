using System;
using CoCoL;
using System.Drawing;
using System.Drawing.Imaging;
using CoCoL.Network;
using System.Collections.Generic;
using System.Linq;

namespace Mandelbrot
{
	/// <summary>
	/// The overall task being sent
	/// </summary>
	struct RenderTask
	{
		/// <summary>
		/// The top offset
		/// </summary>
		public int top;
		/// <summary>
		/// The left offset
		/// </summary>
		public int left;
		/// <summary>
		/// The result width of the image
		/// </summary>
		public int width;
		/// <summary>
		/// The results height of the image
		/// </summary>
		public int height;
		/// <summary>
		/// The number of iterations pr. pixel
		/// </summary>
		public int iterations;

		/// <summary>
		/// Initializes a new instance of the <see cref="Mandelbrot.RenderTask"/> struct.
		/// </summary>
		/// <param name="width">The image width</param>
		/// <param name="height">The image height</param>
		/// <param name="iterations">The number of iterations</param>
		public RenderTask(int width, int height, int iterations)
		{
			this.top = height / -2;
			this.left = width / -2;
			this.width = width;
			this.height = height;
			this.iterations = iterations;
		}
	}

	/// <summary>
	/// The pixel input and output
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
	}
		
	/// <summary>
	/// The farmer process is responsible for spawning the workers
	/// </summary>
	class Farmer : IProcess
	{
		public const string FARMER_CHANNEL = "farmer";

		public async void Run()
		{
			//Set up channels
			var farmer_channel = ChannelManager.GetChannel<RenderTask>(Farmer.FARMER_CHANNEL).AsRead();
			var worker_channel = ChannelManager.GetChannel<Pixel>(Worker.WORKER_INPUT_CHANNEL).AsWrite();
			var harvester_channel = ChannelManager.GetChannel<RenderTask>(Harvester.HARVESTER_CHANNEL).AsWrite();

			try
			{
				while (true)
				{
					// Grab a job
					var task = await farmer_channel.ReadAsync();

					// Prepare and wait for the harvester
					await harvester_channel.WriteAsync(task);

					// Emit the individual pixels
					for(var x = 0; x < task.width; x++)
						for(var y = 0; y < task.height; y++)
							await worker_channel.WriteAsync(new Pixel() { x = task.left + x, y = task.top + y, value = task.iterations });
				}
			} 
			catch(RetiredException)
			{
				farmer_channel.Retire();
				worker_channel.Retire();
				harvester_channel.Retire();
			}
		}
	}

	/// <summary>
	/// The harvester process is responsible for collecting pixel results and rendering the image
	/// </summary>
	class Harvester : IProcess
	{
		public const string HARVESTER_CHANNEL = "harvester";
		public const string SHUTDOWN_CHANNEL = "shutdown";

		public async void Run()
		{
			// Set up channels
			var worker_channel = ChannelManager.GetChannel<Pixel>(Worker.WORKER_OUTPUT_CHANNEL).AsRead();
			var harvester_channel = ChannelManager.GetChannel<RenderTask>(Harvester.HARVESTER_CHANNEL).AsRead();
			var shutdown_channel = ChannelManager.GetChannel<bool>(Harvester.SHUTDOWN_CHANNEL).AsWrite();

			try
			{
				while (true)
				{
					// Wait for a new task
					var task = await harvester_channel.ReadAsync();
					var starttime = DateTime.Now;

					// Set up an image buffer
					var pixels = task.width * task.height;
					using(var img = Config.DisableImages ? null : new Bitmap(task.width, task.height))
					{
						// Collect all pixels
						for(var i = 0; i < pixels; i++)
						{
							var px = await worker_channel.ReadAsync();
							if (img != null)
								img.SetPixel(px.x - task.left, px.y - task.top, ColorMap(px.value, task.iterations));
						}

						if (img != null)
							img.Save(string.Format("{0}-{1}x{2}-{3}.png", DateTime.Now.Ticks, task.width, task.height, task.iterations), ImageFormat.Png);
						
						Console.WriteLine("Rendered a {0}x{1}:{2} image in {3}", task.width, task.height, task.iterations, DateTime.Now - starttime);
					}
				}
			} 
			catch(RetiredException)
			{
				worker_channel.Retire();
				harvester_channel.Retire();
				shutdown_channel.Retire();
			}
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
	[Process(WORKER_COUNT)]
	class Worker : IProcess
	{
		public const string WORKER_INPUT_CHANNEL = "worker_input";
		public const string WORKER_OUTPUT_CHANNEL = "worker_output";

		private const int WORKER_COUNT = 32;

		public async void Run()
		{
			// Set up the channels
			var input_channel = ChannelManager.GetChannel<Pixel>(Worker.WORKER_INPUT_CHANNEL).AsRead();
			var output_channel = ChannelManager.GetChannel<Pixel>(Worker.WORKER_OUTPUT_CHANNEL).AsWrite();

			try
			{
				while(true)
				{
					// Grab work
					var px = await input_channel.ReadAsync();

					// Compute the value
					var n = Compute(px.x / 100.0, px.y / 100.0, px.value);
					if (n == px.value)
						n = 0;

					// Write the result
					await output_channel.WriteAsync(new Pixel() { x = px.x, y = px.y, value = n });
				}
			}
			catch(RetiredException)
			{
				input_channel.Retire();
				output_channel.Retire();
			}
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
		[CommandlineOption("The width of the image", "width", "w", "500")]
		public static int Width = -1;

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
	public class MainClass
	{
		public static void Main(string[] _args)
		{
			var args = new List<string>(_args);
			// Send jobs into the network
			if (!Config.Parse(args))
				return;

			Console.WriteLine("Config is: {0}", Config.AsString());

			var farmer_channel = ChannelManager.GetChannel<RenderTask>(Farmer.FARMER_CHANNEL).AsWrite();
			var shutdown_channel = ChannelManager.GetChannel<bool>(Harvester.SHUTDOWN_CHANNEL).AsRead();

			// Auto-start all defined processes in this assembly
			CoCoL.Loader.StartFromAssembly(typeof(MainClass).Assembly);

			if (Config.Width != -1)
			{
				for (var i = 0; i < Config.Repeats; i++)
					farmer_channel.Write(new RenderTask(Config.Width, Config.Height, Config.Iterations));
			}
			else
			{
				// Send jobs into the network
				farmer_channel.Write(new RenderTask(500, 500, 10));
				farmer_channel.Write(new RenderTask(500, 500, 100));
				farmer_channel.Write(new RenderTask(500, 500, 256));
				farmer_channel.Write(new RenderTask(500, 500, 1000));
			}

			// Signal completion
			farmer_channel.Retire();

			try
			{
				// Block main thread until all jobs complete
				shutdown_channel.Read();
			}
			catch(RetiredException)
			{
			}
		}
	}


}
