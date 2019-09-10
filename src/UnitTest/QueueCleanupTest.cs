using System;
using CoCoL;
using System.Linq;
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
	public class QueueCleanupTest
	{
		private class TestableTwoPhase : ITwoPhaseOffer
		{
			public bool AllowPass { get; set; }
			public bool HasOffered { get; private set;}
			public bool HasWithdrawn { get; private set; }
			public bool HasComitted { get; private set; }

			public TestableTwoPhase(bool allowPass)
			{
				AllowPass = allowPass;
			}

			public Task<bool> OfferAsync(object caller)
			{
				HasOffered = true;
				return Task.FromResult(AllowPass);
			}
			public Task CommitAsync(object caller)
			{
				if (!AllowPass)
					throw new Exception("Unexpected commit?");

				HasComitted = true;
				return Task.FromResult(true);
			}

			public Task WithdrawAsync(object caller)
			{
				if (!HasOffered)
					throw new Exception("Withdraw without offer?");
				if (HasWithdrawn && !AllowPass)
					throw new Exception("Multple withdraws?");

				HasWithdrawn = true;
				return Task.FromResult(true);
			}
		}

        [TEST_METHOD]
		public void TestBufferCleanup()
		{
			var c = ChannelManager.CreateChannel<int>();

			var offers = Enumerable.Range(0, 550).Select(x => new TestableTwoPhase(false)).ToArray();
			var readtasks = offers.Select(x => c.ReadAsync(x)).ToArray();

			var cleared = offers.Where(x => x.HasOffered && !x.HasWithdrawn && !x.HasComitted).Count();
			if (cleared < 500)
				throw new Exception(string.Format("Expected {0} items cleared but got {1}", 500, cleared));

			var tx = c.ReadAsync();
			cleared = offers.Where(x => x.HasOffered && !x.HasWithdrawn && !x.HasComitted).Count();
			if (cleared < 500)
				throw new Exception(string.Format("Expected {0} items cleared but got {1}", 500, cleared));

			if (!c.TryWrite(42))
				throw new Exception("Write not allowed?");

			cleared = offers.Where(x => x.HasOffered && !x.HasWithdrawn && !x.HasComitted).Count();
			if (cleared != 550)
				throw new Exception(string.Format("Expected {0} items cleared but got {1}", 550, cleared));

			Task.WhenAll(readtasks).WaitForTask();

			if (tx.Result != 42)
				throw new Exception("Read failed?");
		}
	}
}

