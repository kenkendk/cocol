using System;
using CoCoL;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Linq;

namespace MandelbrotDynamic
{
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
	}

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
			RunAsync().Wait();
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

			// Set up an image buffer
			var pixels = m_width * m_height;
			using(var img = new Bitmap(m_width, m_height))
			{
				// Collect all pixels
				for(var i = 0; i < pixels; i++)
				{
					var px = await worker_channel.ReadAsync();
					img.SetPixel(px.x - m_left, px.y - m_top, ColorMap(px.value, m_iterations));
				}

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
			m_channel.WriteAsync(new Pixel() { x = m_x, y = m_y, value = n });
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

	/// <summary>
	/// The main class provides the driver for starting jobs
	/// </summary>
	class MainClass
	{
		public static void Main(string[] args)
		{
			// Send jobs into the network

			Render[] jobs;

			if (args.Length == 3)
			{
				jobs = (
					from n in Enumerable.Range(0, 11)
					select new Render(int.Parse(args[0]), int.Parse(args[1]), int.Parse(args[2]))
				).ToArray();
			}
			else
			{
				jobs = new Render[] {
					new Render(500, 500, 10),	
					new Render(500, 500, 10),	
					new Render(500, 500, 100),
					new Render(500, 500, 256),
					new Render(500, 500, 1000)
				};
			}
			foreach (var job in jobs)
				job.Run();
		}
	}


}
