using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoCoL
{
	/// <summary>
	/// A channel that uses continuation callbacks
	/// </summary>
	public class ContinuationChannel<T> : IBlockingChannel<T>, IChannel<T>, IUntypedContinuationChannel, INamedItem
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
			public ChannelCallback<T> Callback;
			/// <summary>
			/// The timeout value
			/// </summary>
			public DateTime Expires;

			/// <summary>
			/// Initializes a new instance of the <see cref="CoCoL.ContinuationChannel`1+ReaderEntry"/> struct.
			/// </summary>
			public ReaderEntry() 
			{
				Offer = null;	
				Callback = null;
				Expires = new DateTime(0);
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="CoCoL.ContinuationChannel`1+ReaderEntry"/> struct.
			/// </summary>
			/// <param name="offer">The offer handler</param>
			/// <param name="callback">The callback method for reporting progress.</param>
			/// <param name="expires">The timeout value.</param>
			public ReaderEntry(ITwoPhaseOffer offer, ChannelCallback<T> callback, DateTime expires)
			{
				Offer = offer;
				Callback = callback;
				Expires = expires;
			}

			/// <summary>
			/// Helper method for registering a workitem callback with no additional overhead
			/// </summary>
			/// <param name="result">The <see cref="CoCoL.ICallbackResult`1"/> response item</param>
			public void PerformCallback(object result)
			{
				Callback(result as ICallbackResult<T>);
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
			public ChannelCallback<T> Callback;
			/// <summary>
			/// The timeout value
			/// </summary>
			public DateTime Expires;
			/// <summary>
			/// The value being written
			/// </summary>
			public T Value;

			/// <summary>
			/// Initializes a new instance of the <see cref="CoCoL.ContinuationChannel`1+WriterEntry"/> struct.
			/// </summary>
			public WriterEntry() 
			{
				Offer = null;	
				Callback = null;
				Expires = new DateTime(0);
				Value = default(T);
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="CoCoL.ContinuationChannel`1+WriterEntry"/> struct.
			/// </summary>
			/// <param name="offer">The offer handler</param>
			/// <param name="callback">The callback method for reporting progress.</param>
			/// <param name="expires">The timeout value.</param>
			/// <param name="value">The value being written.</param>
			public WriterEntry(ITwoPhaseOffer offer, ChannelCallback<T> callback, DateTime expires, T value)
			{
				Offer = offer;
				Callback = callback;
				Expires = expires;
				Value = value;
			}

			/// <summary>
			/// Helper method for registering a workitem callback with no additional overhead
			/// </summary>
			/// <param name="result">The <see cref="CoCoL.ICallbackResult`1"/> response item</param>
			public void PerformCallback(object result)
			{
				Callback(result as ICallbackResult<T>);
			}
		}

		/// <summary>
		/// The callback entry, with a callback method, so it can be called without creating extra delegates
		/// </summary>
		private class HelperCallbackItem : CallbackResult<T>
		{
			public HelperCallbackItem(T result, Exception exception, IChannel<T> channel)
				: base(result, exception, channel)
			{
			}
				
			/// <summary>
			/// Callback method for registering a callback
			/// </summary>
			/// <param name="dummy">The method to call with the results</param>
			public void CallbackWithMethod(object channel) 
			{
				((ChannelCallback<T>)channel)(this);
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
		/// A stopwatch instance shared by all instances of the queue
		/// </summary>
		private static Stopwatch watch = Stopwatch.StartNew();

		/// <summary>
		/// Gets the last time the channel was read, in ticks from the library was loaded
		/// </summary>
		public long LastRead { get; private set; }

		/// <summary>
		/// Gets the last time the channel was written, in ticks from library was loaded
		/// </summary>
		public long LastWrite { get; private set; }

		/// <summary>
		/// A cached instance of the timeout exception
		/// </summary>
		private static readonly Exception TimeoutException = new TimeoutException();

		/// <summary>
		/// A cached callback item for timeout issues
		/// </summary>
		private readonly HelperCallbackItem TimeoutCallbackItem;

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
		/// Initializes a new instance of the <see cref="CoCoL.ContinuationChannel`1"/> class.
		/// </summary>
		/// <param name="size">The size of the write buffer</param>
		internal ContinuationChannel(string name = null, int size = 0)
		{
			if (size < 0)
				throw new ArgumentOutOfRangeException("size", "The size parameter must be greater than or equal to zero");

			this.Name = name;

			m_bufferSize = size;

			TimeoutCallbackItem = new HelperCallbackItem(default(T), TimeoutException, this);
		}

		/// <summary>
		/// Read from the channel in a blocking manner
		/// </summary>
		public T Read()
		{
			return ContinuationChannelAsBlocking.Read(this);
		}

		/// <summary>
		/// Write to the channel in a blocking manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		public void Write(T value)
		{
			ContinuationChannelAsBlocking.Write(this, value);
		}


		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		public void RegisterRead(ChannelCallback<T> commitCallback)
		{
			RegisterRead(null, commitCallback, Timeout.Infinite);
		}

		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public void RegisterRead(ChannelCallback<T> commitCallback, TimeSpan timeout)
		{
			RegisterRead(null, commitCallback, Timeout.Infinite);
		}

		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		public void RegisterRead(ITwoPhaseOffer offer, ChannelCallback<T> commitCallback)
		{
			RegisterRead(offer, commitCallback, Timeout.Infinite);
		}

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="value">The value to write to the channel.</param>
		public void RegisterWrite(ChannelCallback<T> commitCallback, T value)
		{
			RegisterWrite(null, commitCallback, value, Timeout.Infinite);
		}

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public void RegisterWrite(ChannelCallback<T> commitCallback, T value, TimeSpan timeout)
		{
			RegisterWrite(null, commitCallback, value, timeout);
		}

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="value">The value to write to the channel.</param>
		public void RegisterWrite(ITwoPhaseOffer offer, ChannelCallback<T> commitCallback, T value)
		{
			RegisterWrite(offer, commitCallback, value, Timeout.Infinite);
		}

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="value">The value to write to the channel.</param>
		public void RegisterWrite(T value)
		{
			RegisterWrite(null, null, value, Timeout.Infinite);
		}

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public void RegisterWrite(T value, TimeSpan timeout)
		{
			RegisterWrite(null, null, value, timeout);
		}

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public void RegisterWrite(ITwoPhaseOffer offer, T value, TimeSpan timeout)
		{
			RegisterWrite(offer, null, value, timeout);
		}


		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public void RegisterRead(ITwoPhaseOffer offer, ChannelCallback<T> callback, TimeSpan timeout)
		{				
			// Store entry time in case we need it and the offer dance takes some time
			var entry = DateTime.Now;

			lock (m_lock)
			{
				if (IsRetired)
				{
					if (callback != null)
						ThreadPool.QueueItem(new HelperCallbackItem(default(T), new RetiredException(), this).CallbackWithMethod, callback);

					return;
				}

				while (m_writerQueue.Count > 0)
				{
					var kp = m_writerQueue[0];

					var offerReader = offer == null || offer.Offer(this);
					var offerWriter = kp.Offer == null || kp.Offer.Offer(this);

					if (!(offerReader && offerWriter))
					{
						if (kp.Offer != null && offerWriter)
							kp.Offer.Withdraw(this);

						if (offer != null && offerReader)
							offer.Withdraw(this);

						// if the reader bailed, the queue is intact but we offer no more
						if (!offerReader)
							return;
					}
					else
					{
						// transaction complete
						m_writerQueue.RemoveAt(0);

						if (kp.Offer != null)
							kp.Offer.Commit(this);
						if (offer != null)
							offer.Commit(this);
						
						if (callback != null || kp.Callback != null)
						{
							var item = new HelperCallbackItem(kp.Value, null, this);
							if (callback != null)
								ThreadPool.QueueItem(item.CallbackWithMethod, callback);
							if (kp.Callback != null)
								ThreadPool.QueueItem(item.CallbackWithMethod, kp.Callback);
						}
						
						LastRead = watch.ElapsedTicks;

						// Release items if there is space in the buffer
						ProcessWriteQueueBuffer();

						// If this was the last item before the retirement, 
						// flush all following and set the retired flag
						EmptyQueueIfRetired();

						return;
					}
				}

				var expires = timeout.Ticks <= 0 ? Timeout.InfiniteDateTime : entry + timeout;

				// If this was a probe call, return a timeout now
				if (timeout.Ticks >= 0 && expires < DateTime.Now)
				{
					if (callback != null)
						ThreadPool.QueueItem(TimeoutCallbackItem.CallbackWithMethod, callback);
					
					return;
				}
				else
				{
					// Register the pending reader
					m_readerQueue.Add(new ReaderEntry(offer, callback, expires));

					if (expires != Timeout.InfiniteDateTime)
						ExpirationManager.AddExpirationCallback(expires, ExpireItems);
				}
			}
		}
			
		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		public void RegisterWrite(ITwoPhaseOffer offer, ChannelCallback<T> callback, T value, TimeSpan timeout)
		{
			// Store entry time in case we need it and the offer dance takes some time
			var entry = DateTime.Now;

			lock (m_lock)
			{
				if (IsRetired)
				{
					if (callback != null)
						ThreadPool.QueueItem(new HelperCallbackItem(default(T), new RetiredException(), this).CallbackWithMethod, callback);

					return;
				}

				while (m_readerQueue.Count > 0)
				{
					var kp = m_readerQueue[0];

					var offerReader = kp.Offer == null || kp.Offer.Offer(this);
					var offerWriter = offer == null || offer.Offer(this);

					// If the reader accepts ...
					if (!(offerReader && offerWriter))
					{
						if (kp.Offer != null && offerReader)
							kp.Offer.Withdraw(this);

						if (offer != null && offerWriter)
							offer.Withdraw(this);

						// if the writer bailed, the queue is intact, but we stop offering
						if (!offerWriter)
							return;
					}
					else
					{
						// Transaction complete
						m_readerQueue.RemoveAt(0);

						if (kp.Offer != null)
							kp.Offer.Commit(this);
						if (offer != null)
							offer.Commit(this);

						if (callback != null || kp.Callback != null)
						{
							var item = new HelperCallbackItem(value, null, this);
							if (callback != null)
								ThreadPool.QueueItem(item.CallbackWithMethod, callback);

							if (kp.Callback != null)
								ThreadPool.QueueItem(item.CallbackWithMethod, kp.Callback);
						}

						LastWrite = watch.ElapsedTicks;

						// If this was the last item before the retirement, 
						// flush all following and set the retired flag
						EmptyQueueIfRetired();

						return;
					}
				}

				var expires = timeout.Ticks <= 0 ? Timeout.InfiniteDateTime : entry + timeout;

				// If this was a probe call, return a timeout now
				if (timeout.Ticks >= 0 && expires < DateTime.Now)
				{
					if (callback != null)
						ThreadPool.QueueItem(TimeoutCallbackItem.CallbackWithMethod, callback);
					return;
				}
				else
				{
					if (m_writerQueue.Count < m_bufferSize && m_retireCount < 0)
					{
						// We have a buffer slot to use
						if (offer == null || offer.Offer(this))
						{
							if (offer != null)
								offer.Commit(this);
							
							m_writerQueue.Add(new WriterEntry(null, null, Timeout.InfiniteDateTime, value));
							if (callback != null)
								ThreadPool.QueueItem(new HelperCallbackItem(value, null, this).CallbackWithMethod, callback);

							LastWrite = watch.ElapsedTicks;
						}
					}
					else
					{
						// Register the pending writer
						m_writerQueue.Add(new WriterEntry(offer, callback, expires, value));
						if (expires != Timeout.InfiniteDateTime)
							ExpirationManager.AddExpirationCallback(expires, ExpireItems);
					}
				}
			}
		}

		/// <summary>
		/// Helper method for dequeueing write requests after space has been allocated in the writer queue
		/// </summary>
		private void ProcessWriteQueueBuffer()
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

						if (nextItem.Callback != null)
							ThreadPool.QueueItem(new HelperCallbackItem(nextItem.Value, null, this).CallbackWithMethod, nextItem.Callback);

						// Now that the transaction has completed for the writer, record it as waiting forever
						if (nextItem.Expires != Timeout.InfiniteDateTime)
							m_writerQueue[m_bufferSize - 1] = new WriterEntry(nextItem.Offer, nextItem.Callback, Timeout.InfiniteDateTime, nextItem.Value);

						LastWrite = watch.ElapsedTicks;
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
			lock (m_lock)
			{
				if (IsRetired)
					return;

				if (m_retireCount < 0)
				{
					// If we have responded to buffered writes, 
					// make sure we pair those before retiring
					m_retireCount = Math.Min(m_writerQueue.Count, m_bufferSize) + 1;
				}
				
				EmptyQueueIfRetired();
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
				var retiredItem = new HelperCallbackItem(default(T), new RetiredException(), this);

				if (readers != null)
					foreach (var r in readers)
						if (r.Callback != null)
							ThreadPool.QueueItem(retiredItem.CallbackWithMethod, r.Callback);
				if (writers != null)
					foreach (var w in writers)
						if (w.Callback != null)
							ThreadPool.QueueItem(retiredItem.CallbackWithMethod, w.Callback);
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
				expiredReaders = m_readerQueue.Where(x => x.Expires < now).Zip(Enumerable.Range(0, m_readerQueue.Count), (n, i) => new KeyValuePair<int, ReaderEntry>(i, n)).ToArray();
				expiredWriters = m_writerQueue.Where(x => x.Expires < now).Zip(Enumerable.Range(0, m_readerQueue.Count), (n, i) => new KeyValuePair<int, WriterEntry>(i, n)).ToArray();

				foreach (var r in expiredReaders.OrderByDescending(x => x.Key))
					m_readerQueue.RemoveAt(r.Key);
				foreach (var r in expiredWriters.OrderByDescending(x => x.Key))
					m_writerQueue.RemoveAt(r.Key);

				// After writer expiration, we could have waiting items
				ProcessWriteQueueBuffer();
			}

			// Send the notifications
			foreach (var r in expiredReaders.OrderBy(x => x.Value.Expires))
				if (r.Value.Callback != null)
					ThreadPool.QueueItem(TimeoutCallbackItem.CallbackWithMethod, r.Value.Callback);

			// Send the notifications
			foreach (var w in expiredWriters.OrderBy(x => x.Value.Expires))
				if (w.Value.Callback != null)
					ThreadPool.QueueItem(TimeoutCallbackItem.CallbackWithMethod, w.Value.Callback);
		}
	}
}

