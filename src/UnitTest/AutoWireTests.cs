using System;
using CoCoL;

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
	public class AutoWireTests
	{
		private class Reader
		{
			[ChannelName("input")]
			private IReadChannel<int> m_read;

			public bool HasChannel { get { return m_read != null; } }

			public bool IsChannelRetired { get { return m_read.IsRetiredAsync.WaitForTask().Result; } }
		}

		private class Writer
		{
			[ChannelName("input")]
			private IWriteChannel<int> m_write;

			public bool HasChannel { get { return m_write != null; } }

			public bool IsChannelRetired { get { return m_write.IsRetiredAsync.WaitForTask().Result; } }
		}

		private class ReaderEnd
		{
			[ChannelName("input")]
			private IReadChannelEnd<int> m_read;

			public bool HasChannel { get { return m_read != null; } }

			public bool IsChannelRetired { get { return m_read.IsRetiredAsync.WaitForTask().Result; } }
		}

		private class WriterEnd
		{
			[ChannelName("input")]
			private IWriteChannelEnd<int> m_write;

			public bool HasChannel { get { return m_write != null; } }

			public bool IsChannelRetired { get { return m_write.IsRetiredAsync.WaitForTask().Result; } }
		}

        [TEST_METHOD]
		public void TestChannelWire()
		{
			Reader x1, x2;
			Writer y;

			IRetireAbleChannel c;
			using (new ChannelScope())
			{
				AutomationExtensions.AutoWireChannels(new object[] {
					x1 = new Reader(),
					x2 = new Reader(),
					y = new Writer()
				});

				c = ChannelManager.GetChannel<int>("input");
			}

			if (x1 == null || !x1.HasChannel || x2 == null || !x2.HasChannel)
				throw new Exception("Autoloader failed to load channel");

			if (ChannelScope.Current != ChannelScope.Root)
				throw new Exception("Unexpected current scope");

			AutomationExtensions.RetireAllChannels(x1);

			if (c.IsRetiredAsync.WaitForTask().Result)
				throw new Exception("Unexpected early retire");

			AutomationExtensions.RetireAllChannels(x2);

			if (!c.IsRetiredAsync.WaitForTask().Result)
				throw new Exception("Unexpected non-retire");

			using (new ChannelScope())
				AutomationExtensions.AutoWireChannels(y = new Writer());

			if (y == null || !y.HasChannel || y.IsChannelRetired)
				throw new Exception("Scope does not appear isolated");

		}

        [TEST_METHOD]
		public void TestChannelEndWire()
		{
			ReaderEnd x1, x2;
			WriterEnd y;

			IRetireAbleChannel c;
			using (new ChannelScope())
			{
				AutomationExtensions.AutoWireChannels(new object[] {
					x1 = new ReaderEnd(),
					x2 = new ReaderEnd(),
					y = new WriterEnd()
				});

				c = ChannelManager.GetChannel<int>("input");
			}

			if (x1 == null || !x1.HasChannel || x2 == null || !x2.HasChannel)
				throw new Exception("Autoloader failed to load channel");

			if (ChannelScope.Current != ChannelScope.Root)
				throw new Exception("Unexpected current scope");

			AutomationExtensions.RetireAllChannels(x1);

			if (c.IsRetiredAsync.WaitForTask().Result)
				throw new Exception("Unexpected early retire");

			AutomationExtensions.RetireAllChannels(x2);

			if (!c.IsRetiredAsync.WaitForTask().Result)
				throw new Exception("Unexpected non-retire");

			using (new ChannelScope())
				AutomationExtensions.AutoWireChannels(y = new WriterEnd());

			if (y == null || !y.HasChannel || y.IsChannelRetired)
				throw new Exception("Scope does not appear isolated");

		}
	}
}

