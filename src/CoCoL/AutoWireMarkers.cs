using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoCoL
{
    /// <summary>
    /// Static factory class for creating channel markers
    /// </summary>
    public static class ChannelMarker
    {
        /// <summary>
        /// Creates a marker property for a read channel
        /// </summary>
        /// <returns>The marker instance.</returns>
        /// <param name="attribute">The attribute describing the channel.</param>
        /// <typeparam name="T">The type of data passed on the channel.</typeparam>
        public static IReadChannel<T> ForRead<T>(ChannelNameAttribute attribute)
        {
            return new ReadMarker<T>(attribute);
        }

        /// <summary>
        /// Creates a marker property for a read channel
        /// </summary>
        /// <returns>The marker instance.</returns>
        /// <param name="name">The name of the channel.</param>
        /// <param name="buffersize">The desired buffersize to use if the channel is created.</param>
        /// <param name="targetScope">The scope to create or locate the name in.</param>
        /// <typeparam name="T">The type of data passed on the channel.</typeparam>
        /// <param name="maxPendingReaders">The maximum number of pending readers. A negative value indicates infinite</param>
        /// <param name="maxPendingWriters">The maximum number of pending writers. A negative value indicates infinite</param>
        /// <param name="pendingReadersOverflowStrategy">The strategy for dealing with overflow for read requests</param>
        /// <param name="pendingWritersOverflowStrategy">The strategy for dealing with overflow for write requests</param>
        /// <param name="broadcast"><c>True</c> will create the channel as a broadcast channel, the default <c>false</c> will create a normal channel</param>
        /// <param name="initialBroadcastBarrier">The number of readers required on the channel before sending the first broadcast, can only be used with broadcast channels</param>
        /// <param name="broadcastMinimum">The minimum number of readers required on the channel, before a broadcast can be performed, can only be used with broadcast channels</param>
        public static IReadChannel<T> ForRead<T>(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local, int maxPendingReaders = -1, int maxPendingWriters = -1, QueueOverflowStrategy pendingReadersOverflowStrategy = QueueOverflowStrategy.Reject, QueueOverflowStrategy pendingWritersOverflowStrategy = QueueOverflowStrategy.Reject, bool broadcast = false, int initialBroadcastBarrier = -1, int broadcastMinimum = -1)
        {
            if (broadcast)
                return ForRead<T>(new BroadcastChannelNameAttribute(name, buffersize, targetScope, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy, initialBroadcastBarrier, broadcastMinimum));

            if (initialBroadcastBarrier >= 0 || broadcastMinimum >= 0)
                throw new ArgumentException(string.Format("Cannot set \"{0}\" or \"{1}\" unless the channel is a broadcast channel", "initialBroadcastBarrier", "broadcastMinimum"));
            return ForRead<T>(new ChannelNameAttribute(name, buffersize, targetScope, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy));
        }

        /// <summary>
        /// Creates a marker property for a write channel
        /// </summary>
        /// <returns>The marker instance.</returns>
        /// <param name="attribute">The attribute describing the channel.</param>
        /// <typeparam name="T">The type of data passed on the channel.</typeparam>
        public static IWriteChannel<T> ForWrite<T>(ChannelNameAttribute attribute)
        {
            return new WriteMarker<T>(attribute);
        }

        /// <summary>
        /// Creates a marker property for a write channel
        /// </summary>
        /// <returns>The marker instance.</returns>
        /// <param name="name">The name of the channel.</param>
        /// <param name="buffersize">The desired buffersize to use if the channel is created.</param>
        /// <param name="targetScope">The scope to create or locate the name in.</param>
        /// <typeparam name="T">The type of data passed on the channel.</typeparam>
        /// <param name="maxPendingReaders">The maximum number of pending readers. A negative value indicates infinite</param>
        /// <param name="maxPendingWriters">The maximum number of pending writers. A negative value indicates infinite</param>
        /// <param name="pendingReadersOverflowStrategy">The strategy for dealing with overflow for read requests</param>
        /// <param name="pendingWritersOverflowStrategy">The strategy for dealing with overflow for write requests</param>
        /// <param name="broadcast"><c>True</c> will create the channel as a broadcast channel, the default <c>false</c> will create a normal channel</param>
        /// <param name="initialBroadcastBarrier">The number of readers required on the channel before sending the first broadcast, can only be used with broadcast channels</param>
        /// <param name="broadcastMinimum">The minimum number of readers required on the channel, before a broadcast can be performed, can only be used with broadcast channels</param>
        public static IWriteChannel<T> ForWrite<T>(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local, int maxPendingReaders = -1, int maxPendingWriters = -1, QueueOverflowStrategy pendingReadersOverflowStrategy = QueueOverflowStrategy.Reject, QueueOverflowStrategy pendingWritersOverflowStrategy = QueueOverflowStrategy.Reject, bool broadcast = false, int initialBroadcastBarrier = -1, int broadcastMinimum = -1)
        {
            if (broadcast)
                return ForWrite<T>(new BroadcastChannelNameAttribute(name, buffersize, targetScope, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy, initialBroadcastBarrier, broadcastMinimum));

            if (initialBroadcastBarrier >= 0 || broadcastMinimum >= 0)
                throw new ArgumentException(string.Format("Cannot set \"{0}\" or \"{1}\" unless the channel is a broadcast channel", "initialBroadcastBarrier", "broadcastMinimum"));
            return ForWrite<T>(new ChannelNameAttribute(name, buffersize, targetScope, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy));
        }
    }

    /// <summary>
    /// Helper class for creating typed-and-named markers
    /// </summary>
    public class ChannelMarkerWrapper<T> : INamedItem
    {
        /// <summary>
        /// Gets the channel as a write request
        /// </summary>
        public IWriteChannel<T> ForWrite { get; private set; }
        /// <summary>
        /// Gets the channel as a read request
        /// </summary>
        public IReadChannel<T> ForRead { get; private set; }

        /// <summary>
        /// The attribute representing this marker
        /// </summary>
        public ChannelNameAttribute Attribute { get; private set; }

        /// <summary>
        /// Gets the name of the channel
        /// </summary>
        public string Name { get { return Attribute.Name; } }

        /// <summary>
        /// The buffer size for the channel
        /// </summary>
        public int BufferSize { get { return Attribute.BufferSize; } }

        /// <summary>
        /// The target channel scope
        /// </summary>
        public ChannelNameScope TargetScope { get { return Attribute.TargetScope; } }

        /// <summary>
        /// The maximum number of pending readers
        /// </summary>
        public int MaxPendingReaders { get { return Attribute.MaxPendingReaders; } }

        /// <summary>
        /// The maximum number of pendinger writers
        /// </summary>
        public int MaxPendingWriters { get { return Attribute.MaxPendingWriters; } }

        /// <summary>
        /// The strategy for selecting pending readers to discard on overflow
        /// </summary>
        public QueueOverflowStrategy PendingReadersOverflowStrategy { get { return Attribute.PendingReadersOverflowStrategy; } }

        /// <summary>
        /// The strategy for selecting pending readers to discard on overflow
        /// </summary>
        public QueueOverflowStrategy PendingWritersOverflowStrategy { get { return Attribute.PendingWritersOverflowStrategy; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:CoCoL.ChannelMarkerWrapper&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="attribute">The attribute describing the channel.</param>
        public ChannelMarkerWrapper(ChannelNameAttribute attribute)
        {
            Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            ForWrite = ChannelMarker.ForWrite<T>(attribute);
            ForRead = ChannelMarker.ForRead<T>(attribute);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:CoCoL.ChannelMarkerWrapper&lt;T&gt;"/> class,
        /// </summary>
        /// <param name="name">The name of the channel.</param>
        /// <param name="buffersize">The size of the buffer on the created channel</param>
        /// <param name="targetScope">The scope where the channel is created</param>
        /// <param name="maxPendingReaders">The maximum number of pending readers. A negative value indicates infinite</param>
        /// <param name="maxPendingWriters">The maximum number of pending writers. A negative value indicates infinite</param>
        /// <param name="pendingReadersOverflowStrategy">The strategy for dealing with overflow for read requests</param>
        /// <param name="pendingWritersOverflowStrategy">The strategy for dealing with overflow for write requests</param>
        public ChannelMarkerWrapper(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local, int maxPendingReaders = -1, int maxPendingWriters = -1, QueueOverflowStrategy pendingReadersOverflowStrategy = QueueOverflowStrategy.Reject, QueueOverflowStrategy pendingWritersOverflowStrategy = QueueOverflowStrategy.Reject)
            : this(new ChannelNameAttribute(name, buffersize, targetScope, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy))
        {
        }

        /// <summary>
        /// Gets or creates the channel in the current scope
        /// </summary>
        /// <returns>The channel instance.</returns>
        public IChannel<T> Get()
        {
            return ChannelManager.GetChannel<T>(this);
        }

        /// <summary>
        /// Gets or creates the channel in the current scope
        /// </summary>
        /// <returns>The channel instance.</returns>
        public IReadChannelEnd<T> GetForRead()
        {
            return ChannelManager.GetChannel<T>(this).AsReadOnly();
        }
        /// <summary>
        /// Gets or creates the channel in the current scope
        /// </summary>
        /// <returns>The channel instance.</returns>
        public IWriteChannelEnd<T> GetForWrite()
        {
            return ChannelManager.GetChannel<T>(this).AsWriteOnly();
        }
    }

	/// <summary>
	/// Marker class for specifying channel attributes in anonymous types
	/// </summary>
	public abstract class ChannelNameMarker : INamedItem
	{
		/// <summary>
		/// Gets the attribute representation of the data.
		/// </summary>
		public ChannelNameAttribute Attribute { get; protected set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelNameMarker"/> class.
		/// </summary>
		/// <param name="attribute">The attribute describing the channel.</param>
        protected ChannelNameMarker(ChannelNameAttribute attribute)
		{
            this.Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get { return Attribute.Name; } }
	}

	/// <summary>
	/// Marker class for specifying channel attributes in anonymous types
	/// </summary>
	internal class ReadMarker<T> : ChannelNameMarker, IReadChannel<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ReadMarker&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="attribute">The attribute describing the channel.</param>
		public ReadMarker(ChannelNameAttribute attribute)
			: base(attribute)
		{
		}

        // Since this is just a marker, we do not implement any methods

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
        public Task<T> ReadAsync(ITwoPhaseOffer offer)
		{
			throw new NotImplementedException();
		}
		#endregion
		#region IRetireAbleChannel implementation
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		public Task RetireAsync()
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		public Task RetireAsync(bool immediate)
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Gets a value indicating whether this instance is retired.
		/// </summary>
		/// <value><c>true</c> if this instance is retired; otherwise, <c>false</c>.</value>
		public Task<bool> IsRetiredAsync
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		#endregion
	}

	internal class WriteMarker<T> : ChannelNameMarker, IWriteChannel<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.WriteMarker&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="attribute">The attribute describing the channel.</param>
		public WriteMarker(ChannelNameAttribute attribute)
			: base(attribute)
		{
		}

        // Since this is just a marker, we do not implement any methods

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
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="offer">The two-phase offer</param>
		/// <returns>The async.</returns>
        public Task WriteAsync(T value, ITwoPhaseOffer offer)
		{
			throw new NotImplementedException();
		}
		#endregion
		#region IRetireAbleChannel implementation
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		public Task RetireAsync()
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		public Task RetireAsync(bool immediate)
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Gets a value indicating whether this instance is retired.
		/// </summary>
		/// <value><c>true</c> if this instance is retired; otherwise, <c>false</c>.</value>
		public Task<bool> IsRetiredAsync
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		#endregion
	}
}

