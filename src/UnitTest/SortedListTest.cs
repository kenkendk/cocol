using System;
using System.Linq;

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
	public class SortedListTest
	{

        [TEST_METHOD]
		public void TestListSimple()
		{
			TestAndCompare(new[] { 4, 3, 2, 1 });
			TestAndCompare(new[] { 1, 2, 3, 4 });
			TestAndCompare(new[] { 3, 4, 1, 2 });

			TestAndCompare(new[] { 1, 2, 4, 3 });
			TestAndCompare(new[] { 1, 3, 4, 2 });

			TestAndCompare(new[] { 1, 2, 5, 4, 3, 6, 7, 8, 9, 10 });
		}

        [TEST_METHOD]
		public void TestList()
		{
			for (var rp = 0; rp < 100; rp++)
			{
				var rnd = new Random();
				TestAndCompare((from x in Enumerable.Range(0, 1000)
					select rnd.Next()).Distinct().ToArray());
			}
		}

		private void TestAndCompare<T>(T[] numbers)
			where T : IComparable<T>
		{
			var sorted = from n in numbers
				orderby n
				select n;

			var sl = new CoCoL.SortedList<T, T>();
			foreach (var n in numbers)
				sl.Add(n, n);

			var cmp = sl.Zip(sorted, (a, b) => new { SL = a, Real = b });

			foreach (var x in cmp)
				if (x.Real.CompareTo(x.SL.Key) != 0 || x.Real.CompareTo(x.SL.Value) != 0)
				{
					foreach(var y in cmp)
						Console.WriteLine("{0}: ({1}, {2})", y.Real, y.SL.Key, y.SL.Value);
					throw new UnittestException("Sorted sequence was incorrect!");
				}
		}
	}
}

