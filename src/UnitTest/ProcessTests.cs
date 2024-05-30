using System;
using CoCoL;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
	[TestClass]
	public class ProcessTests
	{
		[Process(10)]
		private class Producer : ProcessHelper
		{
			[ChannelName("work")]
			private IWriteChannelEnd<int> m_channel = null;

			protected override async Task Start()
			{
				for (var i = 0; i < 5; i++)
					await m_channel.WriteAsync(i);
			}
		}

		private class Consumer : ProcessHelper
		{
			public int Sum { get; private set; }

			[ChannelName("work")]
			private IReadChannelEnd<int> m_channel = null;

			protected override async Task Start()
			{
				while (true)
					Sum += await m_channel.ReadAsync();
			}
		}

		[TestMethod]
		public void TestSimple()
		{
			Consumer c;
			using (new ChannelScope())
			{
				Loader.StartFromTypes(typeof(Producer));

				// Auto-load the channels while the scope is active
				c = new Consumer();
			}

			c.Run();

			if (c.Sum != 100)
				throw new UnittestException(string.Format("Autowire or loader failed, sum was supposed to be {0} but was {1}", 100, c.Sum));
		}
	}
}

