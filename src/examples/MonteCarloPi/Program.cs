using System;
using CoCoL;
using System.Threading.Tasks;
using System.Linq;

namespace MonteCarloPi
{
	/// <summary>
	/// The data type passed in Monte carlo
	/// </summary>
	struct Point
	{
		/// <summary>
		/// The x coordinate
		/// </summary>
		public double X;
		/// <summary>
		/// The y coordinate
		/// </summary>
		public double Y;
	}

	class MainClass
	{
		public static void Main(string[] args)
		{
			var pointCount = 10000;
			var workerCount = 100;

			if (args.Length >= 2)
			{
				pointCount = int.Parse(args[0]);
				workerCount = int.Parse(args[1]);
			}

			var rnd = new Random();
			var count = 0L;

			// Values are in a lazy evaluated list
			var values = from n in Enumerable.Range(0, pointCount)
			             select new Point() { X = rnd.NextDouble(), Y = rnd.NextDouble() };

			// Perform fork/join, aka map/reduce
			var inside = ForkJoinProcessing.ForkJoinProcessAsync<Point, bool, long>(
				values,
	            p => 
				{
					// To test random-length workloads, sleep a random amount of time
					//System.Threading.Thread.Sleep(TimeSpan.FromSeconds(rnd.NextDouble() * 0.1));
					return Math.Sqrt((p.X * p.X) + (p.Y * p.Y)) < 1.0;
				},
                (prev, cur) =>
				{ 
					// The count is accessed from the scope
					count++;  
					return cur ? prev + 1 : prev;
				},
            	workerCount
			).Result;

			Console.WriteLine("Sent {0} points into the network and got {1} results, with PI computed to {2}", pointCount, count, (4.0 * inside) / count);
		}
	}
}
