using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoCoL
{
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
			/// <summary>
			/// Convenience method for setting the offer cancelled
			/// </summary>
			void TrySetCancelled();
		}

		/// <summary>
		/// Structure for keeping a read request
		/// </summary>
		protected struct ReaderEntry : IEntry
		{
			/// <summary>
			/// The offer handler for the request
			/// </summary>
			public ITwoPhaseOffer Offer;
			/// <summary>
			/// The callback method for reporting progress
			/// </summary>
			public TaskCompletionSource<T> Source;
			/// <summary>
			/// The timeout value
			/// </summary>
			public DateTime Expires;

			/// <summary>
			/// Initializes a new instance of the <see cref="CoCoL.Channel&lt;T&gt;.ReaderEntry"/> struct.
			/// </summary>
			/// <param name="offer">The offer handler</param>
			/// <param name="callback">The callback method for reporting progress.</param>
			/// <param name="expires">The timeout value.</param>
			public ReaderEntry(ITwoPhaseOffer offer, TaskCompletionSource<T> callback, DateTime expires)
			{
				Offer = offer;
				Source = callback;
				Expires = expires;
			}

			/// <summary>
			/// The offer handler for the request
			/// </summary>
			ITwoPhaseOffer IEntry.Offer { get { return Offer; } }
			/// <summary>
			/// Tries to set the source to Cancelled
			/// </summary>
			void IEntry.TrySetCancelled() { Source.TrySetCanceled(); }
		}

		/// <summary>
		/// Structure for keeping a write request
		/// </summary>
		protected struct WriterEntry : IEntry
		{
			/// <summary>
			/// The offer handler for the request
			/// </summary>
			public ITwoPhaseOffer Offer;
			/// <summary>
			/// The callback method for reporting progress
			/// </summary>
			public TaskCompletionSource<bool> Source;
			/// <summary>
			/// The timeout value
			/// </summary>
			public DateTime Expires;
			/// <summary>
			/// The value being written
			/// </summary>
			public T Value;

			/// <summary>
			/// Initializes a new instance of the <see cref="CoCoL.Channel&lt;T&gt;.WriterEntry"/> struct.
			/// </summary>
			/// <param name="offer">The offer handler</param>
			/// <param name="callback">The callback method for reporting progress.</param>
			/// <param name="expires">The timeout value.</param>
			/// <param name="value">The value being written.</param>
			public WriterEntry(ITwoPhaseOffer offer, TaskCompletionSource<bool> callback, DateTime expires, T value)
			{
				Offer = offer;
				Source = callback;
				Expires = expires;
				Value = value;
			}

			/// <summary>
			/// The offer handler for the request
			/// </summary>
			ITwoPhaseOffer IEntry.Offer { get { return Offer; } }
			/// <summary>
			/// Tries to set the source to Cancelled
			/// </summary>
			void IEntry.TrySetCancelled() 
			{ 
				if (Source != null) 
					Source.TrySetCanceled(); 
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
				throw new ArgumentNullException("attribute");
			if (attribute.BufferSize < 0)
				throw new ArgumentOutOfRangeException("BufferSize", "The size parameter must be greater than or equal to zero");

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
				accept = (wr.Source == null || wr.Source.Task.Status == TaskStatus.WaitingForActivation) && (wr.Offer == null || await wr.Offer.OfferAsync(this));
			}
			catch (Exception ex)
			{
				tex = ex; // Workaround to support C# 5.0, with no await in catch clause
			}

			if (tex != null)
			{
				wr.Source.TrySetException(tex);
				m_writerQueue.RemoveAt(0);

				return false;
			}

			if (!accept)
			{
				if (wr.Source != null)
					wr.Source.TrySetCanceled();
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
				accept = (rd.Source == null || rd.Source.Task.Status == TaskStatus.WaitingForActivation) && (rd.Offer == null || await rd.Offer.OfferAsync(this));
			}
			catch (Exception ex)
			{
				tex = ex; // Workaround to support C# 5.0, with no await in catch clause
			}

			if (tex != null)
			{
				rd.Source.TrySetException(tex);
				m_readerQueue.RemoveAt(0);

				return false;
			}

			if (!accept)
			{
				if (rd.Source != null)
					rd.Source.TrySetCanceled();
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
					if (!(offerWriter = await Offer(wr)))
						continue;
					
					offerReader = await Offer(rd);
				}
				else
				{
					if (!(offerReader = await Offer(rd)))
						continue;
					
					offerWriter = await Offer(wr);
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
						await wr.Offer.WithdrawAsync(this);

					if (rd.Offer != null && offerReader)
						await rd.Offer.WithdrawAsync(this);
				}
				else
				{
					// transaction complete
					m_writerQueue.RemoveAt(0);
					m_readerQueue.RemoveAt(0);

					if (wr.Offer != null)
						await wr.Offer.CommitAsync(this);
					if (rd.Offer != null)
						await rd.Offer.CommitAsync(this);

					if (caller == rd.Source.Task || (wr.Source != null && caller == wr.Source.Task))
						processed = true;

					ThreadPool.QueueItem(() => rd.Source.SetResult(wr.Value));
					if (wr.Source != null)
						ThreadPool.QueueItem(() => wr.Source.SetResult(true));

					// Release items if there is space in the buffer
					await ProcessWriteQueueBufferAfterReadAsync(true);

					// Adjust the cleanup threshold
					if (m_writerQueue.Count <= m_writerQueueCleanup - MIN_QUEUE_CLEANUP_THRESHOLD)
						m_writerQueueCleanup = Math.Max(MIN_QUEUE_CLEANUP_THRESHOLD, m_writerQueue.Count + MIN_QUEUE_CLEANUP_THRESHOLD);

					// Adjust the cleanup threshold
					if (m_readerQueue.Count <= m_readerQueueCleanup - MIN_QUEUE_CLEANUP_THRESHOLD)
						m_readerQueueCleanup = Math.Max(MIN_QUEUE_CLEANUP_THRESHOLD, m_readerQueue.Count + MIN_QUEUE_CLEANUP_THRESHOLD);

					// If this was the last item before the retirement, 
					// flush all following and set the retired flag
					await EmptyQueueIfRetiredAsync(true);
				}
			}

			return processed || caller.Status != TaskStatus.WaitingForActivation;
		}

		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public async Task<T> ReadAsync(TimeSpan timeout, ITwoPhaseOffer offer = null)
		{				
			var rd = new ReaderEntry(offer, new TaskCompletionSource<T>(), timeout.Ticks <= 0 ? Timeout.InfiniteDateTime : DateTime.Now + timeout);

			using (await m_asynclock.LockAsync())
			{
				if (m_isRetired)
				{
					ThreadPool.QueueItem(() => rd.Source.SetException(new RetiredException()));
					return await rd.Source.Task;
				}

				m_readerQueue.Add(rd);
				if (!await MatchReadersAndWriters(true, rd.Source.Task))
				{
					System.Diagnostics.Debug.Assert(m_readerQueue[m_readerQueue.Count - 1].Source == rd.Source);

					// If this was a probe call, return a timeout now
					if (timeout.Ticks >= 0 && rd.Expires < DateTime.Now)
					{
						m_readerQueue.RemoveAt(m_readerQueue.Count - 1);
						ThreadPool.QueueItem(() => rd.Source.TrySetException(new TimeoutException()));
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
									var exp = m_readerQueue[0].Source;
									m_readerQueue.RemoveAt(0);
									ThreadPool.QueueItem(() => exp.TrySetException(new ChannelOverflowException()));
								}

								break;
							case QueueOverflowStrategy.LIFO:
								{
									var exp = m_readerQueue[m_readerQueue.Count - 2].Source;
									m_readerQueue.RemoveAt(m_readerQueue.Count - 2);
									ThreadPool.QueueItem(() => exp.TrySetException(new ChannelOverflowException()));
								}

								break;
							case QueueOverflowStrategy.Reject:
							default:
								{
									var exp = m_readerQueue[m_readerQueue.Count - 1].Source;
									m_readerQueue.RemoveAt(m_readerQueue.Count - 1);
									ThreadPool.QueueItem(() => exp.TrySetException(new ChannelOverflowException()));

									await rd.Source.Task;
								}

								return await rd.Source.Task;
							}
						}

						// If we have expanded the queue with a new batch, see if we can purge old entries
						m_readerQueueCleanup = await PerformQueueCleanupAsync(m_readerQueue, true, m_readerQueueCleanup);

						if (rd.Expires != Timeout.InfiniteDateTime)
							ExpirationManager.AddExpirationCallback(rd.Expires, () => ExpireItemsAsync().FireAndForget());
					}
				}
			}

			return await rd.Source.Task;
		}
			
		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public async Task WriteAsync(T value, TimeSpan timeout, ITwoPhaseOffer offer = null)
		{
			var wr = new WriterEntry(offer, new TaskCompletionSource<bool>(), timeout.Ticks <= 0 ? Timeout.InfiniteDateTime : DateTime.Now + timeout, value);

			using (await m_asynclock.LockAsync())
			{
				if (m_isRetired)
				{
					ThreadPool.QueueItem(() => wr.Source.SetException(new RetiredException()));
					await wr.Source.Task;
					return;
				}

				m_writerQueue.Add(wr);
				if (!await MatchReadersAndWriters(false, wr.Source.Task))
				{
					System.Diagnostics.Debug.Assert(m_writerQueue[m_writerQueue.Count - 1].Source == wr.Source);

					// If we have a buffer slot to use
					if (m_writerQueue.Count <= m_bufferSize && m_retireCount < 0)
					{
						if (offer == null || await offer.OfferAsync(this))
						{
							if (offer != null)
								await offer.CommitAsync(this);

							m_writerQueue[m_writerQueue.Count - 1] = new WriterEntry(null, null, Timeout.InfiniteDateTime, value);
							wr.Source.TrySetResult(true);
						}
						else
						{
							wr.Source.TrySetCanceled();
						}
					}
					else
					{
						// If this was a probe call, return a timeout now
						if (timeout.Ticks >= 0 && wr.Expires < DateTime.Now)
						{
							m_writerQueue.RemoveAt(m_writerQueue.Count - 1);
							ThreadPool.QueueItem(() => wr.Source.SetException(new TimeoutException()));
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
											var exp = m_writerQueue[m_bufferSize].Source;
											m_writerQueue.RemoveAt(m_bufferSize);
											if (exp != null)
												ThreadPool.QueueItem(() => exp.TrySetException(new ChannelOverflowException()));
										}

										break;
									case QueueOverflowStrategy.LIFO:
										{
											var exp = m_writerQueue[m_writerQueue.Count - 2].Source;
											m_writerQueue.RemoveAt(m_writerQueue.Count - 2);
											if (exp != null)
												ThreadPool.QueueItem(() => exp.TrySetException(new ChannelOverflowException()));
										}

										break;
									case QueueOverflowStrategy.Reject:
									default:
										{
											var exp = m_writerQueue[m_writerQueue.Count - 1].Source;
											m_writerQueue.RemoveAt(m_writerQueue.Count - 1);
											if (exp != null)
												ThreadPool.QueueItem(() => exp.TrySetException(new ChannelOverflowException()));
											await wr.Source.Task;
										}

										return;
								}
							}

							// If we have expanded the queue with a new batch, see if we can purge old entries
							m_writerQueueCleanup = await PerformQueueCleanupAsync(m_writerQueue, true, m_writerQueueCleanup);

							if (wr.Expires != Timeout.InfiniteDateTime)
								ExpirationManager.AddExpirationCallback(wr.Expires, () => ExpireItemsAsync());
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
			using(isLocked ? default(AsyncLock.Releaser) : await m_asynclock.LockAsync())
			{
				if (queue.Count > queueCleanup)
				{
					for (var i = queue.Count - 1; i >= 0; i--)
					{
						if (queue[i].Offer != null)
						if (await queue[i].Offer.OfferAsync(this))
							await queue[i].Offer.WithdrawAsync(this);
						else
						{
							queue[i].TrySetCancelled();
							queue.RemoveAt(i);
						}
					}

					// Prevent repeated cleanup requests
					queueCleanup = Math.Max(MIN_QUEUE_CLEANUP_THRESHOLD, queue.Count + MIN_QUEUE_CLEANUP_THRESHOLD);
				}
			}

			return queueCleanup;
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

					if (nextItem.Offer == null || await nextItem.Offer.OfferAsync(this))
					{
						if (nextItem.Offer != null)
							await nextItem.Offer.CommitAsync(this);

						if (nextItem.Source != null)
							nextItem.Source.SetResult(true);

						// Now that the transaction has completed for the writer, record it as waiting forever
						m_writerQueue[m_bufferSize - 1] = new WriterEntry(null, null, Timeout.InfiniteDateTime, nextItem.Value);

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
								m_writerQueue[0].Source.TrySetException(new RetiredException());
							m_writerQueue.RemoveAt(0);
							m_retireCount--;
						}
				}
				
				await EmptyQueueIfRetiredAsync(true);
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
					throw new RetiredException();

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
					await RetireAsync(false, true);
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
						ThreadPool.QueueItem(() => r.Source.TrySetException(new RetiredException()));

				if (writers != null)
					foreach (var w in writers)
						if (w.Source != null)
							ThreadPool.QueueItem(() => w.Source.TrySetException(new RetiredException()));
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
				r.Value.Source.TrySetException(new TimeoutException());

			// Send the notifications
			foreach (var w in expiredWriters.OrderBy(x => x.Value.Expires))
				if (w.Value.Source != null)
					w.Value.Source.TrySetException(new TimeoutException());
		}
	}
}

