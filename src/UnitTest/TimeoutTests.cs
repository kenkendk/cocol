using System;
using NUnit.Framework;
using System.Threading.Tasks;
using CoCoL;

namespace UnitTest
{
	[TestFixture]
	public class TimeoutTests
	{
		[Test]
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
			System.Threading.Thread.Sleep(TimeSpan.FromSeconds(3));

			if (!t.IsCompleted)
				throw new Exception("Failed to get timeout");
		}

		[Test]
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
	}
}

