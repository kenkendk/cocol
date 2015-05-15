using System;
using CoCoL;

namespace CommsTimeAwait
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			CoCoL.Loader.StartFromTypes(typeof(CommsTime));

			var p = new TickCollector();
			Console.WriteLine("Running, press CTRL+C to stop");

			// Start the collector and wait for exit
			p.Run();


		}
	}
}
