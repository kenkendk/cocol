using System;
using NUnit.Framework;

namespace UnitTest
{
	[TestFixture]
	public class BenchmarkTests
	{
		[Test]
		public void CommsTimeTest()
		{
			CommsTimeAwait.MainClass.Main(new string[] { "--processes=3", "--ticks=10000" });
		}

		[Test]
		public void MandelbrotTest()
		{
			Mandelbrot.MainClass.Main(new string[] { "--width=64", "--height=64", "--iterations=10", "--repeats=10", "--noimages" });
		}

		[Test]
		public void StressedAltTest()
		{
			StressedAlt.MainClass.Main(new string[] { "--writers=10", "--channels=10" });
		}

		[Test]
		public void MonteCarloPiTest()
		{
			MonteCarloPi.MainClass.Main(new string[] { "1000", "10" });
		}

	}
}

