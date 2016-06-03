using System;
using System.Threading.Tasks;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "Logging.config", Watch = true)]

namespace CoCoL.Network
{
	/// <summary>
	/// The different types of network requests
	/// </summary>
	public enum NetworkMessageType
	{
		/// <summary>
		/// A request for reading a channel
		/// </summary>
		ReadRequest,
		/// <summary>
		/// A request for writing a channel
		/// </summary>
		WriteRequest,
		/// <summary>
		/// A request for retiring a channel
		/// </summary>
		RetireRequest,
		/// <summary>
		/// A request for joining a channel
		/// </summary>
		JoinRequest,
		/// <summary>
		/// A request for leaving a channel
		/// </summary>
		LeaveRequest,

		/// <summary>
		/// The message is a response to a previously issued read
		/// </summary>
		ReadResponse,

		/// <summary>
		/// The message is a response to a previously issued write
		/// </summary>
		WriteResponse,

		/// <summary>
		/// The message is a timeout response to a previously issued read or write
		/// </summary>
		TimeoutResponse,

		/// <summary>
		/// The message is a cancel response to a previously issued read or write
		/// </summary>
		CancelResponse,

		/// <summary>
		/// The message is a retired response to a previously issued read or write
		/// </summary>
		RetiredResponse,

		/// <summary>
		/// The message is a fail response to a previously issued read or write
		/// </summary>
		FailResponse,

		/// <summary>
		/// The message is a request to initialize a channel with the given properties
		/// </summary>
		CreateChannelRequest,

		/// <summary>
		/// An offer to process a previously requested read or write
		/// </summary>
		OfferRequest,
		/// <summary>
		/// A positive response to an offer to process a previously requested read or write
		/// </summary>
		OfferAcceptResponse,
		/// <summary>
		/// A negative response to an offer to process a previously requested read or write
		/// </summary>
		OfferDeclineResponse,
		/// <summary>
		/// A negative response to a previously offered read or write
		/// </summary>
		OfferWithdrawRequest,
		/// <summary>
		/// A positive response to a previously offered read or write
		/// </summary>
		OfferCommitRequest
	}

	/// <summary>
	/// Representation of a pending network request
	/// </summary>
	public class PendingNetworkRequest
	{
		/// <summary>
		/// The target channel ID
		/// </summary>
		public string ChannelID { get; private set; }
		/// <summary>
		/// The pending request ID
		/// </summary>
		public string RequestID { get; private set; }
		/// <summary>
		/// The initiaters ID
		/// </summary>
		public string SourceID { get; private set; }
		/// <summary>
		/// The channel data type
		/// </summary>
		public Type ChannelDataType { get; private set; }
		/// <summary>
		/// The task signalling completion
		/// </summary>
		public object Task { get; private set; }
		/// <summary>
		/// The timeout associated with the request.
		/// </summary>
		public DateTime Timeout { get; private set; }
		/// <summary>
		/// Gets a value indicating if this network request is not using a two-phase commit handler
		/// </summary>
		public bool NoOffer { get; private set; }
		/// <summary>
		/// The two-phase offer asssociated with the request
		/// </summary>
		public ITwoPhaseOffer Offer { get; private set; }
		/// <summary>
		/// The value to write into the channel
		/// </summary>
		public object Value { get; private set; }
		/// <summary>
		/// A value indicating what kind of message this is
		/// </summary>
		public NetworkMessageType RequestType { get; private set; }
		/// <summary>
		/// The channel associated with the request
		/// </summary>
		public INamedItem AssociatedChannel { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.PendingNetworkRequest"/> class for representing a read request.
		/// </summary>
		/// <param name="channel">The channel to communicate over.</param>
		/// <param name="channeldatatype">The datatype on the channel.</param>
		/// <param name="timeout">The timeout associated with the request.</param>
		/// <param name="offer">The two-phase offer, if any.</param>
		/// <param name="task">The <see cref="System.Threading.Tasks.TaskCompletionSource&lt;T&gt;"/> instance for signaling.</param>
		public PendingNetworkRequest(INamedItem channel, Type channeldatatype, DateTime timeout, ITwoPhaseOffer offer, object task)
		{
			ChannelID = channel.Name;
			AssociatedChannel = channel;
			ChannelDataType = channeldatatype;
			RequestID = Guid.NewGuid().ToString("N");
			SourceID = NetworkConfig.SelfID;
			Timeout = timeout;
			Offer = offer;
			Task = task;
			RequestType = NetworkMessageType.ReadRequest;
			NoOffer = offer == null;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.PendingNetworkRequest"/> class for representing a write request.
		/// </summary>
		/// <param name="channel">The channel to communicate over.</param>
		/// <param name="channeldatatype">The datatype on the channel.</param>
		/// <param name="timeout">The timeout associated with the request.</param>
		/// <param name="offer">The two-phase offer, if any.</param>
		/// <param name="task">The <see cref="System.Threading.Tasks.TaskCompletionSource&lt;T&gt;"/> instance for signaling.</param>
		/// <param name="value">The value to write into the channel.</param>
		public PendingNetworkRequest(INamedItem channel, Type channeldatatype, DateTime timeout, ITwoPhaseOffer offer, object task, object value)
		{
			ChannelID = channel.Name;
			AssociatedChannel = channel;
			ChannelDataType = channeldatatype;
			RequestID = Guid.NewGuid().ToString("N");
			SourceID = NetworkConfig.SelfID;
			Timeout = timeout;
			Offer = offer;
			Task = task;
			Value = value;
			RequestType = NetworkMessageType.WriteRequest;
			NoOffer = offer == null;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.PendingNetworkRequest"/> class for representing a locally created request.
		/// </summary>
		/// <param name="channelid">The channel to communicate over.</param>
		/// <param name="channeldatatype">The datatype on the channel.</param>
		/// <param name="type">The message type.</param>
		/// <param name="value">The data value, if any.</param>
		public PendingNetworkRequest(string channelid, Type channeldatatype, NetworkMessageType type, object value)
		{
			ChannelID = channelid;
			ChannelDataType = channeldatatype;
			RequestID = Guid.NewGuid().ToString("N");
			SourceID = NetworkConfig.SelfID;
			RequestType = type;
			Value = value;
			NoOffer = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.PendingNetworkRequest"/> class for representing an incoming request.
		/// </summary>
		/// <param name="channelid">The channel to communicate over.</param>
		/// <param name="channeldatatype">The datatype on the channel.</param>
		/// <param name="requestid">The request ID.</param>
		/// <param name="sourceid">The source ID.</param>
		/// <param name="timeout">The request timeout</param>
		/// <param name="type">The message type.</param>
		/// <param name="value">The data in the message, if any.</param>
		/// <param name="nooffer">If set to <c>true</c> the remote end does not have a two-phase instance associated.</param>
		public PendingNetworkRequest(string channelid, Type channeldatatype, string requestid, string sourceid, DateTime timeout, NetworkMessageType type, object value, bool nooffer)
		{
			ChannelID = channelid;
			RequestID = requestid;
			SourceID = sourceid;
			Timeout = timeout;
			ChannelDataType = channeldatatype;
			RequestType = type;
			Value = value;
			NoOffer = nooffer;
		}
	}
}

