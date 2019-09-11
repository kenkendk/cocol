using System;
using CoCoL;
using CoCoL.Network;
using System.Threading.Tasks;
using System.Threading;

#if NETCOREAPP2_0
using TOP_LEVEL = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using TEST_METHOD = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#else
using TOP_LEVEL = NUnit.Framework.TestFixtureAttribute;
using TEST_METHOD = NUnit.Framework.TestAttribute;
#endif

namespace UnitTest
{
    //[TOP_LEVEL]
	public class NetworkChannelTest
	{
		private class SelfHoster : IDisposable
		{
			private readonly CancellationTokenSource m_source = new CancellationTokenSource();
			private Task m_server;

			public SelfHoster()
			{
				var channelserver = new NetworkChannelServer(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8888));
				NetworkConfig.Configure("localhost", 8888, true);

				m_source.Token.Register(() => {
					channelserver.Dispose();
				});

				m_server = channelserver.RunAsync();
			}

			public async Task StopAsync()
			{
				var t = NetworkConfig.StopAsync();
				m_source.Cancel();

				await t;
				await m_server;
			}

			#region IDisposable implementation

			public void Dispose()
			{
				StopAsync().WaitForTaskOrThrow();
			}

			#endregion
		}

        //[TEST_METHOD]
		public void SimpleNetwork()
		{
			using (new SelfHoster())
			using (new NetworkChannelScope("network:", true))
			{
				var networkchannel = new NetworkChannel<int>("network:test");

				var wrtask = networkchannel.WriteAsync(42);
				var res = networkchannel.AsRead().Read();
				if (res != 42)
					throw new UnittestException("Broken network channel");

				wrtask.WaitForTaskOrThrow();
			}
		}

        //[TEST_METHOD]
		public void SimpleNetworkTimeout()
		{
			using (new SelfHoster())
			using (new NetworkChannelScope("network:", true))
			{
				var networkchannel = new NetworkChannel<int>("network:test");

				var res = networkchannel.AsRead().ReadAsync(TimeSpan.FromSeconds(1));
				res.WaitForTask();

				if (!res.IsFaulted || !res.Exception.IsTimeoutException())
					throw new UnittestException("Broken timeout implementation");

				var wrtask = networkchannel.WriteAsync(42);

				if (networkchannel.AsRead().Read(TimeSpan.FromSeconds(1)) != 42)
					throw new UnittestException("Broken network channel");
			
				wrtask.WaitForTaskOrThrow();
			}
		}

        //[TEST_METHOD]
		public void MixedOperationOnNetwork()
		{
			using (new SelfHoster())
			using (new NetworkChannelScope(null, true))
				new MixedOperationTest().TestMultiTypeReadWrite();
		}

        //[TEST_METHOD]
		public void TestMultiAccessOperationOnNetwork()
		{
			using (new SelfHoster())
			using (new NetworkChannelScope(null, true))
				new MixedOperationTest().TestMultiAccessOperation();

		}

	}
}

