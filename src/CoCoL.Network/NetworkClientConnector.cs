using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Sockets;

namespace CoCoL.Network
{
	/// <summary>
	/// Process that keeps track of connections from a client to a network channel server
	/// </summary>
	public class NetworkClientConnector : ProcessHelper
	{
		/// <summary>
		/// The log instance
		/// </summary>
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// List of active clients, key is the channelid
		/// </summary>
		private Dictionary<string, NetworkClient> m_clientChannelLookup = new Dictionary<string, NetworkClient>();

		/// <summary>
		/// List of active clients, key is hostname+port
		/// </summary>
		private Dictionary<string, NetworkClient> m_connectedClientLookup = new Dictionary<string, NetworkClient>();

		/// <summary>
		/// The channel where we get requests from
		/// </summary>
		private IReadChannelEnd<PendingNetworkRequest> m_requests;

		/// <summary>
		/// List of pending network requests
		/// </summary>
		private Dictionary<string, Dictionary<string, PendingNetworkRequest>> m_pendingRequests = new Dictionary<string, Dictionary<string, PendingNetworkRequest>>();

		/// <summary>
		/// The nameserver client instance
		/// </summary>
		private NameServerClient m_nameserverclient;

		/// <summary>
		/// Gets the channel used to send network requests
		/// </summary>
		public IWriteChannelEnd<PendingNetworkRequest> Requests { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.NetworkClientConnector"/> class.
		/// </summary>
		/// <param name="hostname">The hostname for the nameserver.</param>
		/// <param name="port">The port for the nameserver.</param>
		public NetworkClientConnector(string hostname, int port)
			: base()
		{
			using (new IsolatedChannelScope())
			{
				m_nameserverclient = new NameServerClient(hostname, port);
				var chan = ChannelManager.CreateChannel<PendingNetworkRequest>();
				m_requests = chan.AsReadOnly();
				Requests = chan.AsWriteOnly();
			}
		}

		/// <summary>
		/// The method that implements this process
		/// </summary>
		protected override async Task Start()
		{
			while (true)
			{
				// Grab the next request
				var req = await m_requests.ReadAsync();
				LOG.DebugFormat("Found request with ID: {0}, channel: {3}, type: {1}, dtype: {2}", req.RequestID, req.RequestType, req.ChannelDataType, req.ChannelID);

				var nwc = await LocateChannelHandler(req.ChannelID);
				lock (m_lock)
				{
					LOG.DebugFormat("Registered pending request with ID: {0}, channel: {3}, type: {1}, dtype: {2}", req.RequestID, req.RequestType, req.ChannelDataType, req.ChannelID);
					m_pendingRequests[req.ChannelID].Add(req.RequestID, req);
				}

				try
				{
					await nwc.WriteAsync(req);
					LOG.DebugFormat("Passed req {0}, channel: {3}, with type {1}, dtype: {2}", req.RequestID, req.RequestType, req.ChannelDataType, req.ChannelID);
				}
				catch(Exception ex)
				{
					LOG.Error("Failed to send request", ex);
					// If the connection closed in some way
					// restart the connection and try again

					// otherwise:
					TrySetException(req.Task, ex);
				}
			}
		}


		/// <summary>
		/// Calls the <see cref="System.Threading.Tasks.TaskCompletionSource&lt;T&gt;.TrySetException(Exception)"/> method through reflection
		/// </summary>
		/// <param name="task">The task to invoke the method on.</param>
		/// <param name="ex">The exception.</param>
		private static void TrySetException(object task, Exception ex)
		{
			task.GetType().GetMethod("TrySetException", new [] { typeof(Exception) }).Invoke(task, new object[] { ex });
		}

		/// <summary>
		/// Calls the <see cref="System.Threading.Tasks.TaskCompletionSource&lt;T&gt;.TrySetCanceled()"/> method through reflection
		/// </summary>
		/// <param name="task">The task to invoke the method on.</param>
		private static void TrySetCanceled(object task)
		{
			task.GetType().GetMethod("TrySetCanceled", new Type[0]).Invoke(task, new object[0]);
		}

		/// <summary>
		/// Calls the <see cref="System.Threading.Tasks.TaskCompletionSource&lt;T&gt;.TrySetResult"/> method through reflection
		/// </summary>
		/// <param name="task">The task to invoke the method on.</param>
		/// <param name="data">The result.</param>
		private static void TrySetResult(object task, object data)
		{
			task.GetType().GetMethod("TrySetResult").Invoke(task, new object[] { data });
		}

		/// <summary>
		/// Locates the channel server that handles the request.
		/// </summary>
		/// <returns>The channel handler.</returns>
		/// <param name="channelid">The channel to locate.</param>
		private async Task<NetworkClient> LocateChannelHandler(string channelid)
		{
			NetworkClient nwc;
			if (m_clientChannelLookup.TryGetValue(channelid, out nwc))
				return nwc;

			if (!m_pendingRequests.ContainsKey(channelid))
				m_pendingRequests[channelid] = new Dictionary<string, PendingNetworkRequest>();

			var ep = await m_nameserverclient.GetChannelHomeAsync(channelid);
			var key = string.Format("{0}:{1}", ep.Item1, ep.Item2);

			if (m_connectedClientLookup.TryGetValue(key, out nwc))
				return nwc;

			try
			{
				var tcl = new TcpClient();
				await tcl.ConnectAsync(ep.Item1, ep.Item2);
				var ncl = new NetworkClient(tcl);
				ncl.m_selfid = string.Format("CLIENT:{0}", m_connectedClientLookup.Count);
				await ncl.ConnectAsync();

				RunClient(ncl).FireAndForget();

				return m_connectedClientLookup[key] = m_clientChannelLookup[channelid] = ncl;
			}
			catch(Exception ex)
			{
				LOG.Error(string.Format("Failed to locate channel {0}", channelid), ex);
				throw;
			}
		}

		/// <summary>
		/// Runs the client.
		/// </summary>
		/// <returns>The client.</returns>
		/// <param name="nwc">The network client.</param>
		private async Task RunClient(NetworkClient nwc)
		{
			try
			{
				while (true)
				{
					var req = await nwc.ReadAsync();

					LOG.DebugFormat("Processing request with ID: {0}", req.RequestID);

					if (!m_pendingRequests.ContainsKey(req.ChannelID))
						Console.WriteLine();
					if (!m_pendingRequests[req.ChannelID].ContainsKey(req.RequestID))
						Console.WriteLine("Unknown request ID: {0}", req.RequestID);

					var prq = m_pendingRequests[req.ChannelID][req.RequestID];

					switch (req.RequestType)
					{
						case NetworkMessageType.OfferRequest:
							Task.Run(async () => {
								var res = prq.NoOffer ? true : await prq.Offer.OfferAsync(prq.AssociatedChannel);
								await nwc.WriteAsync(new PendingNetworkRequest(
									prq.ChannelID,
									prq.ChannelDataType,
									prq.RequestID,
									prq.SourceID,
									new DateTime(0),
									res ? NetworkMessageType.OfferAcceptResponse : NetworkMessageType.OfferDeclineResponse,
									null,
									true
								));
							}).FireAndForget();
							break;

						case NetworkMessageType.OfferCommitRequest:
							if (!prq.NoOffer)
								await prq.Offer.CommitAsync(prq.AssociatedChannel);
							break;

						case NetworkMessageType.OfferWithdrawRequest:
							if (!prq.NoOffer)
								await prq.Offer.WithdrawAsync(prq.AssociatedChannel);
							break;

						case NetworkMessageType.CancelResponse:
							lock (m_lock)
								m_pendingRequests[req.ChannelID].Remove(req.RequestID);
							TrySetCanceled(prq.Task);
							break;
						case NetworkMessageType.FailResponse:
							lock (m_lock)
								m_pendingRequests[req.ChannelID].Remove(req.RequestID);
							TrySetException(prq.Task, req.Value as Exception);
							break;
						case NetworkMessageType.RetiredResponse:
							lock (m_lock)
								m_pendingRequests[req.ChannelID].Remove(req.RequestID);
							TrySetException(prq.Task, new RetiredException());
							break;
						case NetworkMessageType.TimeoutResponse:
							lock (m_lock)
								m_pendingRequests[req.ChannelID].Remove(req.RequestID);
							TrySetException(prq.Task, new TimeoutException());
							break;

						case NetworkMessageType.ReadResponse:
							lock (m_lock)
								m_pendingRequests[req.ChannelID].Remove(req.RequestID);
							TrySetResult(prq.Task, req.Value);
							break;

						case NetworkMessageType.WriteResponse:
							lock (m_lock)
								m_pendingRequests[req.ChannelID].Remove(req.RequestID);
							TrySetResult(prq.Task, true);
							break;

						default:
							throw new Exception(string.Format("Invalid request type: {0}", req.RequestType));
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Error(string.Format("Crashed network client"), ex);

				try { nwc.Dispose(); }
				catch { }

				throw;
			}
		}
	}
}

