using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CoCoL
{
	/// <summary>
	/// Helper class for performing multi-channel access
	/// </summary>
	public static class MultiChannelAccess
	{
		#region Overloads for setting default parameters in the read method
		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void ReadFromAny<T>(ChannelCallback<T> callback, TimeSpan timeout, MultiChannelPriority priority, params IContinuationChannel<T>[] channels)
		{
			ReadFromAny(callback, channels.AsEnumerable(), timeout, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void ReadFromAny<T>(ChannelCallback<T> callback, TimeSpan timeout, params IContinuationChannel<T>[] channels)
		{
			ReadFromAny(callback, channels.AsEnumerable(), timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void ReadFromAny<T>(ChannelCallback<T> callback, MultiChannelPriority priority, params IContinuationChannel<T>[] channels)
		{
			ReadFromAny(callback, channels.AsEnumerable(), Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void ReadFromAny<T>(ChannelCallback<T> callback, params IContinuationChannel<T>[] channels)
		{
			ReadFromAny(callback, channels.AsEnumerable(), Timeout.Infinite, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void ReadFromAny<T>(ChannelCallback<T> callback, IEnumerable<IContinuationChannel<T>> channels, TimeSpan timeout)
		{
			ReadFromAny(callback, channels, timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void ReadFromAny<T>(ChannelCallback<T> callback, IEnumerable<IContinuationChannel<T>> channels, MultiChannelPriority priority)
		{
			ReadFromAny(callback, channels, Timeout.Infinite, priority);
		}
		#endregion

		#region Overloads for setting default parameters in the write method
		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void WriteToAny<T>(ChannelCallback<T> callback, T value, TimeSpan timeout, MultiChannelPriority priority, params IContinuationChannel<T>[] channels)
		{
			WriteToAny(callback, value, channels.AsEnumerable(), timeout, priority);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void WriteToAny<T>(ChannelCallback<T> callback, T value, TimeSpan timeout, params IContinuationChannel<T>[] channels)
		{
			WriteToAny(callback, value, channels.AsEnumerable(), timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void WriteToAny<T>(ChannelCallback<T> callback, T value, MultiChannelPriority priority, params IContinuationChannel<T>[] channels)
		{
			WriteToAny(callback, value, channels.AsEnumerable(), Timeout.Infinite, priority);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void WriteToAny<T>(ChannelCallback<T> callback, T value, params IContinuationChannel<T>[] channels)
		{
			WriteToAny(callback, value, channels.AsEnumerable(), Timeout.Infinite, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void WriteToAny<T>(ChannelCallback<T> callback, T value, IEnumerable<IContinuationChannel<T>> channels, TimeSpan timeout)
		{
			WriteToAny(callback, value, channels, timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void WriteToAny<T>(ChannelCallback<T> callback, T value, IEnumerable<IContinuationChannel<T>> channels, MultiChannelPriority priority)
		{
			WriteToAny(callback, value, channels, Timeout.Infinite, priority);
		}
		#endregion

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void ReadFromAny<T>(ChannelCallback<T> callback, IEnumerable<IContinuationChannel<T>> channels, TimeSpan timeout, MultiChannelPriority priority)
		{
			// We only accept the first offer
			var offer = new SingleOffer();

			switch (priority)
			{
				case MultiChannelPriority.Fair:
					throw new Exception(string.Format("Construct a {0} object to use fair multichannel reads", typeof(MultiChannelSet<>).Name));
				case MultiChannelPriority.Random:
					channels = ShuffleList(channels);
					break;
				default:
					// Use the order the input has
					break;
			}

			// Then we register the intent to read from a channel in order
			foreach (var c in channels)
			{
				c.RegisterRead(offer, callback, timeout);

				// Fast exit to avoid littering the channels if we are done
				if (offer.IsTaken)
					return;
			}
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void WriteToAny<T>(ChannelCallback<T> callback, T value, IEnumerable<IContinuationChannel<T>> channels, TimeSpan timeout, MultiChannelPriority priority = MultiChannelPriority.Any)
		{
			// We only accept the first offer
			var offer = new SingleOffer();

			switch (priority)
			{
				case MultiChannelPriority.Fair:
					throw new Exception(string.Format("Construct a {0} object to use fair multichannel writes", typeof(MultiChannelSet<>).Name));
				case MultiChannelPriority.Random:
					channels = ShuffleList(channels);
					break;
				default:
					// Use the order the input has
					break;
			}

			// Then we register the intent to read from a channel in order
			foreach (var c in channels)
			{
				c.RegisterWrite(offer, callback, value, timeout);

				// Fast exit to avoid littering the channels if we are done
				if (offer.IsTaken)
					return;
			}
		}

		/// <summary>
		/// Takes an IEnumerable and returns it in random order
		/// </summary>
		/// <returns>The shuffled list</returns>
		/// <param name="source">The input source</param>
		/// <typeparam name="T">The IEnumerable type.</typeparam>
		private static IEnumerable<T> ShuffleList<T>(IEnumerable<T> source)
		{
			var buffer = source.ToArray();
			var rng = new Random();

			for (int i = 0; i < buffer.Length; i++)
			{
				int j = rng.Next(i, buffer.Length);
				yield return buffer[j];

				buffer[j] = buffer[i];
			}
		}
	}
}

