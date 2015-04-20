using System;
using CoCoL;

namespace Tester
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			//CoCoL.Loader.StartFromTypes(typeof(TickCollector), typeof(CommsTime));
			CoCoL.Loader.StartFromTypes(typeof(TickCollectorCallback), typeof(CommsTimeCallback));

			Console.WriteLine("Running, press CTRL+C to stop");
			var rv = new System.Threading.ManualResetEventSlim(false);
			rv.Wait();
			//System.Threading.Thread.Sleep(50000);


			// WORK:

			//** Poison support
			//** Poison all through channel manager ?
			//** Mixed-type multi-channel operations
			//** Mixed-mode multi-channel operations

		}
	}


}
