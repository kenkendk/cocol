using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// Helper class for optimized storage of a fair-priority sorted list
	/// </summary>
	internal class SortedChannelList<T>
	{
		/// <summary>
		/// The index where the arrays are split, such that
		/// any index &lt; split is in the top half, where
		/// all values are the same
		/// </summary>
		private int m_split;

		/// <summary>
		/// The total number of usages
		/// </summary>
		private long m_totalUsage = 0;

		/// <summary>
		/// The count for the number of times each channel has been used
		/// </summary>
		private long[] m_usageCounts;
		/// <summary>
		/// The sorted list of channels, such that the index in
		/// m_channels and m_usageCounts match
		/// </summary>
		private T[] m_channels;

		/// <summary>
		/// A channel lookup for constant time mapping a channel to the index
		/// </summary>
		private Dictionary<object, int> m_channelLookup;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.SortedChannelList&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="channels">The channels to keep sorted</param>
		public SortedChannelList(T[] channels)
		{
			m_channels = channels;
			m_usageCounts = new long[m_channels.Length];
			m_split = m_channels.Length;
			m_channelLookup = new Dictionary<object, int>(m_channels.Length);
			for (var i = 0; i < m_channels.Length; i++)
				m_channelLookup[m_channels[i]] = i;
		}

		/// <summary>
		/// Callback method invoked when a channel has been used
		/// </summary>
		/// <param name="item">The channel that was used</param>
		public void NotifyUsed(object item)
		{
			var ix = m_channelLookup[item];
			m_usageCounts[ix]++;
			m_totalUsage++;

			// If we were in the upper part, move to the lower part
			if (ix < m_split)
			{
				// Move to right after the split
				Swap(ix, m_split - 1);

				// Update the split, and reset the split 
				// if all items are now in the lower part
				if (--m_split == 0)
				{
					var min = m_usageCounts[0];
					m_split = m_channels.Length;
					while (m_usageCounts[m_split - 1] > min)
						m_split--;
				}
			}
			else
			{
				// If we are the last in the list, don't care
				while (ix < m_channels.Length - 1)
				{
					// Partially bubble-sort the list
					if (m_usageCounts[ix] > m_usageCounts[ix + 1])
					{
						Swap(ix, ix + 1);
						ix++;
					}
					else
						break;
				}
			}
		}

		/// <summary>
		/// Swaps the elements with index a and b
		/// </summary>
		/// <param name="a">One index</param>
		/// <param name="b">Another index</param>
		private void Swap(int a, int b)
		{
			// Update the lookup table
			m_channelLookup[m_channels[a]] = b;
			m_channelLookup[m_channels[b]] = a;

			//... and no, it is NOT faster to use XOR
			var t0 = m_channels[a];
			var t1 = m_usageCounts[a];

			m_channels[a] = m_channels[b];
			m_usageCounts[a] = m_usageCounts[b];
			m_channels[b] = t0;
			m_usageCounts[b] = t1;
		}

		/// <summary>
		/// Gets the channels in sorted order
		/// </summary>
		/// <value>The channels.</value>
		public T[] Channels
		{
			get { return m_channels; }
		}
	}

	/// <summary>
	/// A collection of channels that can be read or written
	/// </summary>
	public class MultiChannelSetRead<T>
	{
		/// <summary>
		/// The channels to consider
		/// </summary>
		private readonly IReadChannel<T>[] m_channels;
		/// <summary>
		/// The usage of the channels, used for tracking fair usage
		/// </summary>
		private readonly SortedChannelList<IReadChannel<T>> m_sortedChannels;
		/// <summary>
		/// The order in which the channels are picked
		/// </summary>
		private readonly MultiChannelPriority m_priority;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSetRead&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="priority">The priority to use when selecting a channel.</param>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSetRead(MultiChannelPriority priority, params IReadChannel<T>[] channels)
		{
			
			m_channels = channels;
			m_sortedChannels = priority == MultiChannelPriority.Fair ? new SortedChannelList<IReadChannel<T>>(channels) : null;
			m_priority = priority;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSetRead&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSetRead(params IReadChannel<T>[] channels)
			: this(MultiChannelPriority.Any, channels) 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSetRead&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="priority">The priority to use when selecting a channel.</param>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSetRead(IEnumerable<IReadChannel<T>> channels, MultiChannelPriority priority = MultiChannelPriority.Any)
			: this(priority, channels.ToArray())
		{
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSetRead&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSetRead(IEnumerable<IReadChannel<T>> channels)
			: this(MultiChannelPriority.Any, channels.ToArray())
		{
		}

		/// <summary>
		/// Reads from any channel
		/// </summary>
		public Task<MultisetResult<T>> ReadFromAnyAsync()
		{
			return ReadFromAnyAsync(Timeout.Infinite);
		}

		/// <summary>
		/// Reads from any channel
		/// </summary>
		/// <param name="timeout">The maximum time to wait for a result.</param>
		public Task<MultisetResult<T>> ReadFromAnyAsync(TimeSpan timeout)
		{
			if (m_priority == MultiChannelPriority.Fair)
				return MultiChannelAccess.ReadFromAnyAsync(
					m_sortedChannels.NotifyUsed,
					m_sortedChannels.Channels, 
					timeout,
					MultiChannelPriority.First
				);
			else
				return MultiChannelAccess.ReadFromAnyAsync(
					null,
					m_channels, 
					timeout,
					m_priority
				);
		}

		/// <summary>
		/// Retires all channels in the set
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		/// <returns>An awaitable task</returns>
		public async Task RetireAsync(bool immediate = false)
		{
			foreach (var c in m_channels)
				await c.RetireAsync(immediate);
		}

		/// <summary>
		/// Gets all channels in the set
		/// </summary>
		/// <value>The channels in the set</value>
		public IEnumerable<IReadChannel<T>> Channels
		{
			get { return m_channels; }
		}
	}

	/// <summary>
	/// A collection of channels that can be read or written
	/// </summary>
	public class MultiChannelSetWrite<T>
	{
		/// <summary>
		/// The channels to consider
		/// </summary>
		private readonly IWriteChannel<T>[] m_channels;
		/// <summary>
		/// The usage of the channels, used for tracking fair usage
		/// </summary>
		private readonly SortedChannelList<IWriteChannel<T>> m_sortedChannels;
		/// <summary>
		/// The order in which the channels are picked
		/// </summary>
		private readonly MultiChannelPriority m_priority;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSetWrite&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="priority">The priority to use when selecting a channel.</param>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSetWrite(MultiChannelPriority priority, params IWriteChannel<T>[] channels)
		{

			m_channels = channels;
			m_sortedChannels = priority == MultiChannelPriority.Fair ? new SortedChannelList<IWriteChannel<T>>(channels) : null;
			m_priority = priority;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSetWrite&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSetWrite(params IWriteChannel<T>[] channels)
			: this(MultiChannelPriority.Any, channels) 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSetWrite&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="priority">The priority to use when selecting a channel.</param>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSetWrite(IEnumerable<IWriteChannel<T>> channels, MultiChannelPriority priority = MultiChannelPriority.Any)
			: this(priority, channels.ToArray())
		{
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSetWrite&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSetWrite(IEnumerable<IWriteChannel<T>> channels)
			: this(MultiChannelPriority.Any, channels.ToArray())
		{
		}
			
		/// <summary>
		/// Writes to any of the channels.
		/// </summary>
		/// <param name="value">The value to write into the channel.</param>
		public Task<IWriteChannel<T>> WriteToAnyAsync(T value)
		{
			return WriteToAnyAsync(value, Timeout.Infinite);
		}

		/// <summary>
		/// Writes to any of the channels.
		/// </summary>
		/// <param name="value">The value to write into the channel.</param>
		/// <param name="timeout">The maximum time to wait for any channel to become ready.</param>
		public Task<IWriteChannel<T>> WriteToAnyAsync(T value, TimeSpan timeout)
		{
			if (m_priority == MultiChannelPriority.Fair)
				return MultiChannelAccess.WriteToAnyAsync(
					m_sortedChannels.NotifyUsed,
					value, 
					m_sortedChannels.Channels, 
					timeout,
					MultiChannelPriority.First
				);
			else
				return MultiChannelAccess.WriteToAnyAsync(
					null,
					value, 
					m_channels, 
					timeout,
					m_priority
				);
		}

		/// <summary>
		/// Retires all channels in the set
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		/// <returns>An awaitable task</returns>
		public async Task RetireAsync(bool immediate = false)
		{
			foreach (var c in m_channels)
				await c.RetireAsync(immediate);
		}

		/// <summary>
		/// Gets all channels in the set
		/// </summary>
		/// <value>The channels in the set</value>
		public IEnumerable<IWriteChannel<T>> Channels
		{
			get { return m_channels; }
		}
	}
}

