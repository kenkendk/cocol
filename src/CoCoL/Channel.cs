using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace CoCoL
{
    /// <summary>
    /// Static helper class to create channels
    /// </summary>
    public static class Channel
    {
        /// <summary>
        /// Gets or creates a named channel.
        /// </summary>
        /// <returns>The named channel.</returns>
        /// <param name="name">The name of the channel to find.</param>
        /// <param name="buffersize">The number of buffers in the channel.</param>
        /// <param name="scope">The scope to create a named channel in, defaults to null which means the current scope</param>
        /// <param name="maxPendingReaders">The maximum number of pending readers. A negative value indicates infinite</param>
        /// <param name="maxPendingWriters">The maximum number of pending writers. A negative value indicates infinite</param>
        /// <param name="pendingReadersOverflowStrategy">The strategy for dealing with overflow for read requests</param>
        /// <param name="pendingWritersOverflowStrategy">The strategy for dealing with overflow for write requests</param>
        /// <param name="broadcast"><c>True</c> will create the channel as a broadcast channel, the default <c>false</c> will create a normal channel</param>
        /// <param name="initialBroadcastBarrier">The number of readers required on the channel before sending the first broadcast, can only be used with broadcast channels</param>
        /// <param name="broadcastMinimum">The minimum number of readers required on the channel, before a broadcast can be performed, can only be used with broadcast channels</param>
        /// <typeparam name="T">The channel type.</typeparam>
        public static IChannel<T> Get<T>(string name, int buffersize = 0, ChannelScope scope = null, int maxPendingReaders = -1, int maxPendingWriters = -1, QueueOverflowStrategy pendingReadersOverflowStrategy = QueueOverflowStrategy.Reject, QueueOverflowStrategy pendingWritersOverflowStrategy = QueueOverflowStrategy.Reject, bool broadcast = false, int initialBroadcastBarrier = -1, int broadcastMinimum = -1)
        {
            return ChannelManager.GetChannel<T>(name, buffersize, scope, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy, broadcast, initialBroadcastBarrier, broadcastMinimum);
        }

        /// <summary>
        /// Gets or creates a named channel.
        /// </summary>
        /// <returns>The named channel.</returns>
        /// <param name="attr">The attribute describing the channel.</param>
        /// <param name="scope">The scope to create a named channel in, defaults to null which means the current scope</param>
        /// <typeparam name="T">The channel type.</typeparam>
        public static IChannel<T> Get<T>(ChannelNameAttribute attr, ChannelScope scope = null)
        {
            return ChannelManager.GetChannel<T>(attr, scope);
        }

        /// <summary>
        /// Gets or creates a named channel from a marker setup
        /// </summary>
        /// <returns>The named channel.</returns>
        /// <param name="marker">The channel marker instance that describes the channel.</param>
        /// <typeparam name="T">The channel type.</typeparam>
        public static IChannel<T> Get<T>(ChannelMarkerWrapper<T> marker)
        {
            return ChannelManager.GetChannel<T>(marker);
        }

        /// <summary>
        /// Gets a write channel from a marker interface.
        /// </summary>
        /// <returns>The requested channel.</returns>
        /// <param name="channel">The marker interface, or a real channel instance.</param>
        /// <typeparam name="T">The channel type.</typeparam>
        public static IWriteChannelEnd<T> Get<T>(IWriteChannel<T> channel)
        {
            return ChannelManager.GetChannel<T>(channel);
        }

        /// <summary>
        /// Gets a read channel from a marker interface.
        /// </summary>
        /// <returns>The requested channel.</returns>
        /// <param name="channel">The marker interface, or a real channel instance.</param>
        /// <typeparam name="T">The channel type.</typeparam>
        public static IReadChannelEnd<T> Get<T>(IReadChannel<T> channel)
        {
            return ChannelManager.GetChannel<T>(channel);
        }

        /// <summary>
        /// Creates a channel, possibly unnamed.
        /// If a channel name is provided, the channel is created in the supplied scope.
        /// If a channel with the given name is already found in the supplied scope, the named channel is returned.
        /// </summary>
        /// <returns>The channel.</returns>
        /// <param name="name">The name of the channel, or null.</param>
        /// <param name="buffersize">The number of buffers in the channel.</param>
        /// <param name="scope">The scope to create a named channel in, defaults to null which means the current scope</param>
        /// <param name="maxPendingReaders">The maximum number of pending readers. A negative value indicates infinite</param>
        /// <param name="maxPendingWriters">The maximum number of pending writers. A negative value indicates infinite</param>
        /// <param name="pendingReadersOverflowStrategy">The strategy for dealing with overflow for read requests</param>
        /// <param name="pendingWritersOverflowStrategy">The strategy for dealing with overflow for write requests</param>
        /// <param name="broadcast"><c>True</c> will create the channel as a broadcast channel, the default <c>false</c> will create a normal channel</param>
        /// <param name="initialBroadcastBarrier">The number of readers required on the channel before sending the first broadcast, can only be used with broadcast channels</param>
        /// <param name="broadcastMinimum">The minimum number of readers required on the channel, before a broadcast can be performed, can only be used with broadcast channels</param>
        /// <typeparam name="T">The channel type.</typeparam>
        public static IChannel<T> Create<T>(string name = null, int buffersize = 0, ChannelScope scope = null, int maxPendingReaders = -1, int maxPendingWriters = -1, QueueOverflowStrategy pendingReadersOverflowStrategy = QueueOverflowStrategy.Reject, QueueOverflowStrategy pendingWritersOverflowStrategy = QueueOverflowStrategy.Reject, bool broadcast = false, int initialBroadcastBarrier = -1, int broadcastMinimum = -1)
        {
            return ChannelManager.CreateChannel<T>(name, buffersize, scope, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy, broadcast, initialBroadcastBarrier, broadcastMinimum);
        }

        /// <summary>
        /// Creates a channel, possibly unnamed.
        /// If a channel name is provided, the channel is created in the supplied scope.
        /// If a channel with the given name is already found in the supplied scope, the named channel is returned.
        /// </summary>
        /// <returns>The named channel.</returns>
        /// <param name="attr">The attribute describing the channel.</param>
        /// <param name="scope">The scope to create a named channel in, defaults to null which means the current scope</param>
        /// <typeparam name="T">The channel type.</typeparam>
        public static IChannel<T> Create<T>(ChannelNameAttribute attr, ChannelScope scope = null)
        {
            return ChannelManager.CreateChannel<T>(attr, scope);
        }
    }

	/// <summary>
	/// A channel that uses continuation callbacks
	/// </summary>
	public class Channel<T> : IChannel<T>, IUntypedChannel, IJoinAbleChannel, INamedItem
	{
		/// <summary>
		/// The minium value for the cleanup threshold
		/// </summary>
		protected const int MIN_QUEUE_CLEANUP_THRESHOLD = 100;

		/// <summary>
		/// Interface for an offer
		/// </summary>
		protected interface IEntry
		{
			/// <summary>
			/// The two-phase offer instance
			/// </summary>
			/// <value>The offer.</value>
			ITwoPhaseOffer Offer { get; }
		}

		/// <summary>
		/// Structure for keeping a read request
		/// </summary>
		protected struct ReaderEntry : IEntry, IEquatable<ReaderEntry>
		{
			/// <summary>
			/// The offer handler for the request
			/// </summary>
			public readonly ITwoPhaseOffer Offer;
			/// <summary>
			/// The callback method for reporting progress
			/// </summary>
			public readonly TaskCompletionSource<T> Source;
#if !NO_TASK_ASYNCCONTINUE
			/// <summary>
			/// A flag indicating if signalling task completion must be enqued on the task pool
			/// </summary>
			public readonly bool EnqueueContinuation;
#endif
			/// <summary>
			/// Initializes a new instance of the <see cref="CoCoL.Channel&lt;T&gt;.ReaderEntry"/> struct.
			/// </summary>
			/// <param name="offer">The offer handler</param>
			public ReaderEntry(ITwoPhaseOffer offer)
			{
				Offer = offer;
#if NO_TASK_ASYNCCONTINUE
                Source = new TaskCompletionSource<T>();
#else
				EnqueueContinuation = ExecutionScope.Current.IsLimitingPool;
				Source = new TaskCompletionSource<T>(
					EnqueueContinuation
					? TaskCreationOptions.None
					: TaskCreationOptions.RunContinuationsAsynchronously);
#endif
			}

			/// <summary>
			/// The offer handler for the request
			/// </summary>
			ITwoPhaseOffer IEntry.Offer { get { return Offer; } }

            /// <summary>
            /// Gets a value indicating whether this <see cref="T:CoCoL.Channel`1.ReaderEntry"/> is timed out.
            /// </summary>
            public bool IsTimeout => Offer is IExpiringOffer && ((IExpiringOffer)Offer).Expires < DateTime.Now;

            /// <summary>
            /// Gets a value indicating whether this <see cref="T:CoCoL.Channel`1.ReaderEntry"/> is cancelled.
            /// </summary>
            public bool IsCancelled => Offer is ICancelAbleOffer && ((ICancelAbleOffer)Offer).CancelToken.IsCancellationRequested;

            /// <summary>
            /// Gets a value representing the expiration time of this entry
            /// </summary>
            public DateTime Expires => Offer is IExpiringOffer ? ((IExpiringOffer)Offer).Expires : new DateTime(0);

            /// <summary>
            /// Signals that the probe phase has finished
            /// </summary>
            public void ProbeCompleted()
            {
                if (Offer is IExpiringOffer offer)
                    offer.ProbeComplete();                    
            }

            /// <summary>
            /// Explict disable of compares
            /// </summary>
            /// <param name="other">The item to compare with</param>
            /// <returns>Always throws an exception to avoid compares</returns>
            public bool Equals(ReaderEntry other)
            {
                throw new NotImplementedException();
            }
        }

		/// <summary>
		/// Structure for keeping a write request
		/// </summary>
		protected struct WriterEntry : IEntry, IEquatable<WriterEntry>
		{
			/// <summary>
			/// The offer handler for the request
			/// </summary>
			public readonly ITwoPhaseOffer Offer;
			/// <summary>
			/// The callback method for reporting progress
			/// </summary>
			public readonly TaskCompletionSource<bool> Source;
            /// <summary>
            /// The cancellation token
            /// </summary>
            public readonly CancellationToken CancelToken;
			/// <summary>
			/// The value being written
			/// </summary>
			public readonly T Value;

#if !NO_TASK_ASYNCCONTINUE
			/// <summary>
			/// A flag indicating if signalling task completion must be enqued on the task pool
			/// </summary>
			public readonly bool EnqueueContinuation;
#endif

			/// <summary>
			/// Initializes a new instance of the <see cref="CoCoL.Channel&lt;T&gt;.WriterEntry"/> struct.
			/// </summary>
			/// <param name="offer">The offer handler</param>
			/// <param name="value">The value being written.</param>
			public WriterEntry(ITwoPhaseOffer offer, T value)
			{
				Offer = offer;
#if NO_TASK_ASYNCCONTINUE
                Source = new TaskCompletionSource<bool>();
#else
				EnqueueContinuation = ExecutionScope.Current.IsLimitingPool;
				Source = new TaskCompletionSource<bool>(
                    EnqueueContinuation
                    ? TaskCreationOptions.None
                    : TaskCreationOptions.RunContinuationsAsynchronously);
#endif
                Value = value;
			}

            /// <summary>
            /// Initializes a new empty instance of the <see cref="CoCoL.Channel&lt;T&gt;.WriterEntry"/> struct.
            /// </summary>
            /// <param name="value">The value being written.</param>
            public WriterEntry(T value)
            {
                Offer = null;
                Source = null;
                Value = value;
#if !NO_TASK_ASYNCCONTINUE
				EnqueueContinuation = false;
#endif

			}

            /// <summary>
            /// The offer handler for the request
            /// </summary>
            ITwoPhaseOffer IEntry.Offer { get { return Offer; } }

			/// <summary>
			/// Gets a value indicating whether this <see cref="T:CoCoL.Channel`1.ReaderEntry"/> is timed out.
			/// </summary>
			public bool IsTimeout => Offer is IExpiringOffer && ((IExpiringOffer)Offer).IsExpired;

            /// <summary>
            /// Gets a value indicating whether this <see cref="T:CoCoL.Channel`1.ReaderEntry"/> is cancelled.
            /// </summary>
            public bool IsCancelled => Offer is ICancelAbleOffer && ((ICancelAbleOffer)Offer).CancelToken.IsCancellationRequested;

            /// <summary>
            /// Gets a value representing the expiration time of this entry
            /// </summary>
            public DateTime Expires => Offer is IExpiringOffer ? ((IExpiringOffer)Offer).Expires : new DateTime(0);

            /// <summary>
            /// Signals that the probe phase has finished
            /// </summary>
            public void ProbeCompleted()
            {
                if (Offer is IExpiringOffer offer)
                    offer.ProbeComplete();
            }
            /// <summary>
            /// Explict disable of compares
            /// </summary>
            /// <param name="other">The item to compare with</param>
            /// <returns>Always throws an exception to avoid compares</returns>
            public bool Equals(WriterEntry other)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// The queue with pending readers
        /// </summary>
        protected List<ReaderEntry> m_readerQueue = new List<ReaderEntry>(1);

		/// <summary>
		/// The queue with pending writers
		/// </summary>
		protected List<WriterEntry> m_writerQueue = new List<WriterEntry>(1);

		/// <summary>
		/// The maximal size of the queue
		/// </summary>
		protected readonly int m_bufferSize;

		/// <summary>
		/// The lock object protecting access to the queues
		/// </summary>
		protected readonly AsyncLock m_asynclock = new AsyncLock();

		/// <summary>
		/// Gets or sets the name of the channel
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this instance is retired.
		/// </summary>
		/// <value><c>true</c> if this instance is retired; otherwise, <c>false</c>.</value>
		public Task<bool> IsRetiredAsync { get { return GetIsRetiredAsync(); } }

		/// <summary>
		/// Gets a value indicating whether this instance is retired.
		/// </summary>
		protected bool m_isRetired;

		/// <summary>
		/// The number of messages to process before marking the channel as retired
		/// </summary>
		protected int m_retireCount = -1;

		/// <summary>
		/// The number of reader processes having joined the channel
		/// </summary>
		protected int m_joinedReaderCount = 0;

		/// <summary>
		/// The number of writer processes having joined the channel
		/// </summary>
		protected int m_joinedWriterCount = 0;

		/// <summary>
		/// The threshold for performing writer queue cleanup
		/// </summary>
		protected int m_writerQueueCleanup = MIN_QUEUE_CLEANUP_THRESHOLD;

		/// <summary>
		/// The threshold for performing reader queue cleanup
		/// </summary>
		protected int m_readerQueueCleanup = MIN_QUEUE_CLEANUP_THRESHOLD;

		/// <summary>
		/// The maximum number of pending readers to allow
		/// </summary>
		protected readonly int m_maxPendingReaders;

		/// <summary>
		/// The strategy for selecting pending readers to discard on overflow
		/// </summary>
		protected readonly QueueOverflowStrategy m_pendingReadersOverflowStrategy;

		/// <summary>
		/// The maximum number of pending writers to allow
		/// </summary>
		protected readonly int m_maxPendingWriters;

		/// <summary>
		/// The strategy for selecting pending writers to discard on overflow
		/// </summary>
		protected readonly QueueOverflowStrategy m_pendingWritersOverflowStrategy;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Channel&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="attribute">The attribute describing the channel</param>
		internal Channel(ChannelNameAttribute attribute)
		{
			if (attribute == null)
				throw new ArgumentNullException(nameof(attribute));
			if (attribute.BufferSize < 0)
				throw new ArgumentOutOfRangeException(nameof(attribute), "The size parameter must be greater than or equal to zero");

			this.Name = attribute.Name;

			m_bufferSize = attribute.BufferSize;
			m_maxPendingReaders = attribute.MaxPendingReaders;
			m_maxPendingWriters = attribute.MaxPendingWriters;
			m_pendingReadersOverflowStrategy = attribute.PendingReadersOverflowStrategy;
			m_pendingWritersOverflowStrategy = attribute.PendingWritersOverflowStrategy;
		}

		/// <summary>
		/// Helper method for accessor to get the retired state
		/// </summary>
		/// <returns>The is retired async.</returns>
		protected async Task<bool> GetIsRetiredAsync()
		{
			using (await m_asynclock.LockAsync())
				return m_isRetired;
		}

		/// <summary>
		/// Offers a transaction to the write end
		/// </summary>
		/// <param name="wr">The writer entry.</param>
		protected async Task<bool> Offer(WriterEntry wr)
		{
			Exception tex = null;
			bool accept = false;

			System.Diagnostics.Debug.Assert(wr.Source == m_writerQueue[0].Source);

			try
			{
				accept =
                    (wr.Source == null || wr.Source.Task.Status == TaskStatus.WaitingForActivation)
                    &&
                    (wr.Offer == null || await wr.Offer.OfferAsync(this).ConfigureAwait(false));
			}
			catch (Exception ex)
			{
				tex = ex; // Workaround to support C# 5.0, with no await in catch clause
			}

			if (tex != null)
			{
				TrySetException(wr, tex);
				m_writerQueue.RemoveAt(0);

				return false;
			}

			if (!accept)
			{
				TrySetCancelled(wr);
				m_writerQueue.RemoveAt(0);

				return false;
			}

			return true;
		}

		/// <summary>
		/// Offersa transaction to the read end
		/// </summary>
		/// <param name="rd">The reader entry.</param>
		protected async Task<bool> Offer(ReaderEntry rd)
		{
			Exception tex = null;
			bool accept = false;

			System.Diagnostics.Debug.Assert(rd.Source == m_readerQueue[0].Source);

			try
			{
				accept =
                    (rd.Source == null || rd.Source.Task.Status == TaskStatus.WaitingForActivation)
                    &&
                    (rd.Offer == null || await rd.Offer.OfferAsync(this).ConfigureAwait(false));
			}
			catch (Exception ex)
			{
				tex = ex; // Workaround to support C# 5.0, with no await in catch clause
			}

			if (tex != null)
			{
				TrySetException(rd, tex);
				m_readerQueue.RemoveAt(0);

				return false;
			}

			if (!accept)
			{
				TrySetCancelled(rd);
				m_readerQueue.RemoveAt(0);

				return false;
			}

			return true;
		}

		/// <summary>
		/// Method that examines the queues and matches readers with writers
		/// </summary>
		/// <returns>An awaitable that signals if the caller has been accepted or rejected.</returns>
		/// <param name="asReader"><c>True</c> if the caller method is a reader, <c>false</c> otherwise.</param>
		/// <param name="caller">The caller task.</param>
		protected virtual async Task<bool> MatchReadersAndWriters(bool asReader, Task caller)
		{
			var processed = false;

			while (m_writerQueue != null && m_readerQueue != null && m_writerQueue.Count > 0 && m_readerQueue.Count > 0)
			{
				var wr = m_writerQueue[0];
				var rd = m_readerQueue[0];

				bool offerWriter;
				bool offerReader;

				// If the caller is a reader, we assume that the 
				// read call will always proceed, and start emptying
				// the write queue, and vice versa if the caller
				// is a writer
				if (asReader)
				{
                    offerWriter = await Offer(wr).ConfigureAwait(false);
                    if (!offerWriter)
						continue;
					
					offerReader = await Offer(rd).ConfigureAwait(false);
				}
				else
				{
                    offerReader = await Offer(rd).ConfigureAwait(false);
                    if (!offerReader)
						continue;
					
					offerWriter = await Offer(wr).ConfigureAwait(false);
				}

				// We flip the first entry, so we do not repeatedly
				// offer the side that agrees, and then discover
				// that the other side denies
				asReader = !asReader;

				// If the ends disagree, the declining end
				// has been removed from the queue, so we just
				// withdraw the offer from the other end
				if (!(offerReader && offerWriter))
				{
					if (wr.Offer != null && offerWriter)
						await wr.Offer.WithdrawAsync(this).ConfigureAwait(false);

					if (rd.Offer != null && offerReader)
						await rd.Offer.WithdrawAsync(this).ConfigureAwait(false);
				}
				else
				{
					// transaction complete
					m_writerQueue.RemoveAt(0);
					m_readerQueue.RemoveAt(0);

					if (wr.Offer != null)
						await wr.Offer.CommitAsync(this).ConfigureAwait(false);
					if (rd.Offer != null)
						await rd.Offer.CommitAsync(this).ConfigureAwait(false);

					if (caller == rd.Source.Task || (wr.Source != null && caller == wr.Source.Task))
						processed = true;

                    SetResult(rd, wr.Value);
                    SetResult(wr, true);

                    // Release items if there is space in the buffer
                    await ProcessWriteQueueBufferAfterReadAsync(true).ConfigureAwait(false);

					// Adjust the cleanup threshold
					if (m_writerQueue.Count <= m_writerQueueCleanup - MIN_QUEUE_CLEANUP_THRESHOLD)
						m_writerQueueCleanup = Math.Max(MIN_QUEUE_CLEANUP_THRESHOLD, m_writerQueue.Count + MIN_QUEUE_CLEANUP_THRESHOLD);

					// Adjust the cleanup threshold
					if (m_readerQueue.Count <= m_readerQueueCleanup - MIN_QUEUE_CLEANUP_THRESHOLD)
						m_readerQueueCleanup = Math.Max(MIN_QUEUE_CLEANUP_THRESHOLD, m_readerQueue.Count + MIN_QUEUE_CLEANUP_THRESHOLD);

					// If this was the last item before the retirement, 
					// flush all following and set the retired flag
					await EmptyQueueIfRetiredAsync(true).ConfigureAwait(false);
				}
			}

			return processed || caller.Status != TaskStatus.WaitingForActivation;
		}

        /// <summary>
        /// Registers a desire to read from the channel
        /// </summary>
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
            var rd = new ReaderEntry(offer);
            if (rd.IsCancelled)
                throw new TaskCanceledException();

			using (await m_asynclock.LockAsync())
			{
				if (m_isRetired)
				{
					TrySetException(rd, new RetiredException(this.Name));
                    return await rd.Source.Task.ConfigureAwait(false);
                }

				m_readerQueue.Add(rd);
				if (!await MatchReadersAndWriters(true, rd.Source.Task).ConfigureAwait(false))
				{
                    rd.ProbeCompleted();
					System.Diagnostics.Debug.Assert(m_readerQueue[m_readerQueue.Count - 1].Source == rd.Source);

                    // If this was a probe call, return a timeout now
                    if (rd.IsTimeout)
                    {
                        m_readerQueue.RemoveAt(m_readerQueue.Count - 1);
                        TrySetException(rd, new TimeoutException());
                    }
                    else if (rd.IsCancelled)
                    {
                        m_readerQueue.RemoveAt(m_readerQueue.Count - 1);
                        TrySetException(rd, new TaskCanceledException());
                    }
                    else
					{
						// Make room if we have too many
						if (m_maxPendingReaders > 0 && (m_readerQueue.Count - 1) >= m_maxPendingReaders)
						{
							switch (m_pendingReadersOverflowStrategy)
							{
							    case QueueOverflowStrategy.FIFO:
								{
									var exp = m_readerQueue[0];
								    m_readerQueue.RemoveAt(0);
                                    TrySetException(exp, new ChannelOverflowException(this.Name));
                                }
                                break;

							    case QueueOverflowStrategy.LIFO:
								{
									var exp = m_readerQueue[m_readerQueue.Count - 2];
									m_readerQueue.RemoveAt(m_readerQueue.Count - 2);
                                    TrySetException(exp, new ChannelOverflowException(this.Name));
                                }

                                break;

							    case QueueOverflowStrategy.Reject:
							    default:
								{
									var exp = m_readerQueue[m_readerQueue.Count - 1];
									m_readerQueue.RemoveAt(m_readerQueue.Count - 1);
                                    TrySetException(exp, new ChannelOverflowException(this.Name));
								}

                                break;
							}
						}

						// If we have expanded the queue with a new batch, see if we can purge old entries
						m_readerQueueCleanup = await PerformQueueCleanupAsync(m_readerQueue, true, m_readerQueueCleanup).ConfigureAwait(false);

                        if (rd.Offer is IExpiringOffer && ((IExpiringOffer)rd.Offer).Expires != Timeout.InfiniteDateTime)
                            ExpirationManager.AddExpirationCallback(((IExpiringOffer)rd.Offer).Expires, () => ExpireItemsAsync().FireAndForget());
					}
				}
			}

			return await rd.Source.Task.ConfigureAwait(false);
		}

        /// <summary>
        /// Registers a desire to write to the channel
        /// </summary>
        /// <param name="value">The value to write to the channel.</param>
        public Task WriteAsync(T value)
        {
            return WriteAsync(value, null);
        }

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="value">The value to write to the channel.</param>
        public async Task WriteAsync(T value, ITwoPhaseOffer offer)
		{
			var wr = new WriterEntry(offer, value);
            if (wr.IsCancelled)
                throw new TaskCanceledException();

			using (await m_asynclock.LockAsync())
			{
				if (m_isRetired)
				{
                    TrySetException(wr, new RetiredException(this.Name));
                    await wr.Source.Task.ConfigureAwait(false);
					return;
				}

				m_writerQueue.Add(wr);
				if (!await MatchReadersAndWriters(false, wr.Source.Task).ConfigureAwait(false))
				{
					System.Diagnostics.Debug.Assert(m_writerQueue[m_writerQueue.Count - 1].Source == wr.Source);

					// If we have a buffer slot to use
					if (m_writerQueue.Count <= m_bufferSize && m_retireCount < 0)
					{
						if (offer == null || await offer.OfferAsync(this))
						{
							if (offer != null)
								await offer.CommitAsync(this).ConfigureAwait(false);

                            m_writerQueue[m_writerQueue.Count - 1] = new WriterEntry(value);
							TrySetResult(wr, true);
						}
						else
						{
							TrySetCancelled(wr);
						}

                        // For good measure, we also make sure the probe phase is completed
                        wr.ProbeCompleted();
                    }
                    else
					{
                        wr.ProbeCompleted();

                        // If this was a probe call, return a timeout now
                        if (wr.IsTimeout)
                        {
                            m_writerQueue.RemoveAt(m_writerQueue.Count - 1);
                            TrySetException(wr, new TimeoutException());
                        }
                        else if (wr.IsCancelled)
                        {
                            m_writerQueue.RemoveAt(m_writerQueue.Count - 1);
                            TrySetException(wr, new TaskCanceledException());
                        }
                        else
						{
							// Make room if we have too many
							if (m_maxPendingWriters > 0 && (m_writerQueue.Count - m_bufferSize - 1) >= m_maxPendingWriters)
							{
								switch (m_pendingWritersOverflowStrategy)
								{
									case QueueOverflowStrategy.FIFO:
										{
											var exp = m_writerQueue[m_bufferSize];
											m_writerQueue.RemoveAt(m_bufferSize);
                                            TrySetException(exp, new ChannelOverflowException(this.Name));
                                        }

                                        break;
									case QueueOverflowStrategy.LIFO:
										{
											var exp = m_writerQueue[m_writerQueue.Count - 2];
											m_writerQueue.RemoveAt(m_writerQueue.Count - 2);
                                            TrySetException(exp, new ChannelOverflowException(this.Name));
                                        }

                                        break;
									case QueueOverflowStrategy.Reject:
									default:
										{
											var exp = m_writerQueue[m_writerQueue.Count - 1];
											m_writerQueue.RemoveAt(m_writerQueue.Count - 1);
                                            TrySetException(exp, new ChannelOverflowException(this.Name));
										}

										return;
								}
							}

							// If we have expanded the queue with a new batch, see if we can purge old entries
							m_writerQueueCleanup = await PerformQueueCleanupAsync(m_writerQueue, true, m_writerQueueCleanup).ConfigureAwait(false);

                            if (wr.Offer is IExpiringOffer && ((IExpiringOffer)wr.Offer).Expires != Timeout.InfiniteDateTime)
                                ExpirationManager.AddExpirationCallback(((IExpiringOffer)wr.Offer).Expires, () => ExpireItemsAsync().FireAndForget());
						}
					}
				}
			}

			await wr.Source.Task;
			return;
		}

		/// <summary>
		/// Purges items in the queue that are no longer active
		/// </summary>
		/// <param name="queue">The queue to remove from.</param>
		/// <param name="queueCleanup">The threshold parameter.</param>
		/// <param name="isLocked"><c>True</c> if we are already holding the lock, <c>false</c> otherwise</param>
		/// <typeparam name="Tx">The type of list data.</typeparam>
		private async Task<int> PerformQueueCleanupAsync<Tx>(List<Tx> queue, bool isLocked, int queueCleanup)
			where Tx : IEntry
		{
            var res = queueCleanup;
			using(isLocked ? default(AsyncLock.Releaser) : await m_asynclock.LockAsync())
			{
				if (queue.Count > queueCleanup)
				{
					for (var i = queue.Count - 1; i >= 0; i--)
					{
						if (queue[i].Offer != null)
						if (await queue[i].Offer.OfferAsync(this).ConfigureAwait(false))
							await queue[i].Offer.WithdrawAsync(this).ConfigureAwait(false);
						else
						{
							TrySetCancelled(queue[i]);
							queue.RemoveAt(i);
						}
					}

					// Prevent repeated cleanup requests
					res = Math.Max(MIN_QUEUE_CLEANUP_THRESHOLD, queue.Count + MIN_QUEUE_CLEANUP_THRESHOLD);
				}
			}

			return res;
		}
			
		/// <summary>
		/// Helper method for dequeueing write requests after space has been allocated in the writer queue
		/// </summary>
		/// <param name="isLocked"><c>True</c> if we are already holding the lock, <c>false</c> otherwise</param>
		private async Task ProcessWriteQueueBufferAfterReadAsync(bool isLocked)
		{
			using(isLocked ? default(AsyncLock.Releaser) : await m_asynclock.LockAsync())
			{
				// If there is now a buffer slot in the queue, trigger a callback to a waiting item
				while (m_retireCount < 0 && m_bufferSize > 0 && m_writerQueue.Count >= m_bufferSize)
				{
					var nextItem = m_writerQueue[m_bufferSize - 1];

					if (nextItem.Offer == null || await nextItem.Offer.OfferAsync(this).ConfigureAwait(false))
					{
						if (nextItem.Offer != null)
							await nextItem.Offer.CommitAsync(this).ConfigureAwait(false);

						SetResult(nextItem, true);

						// Now that the transaction has completed for the writer, record it as waiting forever
                        m_writerQueue[m_bufferSize - 1] = new WriterEntry(nextItem.Value);

						// We can have at most one, since we process at most one read
						break;
					}
					else
						m_writerQueue.RemoveAt(m_bufferSize - 1);
				}
			}
		}

		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		public Task RetireAsync()
		{
			return RetireAsync(false, false);
		}

		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		public Task RetireAsync(bool immediate)
		{
			return RetireAsync(immediate, false);
		}

		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		/// <param name="isLocked"><c>True</c> if we are already holding the lock, <c>false</c> otherwise</param>
		private async Task RetireAsync(bool immediate, bool isLocked)
		{
			using (isLocked ? default(AsyncLock.Releaser) : await m_asynclock.LockAsync())
			{
				if (m_isRetired)
					return;

				if (m_retireCount < 0)
				{
					// If we have responded to buffered writes, 
					// make sure we pair those before retiring
					m_retireCount = Math.Min(m_writerQueue.Count, m_bufferSize) + 1;

					// For immediate retire, remove buffered writes
					if (immediate)
						while (m_retireCount > 1)
						{
							if (m_writerQueue[0].Source != null)
                            TrySetException(m_writerQueue[0], new RetiredException(this.Name));
							m_writerQueue.RemoveAt(0);
							m_retireCount--;
						}
				}
				
				await EmptyQueueIfRetiredAsync(true).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Join the channel
		/// </summary>
		/// <param name="asReader"><c>true</c> if joining as a reader, <c>false</c> otherwise</param>
		public virtual async Task JoinAsync(bool asReader)
		{
			using (await m_asynclock.LockAsync())
			{
				// Do not allow anyone to join after we retire the channel
				if (m_isRetired)
                    throw new RetiredException(this.Name);

				if (asReader)
					m_joinedReaderCount++;
				else
					m_joinedWriterCount++;
			}
		}

		/// <summary>
		/// Leave the channel.
		/// </summary>
		/// <param name="asReader"><c>true</c> if leaving as a reader, <c>false</c> otherwise</param>
		public virtual async Task LeaveAsync(bool asReader)
		{
			using (await m_asynclock.LockAsync())
			{
				// If we are already retired, skip this call
				if (m_isRetired)
					return;

				// Countdown
				if (asReader)
					m_joinedReaderCount--;
				else
					m_joinedWriterCount--;

				// Retire if required
				if ((asReader && m_joinedReaderCount <= 0) || (!asReader && m_joinedWriterCount <= 0))
					await RetireAsync(false, true).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Empties the queue if the channel is retired.
		/// </summary>
		/// <param name="isLocked"><c>True</c> if we are already holding the lock, <c>false</c> otherwise</param>
		private async Task EmptyQueueIfRetiredAsync(bool isLocked)
		{
			List<ReaderEntry> readers = null;
			List<WriterEntry> writers = null;

			using (isLocked ? default(AsyncLock.Releaser) : await m_asynclock.LockAsync())
			{
				// Countdown as required
				if (m_retireCount > 0)
				{
					m_retireCount--;
					if (m_retireCount == 0)
					{
						// Empty the queues, as we are now retired
						readers = m_readerQueue;
						writers = m_writerQueue;

						// Make sure nothing new enters the queues
						m_isRetired = true;
						m_readerQueue = null;
						m_writerQueue = null;

					}
				}
			}

			// If there are pending retire messages, send them
			if (readers != null || writers != null)
			{
                if (readers != null)
                    foreach (var r in readers)
                        TrySetException(r, new RetiredException(this.Name));

                if (writers != null)
                    foreach (var w in writers)
                        TrySetException(w, new RetiredException(this.Name));
            }
        }


		/// <summary>
		/// Callback method used to signal timeout on expired items
		/// </summary>
		private async Task ExpireItemsAsync()
		{
			KeyValuePair<int, ReaderEntry>[] expiredReaders;
			KeyValuePair<int, WriterEntry>[] expiredWriters;

			// Extract all expired items from their queues
			using (await m_asynclock.LockAsync())
			{
				// If the channel is retired, there is nothing to do here
				if (m_readerQueue == null || m_writerQueue == null)
					return;
				
				var now = DateTime.Now;
				expiredReaders = m_readerQueue.Zip(Enumerable.Range(0, m_readerQueue.Count), (n, i) => new KeyValuePair<int, ReaderEntry>(i, n)).Where(x => x.Value.Expires.Ticks != 0 && (x.Value.Expires - now).Ticks <= ExpirationManager.ALLOWED_ADVANCE_EXPIRE_TICKS).ToArray();
				expiredWriters = m_writerQueue.Zip(Enumerable.Range(0, m_writerQueue.Count), (n, i) => new KeyValuePair<int, WriterEntry>(i, n)).Where(x => x.Value.Expires.Ticks != 0 && (x.Value.Expires - now).Ticks <= ExpirationManager.ALLOWED_ADVANCE_EXPIRE_TICKS).ToArray();

				foreach (var r in expiredReaders.OrderByDescending(x => x.Key))
					m_readerQueue.RemoveAt(r.Key);
				foreach (var r in expiredWriters.OrderByDescending(x => x.Key))
					m_writerQueue.RemoveAt(r.Key);
			}

			// Send the notifications
			foreach (var r in expiredReaders.OrderBy(x => x.Value.Expires))
				TrySetException(r.Value, new TimeoutException());

			// Send the notifications
			foreach (var w in expiredWriters.OrderBy(x => x.Value.Expires))
				TrySetException(w.Value, new TimeoutException());
		}

        #region Task continuation support methods
        /// <summary>
        /// Sets the task to be failed
        /// </summary>
        /// <param name="entry">The task to set</param>
        /// <param name="ex">The exception to set</param>
        private static void TrySetException(ReaderEntry entry, Exception ex)
		{
#if NO_TASK_ASYNCCONTINUE
			ThreadPool.QueueItem(() => entry.Source.TrySetException(ex));
#else
			if (entry.EnqueueContinuation)
				ThreadPool.QueueItem(() => entry.Source.TrySetException(ex));
			else
				entry.Source.TrySetException(ex);
#endif
        }

        /// <summary>
        /// Sets the task to be failed
        /// </summary>
        /// <param name="entry">The task to set</param>
        /// <param name="ex">The exception to set</param>
        private static void TrySetException(WriterEntry entry, Exception ex)
		{
			if (entry.Source != null)
			{
#if NO_TASK_ASYNCCONTINUE
			    ThreadPool.QueueItem(() => entry.Source.TrySetException(ex));
#else
				if (entry.EnqueueContinuation)
					ThreadPool.QueueItem(() => entry.Source.TrySetException(ex));
				else
					entry.Source.TrySetException(ex);
#endif
            }
        }

		/// <summary>
		/// Tries to set the source to Cancelled
		/// </summary>
        /// <param name="entry">The entry to signal</param>
		private static void TrySetCancelled(IEntry entry)
		{
			if (entry is ReaderEntry re)
				TrySetCancelled(re);
			else if (entry is WriterEntry we)
				TrySetCancelled(we);
			else
				throw new InvalidOperationException("No such type");
		}

        /// <summary>
        /// Tries to set the source to Cancelled
        /// </summary>
        /// <param name="entry">The entry to signal</param>
        private static void TrySetCancelled(ReaderEntry entry)
		{
#if NO_TASK_ASYNCCONTINUE
            ThreadPool.QueueItem(() => entry.Source.TrySetCanceled());
#else
			if (entry.EnqueueContinuation)
				ThreadPool.QueueItem(() => entry.Source.TrySetCanceled());
			else
				entry.Source.TrySetCanceled();
#endif
		}

        /// <summary>
        /// Tries to set the source to Cancelled
        /// </summary>
        /// <param name="entry">The entry to signal</param>
        private static void TrySetCancelled(WriterEntry entry)
		{
			if (entry.Source != null)
			{
#if NO_TASK_ASYNCCONTINUE
                    ThreadPool.QueueItem(() => entry.Source.TrySetCanceled());
#else
				if (entry.EnqueueContinuation)
					ThreadPool.QueueItem(() => entry.Source.TrySetCanceled());
				else
					entry.Source.TrySetCanceled();
#endif
            }
        }

        /// <summary>
        /// Tries to set the source result
        /// </summary>
        /// <param name="entry">The entry to signal</param>
        /// <param name="value">The value to signal</param>
        private static void TrySetResult(WriterEntry entry, bool value)
		{
			if (entry.Source != null)
			{
#if NO_TASK_ASYNCCONTINUE
                    ThreadPool.QueueItem(() => entry.Source.TrySetResult(value));
#else
				if (entry.EnqueueContinuation)
					ThreadPool.QueueItem(() => entry.Source.TrySetResult(value));
				else
					entry.Source.TrySetResult(value);
#endif
            }
        }

        /// <summary>
        /// Sets the source result
        /// </summary>
        /// <param name="entry">The entry to signal</param>
        /// <param name="value">The value to signal</param>
        private static void SetResult(WriterEntry entry, bool value)
        {
            if (entry.Source != null)
            {
#if NO_TASK_ASYNCCONTINUE
                ThreadPool.QueueItem(() => entry.Source.SetResult(value));
#else
				if (entry.EnqueueContinuation)
					ThreadPool.QueueItem(() => entry.Source.SetResult(value));
				else
					entry.Source.SetResult(value);
#endif
            }
        }

        /// <summary>
        /// Sets the source result
        /// </summary>
        /// <param name="entry">The entry to signal</param>
        /// <param name="value">The value to signal</param>
        private static void SetResult(ReaderEntry entry, T value)
        {
#if NO_TASK_ASYNCCONTINUE
            ThreadPool.QueueItem(() => entry.Source.SetResult(value));
#else
			if (entry.EnqueueContinuation)
				ThreadPool.QueueItem(() => entry.Source.SetResult(value));
			else
				entry.Source.SetResult(value);
#endif
        }
        #endregion
    }
}

