using System;
using System.Threading.Tasks;
using CoCoL;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

#nullable enable

namespace UnitTest
{
	[TestClass]
	public class TimeoutTests
	{
		bool anyUnobservedExceptions = false;

		[TestInitialize]
		public void Setup()
		{
			// Force finalizers to run
			GC.Collect();
			GC.WaitForPendingFinalizers();

			// Clean up any previous handlers
			anyUnobservedExceptions = false;
			TaskScheduler.UnobservedTaskException += (s, e) =>
			{
				anyUnobservedExceptions = true;
				e.SetObserved();
			};
		}

		[TestCleanup]
		public void Cleanup()
		{
			// Force finalizers to run
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Assert.IsFalse(anyUnobservedExceptions, "Unobserved exception should have been caught.");
		}

		[TestMethod]
		public void TestTimeoutSimple()
		{
			var c = ChannelManager.CreateChannel<int>();

			async Task p()
			{
				try
				{
					await c.ReadAsync(TimeSpan.FromSeconds(2));
					throw new UnittestException("Timeout did not happen?");
				}
				catch (TimeoutException)
				{
				}
			}

			var t = p();
			if (!t.Wait(TimeSpan.FromSeconds(3)))
				throw new UnittestException("Failed to get timeout");
		}

		[TestMethod]
		public void TestTimeoutMultiple()
		{
			var c1 = ChannelManager.CreateChannel<int>();
			var c2 = ChannelManager.CreateChannel<int>();
			var c3 = ChannelManager.CreateChannel<int>();

			async Task p()
			{
				try
				{
					await MultiChannelAccess.ReadFromAnyAsync(TimeSpan.FromSeconds(2), c1, c2, c3);
					throw new UnittestException("Timeout did not happen?");
				}
				catch (TimeoutException)
				{
				}
			}

			var t = p();
			if (!t.Wait(TimeSpan.FromSeconds(3)))
				throw new UnittestException("Failed to get timeout");
		}

		[TestMethod]
		public async Task TestTimeoutMultipleTimes()
		{
			var c1 = ChannelManager.CreateChannel<int>();
			var c2 = ChannelManager.CreateChannel<int>();
			var c3 = ChannelManager.CreateChannel<int>();
			var c4 = ChannelManager.CreateChannel<int>();
			Task? r1 = null, r2 = null, r3 = null, r4 = null;
			var token = new CancellationTokenSource();

			async Task ObservePotentialExceptions()
			{
				token.Cancel();
				// Observe the exceptions on the other tasks
				if (r1 != null) try { await r1; } catch { }
				if (r2 != null) try { await r2; } catch { }
				if (r3 != null) try { await r3; } catch { }
				if (r4 != null) try { await r4; } catch { }
				token.Dispose();
				token = new CancellationTokenSource();
			}

			async Task p()
			{
				r1 = c1.ReadAsync(TimeSpan.FromSeconds(5), token.Token);
				r2 = c2.ReadAsync(TimeSpan.FromSeconds(4), token.Token);
				r3 = c3.ReadAsync(TimeSpan.FromSeconds(2), token.Token);
				r4 = c4.ReadAsync(TimeSpan.FromSeconds(4), token.Token);
				try
				{
					var t = await Task.WhenAny(
						r1, r2, r3, r4
					);

					if (!t.IsFaulted || t.Exception.InnerException is not TimeoutException)
						throw new UnittestException("Timeout did not happen?");
				}
				catch (TimeoutException)
				{
				}
			}

			if (!p().Wait(TimeSpan.FromSeconds(3)))
				throw new UnittestException("Failed to get timeout");
			await ObservePotentialExceptions();

			if (!p().Wait(TimeSpan.FromSeconds(3)))
				throw new UnittestException("Failed to get timeout");
			await ObservePotentialExceptions();

			token.Dispose();
		}

		[TestMethod]
		public async Task TestTimeoutMultipleTimesSuccession()
		{
			var c1 = ChannelManager.CreateChannel<int>();
			var c2 = ChannelManager.CreateChannel<int>();
			var c3 = ChannelManager.CreateChannel<int>();
			var c4 = ChannelManager.CreateChannel<int>();
			Task? r1 = null, r2 = null, r3 = null, r4 = null;
			var token = new CancellationTokenSource();

			async Task ObservePotentialExceptions()
			{
				token.Cancel();
				// Observe the exceptions on the other tasks
				if (r1 != null) try { await r1; } catch { }
				if (r2 != null) try { await r2; } catch { }
				if (r3 != null) try { await r3; } catch { }
				if (r4 != null) try { await r4; } catch { }
				token.Dispose();
				token = new CancellationTokenSource();
			}

			async Task p()
			{
				r1 = c1.ReadAsync(TimeSpan.FromSeconds(7), token.Token);
				r2 = c2.ReadAsync(TimeSpan.FromSeconds(3), token.Token);
				r3 = c3.ReadAsync(TimeSpan.FromSeconds(2), token.Token);
				r4 = c4.ReadAsync(TimeSpan.FromSeconds(7), token.Token);
				try
				{
					var tasks = new List<Task> { r1, r2, r3, r4 };

					//Console.WriteLine("Waiting for c3");
					var t = await Task.WhenAny(tasks);
					//Console.WriteLine("Not waiting for c3");

					if (!t.IsFaulted || t.Exception.InnerException is not TimeoutException)
						throw new UnittestException("Timeout did not happen on c3?");

					if (!tasks[2].IsFaulted || tasks[2].Exception?.InnerException is not TimeoutException)
					{
						for (var i = 0; i < tasks.Count; i++)
							if (tasks[i].IsFaulted && i != 2)
								throw new UnittestException(string.Format("Timeout happened on c{0}, but should have happened on c3?", i + 1));

						throw new UnittestException("Timeout happened on another channel than c3?");
					}

					tasks.RemoveAt(2);

					if (tasks.Any(x => x.IsFaulted))
						throw new UnittestException("Unexpected task fault?");

					//Console.WriteLine("Waiting for c2");
					t = await Task.WhenAny(tasks);
					//Console.WriteLine("Not waiting for c2");

					if (!t.IsFaulted || t.Exception.InnerException is not TimeoutException)
						throw new UnittestException("Timeout did not happen for c2?");

					if (!tasks[1].IsFaulted || tasks[1].Exception?.InnerException is not TimeoutException)
					{
						for (var i = 0; i < tasks.Count; i++)
							if (tasks[i].IsFaulted && i != 1)
								throw new UnittestException(string.Format("Timeout happened on c{0}, but should have happened on c2?", i + 1));
						throw new UnittestException("Timeout happened on another channel than c2?");
					}

					tasks.RemoveAt(1);

					if (tasks.Any(x => x.IsFaulted))
						throw new UnittestException("Unexpected task fault?");

					//Console.WriteLine("Completed");
				}
				catch (TimeoutException)
				{
				}
			}

			for (var i = 0; i < 5; i++)
			{
				if (!p().Wait(TimeSpan.FromSeconds(4)))
					throw new UnittestException("Failed to get timeout");
				// Observe the exceptions on the other tasks
				await ObservePotentialExceptions();
			}

			token.Dispose();
		}

		[TestMethod]
		public void TestMixedTimeout()
		{
			var c = ChannelManager.CreateChannel<int>();

			async Task p()
			{
				try
				{
					var tasks = new List<Task> {
							c.ReadAsync(),
							c.ReadAsync(TimeSpan.FromSeconds(1)),
							c.ReadAsync(TimeSpan.FromSeconds(2))
						};

					var t = await Task.WhenAny(tasks);

					if (!t.IsFaulted || t.Exception.InnerException is not TimeoutException)
						throw new UnittestException("Timeout did not happen on op2?");

					if (!tasks[1].IsFaulted || tasks[1].Exception?.InnerException is not TimeoutException)
					{
						for (var i = 0; i < tasks.Count; i++)
							if (tasks[i].IsFaulted && i != 1)
								throw new UnittestException(string.Format("Timeout happened on op{0}, but should have happened on op2?", i + 1));

						throw new UnittestException("Timeout happened on another channel than op2?");
					}

					tasks.RemoveAt(1);

					t = await Task.WhenAny(tasks);

					if (!t.IsFaulted || !(t.Exception.InnerException is TimeoutException))
						throw new UnittestException("Timeout did not happen on op2?");

					if (!tasks[1].IsFaulted || tasks[1].Exception?.InnerException is not TimeoutException)
					{
						for (var i = 0; i < tasks.Count; i++)
							if (tasks[i].IsFaulted && i != 1)
								throw new UnittestException(string.Format("Timeout happened on op{0}, but should have happened on op3?", i + 1));

						throw new UnittestException("Timeout happened on another channel than op3?");
					}

				}
				catch (TimeoutException)
				{
				}
			}

			for (var i = 0; i < 5; i++)
				if (!p().Wait(TimeSpan.FromSeconds(3)))
					throw new UnittestException("Failed to get timeout");
		}

		[TestMethod]
		public async Task TestTimeoutWithBuffers()
		{
			var c = ChannelManager.CreateChannel<int>(buffersize: 1);
			Task? w1 = null, w2 = null, w3 = null;
			var token = new CancellationTokenSource();

			async Task ObservePotentialExceptions()
			{
				token.Cancel();
				// Observe the exceptions on the other tasks
				if (w1 != null) try { await w1; } catch { }
				if (w2 != null) try { await w2; } catch { }
				if (w3 != null) try { await w3; } catch { }
				token.Dispose();
				token = new CancellationTokenSource();
			}

			async Task p()
			{
				try
				{
					w1 = c.WriteAsync(4, token.Token);
					w2 = c.WriteAsync(5, TimeSpan.FromSeconds(1), token.Token);
					w3 = c.WriteAsync(6, TimeSpan.FromSeconds(2), token.Token);
					var tasks = new List<Task> { w1, w2, w3 };

					var t = await Task.WhenAny(tasks);

					if (!t.IsCompleted)
						throw new UnittestException("Buffered write failed?");

					tasks.RemoveAt(0);

					t = await Task.WhenAny(tasks);

					if (!t.IsFaulted || t.Exception.InnerException is not TimeoutException)
						throw new UnittestException("Timeout did not happen on op1?");

					if (!tasks[0].IsFaulted || tasks[0].Exception?.InnerException is not TimeoutException)
					{
						for (var i = 0; i < tasks.Count; i++)
							if (tasks[i].IsFaulted && i != 0)
								throw new UnittestException(string.Format("Timeout happened on op{0}, but should have happened on op1?", i + 1));

						throw new UnittestException("Timeout happened on another channel than op1?");
					}
				}
				catch (TimeoutException)
				{
				}
			}

			for (var i = 0; i < 5; i++)
			{
				if (!p().Wait(TimeSpan.FromSeconds(3)))
					throw new UnittestException("Failed to get timeout");
				await ObservePotentialExceptions();
			}

			token.Dispose();
		}
	}
}

