using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
	public class BroadcastTest
	{
		public class CounterShim
		{
			public int Count;

			public void Increment()
			{
				System.Threading.Interlocked.Increment(ref Count);
			}
		}

		public static Task RunReader<T>(IChannel<T> channel, IEnumerable<T> values, CounterShim counter)
		{
			return AutomationExtensions.RunTask(
				new { chan = channel.AsReadOnly() },
				async self =>
				{
					foreach (var v in values)
					{
						var r = await self.chan.ReadAsync();
						counter.Increment();
						if (Comparer<T>.Default.Compare(v, r) != 0)
							throw new UnittestException(string.Format("Got {0} but expected {1}", r, v));
					}
				}
			);
		}

		public static Task RunWriter<T>(IChannel<T> channel, IEnumerable<T> values)
		{
			return AutomationExtensions.RunTask(
				new { chan = channel.AsWriteOnly() },
				async self =>
				{
					foreach (var v in values)
						await self.chan.WriteAsync(v);
				}
			);
		}

		private class Writer
		{
			[BroadcastChannelName("bcast", initialBarrierSize: 10)]
			public IWriteChannel<int> chan = null;
		}

        [TEST_METHOD]
		public void TestSimple()
		{
			var values = new[] { 0, 1, 2, 3, 4 };
			var c = ChannelManager.CreateChannel<int>(broadcast: true); ;
			var counter = new CounterShim();
			var readercount = 10;

			var readers = Enumerable.Range(0, readercount).Select(x => RunReader(c, values, counter)).ToArray();
			var writer = RunWriter(c, values);

			Task.WhenAll(readers.Union(new[] { writer })).WaitForTaskOrThrow();
			if (counter.Count != readercount * values.Length)
				throw new UnittestException(string.Format("The counter said {0} values were read, but {1} was expected", counter.Count, readercount * values.Length));
		}

        [TEST_METHOD]
		public void TestMultiWriter()
		{
			var values = new[] { 0, 1, 2, 3, 4 };
			var readervalues = new[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4 };
			var counter = new CounterShim();
			var readercount = 10;

			var c = ChannelManager.CreateChannel<int>(broadcast: true, initialBroadcastBarrier: readercount);

			var writer1 = RunWriter(c, values);
			var writer2 = RunWriter(c, values);

			var readers = Enumerable.Range(0, readercount).Select(x => RunReader(c, readervalues, counter)).ToArray();

			Task.WhenAll(readers.Union(new[] { writer1, writer2 })).WaitForTaskOrThrow();
			if (counter.Count != readercount * readervalues.Length)
				throw new UnittestException(string.Format("The counter said {0} values were read, but {1} was expected", counter.Count, readercount * readervalues.Length));
		}

        [TEST_METHOD]
		public void TestLeave()
		{
			var values = new[] { 0, 1, 2, 3, 4 };
			var readervalues = new[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4 };
			var c = ChannelManager.CreateChannel<int>(broadcast: true);
			var counter = new CounterShim();
			var readercount = 10;

			((IJoinAbleChannel)c).Join(true);

			var writer1 = RunWriter(c, values);
			var writer2 = RunWriter(c, values);

			var readers = Enumerable.Range(0, readercount).Select(x => RunReader(c, readervalues, counter)).ToArray();

			System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
			if (counter.Count != 0)
				throw new UnittestException("Broadcast has progressed even when there are not enough readers");

			((IJoinAbleChannel)c).Leave(true);

			Task.WhenAll(readers.Union(new[] { writer1, writer2 })).WaitForTaskOrThrow();
			if (counter.Count != readercount * readervalues.Length)
				throw new UnittestException(string.Format("The counter said {0} values were read, but {1} was expected", counter.Count, readercount * readervalues.Length));
		}

        [TEST_METHOD]
		public void TestScope()
		{
			var values = new[] { 0, 1, 2, 3, 4 };
			var counter = new CounterShim();
			var readercount = 10;
			var name = "bcast";

			using (new IsolatedChannelScope())
			{
				var writer = AutomationExtensions.RunTask(
					new { chan = ChannelMarker.ForWrite<int>(name, broadcast: true, initialBroadcastBarrier: readercount) },
					async self =>
					{
						foreach (var v in values)
							await self.chan.WriteAsync(v);
					}
				);

				var readers = Enumerable.Range(0, readercount).Select(x => AutomationExtensions.RunTask(
					new { chan = ChannelMarker.ForRead<int>(name) },
					async self =>
					{
						foreach (var v in values)
						{
							var r = await self.chan.ReadAsync();
							counter.Increment();
							if (Comparer<int>.Default.Compare(v, r) != 0)
								throw new UnittestException(string.Format("Got {0} but expected {1}", r, v));
						}
					}
				)).ToArray();

				Task.WhenAll(readers.Union(new[] { writer })).WaitForTaskOrThrow();
				if (counter.Count != readercount * values.Length)
					throw new UnittestException(string.Format("The counter said {0} values were read, but {1} was expected", counter.Count, readercount * values.Length));
			}
		}

        [TEST_METHOD]
		public void TestAttributes()
		{
			var values = new[] { 0, 1, 2, 3, 4 };
			var counter = new CounterShim();
			var readercount = 10;
			var name = "bcast";

			using (new IsolatedChannelScope())
			{
				var writer = AutomationExtensions.RunTask(
					new Writer(),
					async self =>
					{
						foreach (var v in values)
							await self.chan.WriteAsync(v);
					}
				);

				var readers = Enumerable.Range(0, readercount).Select(x => AutomationExtensions.RunTask(
					new { chan = ChannelMarker.ForRead<int>(name) },
					async self =>
					{
						foreach (var v in values)
						{
							var r = await self.chan.ReadAsync();
							counter.Increment();
							if (Comparer<int>.Default.Compare(v, r) != 0)
								throw new UnittestException(string.Format("Got {0} but expected {1}", r, v));
						}
					}
				)).ToArray();

				Task.WhenAll(readers.Union(new[] { writer })).WaitForTaskOrThrow();
				if (counter.Count != readercount * values.Length)
					throw new UnittestException(string.Format("The counter said {0} values were read, but {1} was expected", counter.Count, readercount * values.Length));
			}
		}
	}
}

