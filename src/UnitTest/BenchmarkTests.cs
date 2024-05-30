using System;
using CoCoL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{

	public class BenchmarkTests
	{
		[TestMethod]
		public void CommsTimeTest()
		{
			CommsTimeAwait.MainClass.Main(new string[] { "--processes=3", "--ticks=10000" });
		}

		[TestMethod]
		public void MandelbrotTest()
		{
			Mandelbrot.MainClass.Main(new string[] { "--width=64", "--height=64", "--iterations=10", "--repeats=10", "--noimages" });
		}

		// This one depends too much on the scheduler.
		// The channels are indeed fair, but the scheduler is not guaranteed to
		// continue threads in the exact order that they are ready

		/*[TestMethod]
		public void StressedAltTest()
		{
			StressedAlt.MainClass.Main(new string[] { "--writers=10", "--channels=10" });
		}*/

		[TestMethod]
		public void MonteCarloPiTest()
		{
			MonteCarloPi.MainClass.Main(new string[] { "1000", "10" });
		}

	}
}

