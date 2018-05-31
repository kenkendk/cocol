using System;

namespace CoCoL.Network
{
	/// <summary>
	/// A channel scope that automatically registers some or all channels as network channels
	/// </summary>	
    public class NetworkChannelScope : PrefixCreateChannelScope
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.NetworkChannelScope"/> class.
		/// </summary>
		/// <param name="prefix">The prefix for a channel name that indicates network channels, or an empty string to make all channels network based.</param>
		/// <param name="redirectunnamed">Set to <c>true</c> if unnamed channels should be created as network channels. Usually this is not desired as they can only be passed by reference, and thus all access must be local anyway. Consider only using this option for testing network channels.</param>
		public NetworkChannelScope(string prefix = null, bool redirectunnamed = false)
            : base(Creator, prefix, redirectunnamed)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="T:CoCoL.Network.NetworkChannelScope"/> class.
        /// </summary>
        /// <param name="names">Names of the channels to make networked.</param>
        public NetworkChannelScope(params string[] names)
            : base(Creator, names)
        {
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.NetworkChannelScope"/> class.
		/// </summary>
		/// <param name="selector">The selector method used too determine if a channel should be network based or not. Return <c>true</c> to create the channel as a network channel, <c>false</c> to use a local.</param>
		public NetworkChannelScope(Func<string, bool> selector)
            : base(Creator, selector)
		{
		}

        /// <summary>
        /// Handler method to create a network channel
        /// </summary>
        /// <returns>The network channel.</returns>
        /// <param name="scope">The scope calling the method.</param>
        /// <param name="attr">The channel attribute.</param>
        /// <param name="type">The type to create the channel for</param>
        private static IUntypedChannel Creator(ChannelScope scope, ChannelNameAttribute attr, Type type)
        {
            return 
                (IUntypedChannel)typeof(NetworkChannelScope)
                .GetMethod(nameof(CreateNetworkChannel), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.NonPublic)
                .MakeGenericMethod(type)
                .Invoke(scope, new object[] { attr });
        }

        /// <summary>
        /// Creates a channel that is wrapped in a profiling channel instance
        /// </summary>
        /// <returns>The profiling-wrapped channel.</returns>
        /// <param name="attribute">The channel attribute.</param>
        /// <typeparam name="T">The channel type parameter.</typeparam>
        private IChannel<T> CreateNetworkChannel<T>(ChannelNameAttribute attribute)
        {
            // We do not support annoymous channels, so we assign a random ID
            if (string.IsNullOrWhiteSpace(attribute.Name))
                attribute.Name = Guid.NewGuid().ToString("N");

            // Transmit the desired channel properties to the channel server
            NetworkConfig.TransmitRequestAsync(new PendingNetworkRequest(attribute.Name, typeof(T), NetworkMessageType.CreateChannelRequest, attribute));
            return new NetworkChannel<T>(attribute);
        }
	}
}

