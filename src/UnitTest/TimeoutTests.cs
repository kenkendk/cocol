using System;
using System.Threading.Tasks;
using CoCoL;
using System.Collections.Generic;
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
	public class TimeoutTests
	{
        [TEST_METHOD]
		public void TestTimeoutSimple()
		{
			var c = ChannelManager.CreateChannel<int>();

			Func<Task> p = async() =>
			{
				try
				{
					await c.ReadAsync(TimeSpan.FromSeconds(2));
					throw new Exception("Timeout did not happen?");
				}
				catch (TimeoutException)
				{
				}
			};

			var t = p();
			if (!t.Wait(TimeSpan.FromSeconds(3)))
				throw new Exception("Failed to get timeout");
		}

        [TEST_METHOD]
		public void TestTimeoutMultiple()
		{
			var c1 = ChannelManager.CreateChannel<int>();
			var c2 = ChannelManager.CreateChannel<int>();
			var c3 = ChannelManager.CreateChannel<int>();

			Func<Task> p = async() =>
				{
					try
					{
						await MultiChannelAccess.ReadFromAnyAsync(TimeSpan.FromSeconds(2), c1, c2, c3);
						throw new Exception("Timeout did not happen?");
					}
					catch (TimeoutException)
					{
					}
				};

			var t = p();
			if (!t.Wait(TimeSpan.FromSeconds(3)))
				throw new Exception("Failed to get timeout");

		}

        [TEST_METHOD]
		public void TestTimeoutMultipleTimes()
		{
			var c1 = ChannelManager.CreateChannel<int>();
			var c2 = ChannelManager.CreateChannel<int>();
			var c3 = ChannelManager.CreateChannel<int>();
			var c4 = ChannelManager.CreateChannel<int>();

			Func<Task> p = async() =>
				{
					try
					{
						var t = await Task.WhenAny(
							c1.ReadAsync(TimeSpan.FromSeconds(5)),
							c2.ReadAsync(TimeSpan.FromSeconds(4)),
							c3.ReadAsync(TimeSpan.FromSeconds(2)),
							c4.ReadAsync(TimeSpan.FromSeconds(4))
						);

						if (!t.IsFaulted || !(t.Exception.InnerException is TimeoutException))
							throw new Exception("Timeout did not happen?");
					}
					catch (TimeoutException)
					{
					}
				};

			if (!p().Wait(TimeSpan.FromSeconds(3)))
				throw new Exception("Failed to get timeout");

			if (!p().Wait(TimeSpan.FromSeconds(3)))
				throw new Exception("Failed to get timeout");
			
		}

        [TEST_METHOD]
		public void TestTimeoutMultipleTimesSuccession()
		{
			var c1 = ChannelManager.CreateChannel<int>();
			var c2 = ChannelManager.CreateChannel<int>();
			var c3 = ChannelManager.CreateChannel<int>();
			var c4 = ChannelManager.CreateChannel<int>();

			Func<Task> p = async() =>
				{
					try
					{
						var tasks = new List<Task>(new [] {
							c1.ReadAsync(TimeSpan.FromSeconds(7)),
							c2.ReadAsync(TimeSpan.FromSeconds(3)),
							c3.ReadAsync(TimeSpan.FromSeconds(2)),
							c4.ReadAsync(TimeSpan.FromSeconds(7))
						});

						//Console.WriteLine("Waiting for c3");
						var t = await Task.WhenAny(tasks);
						//Console.WriteLine("Not waiting for c3");

						if (!t.IsFaulted || !(t.Exception.InnerException is TimeoutException))
							throw new Exception("Timeout did not happen on c3?");

						if (!tasks[2].IsFaulted || !(tasks[2].Exception.InnerException is TimeoutException))
						{
							for (var i = 0; i < tasks.Count; i++)
								if (tasks[i].IsFaulted && i != 2)
									throw new Exception(string.Format("Timeout happened on c{0}, but should have happened on c3?", i + 1));
							
							throw new Exception("Timeout happened on another channel than c3?");
						}

						tasks.RemoveAt(2);

						if (tasks.Any(x => x.IsFaulted))
							throw new Exception("Unexpected task fault?");

						//Console.WriteLine("Waiting for c2");
						t = await Task.WhenAny(tasks);
						//Console.WriteLine("Not waiting for c2");

						if (!t.IsFaulted || !(t.Exception.InnerException is TimeoutException))
							throw new Exception("Timeout did not happen for c2?");

						if (!tasks[1].IsFaulted || !(tasks[1].Exception.InnerException is TimeoutException))
						{
							for (var i = 0; i < tasks.Count; i++)
								if (tasks[i].IsFaulted && i != 1)
									throw new Exception(string.Format("Timeout happened on c{0}, but should have happened on c2?", i + 1));
							throw new Exception("Timeout happened on another channel than c2?");
						}

						tasks.RemoveAt(1);

						if (tasks.Any(x => x.IsFaulted))
							throw new Exception("Unexpected task fault?");
						
						//Console.WriteLine("Completed");
					}
					catch (TimeoutException)
					{
					}
				};

			for (var i = 0; i < 5; i++)
				if (!p().Wait(TimeSpan.FromSeconds(4)))
					throw new Exception("Failed to get timeout");
		}

        [TEST_METHOD]
		public void TestMixedTimeout()
		{
			var c = ChannelManager.CreateChannel<int>();

			Func<Task> p = async() =>
				{
					try
					{
						var tasks = new List<Task>(new [] {
							c.ReadAsync(),
							c.ReadAsync(TimeSpan.FromSeconds(1)),
							c.ReadAsync(TimeSpan.FromSeconds(2))
						});

						var t = await Task.WhenAny(tasks);

						if (!t.IsFaulted || !(t.Exception.InnerException is TimeoutException))
							throw new Exception("Timeout did not happen on op2?");

						if (!tasks[1].IsFaulted || !(tasks[1].Exception.InnerException is TimeoutException))
						{
							for (var i = 0; i < tasks.Count; i++)
								if (tasks[i].IsFaulted && i != 1)
									throw new Exception(string.Format("Timeout happened on op{0}, but should have happened on op2?", i + 1));

							throw new Exception("Timeout happened on another channel than op2?");
						}

						tasks.RemoveAt(1);

						t = await Task.WhenAny(tasks);

						if (!t.IsFaulted || !(t.Exception.InnerException is TimeoutException))
							throw new Exception("Timeout did not happen on op2?");

						if (!tasks[1].IsFaulted || !(tasks[1].Exception.InnerException is TimeoutException))
						{
							for (var i = 0; i < tasks.Count; i++)
								if (tasks[i].IsFaulted && i != 1)
									throw new Exception(string.Format("Timeout happened on op{0}, but should have happened on op3?", i + 1));

							throw new Exception("Timeout happened on another channel than op3?");
						}

					}
					catch (TimeoutException)
					{
					}
				};

			for (var i = 0; i < 5; i++)
				if (!p().Wait(TimeSpan.FromSeconds(3)))
					throw new Exception("Failed to get timeout");
		}

        [TEST_METHOD]
		public void TestTimeoutWithBuffers()
		{
			var c = ChannelManager.CreateChannel<int>(buffersize: 1);

			Func<Task> p = async() =>
				{
					try
					{
						var tasks = new List<Task>(new [] {
							c.WriteAsync(4),
							c.WriteAsync(5, TimeSpan.FromSeconds(1)),
							c.WriteAsync(6, TimeSpan.FromSeconds(2))
						});

						var t = await Task.WhenAny(tasks);

						if (!t.IsCompleted)
							throw new Exception("Buffered write failed?");

						tasks.RemoveAt(0);

						t = await Task.WhenAny(tasks);

						if (!t.IsFaulted || !(t.Exception.InnerException is TimeoutException))
							throw new Exception("Timeout did not happen on op1?");

						if (!tasks[0].IsFaulted || !(tasks[0].Exception.InnerException is TimeoutException))
						{
							for (var i = 0; i < tasks.Count; i++)
								if (tasks[i].IsFaulted && i != 0)
									throw new Exception(string.Format("Timeout happened on op{0}, but should have happened on op1?", i + 1));

							throw new Exception("Timeout happened on another channel than op1?");
						}
					}
					catch (TimeoutException)
					{
					}
				};

			for (var i = 0; i < 5; i++)
				if (!p().Wait(TimeSpan.FromSeconds(3)))
					throw new Exception("Failed to get timeout");

		}
	}
}

