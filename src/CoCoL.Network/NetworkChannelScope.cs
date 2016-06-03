using System;

namespace CoCoL.Network
{
	/// <summary>
	/// A channel scope that automatically registers some or all channels as network channels
	/// </summary>	
	public class NetworkChannelScope : ChannelScope
	{
		/// <summary>
		/// The channel prefix used to choose if a channel should be network based or not.
		/// </summary>
		private readonly string m_prefix;

		/// <summary>
		/// A value indicating if unnamed channels are also created as network channels.
		/// Usually this is not desired as they can only be passed by reference,
		/// and thus all access must be local anyway.
		/// Consider only using this option for testing network channels.
		/// </summary>
		private readonly bool m_redirectunnamed;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.NetworkChannelScope"/> class.
		/// </summary>
		/// <param name="prefix">The prefix for a channel name that indicates network channels, or an empty string to make all channels network based.</param>
		/// <param name="redirectunnamed">Set to <c>true</c> if unnamed channels should be created as network channels. Usually this is not desired as they can only be passed by reference, and thus all access must be local anyway. Consider only using this option for testing network channels.</param>
		public NetworkChannelScope(string prefix = null, bool redirectunnamed = false)
		{
			m_prefix = prefix ?? "";
			m_redirectunnamed = redirectunnamed;
		}

		/// <summary>
		/// Creates the channel by calling the ChannelManager.
		/// </summary>
		/// <returns>The channel with the given name.</returns>
		/// <param name="name">The name of the channel to create.</param>
		/// <param name="buffersize">The size of the channel buffer.</param>
		/// <param name="maxPendingReaders">The maximum number of pending readers. A negative value indicates infinite</param>
		/// <param name="maxPendingWriters">The maximum number of pending writers. A negative value indicates infinite</param>
		/// <param name="pendingReadersOverflowStrategy">The strategy for dealing with overflow for read requests</param>
		/// <param name="pendingWritersOverflowStrategy">The strategy for dealing with overflow for write requests</param>
		/// <typeparam name="T">The type of data in the channel.</typeparam>
		protected override IChannel<T> DoCreateChannel<T>(string name, int buffersize, int maxPendingReaders, int maxPendingWriters, QueueOverflowStrategy pendingReadersOverflowStrategy, QueueOverflowStrategy pendingWritersOverflowStrategy)
		{			
			// If all channels need to be network channels, we assign a name to this channel
			if (string.IsNullOrWhiteSpace(name) && m_redirectunnamed)
				name = m_prefix + Guid.NewGuid().ToString("N");

			// Only create those with the right prefix
			if (string.IsNullOrWhiteSpace(name) || !name.StartsWith(m_prefix))
				return base.DoCreateChannel<T>(name, buffersize, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingReadersOverflowStrategy);

			// Transmit the desired channel properties to the channel server
			var ca = new ChannelNameAttribute(name, buffersize, ChannelNameScope.Local, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy);
			NetworkConfig.TransmitRequestAsync(new PendingNetworkRequest(name, typeof(T), NetworkMessageType.CreateChannelRequest, ca));				
			return new NetworkChannel<T>(ca);
		}
	}
}

