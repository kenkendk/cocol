using System;
using System.Threading.Tasks;

namespace CoCoL.Network
{
	/// <summary>
	/// Configuration of the network for this instance
	/// </summary>
	public static class NetworkConfig
	{
		/// <summary>
		/// The hostname for the nameserver
		/// </summary>
		public static string NameServerHostname { get; private set; }
		/// <summary>
		/// The port for the nameserver
		/// </summary>
		public static int NameServerPort { get; private set; }
		/// <summary>
		/// The ID of this instance
		/// </summary>
		public static string SelfID { get; private set; }

		/// <summary>
		/// A value indicating if the nameserver is bypassed, and a single channel server is used instead
		/// </summary>
		public static bool SingleChannelServer { get; private set; }

		/// <summary>
		/// The connector used to relay messages
		/// </summary>
		private static NetworkClientConnector _connector = null;

		/// <summary>
		/// The connector task used to relay messages
		/// </summary>
		private static Task _connectorTask = null;

		/// <summary>
		/// The lock for guaranteeing exclusive access
		/// </summary>
		private static object _lock = new object();

		/// <summary>
		/// Initializes the <see cref="CoCoL.Network.NetworkConfig"/> class.
		/// </summary>
		static NetworkConfig()
		{
			SelfID = Guid.NewGuid().ToString("N");
		}

		/// <summary>
		/// Configure this instance for network access.
		/// </summary>
		/// <param name="nameserverhostname">The hostname for the nameserver.</param>
		/// <param name="nameserverport">The port for the nameserver.</param>
		/// <param name="singlechannelserver">Set to <c>true</c> to bypass the nameserver and connect directly to the channelserver.</param>
		public static void Configure(string nameserverhostname, int nameserverport, bool singlechannelserver)
		{
			lock (_lock)
			{
				if (_connectorTask != null && !_connectorTask.IsCompleted)
					throw new InvalidOperationException("Cannot change configuration while running");

				NameServerHostname = nameserverhostname;
				NameServerPort = nameserverport;
				SingleChannelServer = singlechannelserver;

				if (_connectorTask != null)
				{
					_connector = null;
					_connectorTask = null;
				}
			}
		}

		/// <summary>
		/// Sends a request into the network
		/// </summary>
		/// <returns>The awaitable task.</returns>
		/// <param name="req">The request to transmit.</param>
		public static Task TransmitRequestAsync(PendingNetworkRequest req)
		{
			if (_connector == null)
				lock (_lock)
					if (_connector == null)
					{
						_connector = new NetworkClientConnector(NameServerHostname, NameServerPort);
						_connectorTask = _connector.RunAsync();
					}

			return _connector.Requests.WriteAsync(req);
		}

		/// <summary>
		/// Sends a request into the network
		/// </summary>
		/// <returns>The awaitable task.</returns>
		/// <param name="req">The request to transmit.</param>
		public static void TransmitRequest(PendingNetworkRequest req)
		{
			if (_connector == null)
			{
				lock (_lock)
					if (_connector == null)
					{
						_connector = new NetworkClientConnector(NameServerHostname, NameServerPort);
						_connectorTask = _connector.RunAsync();
					}
			}
			_connector.Requests.Write(req);
		}

		/// <summary>
		/// Stops the network
		/// </summary>
		/// <returns>The awaitable task.</returns>
		public static Task StopAsync()
		{
			lock (_lock)
				if (_connector == null)
					return Task.FromResult(true);

			_connector.Dispose();

			return _connectorTask;
		}
	
	}
}

