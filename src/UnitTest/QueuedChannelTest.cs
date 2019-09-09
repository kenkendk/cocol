using System;
using CoCoL;

#if NETCOREAPP2_0
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TOP_LEVEL = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using TEST_METHOD = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#else
using NUnit.Framework;
using TOP_LEVEL = NUnit.Framework.TestFixtureAttribute;
using TEST_METHOD = NUnit.Framework.TestAttribute;
#endif

namespace UnitTest
{
    [TOP_LEVEL]
	public class QueuedChannelTest
	{
        [TEST_METHOD]
		public void TestBufferedWrite()
		{
			var c = ChannelManager.CreateChannel<int>(buffersize: 2);

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

        [TEST_METHOD]
        public void TestImmediateWrite()
        {
            var c = ChannelManager.CreateChannel<int>();

            // Register two pending reads
            var n1 = c.ReadAsync();
            var n2 = c.ReadAsync();

            if (!c.TryWrite(4))
                throw new Exception("Failed to write to channel with immediate timeout");
            if (!c.TryWrite(5))
                throw new Exception("Failed to write to channel with immediate timeout");

            if (n1.Result != 4)
                throw new Exception("Invalid data read");
            if (n2.Result != 5)
                throw new Exception("Invalid data read");
        }

        [TEST_METHOD]
		public void TestOrderedRetire()
		{
			var c = ChannelManager.CreateChannel<int>(buffersize: 2);

			c.Write(6);
			c.Write(7);

			c.Retire();

			if (c.Read() != 6)
				throw new Exception("Invalid data read");
			if (c.Read() != 7)
				throw new Exception("Invalid data read");

			if (!c.IsRetiredAsync.WaitForTask().Result)
				throw new Exception("Channel was not retired as expected");
		}

        [TEST_METHOD]
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

