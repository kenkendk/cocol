using System;
using CoCoL;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
	[TestClass]
	public class QueueCleanupTest
	{
		private class TestableTwoPhase : ITwoPhaseOffer
		{
			public bool AllowPass { get; set; }
			public bool HasOffered { get; private set; }
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
					throw new UnittestException("Unexpected commit?");

				HasComitted = true;
				return Task.FromResult(true);
			}

			public Task WithdrawAsync(object caller)
			{
				if (!HasOffered)
					throw new UnittestException("Withdraw without offer?");
				if (HasWithdrawn && !AllowPass)
					throw new UnittestException("Multple withdraws?");

				HasWithdrawn = true;
				return Task.FromResult(true);
			}
		}

		[TestMethod]
		public void TestBufferCleanup()
		{
			var c = ChannelManager.CreateChannel<int>();

			var offers = Enumerable.Range(0, 550).Select(x => new TestableTwoPhase(false)).ToArray();
			var readtasks = offers.Select(x => c.ReadAsync(x)).ToArray();

			var cleared = offers.Where(x => x.HasOffered && !x.HasWithdrawn && !x.HasComitted).Count();
			if (cleared < 500)
				throw new UnittestException(string.Format("Expected {0} items cleared but got {1}", 500, cleared));

			var tx = c.ReadAsync();
			cleared = offers.Where(x => x.HasOffered && !x.HasWithdrawn && !x.HasComitted).Count();
			if (cleared < 500)
				throw new UnittestException(string.Format("Expected {0} items cleared but got {1}", 500, cleared));

			if (!c.TryWrite(42))
				throw new UnittestException("Write not allowed?");

			cleared = offers.Where(x => x.HasOffered && !x.HasWithdrawn && !x.HasComitted).Count();
			if (cleared != 550)
				throw new UnittestException(string.Format("Expected {0} items cleared but got {1}", 550, cleared));

			Task.WhenAll(readtasks).WaitForTask();

			if (tx.Result != 42)
				throw new UnittestException("Read failed?");
		}
	}
}

