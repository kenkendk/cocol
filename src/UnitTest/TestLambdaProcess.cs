using System;
using CoCoL;
using System.Threading.Tasks;

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
	public class TestLambdaProcess
	{
		private const string CHANNEL_NAME = "SomeChannel";

        [TEST_METHOD]
		public void TestRetireWithoutLoss()
		{
			Task[] tasks;
			int count = 0;

			using (new ChannelScope())
			{
				tasks = new Task[] {
					AutomationExtensions.RunTask(
						new { channel = ChannelMarker.ForWrite<int>(CHANNEL_NAME) },

						async self =>
						{
							await Task.Delay(500);
							await self.channel.WriteAsync(1);
						}
					),

					AutomationExtensions.RunTask(
						new { channel = ChannelMarker.ForWrite<int>(CHANNEL_NAME) },

						async self =>
						{
							await Task.Delay(1000);
							await self.channel.WriteAsync(1);
						}
					),

					AutomationExtensions.RunTask(
						new { channel = ChannelMarker.ForRead<int>(CHANNEL_NAME) },

						async self =>
						{
							while (true)
							{
								await self.channel.ReadAsync();
								count++;
							}
						}
					)
				};
			}

			var all = Task.WhenAll(tasks).WaitForTask();

			if (count != 2)
				throw new Exception(string.Format("Unexpected count, expected {0} but got {1}", 2, count));
			if (all.IsFaulted || !all.IsCompleted)
				throw new Exception("Unexpected task state");

		}		
	}
}

