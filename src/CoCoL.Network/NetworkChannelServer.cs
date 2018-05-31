using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

namespace CoCoL.Network
{
	/// <summary>
	/// Server instance for handling hosted channels
	/// </summary>
	public class NetworkChannelServer : ProcessHelper
	{
		/// <summary>
		/// The log instance
		/// </summary>
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The endpoint where this instance listens
		/// </summary>
		private IPEndPoint m_endPoint;

		/// <summary>
		/// The socket in this instance
		/// </summary>
		private TcpListener m_socket;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.NetworkChannelServer"/> class.
		/// </summary>
		/// <param name="listenAddr">The address to listen on.</param>
		public NetworkChannelServer(IPEndPoint listenAddr)
			: base()
		{
			m_endPoint = listenAddr;
		}

		/// <summary>
		/// The method that implements this process
		/// </summary>
		protected override async Task Start()
		{
			var i = 0;
			try
			{
				m_socket = new TcpListener(m_endPoint);
				m_socket.Start();

				// We use a special scope for our channels
				using(var sc = new IsolatedChannelScope())
				while (true)
				{
					TcpClient tcl;
					try { tcl = await m_socket.AcceptTcpClientAsync(); }
					catch(Exception ex)
					{ 	
						if (!(ex is ObjectDisposedException) && !(ex is NullReferenceException))
							LOG.WarnFormat("Stop exception was not ObjectDisposedException nor NullReferenceException", ex);
						return; 
					}

					var nwc = new NetworkClient(tcl);
					nwc.SelfID = string.Format("SERVER:{0}", i++);
					LOG.Debug("Accepted a new connection");
					Task.Run(() => {
						try { RunChannelHandler(nwc, sc); }
						catch(Exception ex) {
							LOG.Error("Crashed in channel handler", ex);			
						}
					}).FireAndForget();
				}
			}
			catch(Exception ex)
			{
				LOG.Fatal("Crashed server", ex);
				throw;
			}
		}

		/// <summary>
		/// Converts a <see cref="System.DateTime"/> instance to a <see cref="System.TimeSpan"/>.
		/// </summary>
		/// <returns>The equivalent timespan.</returns>
		/// <param name="timeout">The timeout <see cref="System.DateTime"/> instance.</param>
		private static TimeSpan TimeoutToTimeSpan(DateTime timeout)
		{
			if (timeout.Ticks <= 0)
				return Timeout.Infinite;
			else if (timeout < DateTime.Now)
				return Timeout.Immediate;
			else
				return timeout - DateTime.Now;
		}

		/// <summary>
		/// Runs the channel handler.
		/// </summary>
		/// <param name="nwc">The network client used for communicating.</param>
		/// <param name="sc">The scope where channels are created.</param>
		protected static async void RunChannelHandler(NetworkClient nwc, ChannelScope sc)
		{	
			await nwc.ConnectAsync();

			var transactions = new Dictionary<string, TwoPhaseNetworkHandler>();
			var lck = new object();

			using(nwc)
			while (true)
			{
				var req = await nwc.ReadAsync();

				// If this is a channel creation request, hijack it here
				if (req.RequestType == NetworkMessageType.CreateChannelRequest)
				{
					var chancfg = req.Value as ChannelNameAttribute;
					sc.GetOrCreate(chancfg, req.ChannelDataType);
					continue;
				}

				var chan = sc.GetOrCreate(req.ChannelID, req.ChannelDataType);

				switch (req.RequestType)
				{
					case NetworkMessageType.JoinRequest:
						((IJoinAbleChannel)chan).Join((bool)req.Value);
						break;
					case NetworkMessageType.LeaveRequest:
						((IJoinAbleChannel)chan).Leave((bool)req.Value);
						break;
					case NetworkMessageType.RetireRequest:
						chan.Retire((bool)req.Value);
						break;

					case NetworkMessageType.OfferAcceptResponse:
						transactions[req.RequestID].Accepted();
						break;
					case NetworkMessageType.OfferDeclineResponse:
						transactions[req.RequestID].Denied();
						break;

						case NetworkMessageType.ReadRequest:
							lock (lck)
								transactions[req.RequestID] = req.NoOffer ? null : new TwoPhaseNetworkHandler(req, nwc);
								
							((IUntypedChannel)chan).ReadAsync(TimeoutToTimeSpan(req.Timeout), transactions[req.RequestID]).ContinueWith(async x =>
								{
									NetworkMessageType responsetype;
									object responseitem = null;

									if (x.IsCanceled)
										responsetype = NetworkMessageType.CancelResponse;
									else if (x.IsFaulted && x.Exception.IsRetiredException())
										responsetype = NetworkMessageType.RetiredResponse;
									else if (x.IsFaulted && x.Exception.IsTimeoutException())
										responsetype = NetworkMessageType.TimeoutResponse;
									else if (x.IsFaulted)
									{
										responsetype = NetworkMessageType.FailResponse;
										responseitem = x.Exception;
									}
									else
									{
										responsetype = NetworkMessageType.ReadResponse;
										responseitem = await x;
									}

									lock (lck)
										transactions.Remove(req.RequestID);

									await nwc.WriteAsync(new PendingNetworkRequest(
											req.ChannelID, 
											req.ChannelDataType, 
											req.RequestID, 
											req.SourceID,
											new DateTime(0),
											responsetype, 
											responseitem,
											true
										));
								}).FireAndForget();

						break;

						case NetworkMessageType.WriteRequest:
							lock (lck)
								transactions[req.RequestID] = req.NoOffer ? null : new TwoPhaseNetworkHandler(req, nwc);
						
							((IUntypedChannel)chan).WriteAsync(req.Value, TimeoutToTimeSpan(req.Timeout), transactions[req.RequestID]).ContinueWith(async x =>
								{
									NetworkMessageType responsetype;
									object responseitem = null;

									if (x.IsCanceled)
										responsetype = NetworkMessageType.CancelResponse;
									else if (x.IsFaulted && x.Exception.IsRetiredException())
										responsetype = NetworkMessageType.RetiredResponse;
									else if (x.IsFaulted && x.Exception.IsTimeoutException())
										responsetype = NetworkMessageType.TimeoutResponse;
									else if (x.IsFaulted)
									{
										responsetype = NetworkMessageType.FailResponse;
										responseitem = x.Exception;
									}
									else
										responsetype = NetworkMessageType.WriteResponse;

									lock (lck)
										transactions.Remove(req.RequestID);

									await nwc.WriteAsync(new PendingNetworkRequest(
											req.ChannelID, 
											req.ChannelDataType, 
											req.RequestID, 
											req.SourceID,
											new DateTime(0),
											responsetype, 
											responseitem,
											true
										));									
								}).FireAndForget();

						break;
						
					default:
						throw new Exception("Unsupported message: {0}");
						
				}
			}
		}

		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.Network.NetworkClient"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CoCoL.Network.NetworkClient"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="CoCoL.Network.NetworkClient"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="CoCoL.Network.NetworkClient"/> so
		/// the garbage collector can reclaim the memory that the <see cref="CoCoL.Network.NetworkClient"/> was occupying.</remarks>
		/// <param name="disposing">If set to <c>true</c> disposing.</param>
		public override void Dispose(bool disposing)
		{
			try { m_socket.Stop(); }
            catch (Exception ex) { LOG.Warn("Failed on socket stop", ex); }

			base.Dispose(disposing);
		}

		/// <summary>
		/// Runs a channel server in the current process
		/// </summary>
		/// <returns>The awaitable task for server exit.</returns>
		/// <param name="token">A cancellationtoken for stopping the server.</param>
		/// <param name="host">The hostname to use.</param>
		/// <param name="port">The port to use.</param>
		/// <param name="configureclient"><c>True</c> if the client config should be set automatically</param>
		public static Task HostServer(CancellationToken token, string host = "localhost", int port = 8888, bool configureclient = true)
		{
			var addr = 
				string.Equals("localhost", host, StringComparison.InvariantCultureIgnoreCase) || string.Equals("127.0.0.1", host, StringComparison.InvariantCultureIgnoreCase)
				? System.Net.IPAddress.Loopback
				: System.Net.IPAddress.Any;
			
			var channelserver = new NetworkChannelServer(new System.Net.IPEndPoint(addr, port));
			if(configureclient)
				NetworkConfig.Configure(host, port, true);

			if (token.CanBeCanceled)
				token.Register(() => {
					channelserver.Dispose();
				});

			return channelserver.RunAsync();
		}
	}
}

