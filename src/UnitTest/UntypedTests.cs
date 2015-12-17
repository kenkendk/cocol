using System;
using NUnit.Framework;
using CoCoL;

namespace UnitTest
{
	[TestFixture]
	public class UntypedTests
	{
		[Test]
		public void Simple()
		{
			var chan = (IUntypedChannel)ChannelManager.CreateChannel<int>();

			chan.WriteAsync(4);
			if ((int)chan.Read() != 4)
				throw new Exception("Unable to use untyped channel");
		}
	}
}

