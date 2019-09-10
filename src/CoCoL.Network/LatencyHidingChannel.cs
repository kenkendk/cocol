using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace CoCoL.Network
{
	/// <summary>
	/// Channel wrapper that performs latency hiding,
	/// by letting a number of write requests be
	/// in-flight without blocking the write calls
	/// </summary>
	public class LatencyHidingWriter<T> : IWriteChannelEnd<T>
	{
		/// <summary>
		/// The queue of pending write requests
		/// </summary>
		private readonly Queue<Task> m_writeQueue = new Queue<Task>();
		/// <summary>
		/// The size of the buffer queue
		/// </summary>
		private readonly int m_buffersize;
		/// <summary>
		/// The channel where the requests are made
		/// </summary>
		private IWriteChannelEnd<T> m_parent;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.LatencyHidingWriter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="parent">The channel where requests are made.</param>
		/// <param name="buffersize">The size of the buffer.</param>
		public LatencyHidingWriter(IWriteChannel<T> parent, int buffersize)
			: this(parent.AsWriteOnly(), buffersize)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.LatencyHidingWriter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="parent">The channel where requests are made.</param>
		/// <param name="buffersize">The size of the buffer.</param>
		public LatencyHidingWriter(IWriteChannelEnd<T> parent, int buffersize)
		{
			if (buffersize <= 0)
				throw new ArgumentOutOfRangeException(nameof(buffersize));
			m_parent = parent;
			m_buffersize = buffersize;
		}

		#region IJoinAbleChannelEnd implementation
		/// <summary>
		/// Join the channel
		/// </summary>
		/// <returns>An awaitable task</returns>
		public Task JoinAsync()
		{
			return m_parent.JoinAsync();
		}
		/// <summary>
		/// Leave the channel.
		/// </summary>
		/// <returns>An awaitable task</returns>
		public Task LeaveAsync()
		{
			return m_parent.LeaveAsync();
		}
		#endregion
		#region IDisposable implementation
		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.Network.LatencyHidingWriter&lt;T&gt;"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CoCoL.Network.LatencyHidingWriter&lt;T&gt;"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="CoCoL.Network.LatencyHidingWriter&lt;T&gt;"/> in an unusable state.
		/// After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="CoCoL.Network.LatencyHidingWriter&lt;T&gt;"/> so the garbage collector can reclaim the memory that the
		/// <see cref="CoCoL.Network.LatencyHidingWriter&lt;T&gt;"/> was occupying.</remarks>
		public void Dispose()
		{
			m_parent.Dispose();
		}
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
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <returns>The async.</returns>
        public async Task WriteAsync(T value, ITwoPhaseOffer offer)
		{
			m_writeQueue.Enqueue(m_parent.WriteAsync(value, offer));
			while (m_writeQueue.Count > m_buffersize)
				await m_writeQueue.Dequeue();
		}
		#endregion
		#region IRetireAbleChannel implementation
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <returns>An awaitable task</returns>
		public Task RetireAsync()
		{
			return m_parent.RetireAsync();
		}
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		/// <returns>An awaitable task</returns>
		public Task RetireAsync(bool immediate)
		{
			return m_parent.RetireAsync(immediate);
		}
		/// <summary>
		/// Gets a value indicating whether this instance is retired async.
		/// </summary>
		/// <value><c>true</c> if this instance is retired async; otherwise, <c>false</c>.</value>
		public Task<bool> IsRetiredAsync
		{
			get
			{
				return m_parent.IsRetiredAsync;
			}
		}
		#endregion
	}

	/// <summary>
	/// Channel wrapper that performs latency hiding,
	/// by registering read requests ahead of time
	/// </summary>
	public class LatencyHidingReader<T> : IReadChannelEnd<T>
	{
		/// <summary>
		/// The list of premature reads
		/// </summary>
		private readonly Queue<Task<T>> m_readQueue = new Queue<Task<T>>(); 
		/// <summary>
		/// The channel being read from
		/// </summary>
		private readonly IReadChannelEnd<T> m_parent;
		/// <summary>
		/// The size of the buffer
		/// </summary>
		private readonly int m_buffersize;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.LatencyHidingReader&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="parent">The channel where requests are made.</param>
		/// <param name="buffersize">The size of the buffer.</param>
		public LatencyHidingReader(IReadChannel<T> parent, int buffersize)
			: this(parent.AsReadOnly(), buffersize)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.LatencyHidingReader&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="parent">The channel where requests are made.</param>
		/// <param name="buffersize">The size of the buffer.</param>
		public LatencyHidingReader(IReadChannelEnd<T> parent, int buffersize)
		{
			if (buffersize <= 0)
				throw new ArgumentOutOfRangeException(nameof(buffersize));
			m_parent = parent;
			m_buffersize = buffersize;
		}
		
		#region IJoinAbleChannelEnd implementation
		/// <summary>
		/// Join the channel
		/// </summary>
		/// <returns>An awaitable task</returns>
		public Task JoinAsync()
		{
			return m_parent.JoinAsync();
		}
		/// <summary>
		/// Leave the channel.
		/// </summary>
		/// <returns>An awaitable task</returns>
		public Task LeaveAsync()
		{
			return m_parent.LeaveAsync();
		}
		#endregion
		#region IDisposable implementation
		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.Network.LatencyHidingReader&lt;T&gt;"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CoCoL.Network.LatencyHidingReader&lt;T&gt;"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="CoCoL.Network.LatencyHidingReader&lt;T&gt;"/> in an unusable state.
		/// After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="CoCoL.Network.LatencyHidingReader&lt;T&gt;"/> so the garbage collector can reclaim the memory that the
		/// <see cref="CoCoL.Network.LatencyHidingReader&lt;T&gt;"/> was occupying.</remarks>
		public void Dispose()
		{
			m_parent.Dispose();
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
		/// <returns>An awaitable task.</returns>
        public Task<T> ReadAsync(ITwoPhaseOffer offer)
		{
			if (offer != null)
				throw new InvalidOperationException("LatencyHidingReader does not support offers");

			while (m_readQueue.Count < m_buffersize)
				m_readQueue.Enqueue(m_parent.ReadAsync(offer));
			
			return m_readQueue.Dequeue();
		}
		#endregion
		#region IRetireAbleChannel implementation
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <returns>An awaitable task</returns>
		public Task RetireAsync()
		{
			return m_parent.RetireAsync();
		}
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		/// <returns>An awaitable task</returns>
		public Task RetireAsync(bool immediate)
		{
			return m_parent.RetireAsync(immediate);
		}
		/// <summary>
		/// Gets a value indicating whether this instance is retired async.
		/// </summary>
		/// <value><c>true</c> if this instance is retired async; otherwise, <c>false</c>.</value>
		public Task<bool> IsRetiredAsync
		{
			get
			{
				return m_parent.IsRetiredAsync;
			}
		}
		#endregion
	}

}

