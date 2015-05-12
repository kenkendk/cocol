using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// A collection of channels that can be read or written
	/// </summary>
	public class MultiChannelSet<T>
	{
		/// <summary>
		/// The channels to consider
		/// </summary>
		private readonly IChannel<T>[] m_channels;
		/// <summary>
		/// The usage of the channels, used for tracking fair usage
		/// </summary>
		private readonly KeyValuePair<long, IChannel<T>>[] m_usageCounts;
		/// <summary>
		/// The order in which the channels are picked
		/// </summary>
		private readonly MultiChannelPriority m_priority;

		/// <summary>
		/// The number of total item usages
		/// </summary>
		private long m_totalUsage;
		/// <summary>
		/// A helper value for preventing repeated rebalancing of the usage counts
		/// </summary>
		private int m_skipRebalancing;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSet`1"/> class.
		/// </summary>
		/// <param name="priority">The priority to use when selecting a channel.</param>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSet(MultiChannelPriority priority, params IChannel<T>[] channels)
		{
			m_channels = channels;

			if (priority == MultiChannelPriority.Fair)
				m_usageCounts = Enumerable.Range(0, m_channels.Length).Select(x => new KeyValuePair<long, IChannel<T>>(0, m_channels[x])).ToArray();
			else
				m_usageCounts = null;
			m_totalUsage = 0;
			m_priority = priority;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSet`1"/> class.
		/// </summary>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSet(params IChannel<T>[] channels)
			: this(MultiChannelPriority.Any, channels) 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSet`1"/> class.
		/// </summary>
		/// <param name="priority">The priority to use when selecting a channel.</param>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSet(IEnumerable<IChannel<T>> channels, MultiChannelPriority priority = MultiChannelPriority.Any)
			: this(priority, channels.ToArray())
		{
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSet`1"/> class.
		/// </summary>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSet(IEnumerable<IChannel<T>> channels)
			: this(MultiChannelPriority.Any, channels.ToArray())
		{
		}

		/// <summary>
		/// Update usage counters after an item has been used
		/// </summary>
		/// <param name="caller">The channel that has been used.</param>
		private void NotifyUsed(object caller)
		{
			for(int i = 0; i < m_usageCounts.Length; i++)
				if (m_usageCounts[i].Value == caller)
				{
					m_totalUsage++;
					m_usageCounts[i] = new KeyValuePair<long, IChannel<T>>(m_usageCounts[i].Key + 1, m_usageCounts[i].Value);

					this.BalanceCounts();

					return;
				}

			System.Diagnostics.Debug.Fail("Notify for Priority matched no entries");
		}

		/// <summary>
		/// Reduces the usage counts by substracting the smallest value from all counts
		/// </summary>
		private void BalanceCounts()
		{
			// Countdown
			if (m_skipRebalancing > 0)
				m_skipRebalancing--;
			else
			{
				// Do rebalancing if required
				var min = m_usageCounts.Select(x => x.Key).Min();
				if (min > 0)
				{
					for (var i = 0; i < m_usageCounts.Length; i++)
						m_usageCounts[i] = new KeyValuePair<long, IChannel<T>>(m_usageCounts[i].Key - min, m_usageCounts[i].Value);

					m_totalUsage -= min;
				}

				// Wait for a while before we try again
				m_skipRebalancing = Math.Max(100, m_usageCounts.Length);
			}
		}

		/// <summary>
		/// Reads from any channel
		/// </summary>
		/// <param name="callback">The continuation callback to invoke after reading a value.</param>
		public Task<MultisetResult<T>> ReadFromAnyAsync()
		{
			return ReadFromAnyAsync(Timeout.Infinite);
		}

		/// <summary>
		/// Reads from any channel
		/// </summary>
		/// <param name="callback">The continuation callback to invoke after reading a value.</param>
		/// <param name="timeout">The maximum time to wait for a result.</param>
		public Task<MultisetResult<T>> ReadFromAnyAsync(TimeSpan timeout)
		{
			return MultiChannelAccess.ReadFromAnyAsync(
				m_priority == MultiChannelPriority.Fair ? PriorityOrderedChannels : m_channels.AsEnumerable(), 
				timeout,
				m_priority == MultiChannelPriority.Fair ? MultiChannelPriority.First : m_priority
			);
		}

		/// <summary>
		/// Writes to any of the channels.
		/// </summary>
		/// <param name="value">The value to write into the channel.</param>
		public Task<IChannel<T>> WriteToAnyAsync(T value)
		{
			return WriteToAnyAsync(value, Timeout.Infinite);
		}
			
		/// <summary>
		/// Writes to any of the channels.
		/// </summary>
		/// <param name="callback">The callback to invoke, or null.</param>
		/// <param name="value">The value to write into the channel.</param>
		/// <param name="timeout">The maximum time to wait for any channel to become ready.</param>
		public Task<IChannel<T>> WriteToAnyAsync(T value, TimeSpan timeout)
		{
			return MultiChannelAccess.WriteToAnyAsync(
				value, 
				m_priority == MultiChannelPriority.Fair ? PriorityOrderedChannels : m_channels.AsEnumerable(), 
				timeout,
				m_priority == MultiChannelPriority.Fair ? MultiChannelPriority.First : m_priority
				);
		}

		/// <summary>
		/// Gets the channels in priority order based on usage, least used first
		/// </summary>
		/// <value>The priority ordered channels.</value>
		private IEnumerable<IChannel<T>> PriorityOrderedChannels
		{
			get
			{
				return from n in m_usageCounts
				       orderby n.Key
				       select n.Value;
			}
		}

		/// <summary>
		/// Retires all channels in the set
		/// </summary>
		public void Retire()
		{
			foreach (var c in m_channels)
				c.Retire();
		}

		/// <summary>
		/// Gets all channels in the set
		/// </summary>
		/// <value>The channels in the set</value>
		public IEnumerable<IChannel<T>> Channels
		{
			get { return m_channels; }
		}

	}
}

