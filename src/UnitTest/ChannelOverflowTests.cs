using System;
using NUnit.Framework;
using System.Threading.Tasks;
using CoCoL;
using System.Linq;

namespace UnitTest
{
	[TestFixture]
	public class ChannelOverflowTests
	{
		[Test]
		public void TestReaderReject()
		{
			TestReaderOverflow(QueueOverflowStrategy.Reject);
		}

		[Test]
		public void TestReaderLIFO()
		{
			TestReaderOverflow(QueueOverflowStrategy.LIFO);
		}

		[Test]
		public void TestReaderFIFO()
		{
			TestReaderOverflow(QueueOverflowStrategy.FIFO);
		}


		[Test]
		public void TestWriterReject()
		{
			TestWriterOverflow(QueueOverflowStrategy.Reject);
		}

		[Test]
		public void TestWriterLIFO()
		{
			TestWriterOverflow(QueueOverflowStrategy.LIFO);
		}

		[Test]
		public void TestWriterFIFO()
		{
			TestWriterOverflow(QueueOverflowStrategy.FIFO);
		}

		[Test]
		public void TestBufferedWriterReject()
		{
			TestWriterOverflow(QueueOverflowStrategy.Reject, 2);
		}

		[Test]
		public void TestBufferedWriterLIFO()
		{
			TestWriterOverflow(QueueOverflowStrategy.LIFO, 2);
		}

		[Test]
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
				Assert.IsInstanceOfType(typeof(ChannelOverflowException), readertasks[discard].Exception.Flatten().InnerExceptions.First());

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
				Assert.IsInstanceOfType(typeof(ChannelOverflowException), writertasks[discard].Exception.Flatten().InnerExceptions.First());

				writertasks.RemoveAt(discard);

				Assert.IsTrue(writertasks.All(x => x.IsCompleted && !x.IsFaulted && !x.IsCanceled));
			}

		}	
	}
}

