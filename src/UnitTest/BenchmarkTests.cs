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
			CommsTimeAwait.MainClass.Main(new string[] { "3", "10000" });
		}

		[Test]
		public void MandelbrotTest()
		{
			Mandelbrot.MainClass.Main(new string[] { "64", "64", "10" });
		}

		[Test]
		public void StressedAltTest()
		{
			StressedAlt.MainClass.Main(new string[] { "10", "10" });
		}

	}
}

