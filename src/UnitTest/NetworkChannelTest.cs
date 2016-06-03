using System;
using CoCoL;
using CoCoL.Network;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Threading;

namespace UnitTest
{
	[TestFixture]
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

		[Test]
		public void SimpleNetwork()
		{
			using (new SelfHoster())
			using (new NetworkChannelScope("network:", true))
			{
				var networkchannel = new NetworkChannel<int>("network:test");

				var wrtask = networkchannel.WriteAsync(42);
				var res = networkchannel.AsRead().Read();
				if (res != 42)
					throw new Exception("Broken network channel");

				wrtask.WaitForTaskOrThrow();
			}
		}

		[Test]
		public void SimpleNetworkTimeout()
		{
			using (new SelfHoster())
			using (new NetworkChannelScope("network:", true))
			{
				var networkchannel = new NetworkChannel<int>("network:test");

				var res = networkchannel.AsRead().ReadAsync(TimeSpan.FromSeconds(1));
				res.WaitForTask();

				if (!res.IsFaulted || !res.Exception.IsTimeoutException())
					throw new Exception("Broken timeout implementation");

				var wrtask = networkchannel.WriteAsync(42);

				if (networkchannel.AsRead().Read(TimeSpan.FromSeconds(1)) != 42)
					throw new Exception("Broken network channel");
			
				wrtask.WaitForTaskOrThrow();
			}
		}

		[Test]
		public void MixedOperationOnNetwork()
		{
			using (new SelfHoster())
			using (new NetworkChannelScope(null, true))
				new MixedOperationTest().TestMultiTypeReadWrite();
		}

		[Test]
		public void TestMultiAccessOperationOnNetwork()
		{
			using (new SelfHoster())
			using (new NetworkChannelScope(null, true))
				new MixedOperationTest().TestMultiAccessOperation();

		}

	}
}

