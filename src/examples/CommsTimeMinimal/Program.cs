using System;

namespace CommsTimeMinimal
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			CoCoL.Loader.StartFromTypes(typeof(TickCollector), typeof(CommsTime));

			Console.WriteLine("Running, press CTRL+C to stop");
			var rv = new System.Threading.ManualResetEventSlim(false);
			rv.Wait();
		}
	}
}
