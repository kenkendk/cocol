using System;
using System.Threading.Tasks;
using CoCoL;
using System.Linq;

#if NETCOREAPP2_0
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TOP_LEVEL = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using TEST_METHOD = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#else
using NUnit.Framework;
using TOP_LEVEL = NUnit.Framework.TestFixtureAttribute;
using TEST_METHOD = NUnit.Framework.TestAttribute;
#endif

namespace UnitTest
{
    [TOP_LEVEL]
	public class ChannelOverflowTests
	{
        [TEST_METHOD]
		public void TestReaderReject()
		{
			TestReaderOverflow(QueueOverflowStrategy.Reject);
		}

        [TEST_METHOD]
		public void TestReaderLIFO()
		{
			TestReaderOverflow(QueueOverflowStrategy.LIFO);
		}

        [TEST_METHOD]
		public void TestReaderFIFO()
		{
			TestReaderOverflow(QueueOverflowStrategy.FIFO);
		}


        [TEST_METHOD]
		public void TestWriterReject()
		{
			TestWriterOverflow(QueueOverflowStrategy.Reject);
		}

        [TEST_METHOD]
		public void TestWriterLIFO()
		{
			TestWriterOverflow(QueueOverflowStrategy.LIFO);
		}

        [TEST_METHOD]
		public void TestWriterFIFO()
		{
			TestWriterOverflow(QueueOverflowStrategy.FIFO);
		}

        [TEST_METHOD]
		public void TestBufferedWriterReject()
		{
			TestWriterOverflow(QueueOverflowStrategy.Reject, 2);
		}

        [TEST_METHOD]
		public void TestBufferedWriterLIFO()
		{
			TestWriterOverflow(QueueOverflowStrategy.LIFO, 2);
		}

        [TEST_METHOD]
		public void TestBufferedWriterFIFO()
		{
			TestWriterOverflow(QueueOverflowStrategy.FIFO, 2);
		}

		private void TestReaderOverflow(QueueOverflowStrategy strategy)
		{
			using (new IsolatedChannelScope())
			{
				var readertasks = Enumerable.Range(0, 4).Select(count =>
					AutomationExtensions.RunTask(new {
						Input = ChannelMarker.ForRead<int>("channel", maxPendingReaders: 3, pendingReadersOverflowStrategy: strategy)
					},
              		async x =>
					{
						//Console.WriteLine("Started {0}", count);

						while (true)
							await x.Input.ReadAsync();
					})
              	).ToList();

				using (ChannelManager.GetChannel<int>("channel").AsWriteOnly())
					Task.Delay(500).WaitForTaskOrThrow();
				Task.WhenAny(readertasks.Union(new [] { Task.Delay(1000) })).WaitForTaskOrThrow();
				Task.Delay(500).WaitForTaskOrThrow();

				int discard;
				switch (strategy)
				{
					case QueueOverflowStrategy.FIFO:
						discard = 0;	
						break;
					case QueueOverflowStrategy.LIFO:
						discard = readertasks.Count - 2;
						break;
					case QueueOverflowStrategy.Reject:
					default:
						discard = readertasks.Count - 1;
						break;
				}

				Assert.IsTrue(readertasks[discard].IsFaulted);
                TestAssert.IsInstanceOf<ChannelOverflowException>(readertasks[discard].Exception.Flatten().InnerExceptions.First());

				readertasks.RemoveAt(discard);

				Assert.IsTrue(readertasks.All(x => x.IsCompleted && !x.IsFaulted && !x.IsCanceled));
			}
		}

		private void TestWriterOverflow(QueueOverflowStrategy strategy, int buffers = 0)
		{
			using (new IsolatedChannelScope())
			{
				var writertasks = Enumerable.Range(0, 4 + buffers).Select(count =>
					AutomationExtensions.RunTask(new {
						Input = ChannelMarker.ForWrite<int>("channel", buffersize: buffers, maxPendingWriters: 3, pendingWritersOverflowStrategy: strategy)
					},
						async x =>
						{
							//Console.WriteLine("Started {0}", count);
							await x.Input.WriteAsync(42);
						})
				).ToList();

				using(ChannelManager.GetChannel<int>("channel").AsReadOnly())
					Task.Delay(500).WaitForTaskOrThrow();
				Task.WhenAny(writertasks.Union(new [] { Task.Delay(1000) })).WaitForTaskOrThrow();
				Task.Delay(500).WaitForTaskOrThrow();

				int discard;
				switch (strategy)
				{
					case QueueOverflowStrategy.FIFO:
						discard = buffers;	
						break;
					case QueueOverflowStrategy.LIFO:
						discard = writertasks.Count - 2;
						break;
					case QueueOverflowStrategy.Reject:
					default:
						discard = writertasks.Count - 1;
						break;
				}

				Assert.IsTrue(writertasks[discard].IsFaulted);
                TestAssert.IsInstanceOf<ChannelOverflowException>(writertasks[discard].Exception.Flatten().InnerExceptions.First());

				writertasks.RemoveAt(discard);

				Assert.IsTrue(writertasks.All(x => x.IsCompleted && !x.IsFaulted && !x.IsCanceled));
			}

		}	
	}
}

