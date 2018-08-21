using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoCoL.Network
{
	/// <summary>
	/// A network channel implementation.
	/// </summary>
	public class NetworkChannel<T> : IChannel<T>, IUntypedChannel, IJoinAbleChannel, INamedItem
	{
		/// <summary>
		/// The channel marker attribute
		/// </summary>
		private ChannelNameAttribute m_attribute;

		/// <summary>
		/// Flag indicating if the channel is retired
		/// </summary>
		private bool m_isRetired = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.NetworkChannel&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="channelid">The channel this instance represents.</param>
		public NetworkChannel(string channelid)
		{
			m_attribute = new ChannelNameAttribute(channelid);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.NetworkChannel&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="attr">The channel this instance represents.</param>
		public NetworkChannel(ChannelNameAttribute attr)
		{
			m_attribute = attr;
		}

		#region INamedItem implementation

		/// <summary>
		/// Gets the name of the item
		/// </summary>
		/// <value>The name of this item</value>
		public string Name { get { return m_attribute.Name; } }

        #endregion

        #region IWriteChannel implementation

        /// <summary>
        /// Registers a desire to write to the channel
        /// </summary>
        /// <param name="value">The value to write to the channel.</param>
        /// <returns>The async.</returns>
        public Task WriteAsync(T value)
        {
            return WriteAsync(value, null);
        }

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A two-phase offer, use null to unconditionally accept</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <returns>The async.</returns>
        public async Task WriteAsync(T value, ITwoPhaseOffer offer)
		{
			var tcs = new TaskCompletionSource<bool>();
            await NetworkConfig.TransmitRequestAsync(new PendingNetworkRequest(this, typeof(T), Timeout.InfiniteDateTime, offer, tcs, value));
			await tcs.Task;
		}

        #endregion

        #region IReadChannel implementation

        /// <summary>
        /// Registers a desire to read from the channel
        /// </summary>
        /// <returns>The async.</returns>
        public Task<T> ReadAsync()
        {
            return ReadAsync(null);
        }

		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
        public async Task<T> ReadAsync(ITwoPhaseOffer offer)
		{
			var tcs = new TaskCompletionSource<T>();
			await NetworkConfig.TransmitRequestAsync(new PendingNetworkRequest(this, typeof(T), Timeout.InfiniteDateTime, offer, tcs));
			return await tcs.Task;
		}

		#endregion

		#region IRetireAbleChannel implementation

		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		public Task RetireAsync()
		{
			return RetireAsync(true);
		}

		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		public Task RetireAsync(bool immediate)
		{
			// TODO: We should wait?
			// TODO: We should propagate the state back

			NetworkConfig.TransmitRequest(new PendingNetworkRequest(m_attribute.Name, typeof(T), NetworkMessageType.RetireRequest, immediate));
			m_isRetired = true;

			return Task.FromResult(true);
		}

		/// <summary>
		/// Gets a value indicating whether this instance is retired.
		/// </summary>
		/// <value><c>true</c> if this instance is retired; otherwise, <c>false</c>.</value>
		public Task<bool> IsRetiredAsync
		{
			get
			{
				// TODO: We should propagate the state back
				return Task.FromResult(m_isRetired);
			}
		}

		#endregion

		#region IJoinAbleChannel implementation

		/// <summary>
		/// Join the channel
		/// </summary>
		/// <param name="asReader">true</param>
		/// <c>false</c>
		public Task JoinAsync(bool asReader)
		{
			// TODO: We should wait?

			NetworkConfig.TransmitRequest(new PendingNetworkRequest(m_attribute.Name, typeof(T), NetworkMessageType.JoinRequest, asReader));
			return Task.FromResult(true);
		}

		/// <summary>
		/// Leave the channel.
		/// </summary>
		/// <param name="asReader">true</param>
		/// <c>false</c>
		public Task LeaveAsync(bool asReader)
		{
			// TODO: We should wait?

			NetworkConfig.TransmitRequest(new PendingNetworkRequest(m_attribute.Name, typeof(T), NetworkMessageType.LeaveRequest, asReader));
			return Task.FromResult(true);
		}

		#endregion
	}
}

