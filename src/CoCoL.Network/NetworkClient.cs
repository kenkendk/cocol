using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.IO;

namespace CoCoL.Network
{
	/// <summary>
	/// Implements the underlying protocol to serialize and deserialize data sent over a socket
	/// </summary>
	public class NetworkClient : IDisposable
	{
		/// <summary>
		/// The log instance
		/// </summary>
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The protocol major version
		/// </summary>
		private const byte SOCKET_PROTOCOL_MAJOR_VERSION = 1;
		/// <summary>
		/// The protocol minor version
		/// </summary>
		private const byte SOCKET_PROTOCOL_MINOR_VERSION = 0;
		/// <summary>
		/// The magic header used to signal what application this is
		/// </summary>
		private static readonly byte[] SOCKET_MAGIC_HEADER = System.Text.Encoding.ASCII.GetBytes("CoCoL");
		/// <summary>
		/// The type of serializer this connection uses
		/// </summary>
		private static readonly byte[] SOCKET_SERIALIZER = System.Text.Encoding.ASCII.GetBytes("JSON");

		/// <summary>
		/// A compiled version of all the initial header bytes
		/// </summary>
		private static readonly byte[] INITIAL_MESSAGE;

		/// <summary>
		/// The maximum size of a tranferable message
		/// </summary>
		private const int MAX_MESSAGE_SIZE = 10 * 1024 * 1024;
		/// <summary>
		/// The expected size of messages, used to pre-allocate buffers
		/// </summary>
		private const int SMALL_MESSAGE_SIZE = 10 * 1024;

		/// <summary>
		/// Initializes the <see cref="CoCoL.Network.NetworkClient"/> class and builds the initial message.
		/// </summary>
		static NetworkClient()
		{
			INITIAL_MESSAGE = new [] {
				(byte)0, //Header length
				SOCKET_PROTOCOL_MAJOR_VERSION,
				SOCKET_PROTOCOL_MINOR_VERSION,
				(byte)SOCKET_MAGIC_HEADER.Length
			}
				.Concat(SOCKET_MAGIC_HEADER)
				.Concat(new [] { (byte)SOCKET_SERIALIZER.Length })
				.Concat(SOCKET_SERIALIZER)
				.ToArray();

			INITIAL_MESSAGE[0] = (byte)INITIAL_MESSAGE.Length;
		}

		/// <summary>
		/// The serializable instance that represents the header of a request
		/// </summary>
		[Serializable]
		private class RequestHeader
		{
			/// <summary>
			/// The target channel ID
			/// </summary>
			public string ChannelID { get; set; }
			/// <summary>
			/// The initiaters ID
			/// </summary>
			public string ChannelDataType { get; set; }
			/// <summary>
			/// The pending request ID
			/// </summary>
			public string RequestID { get; set; }
			/// <summary>
			/// The initiaters ID
			/// </summary>
			public string SourceID { get; set; }
			/// <summary>
			/// <c>True</c> if this request does not have a two-phase commit object
			/// </summary>
			public bool NoOffer { get; set; }
			/// <summary>
			/// A value indicating what kind of message this is
			/// </summary>
			public NetworkMessageType RequestType { get; set; }
			/// <summary>
			/// The timeout associated with the request.
			/// </summary>
			public DateTime Timeout { get; set; }
			/// <summary>
			/// The type of payload, if any
			/// </summary>
			public string PayloadClassName { get; set; }
		}

		/// <summary>
		/// The TCP connection that this instance communicates over
		/// </summary>
		private TcpClient m_socket;

		/// <summary>
		/// The network stream used to read and write messages
		/// </summary>
		private NetworkStream m_stream;

		/// <summary>
		/// The write request raw channel
		/// </summary>
		private IChannel<PendingNetworkRequest> m_writeRequests;
		/// <summary>
		/// The read request raw channel
		/// </summary>
		private IChannel<PendingNetworkRequest> m_readRequests;

		/// <summary>
		/// An ID for this network connection, used for easy identificaiton while debugging
		/// </summary>
		public string SelfID = Guid.NewGuid().ToString("N");

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.NetworkClient"/> class.
		/// </summary>
		/// <param name="client">The underlying TCP client.</param>
		public NetworkClient(TcpClient client)
		{
			if (!client.Connected)
				throw new InvalidOperationException("Cannot use socket if not connected");
			m_socket = client;

			using (new IsolatedChannelScope())
			{
				m_writeRequests = ChannelManager.CreateChannel<PendingNetworkRequest>();
				m_readRequests = ChannelManager.CreateChannel<PendingNetworkRequest>();
			}
		}

		/// <summary>
		/// Negotiates a connection with the other end, ensuring that the two ends use compatible protocols.
		/// </summary>
		/// <returns>The connection.</returns>
		private async Task NegotiateConnection()
		{
			var rbuf = new byte[1024];
			var sent = m_stream.WriteAsync(INITIAL_MESSAGE, 0, INITIAL_MESSAGE.Length);
			var recv = ForceReadAsync(m_stream, rbuf, 0, 3);

			var firstComplete = Task.WhenAny(sent, recv);

			// Bail on errors
			if (firstComplete.IsFaulted || firstComplete.IsCanceled)
				await firstComplete;

			await recv;

			if (rbuf[1] != SOCKET_PROTOCOL_MAJOR_VERSION || rbuf[2] != SOCKET_PROTOCOL_MINOR_VERSION)
				throw new Exception(string.Format("Remote socket has version {0}.{1} but {2}.{3} was expected", rbuf[1], rbuf[2], SOCKET_PROTOCOL_MAJOR_VERSION, SOCKET_PROTOCOL_MINOR_VERSION));

			var headlen = rbuf[0];
			await ForceReadAsync(m_stream, rbuf, 3, rbuf[0] - 3);

			var magiclen = rbuf[3];
			//if (!ArraysEqual(SOCKET_MAGIC_HEADER, rbuf, 4))
			if (!rbuf.Skip(4).Take(magiclen).SequenceEqual(SOCKET_MAGIC_HEADER))
				throw new Exception("Magic header was incorrect");

			var serializerlen = rbuf[4 + magiclen];
			if (!rbuf.Skip(5 + magiclen).Take(serializerlen).SequenceEqual(SOCKET_SERIALIZER))
				throw new Exception("Serializer was incorrect");

			if (serializerlen + magiclen + 5 != headlen)
				throw new Exception("Extra data in header not supported");
		}

		/// <summary>
		/// Helper method to compare if a subset of two methods are equal
		/// </summary>
		/// <returns><c>true</c>, if the two arrays are equal, <c>false</c> otherwise.</returns>
		/// <param name="src">The fixed length source.</param>
		/// <param name="trg">The target array which may be larger.</param>
		/// <param name="offset">The offset into the array.</param>
		/// <typeparam name="T">The type of the array elements.</typeparam>
		private static bool ArraysEqual<T>(T[] src, T[] trg, int offset)
		{
			var cmp = EqualityComparer<T>.Default;

			if (src.Length > trg.Length + offset + src.Length)
				return false;

			for (var i = 0; i < src.Length; i++)
				if (!(cmp.Equals(src[i], trg[i + offset])))
					return false;

			return true;
		}

		/// <summary>
		/// Helper method to read data from the stream, even if it is fragmented
		/// </summary>
		/// <returns>The task that signals all bytes are read, or an error.</returns>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="buffer">The buffer to write into.</param>
		/// <param name="offset">The offset into the buffer.</param>
		/// <param name="len">The total number of bytes to read.</param>
		private static async Task ForceReadAsync(Stream stream, byte[] buffer, int offset, int len)
		{
			var remain = len;
			while (remain > 0)
			{
				var read = await stream.ReadAsync(buffer, offset, remain);
				if (read == 0)
					throw new System.IO.EndOfStreamException("Premature EOS");

				remain -= read;
				offset += read;
			}
		}
			
		/// <summary>
		/// Connect this instance to the other end.
		/// </summary>
		public async Task ConnectAsync()
		{
			LOG.DebugFormat("{0} is connecting", SelfID);
			if (!m_socket.Connected)
				throw new InvalidOperationException("Cannot use socket if not connected");
			try
			{
				m_stream = m_socket.GetStream();
				await NegotiateConnection();
			}
			catch
			{
				try { m_stream.Dispose(); }
				catch { }
				finally { m_stream = null; }

				try { m_socket.Close(); }
				catch { }

				try { m_readRequests.Retire(); }
				catch { }

				try { m_writeRequests.Retire(); }
				catch { }

				throw;
			}

			ReaderProcess(m_socket, m_stream, m_readRequests.AsWriteOnly(), SelfID).FireAndForget();
			WriterProcess(m_socket, m_stream, m_writeRequests.AsReadOnly(), SelfID).FireAndForget();
		}
	
		/// <summary>
		/// The process that reads data from the underlying stream and parses it into a <see cref="CoCoL.Network.PendingNetworkRequest" />.
		/// </summary>
		/// <returns>The awaitable task.</returns>
		/// <param name="client">The <see cref="System.Net.Sockets.TcpClient"/> to read data from.</param>
		/// <param name="stream">The stream to read data from.</param>
		/// <param name="channel">The channel to write requests to.</param>
		/// <param name="selfid">A string used to identify this process in logs</param>
		private static async Task ReaderProcess(TcpClient client, Stream stream, IWriteChannelEnd<PendingNetworkRequest> channel, string selfid)
		{
			try
			{
				var buffer = new byte[SMALL_MESSAGE_SIZE];
				var json = new Newtonsoft.Json.JsonSerializer();

				using(client)
				using(stream)
				using(channel)
				{
					while (true)
					{
						LOG.DebugFormat("{0}: Waiting for data from stream", selfid);
						await ForceReadAsync(stream, buffer, 0, 8);
						LOG.DebugFormat("{0}: Not waiting for data from stream", selfid);

						var streamlen = BitConverter.ToUInt64(buffer, 0);
						if (streamlen > MAX_MESSAGE_SIZE)
							throw new Exception("Perhaps too big data?");

						await ForceReadAsync(stream, buffer, 0, 2);

						var headlen = BitConverter.ToUInt16(buffer, 0);
						if (headlen > buffer.Length || headlen > streamlen)
							throw new Exception("Perhaps too big data?");

						await ForceReadAsync(stream, buffer, 0, headlen);

						RequestHeader header;
						using(var ms = new MemoryStream(buffer, 0, headlen))
						using(var sr = new StreamReader(ms))
						using(var jr = new Newtonsoft.Json.JsonTextReader(sr))
							header = json.Deserialize<RequestHeader>(jr);

						LOG.DebugFormat("{4}: Got {0} - {1} request with {2} bytes from {3}", header.RequestID, header.RequestType, streamlen, client.Client.RemoteEndPoint, selfid);

						await ForceReadAsync(stream, buffer, 0, 8);

						var payloadlen = BitConverter.ToUInt64(buffer, 0);
						if (payloadlen > MAX_MESSAGE_SIZE || payloadlen > streamlen - headlen)
							throw new Exception("Perhaps too big data?");

						object payload = null;
						if (header.PayloadClassName != null)
						{
							var bf = payloadlen <= (ulong)buffer.Length ? buffer : new byte[payloadlen];
							await ForceReadAsync(stream, bf, 0, (int)payloadlen);

							var objtype = Type.GetType(header.PayloadClassName);
							if (objtype == null)
								throw new ArgumentException(string.Format("Unable to determine the target type to create for {0}", header.PayloadClassName));

							using(var ms = new MemoryStream(bf, 0, (int)payloadlen))
							using(var sr = new StreamReader(ms))
							using(var jr = new Newtonsoft.Json.JsonTextReader(sr))
								payload = json.Deserialize(jr, objtype);
						}

						var pnrq = new PendingNetworkRequest(
							header.ChannelID,
							Type.GetType(header.ChannelDataType),
							header.RequestID,
							header.SourceID,
							header.Timeout,
							header.RequestType,
							payload,
							header.NoOffer
						);

						LOG.DebugFormat("{2}: Forwarding {0} - {1} request", header.RequestID, header.RequestType, selfid);
						await channel.WriteAsync(pnrq);
						LOG.DebugFormat("{2}: Forwarded {0} - {1} request", header.RequestID, header.RequestType, selfid);
					}
				}
			}
			catch(Exception ex)
			{
				if (!ex.IsRetiredException())
				{
					LOG.Error("Crashed network client reader side", ex);
					throw;
				}
				else
					LOG.Info("Stopped network client reader");
			}
		}

		/// <summary>
		/// The process that serializes data from a <see cref="CoCoL.Network.PendingNetworkRequest" /> and sends it into the channel.
		/// </summary>
		/// <returns>The awaitable task.</returns>
		/// <param name="client">The <see cref="System.Net.Sockets.TcpClient"/> to read data from.</param>
		/// <param name="stream">The stream to write data to.</param>
		/// <param name="channel">The channel to read requests from.</param>
		/// <param name="selfid">A string used to identify this process in logs</param>
		private static async Task WriterProcess(TcpClient client, Stream stream, IReadChannelEnd<PendingNetworkRequest> channel, string selfid)
		{
			try
			{
				var headbuffer = new byte[SMALL_MESSAGE_SIZE];
				var json = new Newtonsoft.Json.JsonSerializer();

				using(client)
				using(stream)
				using(channel)
				{
					while (true)
					{
						var prnq = await channel.ReadAsync();
						var header = new RequestHeader() {
							ChannelID = prnq.ChannelID,
							ChannelDataType = prnq.ChannelDataType.AssemblyQualifiedName,
							RequestID = prnq.RequestID,
							SourceID = prnq.SourceID,
							RequestType = prnq.RequestType,
							Timeout = prnq.Timeout,
							PayloadClassName = prnq.Value == null ? null : prnq.Value.GetType().AssemblyQualifiedName,
							NoOffer = prnq.Offer == null
						};

						ushort headlen;
						using(var ms = new MemoryStream(headbuffer, true))
						{
							// Make space for the size fields
							ms.Write(headbuffer, 0, 8 + 2);

							using(var tw = new StreamWriter(ms))
							using(var jw = new Newtonsoft.Json.JsonTextWriter(tw))
							{
								json.Serialize(jw, header);

								jw.Flush();
								await tw.FlushAsync();

								headlen = (ushort)(ms.Position - 8 - 2);
							}
						}

						// We write it all into the array before writing to the stream
						if (headlen > SMALL_MESSAGE_SIZE - 8 - 2 - 8)
							throw new Exception("Too larger header");

						// Make a memory stream for the payload
						using(var ms = new MemoryStream())
						using(var tw = new StreamWriter(ms))
						using(var jw = new Newtonsoft.Json.JsonTextWriter(tw))
						{
							ulong payloadlen = 0;

							if (prnq.Value != null)
							{
								json.Serialize(jw, prnq.Value);

								jw.Flush();
								await tw.FlushAsync();

								payloadlen = (ulong)ms.Length;
								ms.Position = 0;
							}

							if (payloadlen > MAX_MESSAGE_SIZE)
								throw new Exception("Too large message payload");

							ulong packlen = 8uL + 2uL + headlen + 8uL + payloadlen;

							Array.Copy(BitConverter.GetBytes(packlen), headbuffer, 8);
							Array.Copy(BitConverter.GetBytes(headlen), 0, headbuffer, 8, 2);
							Array.Copy(BitConverter.GetBytes(payloadlen), 0, headbuffer, 8 + 2 + headlen, 8);

							LOG.DebugFormat("{2}: Sending {0} - {1} request", prnq.RequestID, prnq.RequestType, selfid);
							await stream.WriteAsync(headbuffer, 0, headlen + 8 + 2 + 8);
							if (payloadlen != 0)
								await ms.CopyToAsync(stream);
							
							LOG.DebugFormat("{4}: Sent {0} - {1} request with {2} bytes to {3}", prnq.RequestID, prnq.RequestType, packlen, client.Client.RemoteEndPoint, selfid);
							await stream.FlushAsync();
						}
					}
				}
			}
			catch(Exception ex)
			{
				if (!ex.IsRetiredException())
				{
					LOG.Error("Crashed network client writer side", ex);
					throw;
				}
				else
					LOG.Info("Stopped network client writer");
			}
		}

		/// <summary>
		/// Reads a <see cref="CoCoL.Network.PendingNetworkRequest" /> from the connection
		/// </summary>
		/// <returns>The awaitable result.</returns>
		public Task<PendingNetworkRequest> ReadAsync()
		{
			return m_readRequests.ReadAsync();
		}

		/// <summary>
		/// Writes a <see cref="CoCoL.Network.PendingNetworkRequest" /> to the connection.
		/// </summary>
		/// <returns>The awaitable task.</returns>
		/// <param name="req">The request to write.</param>
		public Task WriteAsync(PendingNetworkRequest req)
		{
			return m_writeRequests.WriteAsync(req);
		}

		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.Network.NetworkClient"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CoCoL.Network.NetworkClient"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="CoCoL.Network.NetworkClient"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="CoCoL.Network.NetworkClient"/> so
		/// the garbage collector can reclaim the memory that the <see cref="CoCoL.Network.NetworkClient"/> was occupying.</remarks>
		public void Dispose()
		{
			try { m_readRequests.Retire(); }
			catch { }

			try { m_writeRequests.Retire(); }
			catch { }
		}
	}
}

