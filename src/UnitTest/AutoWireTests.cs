using System;
using NUnit.Framework;
using CoCoL;

namespace UnitTest
{
	[TestFixture]
	public class AutoWireTests
	{
		private class Reader
		{
			[ChannelName("input")]
			private IReadChannel<int> m_read;

			public bool HasChannel { get { return m_read != null; } }

			public bool IsChannelRetired { get { return m_read.IsRetired; } }
		}

		private class Writer
		{
			[ChannelName("input")]
			private IWriteChannel<int> m_write;

			public bool HasChannel { get { return m_write != null; } }

			public bool IsChannelRetired { get { return m_write.IsRetired; } }
		}

		[Test]
		public void SimpleTest()
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

				c = ChannelScope.Current.GetOrCreate<int>("input");
			}

			if (x1 == null || !x1.HasChannel || x2 == null || !x2.HasChannel)
				throw new Exception("Autoloader failed to load channel");

			if (ChannelScope.Current != ChannelScope.Root)
				throw new Exception("Unexpected current scope");

			AutomationExtensions.RetireAllChannels(x1);

			if (c.IsRetired)
				throw new Exception("Unexpected early retire");

			AutomationExtensions.RetireAllChannels(x2);

			if (!c.IsRetired)
				throw new Exception("Unexpected non-retire");

			using (new ChannelScope())
				AutomationExtensions.AutoWireChannels(y = new Writer());

			if (y == null || !y.HasChannel || y.IsChannelRetired)
				throw new Exception("Scope does not appear isolated");

		}
	}
}

