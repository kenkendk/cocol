using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoCoL
{
	public struct MultisetResult<T>
	{
		/// <summary>
		/// The result value
		/// </summary>
		public readonly T Value;
		/// <summary>
		/// The channel being read from
		/// </summary>
		public readonly IChannel<T> Channel;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultisetResult`1"/> struct.
		/// </summary>
		/// <param name="value">The value read</param>
		/// <param name="channel">The channel read from</param>
		public MultisetResult(T value, IChannel<T> channel)
		{
			Value = value;
			Channel = channel;
		}
	}

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
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(TimeSpan timeout, MultiChannelPriority priority, params IChannel<T>[] channels)
		{
			return ReadFromAnyAsync(channels.AsEnumerable(), timeout, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(TimeSpan timeout, params IChannel<T>[] channels)
		{
			return ReadFromAnyAsync(channels.AsEnumerable(), timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(MultiChannelPriority priority, params IChannel<T>[] channels)
		{
			return ReadFromAnyAsync(channels.AsEnumerable(), Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(params IChannel<T>[] channels)
		{
			return ReadFromAnyAsync(channels.AsEnumerable(), Timeout.Infinite, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(IEnumerable<IChannel<T>> channels, TimeSpan timeout)
		{
			return ReadFromAnyAsync(channels, timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(IEnumerable<IChannel<T>> channels, MultiChannelPriority priority)
		{
			return ReadFromAnyAsync(channels, Timeout.Infinite, priority);
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
		public static Task<IChannel<T>> WriteToAnyAsync<T>(T value, TimeSpan timeout, MultiChannelPriority priority, params IChannel<T>[] channels)
		{
			return WriteToAnyAsync(value, channels.AsEnumerable(), timeout, priority);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IChannel<T>> WriteToAnyAsync<T>(T value, TimeSpan timeout, params IChannel<T>[] channels)
		{
			return WriteToAnyAsync(value, channels.AsEnumerable(), timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IChannel<T>> WriteToAnyAsync<T>(T value, MultiChannelPriority priority, params IChannel<T>[] channels)
		{
			return WriteToAnyAsync(value, channels.AsEnumerable(), Timeout.Infinite, priority);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IChannel<T>> WriteToAnyAsync<T>(T value, params IChannel<T>[] channels)
		{
			return WriteToAnyAsync(value, channels.AsEnumerable(), Timeout.Infinite, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IChannel<T>> WriteToAnyAsync<T>(T value, IEnumerable<IChannel<T>> channels, TimeSpan timeout)
		{
			return WriteToAnyAsync(value, channels, timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IChannel<T>> WriteToAnyAsync<T>(T value, IEnumerable<IChannel<T>> channels, MultiChannelPriority priority)
		{
			return WriteToAnyAsync(value, channels, Timeout.Infinite, priority);
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
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(IEnumerable<IChannel<T>> channels, TimeSpan timeout, MultiChannelPriority priority)
		{
			// We only accept the first offer
			var tcs = new TaskCompletionSource<MultisetResult<T>>();
			var offer = new SingleOffer<MultisetResult<T>>(tcs, timeout == Timeout.Infinite ? Timeout.InfiniteDateTime : DateTime.Now + timeout);

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

			// Keep a map of awaitable items
			var tasks = new Dictionary<Task<T>, IChannel<T>>();

			// Then we register the intent to read from a channel in order
			foreach (var c in channels)
			{
				// Timeout is handled by offer instance
				tasks[c.ReadAsync(offer, Timeout.Infinite)] = c;

				// Fast exit to avoid littering the channels if we are done
				if (offer.IsTaken)
					break;
			}

			offer.ProbePhaseComplete();

			Task.WhenAny(tasks.Keys).ContinueWith(item => Task.Run(() =>
				{
					if (offer.AtomicIsFirst())
					{
						var n = item.Result;

						// Figure out which item was found
						if (n.IsCanceled)
							tcs.SetCanceled();
						else if (n.IsFaulted)
							tcs.SetException(n.Exception);
						else
							tcs.SetResult(new MultisetResult<T>(n.Result, tasks[n]));
					}
				}));
			
			return tcs.Task;
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
		public static Task<IChannel<T>> WriteToAnyAsync<T>(T value, IEnumerable<IChannel<T>> channels, TimeSpan timeout, MultiChannelPriority priority = MultiChannelPriority.Any)
		{
			// We only accept the first offer
			var tcs = new TaskCompletionSource<IChannel<T>>();
			var offer = new SingleOffer<IChannel<T>>(tcs, timeout == Timeout.Infinite ? Timeout.InfiniteDateTime : DateTime.Now + timeout);

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

			// Keep a map of awaitable items
			var tasks = new Dictionary<Task, IChannel<T>>();

			// Then we register the intent to read from a channel in order
			foreach (var c in channels)
			{
				// Timeout is handled by offer instance
				tasks[c.WriteAsync(offer, value, Timeout.Infinite)] = c;

				// Fast exit to avoid littering the channels if we are done
				if (offer.IsTaken)
					break;
			}

			offer.ProbePhaseComplete();
			Task.WhenAny(tasks.Keys).ContinueWith(item => Task.Run(() =>
				{
					if (offer.AtomicIsFirst())
					{
						var n = item.Result;

						// Figure out which item was found
						if (n.IsCanceled)
							tcs.SetCanceled();
						else if (n.IsFaulted)
							tcs.SetException(n.Exception);
						else
							tcs.SetResult(tasks[n]);
					}
				}));
			
			return tcs.Task;
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

