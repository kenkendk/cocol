using System;
using CoCoL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
	[TestClass]
	public class UntypedTests
	{
		[TestMethod]
		public void Simple()
		{
			var chan = (IUntypedChannel)ChannelManager.CreateChannel<int>();

			chan.WriteAsync(4);
			if ((int)chan.Read() != 4)
				throw new UnittestException("Unable to use untyped channel");
		}
	}
}

