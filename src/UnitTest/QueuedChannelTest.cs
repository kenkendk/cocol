using System;
using NUnit.Framework;
using CoCoL;

namespace UnitTest
{
	[TestFixture]
	public class QueuedChannelTest
	{
		[Test]
		public void TestBufferedWrite()
		{
			var c = ChannelManager.CreateChannel<int>(null, 2);

			c.Write(4);
			if (!c.TryWrite(5))
				throw new Exception("Failed to write to buffered channel");
			if (c.TryWrite(6))
				throw new Exception("Succeeded to write to filled buffered channel");

			if (c.Read() != 4)
				throw new Exception("Invalid data read");
			if (c.Read() != 5)
				throw new Exception("Invalid data read");
		}

		[Test]
		public void TestOrderedRetire()
		{
			var c = ChannelManager.CreateChannel<int>(null, 2);

			c.Write(6);
			c.Write(7);

			c.Retire();

			if (c.Read() != 6)
				throw new Exception("Invalid data read");
			if (c.Read() != 7)
				throw new Exception("Invalid data read");

			if (!c.IsRetired)
				throw new Exception("Channel was not retired as expected");
		}

		[Test]
		[ExpectedException(typeof(RetiredException))]
		public void TestImmediateRetire()
		{
			var c = ChannelManager.CreateChannel<int>(2);

			c.Write(7);
			c.Write(8);

			c.Retire(true);

			c.Read();
		}
	}
}

