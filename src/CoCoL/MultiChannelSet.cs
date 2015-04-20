using System;
using System.Linq;
using System.Collections.Generic;

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
		private readonly IContinuationChannel<T>[] m_channels;
		/// <summary>
		/// The usage of the channels, used for tracking fair usage
		/// </summary>
		private readonly KeyValuePair<long, int>[] m_usageCounts;
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
		public MultiChannelSet(MultiChannelPriority priority, params IContinuationChannel<T>[] channels)
		{
			m_channels = channels;

			m_usageCounts = priority == MultiChannelPriority.Fair ? new KeyValuePair<long, int>[m_channels.Length] : null;
			m_totalUsage = 0;
			m_priority = priority;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSet`1"/> class.
		/// </summary>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSet(params IContinuationChannel<T>[] channels)
			: this(MultiChannelPriority.Any, channels) 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSet`1"/> class.
		/// </summary>
		/// <param name="priority">The priority to use when selecting a channel.</param>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSet(IEnumerable<IContinuationChannel<T>> channels, MultiChannelPriority priority = MultiChannelPriority.Any)
			: this(priority, channels.ToArray())
		{
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultiChannelSet`1"/> class.
		/// </summary>
		/// <param name="channels">The channels to consider.</param>
		public MultiChannelSet(IEnumerable<IContinuationChannel<T>> channels)
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
				if (m_channels[m_usageCounts[i].Value] == caller)
				{
					m_totalUsage++;
					m_usageCounts[i] = new KeyValuePair<long, int>(m_usageCounts[i].Key + 1, i);

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
						m_usageCounts[i] = new KeyValuePair<long, int>(m_usageCounts[i].Key - min, i);

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
		public void ReadFromAny(ChannelCallback<T> callback)
		{
			ReadFromAny(callback, Timeout.Infinite);
		}

		/// <summary>
		/// Reads from any channel
		/// </summary>
		/// <param name="callback">The continuation callback to invoke after reading a value.</param>
		/// <param name="timeout">The maximum time to wait for a result.</param>
		public void ReadFromAny(ChannelCallback<T> callback, TimeSpan timeout)
		{
			
			MultiChannelAccess.ReadFromAny(
				callback, 
				m_priority == MultiChannelPriority.Fair ? PriorityOrderedChannels : m_channels.AsEnumerable(), 
				timeout,
				m_priority == MultiChannelPriority.Fair ? MultiChannelPriority.First : m_priority
			);
		}

		/// <summary>
		/// Writes to any of the channels.
		/// </summary>
		/// <param name="value">The value to write into the channel.</param>
		public void WriteToAny(T value)
		{
			WriteToAny(null, value, Timeout.Infinite);
		}

		/// <summary>
		/// Writes to any of the channels.
		/// </summary>
		/// <param name="callback">The callback to invoke, or null.</param>
		/// <param name="value">The value to write into the channel.</param>
		public void WriteToAny(ChannelCallback<T> callback, T value)
		{
			WriteToAny(callback, value, Timeout.Infinite);
		}

		/// <summary>
		/// Writes to any of the channels.
		/// </summary>
		/// <param name="callback">The callback to invoke, or null.</param>
		/// <param name="value">The value to write into the channel.</param>
		/// <param name="timeout">The maximum time to wait for any channel to become ready.</param>
		public void WriteToAny(ChannelCallback<T> callback, T value, TimeSpan timeout)
		{
			MultiChannelAccess.WriteToAny(
				callback, 
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
		private IEnumerable<IContinuationChannel<T>> PriorityOrderedChannels
		{
			get
			{
				return from n in m_usageCounts
				       orderby n.Key
				       select m_channels[n.Value];
			}
		}

	}
}

