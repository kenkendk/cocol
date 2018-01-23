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

        [TEST_METHOD]
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
				throw new Exception(string.Format("Autowire or loader failed, sum was supposed to be {0} but was {1}", 100, c.Sum));
		}	
	}
}

