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
	public class Channel<T> : IBlockingChannel<T>, IChannel<T>, IUntypedChannel, IJoinAbleChannel, INamedItem
	{
		/// <summary>
		/// Structure for keeping a read request
		/// </summary>
		private struct ReaderEntry
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
			/// Initializes a new instance of the <see cref="CoCoL.Channel`1+ReaderEntry"/> struct.
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
		}

		/// <summary>
		/// Structure for keeping a write request
		/// </summary>
		private struct WriterEntry
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
			/// Initializes a new instance of the <see cref="CoCoL.Channel`1+WriterEntry"/> struct.
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
		}

		/// <summary>
		/// The queue with pending readers
		/// </summary>
		private List<ReaderEntry> m_readerQueue = new List<ReaderEntry>(1);

		/// <summary>
		/// The queue with pending writers
		/// </summary>
		private List<WriterEntry> m_writerQueue = new List<WriterEntry>(1);

		/// <summary>
		/// The maximal size of the queue
		/// </summary>
		private readonly int m_bufferSize;

		/// <summary>
		/// The lock object protecting access to the queues
		/// </summary>
		private readonly object m_lock = new object();

		/// <summary>
		/// A cached instance of the timeout exception
		/// </summary>
		private static readonly Exception TimeoutException = new TimeoutException();

		/// <summary>
		/// A cached instance of the timeout exception
		/// </summary>
		private static readonly Exception RetiredException = new RetiredException();

		/// <summary>
		/// Gets or sets the name of the channel
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this instance is retired.
		/// </summary>
		/// <value><c>true</c> if this instance is retired; otherwise, <c>false</c>.</value>
		public bool IsRetired { get; private set; }

		/// <summary>
		/// The number of messages to process before marking the channel as retired
		/// </summary>
		private int m_retireCount = -1;

		/// <summary>
		/// The number of reader processes having joined the channel
		/// </summary>
		private int m_joinedReaderCount = 0;

		/// <summary>
		/// The number of writer processes having joined the channel
		/// </summary>
		private int m_joinedWriterCount = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Channel`1"/> class.
		/// </summary>
		/// <param name="size">The size of the write buffer</param>
		internal Channel(string name = null, int size = 0)
		{
			if (size < 0)
				throw new ArgumentOutOfRangeException("size", "The size parameter must be greater than or equal to zero");

			this.Name = name;

			m_bufferSize = size;
		}

		/// <summary>
		/// Read from the channel in a blocking manner
		/// </summary>
		public T Read()
		{
			var t = ReadAsync();
			return t .WaitForTask().Result;
		}

		/// <summary>
		/// Write to the channel in a blocking manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		public void Write(T value)
		{
			var v = WriteAsync(value);
			v.WaitForTask();
			if (v.Exception != null)
				throw v.Exception;
			else if (v.IsCanceled)
				throw new OperationCanceledException();
		}


		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		public Task<T> ReadAsync()
		{
			return ReadAsync(null, Timeout.Infinite);
		}

		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public Task<T> ReadAsync(TimeSpan timeout)
		{
			return ReadAsync(null, timeout);
		}

		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		public Task<T> ReadAsync(ITwoPhaseOffer offer)
		{
			return ReadAsync(offer, Timeout.Infinite);
		}

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="value">The value to write to the channel.</param>
		public Task WriteAsync(ITwoPhaseOffer offer, T value)
		{
			return WriteAsync(offer, value, Timeout.Infinite);
		}

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="value">The value to write to the channel.</param>
		public Task WriteAsync(T value)
		{
			return WriteAsync(null, value, Timeout.Infinite);
		}

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public Task WriteAsync(T value, TimeSpan timeout)
		{
			return WriteAsync(null, value, timeout);
		}

		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public Task<T> ReadAsync(ITwoPhaseOffer offer, TimeSpan timeout)
		{				
			// Store entry time in case we need it and the offer dance takes some time
			var entry = DateTime.Now;
			var result = new TaskCompletionSource<T>();

			lock (m_lock)
			{
				if (IsRetired)
				{
					ThreadPool.QueueItem(() => result.SetException(RetiredException));
					return result.Task;
				}

				while (m_writerQueue.Count > 0)
				{
					var kp = m_writerQueue[0];

					var offerWriter = kp.Offer == null;
					var offerReader = offer == null;

					if (!offerWriter)
						try
						{
							offerWriter = kp.Offer.Offer(this);
						}
						catch(Exception ex)
						{
							result.SetException(ex);
							return result.Task;
						}

					if (!offerReader)
						try 
						{						
							offerReader = offer.Offer(this); 
						}
						catch(Exception ex) 
						{ 
							if (offerWriter)
								kp.Offer.Withdraw(this);
							
							result.SetException(ex);
							return result.Task;
						}


					if (!(offerReader && offerWriter))
					{
						if (kp.Offer != null && offerWriter)
							kp.Offer.Withdraw(this);

						if (offer != null && offerReader)
							offer.Withdraw(this);

						// if the writer bailed, remove it from the queue
						if (!offerWriter)
							m_writerQueue.RemoveAt(0);

						// if the reader bailed, the queue is intact but we offer no more
						if (!offerReader)
						{
							result.SetCanceled();
							return result.Task;
						}
					}
					else
					{
						// transaction complete
						m_writerQueue.RemoveAt(0);

						if (kp.Offer != null)
							kp.Offer.Commit(this);
						if (offer != null)
							offer.Commit(this);
						
						ThreadPool.QueueItem(() => result.SetResult(kp.Value));
						ThreadPool.QueueItem(() => kp.Source.SetResult(true));

						// Release items if there is space in the buffer
						ProcessWriteQueueBufferAfterRead();

						// If this was the last item before the retirement, 
						// flush all following and set the retired flag
						EmptyQueueIfRetired();

						return result.Task;
					}
				}

				var expires = timeout.Ticks <= 0 ? Timeout.InfiniteDateTime : entry + timeout;

				// If this was a probe call, return a timeout now
				if (timeout.Ticks >= 0 && expires < DateTime.Now)
				{
					ThreadPool.QueueItem(() => result.SetException(TimeoutException));
				}
				else
				{
					// Register the pending reader
					m_readerQueue.Add(new ReaderEntry(offer, result, expires));

					if (expires != Timeout.InfiniteDateTime)
						ExpirationManager.AddExpirationCallback(expires, ExpireItems);
				}
			}

			return result.Task;
		}
			
		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public Task WriteAsync(ITwoPhaseOffer offer, T value, TimeSpan timeout)
		{
			// Store entry time in case we need it and the offer dance takes some time
			var entry = DateTime.Now;
			var result = new TaskCompletionSource<bool>();

			lock (m_lock)
			{
				if (IsRetired)
				{
					ThreadPool.QueueItem(() => result.SetException(RetiredException));
					return result.Task;
				}

				while (m_readerQueue.Count > 0)
				{
					var kp = m_readerQueue[0];

					var offerWriter = offer == null; 
					var offerReader = kp.Offer == null;

					if (!offerReader)
						try 
						{
							offerReader = kp.Offer.Offer(this);
						}
						catch(Exception ex)
						{
							result.SetException(ex);
							return result.Task;
						}

					if (!offerWriter)
						try 
						{ 
							offerWriter = offer.Offer(this); 
						}
						catch(Exception ex)
						{
							if (offerReader)
								kp.Offer.Withdraw(this);
							result.SetException(ex);

							return result.Task;
						}


					// If the reader accepts ...
					if (!(offerReader && offerWriter))
					{
						if (kp.Offer != null && offerReader)
							kp.Offer.Withdraw(this);

						if (offer != null && offerWriter)
							offer.Withdraw(this);

						// If the reader bailed, remove it from the queue
						if (!offerReader)
							m_readerQueue.RemoveAt(0);

						// if the writer bailed, the queue is intact, but we stop offering
						if (!offerWriter)
						{
							result.SetCanceled();
							return result.Task;
						}
					}
					else
					{
						// Transaction complete
						m_readerQueue.RemoveAt(0);

						if (kp.Offer != null)
							kp.Offer.Commit(this);
						if (offer != null)
							offer.Commit(this);

						ThreadPool.QueueItem(() => result.SetResult(true));
						ThreadPool.QueueItem(() => kp.Source.SetResult(value));

						// If this was the last item before the retirement, 
						// flush all following and set the retired flag
						EmptyQueueIfRetired();

						return result.Task;
					}
				}

				// If we have a buffer slot to use
				if (m_writerQueue.Count < m_bufferSize && m_retireCount < 0)
				{
					if (offer == null || offer.Offer(this))
					{
						if (offer != null)
							offer.Commit(this);

						m_writerQueue.Add(new WriterEntry(null, new TaskCompletionSource<bool>(), Timeout.InfiniteDateTime, value));
						result.SetResult(true);
					}
					else
					{
						result.SetCanceled();
					}
				}
				else
				{
					var expires = timeout.Ticks <= 0 ? Timeout.InfiniteDateTime : entry + timeout;

					// If this was a probe call, return a timeout now
					if (timeout.Ticks >= 0 && expires < DateTime.Now)
					{
						ThreadPool.QueueItem(() => result.SetException(TimeoutException));
					}
					else
					{
						// Register the pending writer
						m_writerQueue.Add(new WriterEntry(offer, result, expires, value));
						if (expires != Timeout.InfiniteDateTime)
							ExpirationManager.AddExpirationCallback(expires, ExpireItems);
					}
				}
			}

			return result.Task;

		}

		/// <summary>
		/// Helper method for dequeueing write requests after space has been allocated in the writer queue
		/// </summary>
		private void ProcessWriteQueueBufferAfterRead()
		{
			lock (m_lock)
			{
				// If there is now a buffer slot in the queue, trigger a callback to a waiting item
				while (m_retireCount < 0 && m_bufferSize > 0 && m_writerQueue.Count >= m_bufferSize)
				{
					var nextItem = m_writerQueue[m_bufferSize - 1];

					if (nextItem.Offer == null || nextItem.Offer.Offer(this))
					{
						if (nextItem.Offer != null)
							nextItem.Offer.Commit(this);

						nextItem.Source.SetResult(true);

						// Now that the transaction has completed for the writer, record it as waiting forever
						if (nextItem.Expires != Timeout.InfiniteDateTime)
							m_writerQueue[m_bufferSize - 1] = new WriterEntry(nextItem.Offer, nextItem.Source, Timeout.InfiniteDateTime, nextItem.Value);

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
		public void Retire()
		{
			Retire(false);
		}

		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		public void Retire(bool immediate)
		{
			lock (m_lock)
			{
				if (IsRetired)
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
							m_writerQueue.RemoveAt(0);
							m_retireCount--;
						}
				}
				
				EmptyQueueIfRetired();
			}
		}

		/// <summary>
		/// Join the channel
		/// </summary>
		/// <param name="asReader"><c>true</c> if joining as a reader, <c>false</c> otherwise</param>
		public void Join(bool asReader)
		{
			lock (m_lock)
			{
				// Do not allow anyone to join after we retire the channel
				if (IsRetired)
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
		public void Leave(bool asReader)
		{
			lock (m_lock)
			{
				// If we are already retired, skip this call
				if (IsRetired)
					return;

				// Countdown
				if (asReader)
					m_joinedReaderCount--;
				else
					m_joinedWriterCount--;

				// Retire if required
				if (m_joinedReaderCount <= 0 || m_joinedWriterCount <= 0)
					Retire();
			}
		}

		/// <summary>
		/// Empties the queue if the channel is retired.
		/// </summary>
		private void EmptyQueueIfRetired()
		{
			List<ReaderEntry> readers = null;
			List<WriterEntry> writers = null;

			lock (m_lock)
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
						IsRetired = true;
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
						ThreadPool.QueueItem(() => r.Source.TrySetException(RetiredException));

				if (writers != null)
					foreach (var w in writers)
						ThreadPool.QueueItem(() => w.Source.TrySetException(RetiredException));
			}
		}


		/// <summary>
		/// Callback method used to signal timeout on expired items
		/// </summary>
		private void ExpireItems()
		{
			KeyValuePair<int, ReaderEntry>[] expiredReaders;
			KeyValuePair<int, WriterEntry>[] expiredWriters;

			// Extract all expired items from their queues
			lock (m_lock)
			{
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
				r.Value.Source.TrySetException(TimeoutException);

			// Send the notifications
			foreach (var w in expiredWriters.OrderBy(x => x.Value.Expires))
				w.Value.Source.TrySetException(TimeoutException);
		}
	}
}

