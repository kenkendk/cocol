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
	public class UntypedTests
	{
        [TEST_METHOD]
		public void Simple()
		{
			var chan = (IUntypedChannel)ChannelManager.CreateChannel<int>();

			chan.WriteAsync(4);
			if ((int)chan.Read() != 4)
				throw new UnittestException("Unable to use untyped channel");
		}
	}
}

