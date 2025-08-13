using System;
using System.Threading.Tasks;
using CoCoL;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
	[TestClass]
	public class ChannelOverflowTests
	{
		[TestMethod]
		public void TestInfiniteReadTimeout()
		{
			using (new IsolatedChannelScope())
			{
				var c = ChannelManager.CreateChannel<int>(maxPendingWriters: 2, pendingWritersOverflowStrategy: QueueOverflowStrategy.LIFO);
				var t = c.ReadAsync(Timeout.Infinite);
				Task.Delay(500).WaitForTaskOrThrow();
				if (t.IsFaulted)
					throw new UnittestException("Infinite timeout timed out?");
				var ct = new System.Threading.CancellationTokenSource();
				var t2 = c.ReadAsync(Timeout.Infinite, ct.Token);
				Task.Delay(500).WaitForTaskOrThrow();
				if (t2.IsFaulted)
					throw new UnittestException("Infinite timeout timed out?");
				c.Retire(true);
				Task.Delay(500).WaitForTaskOrThrow();
				if (!t2.IsFaulted)
					throw new UnittestException("Read cancellation failed");
			}
		}


		[TestMethod]
		public void TestReaderReject()
		{
			TestReaderOverflow(QueueOverflowStrategy.Reject);
		}

		[TestMethod]
		public void TestReaderLIFO()
		{
			TestReaderOverflow(QueueOverflowStrategy.LIFO);
		}

		[TestMethod]
		public void TestReaderFIFO()
		{
			TestReaderOverflow(QueueOverflowStrategy.FIFO);
		}


		[TestMethod]
		public void TestWriterReject()
		{
			TestWriterOverflow(QueueOverflowStrategy.Reject);
		}

		[TestMethod]
		public void TestWriterLIFO()
		{
			TestWriterOverflow(QueueOverflowStrategy.LIFO);
		}

		[TestMethod]
		public void TestWriterFIFO()
		{
			TestWriterOverflow(QueueOverflowStrategy.FIFO);
		}

		[TestMethod]
		public void TestBufferedWriterReject()
		{
			TestWriterOverflow(QueueOverflowStrategy.Reject, 2);
		}

		[TestMethod]
		public void TestBufferedWriterLIFO()
		{
			TestWriterOverflow(QueueOverflowStrategy.LIFO, 2);
		}

		[TestMethod]
		public void TestBufferedWriterFIFO()
		{
			TestWriterOverflow(QueueOverflowStrategy.FIFO, 2);
		}

		private void TestReaderOverflow(QueueOverflowStrategy strategy)
		{
			using (new IsolatedChannelScope())
			{
				var readertasks = Enumerable.Range(0, 4).Select(count =>
					AutomationExtensions.RunTask(new
					{
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
				Task.WhenAny(readertasks.Union(new[] { Task.Delay(1000) })).WaitForTaskOrThrow();
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
					AutomationExtensions.RunTask(new
					{
						Input = ChannelMarker.ForWrite<int>("channel", buffersize: buffers, maxPendingWriters: 3, pendingWritersOverflowStrategy: strategy)
					},
						async x =>
						{
							//Console.WriteLine("Started {0}", count);
							await x.Input.WriteAsync(42);
						})
				).ToList();

				using (ChannelManager.GetChannel<int>("channel").AsReadOnly())
					Task.Delay(500).WaitForTaskOrThrow();
				Task.WhenAny(writertasks.Union(new[] { Task.Delay(1000) })).WaitForTaskOrThrow();
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

				Assert.IsTrue(writertasks[discard].IsFaulted, $"Task state was {writertasks[discard].Status}, but expected faulted");
				TestAssert.IsInstanceOf<ChannelOverflowException>(writertasks[discard].Exception.Flatten().InnerExceptions.First());

				writertasks.RemoveAt(discard);

				Assert.IsTrue(writertasks.All(x => x.IsCompleted && !x.IsFaulted && !x.IsCanceled));
			}

		}
	}
}

