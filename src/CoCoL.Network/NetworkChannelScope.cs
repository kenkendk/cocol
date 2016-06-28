using System;

namespace CoCoL.Network
{
	/// <summary>
	/// A channel scope that automatically registers some or all channels as network channels
	/// </summary>	
	public class NetworkChannelScope : ChannelScope
	{
		/// <summary>
		/// The channel selector method used to choose if a channel should be network based or not.
		/// Return <c>true</c> to create the channel as a network channel, <c>false</c> to use a local.
		/// </summary>
		private readonly Func<string, bool> m_selector;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.NetworkChannelScope"/> class.
		/// </summary>
		/// <param name="prefix">The prefix for a channel name that indicates network channels, or an empty string to make all channels network based.</param>
		/// <param name="redirectunnamed">Set to <c>true</c> if unnamed channels should be created as network channels. Usually this is not desired as they can only be passed by reference, and thus all access must be local anyway. Consider only using this option for testing network channels.</param>
		public NetworkChannelScope(string prefix = null, bool redirectunnamed = false)
		{
			prefix = prefix ?? "";

			m_selector = (name) => {
				// If all channels need to be network channels, we assign a name to this channel
				if (string.IsNullOrWhiteSpace(name) && redirectunnamed)
					return true;

				// Only create those with the right prefix
				return !string.IsNullOrWhiteSpace(name) && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
			};

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.NetworkChannelScope"/> class.
		/// </summary>
		/// <param name="selector">The selector method used too determine if a channel should be network based or not. Return <c>true</c> to create the channel as a network channel, <c>false</c> to use a local.</param>
		public NetworkChannelScope(Func<string, bool> selector)
		{
			if (selector == null)
				throw new ArgumentNullException("selector");
			m_selector = selector;
		}

		/// <summary>
		/// Creates the channel by calling the ChannelManager.
		/// </summary>
		/// <returns>The channel with the given name.</returns>
		/// <param name="attribute">The attribute describing the channel to create.</param>
		/// <typeparam name="T">The type of data in the channel.</typeparam>
		protected override IChannel<T> DoCreateChannel<T>(ChannelNameAttribute attribute)
		{
			if (m_selector(attribute.Name))
			{
				// We do not support annoymous channels, so we assign a random ID
				if (string.IsNullOrWhiteSpace(attribute.Name))
					attribute.Name = Guid.NewGuid().ToString("N");

				// Transmit the desired channel properties to the channel server
				NetworkConfig.TransmitRequestAsync(new PendingNetworkRequest(attribute.Name, typeof(T), NetworkMessageType.CreateChannelRequest, attribute));
				return new NetworkChannel<T>(attribute);
			}

			return base.DoCreateChannel<T>(attribute);
		}
	}
}

