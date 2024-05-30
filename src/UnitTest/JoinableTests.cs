using System;
using CoCoL;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
	[TestClass]
	public class JoinableTest
	{
		[TestMethod]
		public void TestRetireWithLostItem()
		{
			var c = ChannelManager.CreateChannel<int>();
			Func<Task> p1 = async () =>
				{
					try
					{
						await Task.Delay(500);
						await c.WriteAsync(1);
					}
					catch (RetiredException)
					{
					}
					finally
					{
						c.Retire();
					}
				};

			Func<Task> p2 = async () =>
				{
					try
					{
						await Task.Delay(1000);
						// The channel will be closed at this point, so we never see this
						await c.WriteAsync(1);
					}
					catch (RetiredException)
					{
					}
					finally
					{
						c.Retire();
					}
				};


			int count = 0;
			Func<Task> p3 = async () =>
				{
					try
					{
						while (true)
						{
							await c.ReadAsync();
							count++;
						}
					}
					catch (RetiredException)
					{
					}
					finally
					{
						c.Retire();
					}
				};

			var all = Task.WhenAll(p1(), p2(), p3()).WaitForTask();

			if (count != 1)
				throw new UnittestException(string.Format("Unexpected count, expected {0} but got {1}", 1, count));
			if (all.IsFaulted || !all.IsCompleted)
				throw new UnittestException("Unexpected task state");

		}

		[TestMethod]
		public void TestRetireWithoutLoss()
		{
			var c = ChannelManager.CreateChannel<int>();
			// Manually access the joinable features of the channel
			var rt = c as IJoinAbleChannel;
			Func<Task> p1 = async () =>
				{
					try
					{
						rt.Join(true);
						await Task.Delay(500);
						await c.WriteAsync(1);
					}
					catch (RetiredException)
					{
					}
					finally
					{
						// This will not retire the channel
						rt.Leave(true);
					}
				};

			Func<Task> p2 = async () =>
				{
					try
					{
						rt.Join(true);
						await Task.Delay(1000);
						await c.WriteAsync(1);
					}
					catch (RetiredException)
					{
					}
					finally
					{
						// This will be the last reader, so it will retire the channel
						rt.Leave(true);
					}
				};


			int count = 0;
			Func<Task> p3 = async () =>
				{
					try
					{
						rt.Join(false);
						while (true)
						{
							await c.ReadAsync();
							count++;
						}
					}
					catch (RetiredException)
					{
					}
					finally
					{
						rt.Leave(false);
					}
				};

			var all = Task.WhenAll(p1(), p2(), p3()).WaitForTask();

			if (count != 2)
				throw new UnittestException(string.Format("Unexpected count, expected {0} but got {1}", 2, count));
			if (all.IsFaulted || !all.IsCompleted)
				throw new UnittestException("Unexpected task state");

		}

		[TestMethod]
		public void TestRetireWithEnds()
		{
			var c = ChannelManager.CreateChannel<int>();
			Func<Task> p1 = async () =>
				{
					try
					{
						// Used protected access to the channel end
						using (var w = c.AsWriteOnly())
						{
							await Task.Delay(500);
							await w.WriteAsync(1);
						}
					}
					catch (RetiredException)
					{
					}
				};

			Func<Task> p2 = async () =>
				{
					try
					{
						// Used protected access to the channel end
						using (var w = c.AsWriteOnly())
						{
							await Task.Delay(1000);
							await w.WriteAsync(1);
						}
					}
					catch (RetiredException)
					{
					}
				};


			int count = 0;
			Func<Task> p3 = async () =>
				{
					try
					{
						// Used protected access to the channel end
						using (var r = c.AsReadOnly())
						{
							while (true)
							{
								await c.ReadAsync();
								count++;
							}
						}
					}
					catch (RetiredException)
					{
					}
				};

			var all = Task.WhenAll(p1(), p2(), p3()).WaitForTask();

			if (count != 2)
				throw new UnittestException(string.Format("Unexpected count, expected {0} but got {1}", 2, count));
			if (all.IsFaulted || !all.IsCompleted)
				throw new UnittestException("Unexpected task state");
		}
	}
}

