using System;
using CoCoL;

#if NETCOREAPP2_0
using TOP_LEVEL = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using TEST_METHOD = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#else
using TOP_LEVEL = NUnit.Framework.TestFixtureAttribute;
using TEST_METHOD = NUnit.Framework.TestAttribute;
#endif

namespace UnitTest
{
    [TOP_LEVEL]
	public class BenchmarkTests
	{
        [TEST_METHOD]
		public void CommsTimeTest()
		{
			CommsTimeAwait.MainClass.Main(new string[] { "--processes=3", "--ticks=10000" });
		}

        [TEST_METHOD]
		public void MandelbrotTest()
		{
			Mandelbrot.MainClass.Main(new string[] { "--width=64", "--height=64", "--iterations=10", "--repeats=10", "--noimages" });
		}

        // This one depends too much on the scheduler.
        // The channels are indeed fair, but the scheduler is not guaranteed to
        // continue threads in the exact order that they are ready

        /*[TEST_METHOD]
		public void StressedAltTest()
		{
			StressedAlt.MainClass.Main(new string[] { "--writers=10", "--channels=10" });
		}*/

        [TEST_METHOD]
		public void MonteCarloPiTest()
		{
			MonteCarloPi.MainClass.Main(new string[] { "1000", "10" });
		}

	}
}

