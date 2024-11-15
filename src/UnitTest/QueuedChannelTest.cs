﻿using System;
using CoCoL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
	[TestClass]
	public class QueuedChannelTest
	{
		[TestMethod]
		public void TestBufferedWrite()
		{
			var c = ChannelManager.CreateChannel<int>(buffersize: 2);

			c.Write(4);
			if (!c.TryWrite(5))
				throw new UnittestException("Failed to write to buffered channel");
			if (c.TryWrite(6))
				throw new UnittestException("Succeeded to write to filled buffered channel");

			if (c.Read() != 4)
				throw new UnittestException("Invalid data read");
			if (c.Read() != 5)
				throw new UnittestException("Invalid data read");
		}

		[TestMethod]
		public void TestImmediateWrite()
		{
			var c = ChannelManager.CreateChannel<int>();

			// Register two pending reads
			var n1 = c.ReadAsync();
			var n2 = c.ReadAsync();

			if (!c.TryWrite(4))
				throw new UnittestException("Failed to write to channel with immediate timeout");
			if (!c.TryWrite(5))
				throw new UnittestException("Failed to write to channel with immediate timeout");

			if (n1.Result != 4)
				throw new UnittestException("Invalid data read");
			if (n2.Result != 5)
				throw new UnittestException("Invalid data read");
		}

		[TestMethod]
		public void TestOrderedRetire()
		{
			var c = ChannelManager.CreateChannel<int>(buffersize: 2);

			c.Write(6);
			c.Write(7);

			c.Retire();

			if (c.Read() != 6)
				throw new UnittestException("Invalid data read");
			if (c.Read() != 7)
				throw new UnittestException("Invalid data read");

			if (!c.IsRetiredAsync.WaitForTask().Result)
				throw new UnittestException("Channel was not retired as expected");
		}

		[TestMethod]
		public void TestImmediateRetire()
		{
			var c = ChannelManager.CreateChannel<int>(buffersize: 2);

			c.Write(7);
			c.Write(8);

			c.Retire(true);

			TestAssert.Throws<RetiredException>(() => c.Read());
		}
	}
}

