using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// Implements a broadcasting channel
	/// </summary>
	public class BroadcastingChannel<T> : Channel<T>
	{
		/// <summary>
		/// The minimum number of readers required for a broadcast to be performed
		/// </summary>
		private int m_minimumReaders;
		/// <summary>
		/// The minimum number of readers required for the first broadcast to be performed
		/// </summary>
		private int m_initialBarrierSize;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:CoCoL.BroadcastingChannel&lt;1&gt;"/> class.
		/// </summary>
		/// <param name="attr">The channel name attributes.</param>
		internal BroadcastingChannel(ChannelNameAttribute attr)
			: base(attr)
		{
			if (attr is BroadcastChannelNameAttribute)
			{
				m_initialBarrierSize = ((BroadcastChannelNameAttribute)attr).InitialBarrierSize;
				m_minimumReaders = ((BroadcastChannelNameAttribute)attr).MinimumReaders;
			}

			if (m_minimumReaders > 0 && m_maxPendingReaders > 0 && m_maxPendingReaders < m_minimumReaders)
				throw new ArgumentOutOfRangeException(string.Format("The setup requires {0} readers waiting, but the channel only allows {1} waiting readers", m_minimumReaders, m_maxPendingReaders));
			if (m_initialBarrierSize > 0 && m_maxPendingReaders > 0 && m_maxPendingReaders < m_initialBarrierSize)
				throw new ArgumentOutOfRangeException(string.Format("The setup requires {0} readers waiting, but the channel only allows {1} waiting readers", m_initialBarrierSize, m_maxPendingReaders));
		}

		/// <summary>
		/// Method that examines the queues and matches readers with writers
		/// </summary>
		/// <returns>An awaitable that signals if the caller has been accepted or rejected.</returns>
		/// <param name="asReader"><c>True</c> if the caller method is a reader, <c>false</c> otherwise.</param>
		/// <param name="caller">The caller task.</param>
		protected override async Task<bool> MatchReadersAndWriters(bool asReader, Task caller)
		{
			var processed = false;

			while (m_writerQueue != null && m_readerQueue != null && m_writerQueue.Count > 0 && m_joinedReaderCount >= Math.Max(m_minimumReaders, m_initialBarrierSize))
			{
				var requiredreaders = m_joinedReaderCount;
				var acceptedreaders = 0;

				var haswriter = false;
				var acceptedwriter = Offer(m_writerQueue[0]);

				while (acceptedreaders < requiredreaders || !haswriter)
				{
					// If we cannot satisfy the constraint, bail
					if (m_readerQueue.Count < (requiredreaders - acceptedreaders) || m_writerQueue.Count == 0)
					{
						// Withdraw all offered reads
						await Task.WhenAll(m_readerQueue.Take(acceptedreaders).Select(x => x.Offer == null ? Task.FromResult(true) : x.Offer.WithdrawAsync(this)));

						if (await acceptedwriter)
							if (m_writerQueue.Count > 0 && m_writerQueue[0].Offer != null)
								await m_writerQueue[0].Offer.WithdrawAsync(this);

						return processed;
					}

					// Grab all the required items
					var successlist = m_readerQueue.Skip(acceptedreaders).Take(requiredreaders).Select(x =>
					{
						if (x.Offer == null)
							return Task.FromResult(true);
						if (x.Source != null && x.Source.Task.Status != TaskStatus.WaitingForActivation)
							return Task.FromResult(false);

						return x.Offer.OfferAsync(this);
					}).ToArray();

					await Task.WhenAll(successlist);
					if (!await acceptedwriter)
						acceptedwriter = Offer(m_writerQueue[0]);
					else
						haswriter = true;

					var accepted = 0;
					for (var i = successlist.Length - 1; i >= 0; i--)
						if (successlist[i].IsCanceled)
						{
							m_readerQueue[acceptedreaders + i].Source.TrySetCanceled();
							m_readerQueue.RemoveAt(acceptedreaders + i);
						}
						else if (successlist[i].IsFaulted)
						{
							m_readerQueue[acceptedreaders + i].Source.TrySetException(successlist[i].Exception);
							m_readerQueue.RemoveAt(acceptedreaders + i);
						}
						else if (successlist[i].IsCompleted)
							accepted++;
						else
							throw new InvalidOperationException("Unexpected state for two-phase probe");

					acceptedreaders += accepted;
				}

				// We have all the readers and a writer
				m_initialBarrierSize = -1;
				var wr = m_writerQueue[0];

				for (var i = acceptedreaders - 1; i >= 0; i--)
				{
					var tcs = m_readerQueue[i].Source;
					if (tcs != null)
					{
						ThreadPool.QueueItem(() => tcs.SetResult(wr.Value));
						processed |= tcs.Task == caller;
					}
				}

				if (wr.Source != null)
				{
					ThreadPool.QueueItem(() => wr.Source.SetResult(true));
					processed |= wr.Source.Task == caller;
				}

				m_writerQueue.RemoveAt(0);
				m_readerQueue.RemoveRange(0, acceptedreaders);

			}

			return processed || (caller != null && caller.Status != TaskStatus.WaitingForActivation);
		}

		/// <summary>
		/// Leave the channel.
		/// </summary>
		/// <param name="asReader"><c>true</c> if leaving as a reader, <c>false</c> otherwise</param>
		public override async Task LeaveAsync(bool asReader)
		{
			await base.LeaveAsync(asReader);
			if (asReader)
				using (await m_asynclock.LockAsync())
					if (m_joinedReaderCount >= Math.Max(m_initialBarrierSize, m_minimumReaders))
						await MatchReadersAndWriters(true, null);
				
		}

		/// <summary>
		/// Gets the minimum number of readers allowed before a broadcast can be performed.
		/// </summary>
		/// <value>The minimum number of readers.</value>
		public int MinimumReaders { get { return m_minimumReaders; } }

		/// <summary>
		/// Sets the minimum number of readers required before a broadcast can be performed.
		/// </summary>
		/// <returns>The awaitable task.</returns>
		/// <param name="value">The minimum number of readers.</param>
		public async Task SetMinimumReadersAsync(int value)
		{
			using (await m_asynclock.LockAsync())
				if (m_minimumReaders != value)
				{
					if (m_minimumReaders > 0 && m_maxPendingReaders < m_minimumReaders)
						throw new ArgumentOutOfRangeException(string.Format("The value requests {0} readers waiting, but the channel only allows {1} waiting readers", m_minimumReaders, m_maxPendingReaders));
				
					m_minimumReaders = value;
					await MatchReadersAndWriters(true, null).ConfigureAwait(false);
				}
		}

		/// <summary>
		/// Gets the number of processes required before the next broadcast is performed
		/// </summary>
		public int NextBarrierCount { get { return m_initialBarrierSize; } }

		/// <summary>
		/// Sets the number of processes required before the next broadcast is performed
		/// </summary>
		/// <returns>The awaitable task.</returns>
		/// <param name="value">The minimum number of processes reqquired before the next broadcast.</param>
		public async Task SetNextBarrierCountAsync(int value)
		{
			using (await m_asynclock.LockAsync())
				if (m_initialBarrierSize != value)
				{
					if (m_initialBarrierSize > 0 && m_maxPendingReaders < m_initialBarrierSize)
						throw new ArgumentOutOfRangeException(string.Format("The value requests {0} readers waiting, but the channel only allows {1} waiting readers", m_initialBarrierSize, m_maxPendingReaders));
				
					m_initialBarrierSize = value;
					await MatchReadersAndWriters(true, null).ConfigureAwait(false);
				}
		}
	}
}

