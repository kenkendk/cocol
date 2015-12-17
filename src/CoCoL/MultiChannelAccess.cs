using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// Result from a mult-channel operation
	/// </summary>
	public struct MultisetResult<T>
	{
		/// <summary>
		/// The result value
		/// </summary>
		public readonly T Value;
		/// <summary>
		/// The channel being read from
		/// </summary>
		public readonly IReadChannel<T> Channel;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultisetResult`1"/> struct.
		/// </summary>
		/// <param name="value">The value read</param>
		/// <param name="channel">The channel read from</param>
		public MultisetResult(T value, IReadChannel<T> channel)
		{
			Value = value;
			Channel = channel;
		}
	}

	/// <summary>
	/// A request in a multi-channel operation
	/// </summary>
	public struct MultisetRequest<T>
	{
		/// <summary>
		/// The result value
		/// </summary>
		public readonly T Value;
		/// <summary>
		/// The channel being read from
		/// </summary>
		public readonly IReadChannel<T> ReadChannel;
		/// <summary>
		/// The channel being written to
		/// </summary>
		public readonly IWriteChannel<T> WriteChannel;
		/// <summary>
		/// Gets a value indicating if this is a read operation
		/// </summary>
		public readonly bool IsRead;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultisetRequest`1"/> struct.
		/// </summary>
		/// <param name="value">The value to write</param>
		/// <param name="channel">The channel to read or write.</param>
		/// <param name="read"><c>true</c> if this is a read operation, <c>false</c> otherwise</param>
		internal MultisetRequest(T value, IReadChannel<T> readChannel, IWriteChannel<T> writeChannel, bool read)
		{
			Value = value;
			ReadChannel = readChannel;
			WriteChannel = writeChannel;
			IsRead = read;
		}

		/// <summary>
		/// Constructs a request to read a channel.
		/// </summary>
		/// <param name="channel">The channel to read from.</param>
		public static MultisetRequest<T> Read(IReadChannel<T> channel)
		{
			return new MultisetRequest<T>(default(T), channel, null, true);
		}

		/// <summary>
		/// Constructs a request to write a channel.
		/// </summary>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channel">The channel to write to.</param>
		public static MultisetRequest<T> Write(T value, IWriteChannel<T> channel)
		{
			return new MultisetRequest<T>(value, null, channel, false);
		}
	}

	/// <summary>
	/// A request in a multi-channel operation
	/// </summary>
	public struct MultisetRequestUntyped
	{
		/// <summary>
		/// The result value
		/// </summary>
		public readonly object Value;
		/// <summary>
		/// The channel being read from
		/// </summary>
		public readonly IUntypedChannel Channel;
		/// <summary>
		/// Gets a value indicating if this is a read operation
		/// </summary>
		public readonly bool IsRead;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultisetRequest`1"/> struct.
		/// </summary>
		/// <param name="value">The value to write</param>
		/// <param name="channel">The channel to read or write.</param>
		/// <param name="read"><c>true</c> if this is a read operation, <c>false</c> otherwise</param>
		internal MultisetRequestUntyped(object value, IUntypedChannel channel, bool read)
		{
			Value = value;
			Channel = channel;
			IsRead = read;
		}

		public static MultisetRequestUntyped Read(IUntypedChannel channel)
		{
			return new MultisetRequestUntyped(null, channel, true);
		}

		public static MultisetRequestUntyped Write(object value, IUntypedChannel channel)
		{
			return new MultisetRequestUntyped(value, channel, true);
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
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(TimeSpan timeout, MultiChannelPriority priority, params IReadChannel<T>[] channels)
		{
			return ReadFromAnyAsync(null, channels.AsEnumerable(), timeout, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(TimeSpan timeout, params IReadChannel<T>[] channels)
		{
			return ReadFromAnyAsync(null, channels.AsEnumerable(), timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(MultiChannelPriority priority, params IReadChannel<T>[] channels)
		{
			return ReadFromAnyAsync(null, channels.AsEnumerable(), Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(params IReadChannel<T>[] channels)
		{
			return ReadFromAnyAsync(null, channels.AsEnumerable(), Timeout.Infinite, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(IEnumerable<IReadChannel<T>> channels, TimeSpan timeout)
		{
			return ReadFromAnyAsync(null, channels, timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(IEnumerable<IReadChannel<T>> channels, MultiChannelPriority priority)
		{
			return ReadFromAnyAsync(null, channels, Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(IEnumerable<IReadChannel<T>> channels, TimeSpan timeout, MultiChannelPriority priority)
		{
			return ReadFromAnyAsync(null, channels, timeout, priority);
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
		public static Task<IWriteChannel<T>> WriteToAnyAsync<T>(T value, TimeSpan timeout, MultiChannelPriority priority, params IWriteChannel<T>[] channels)
		{
			return WriteToAnyAsync(null, value, channels.AsEnumerable(), timeout, priority);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IWriteChannel<T>> WriteToAnyAsync<T>(T value, TimeSpan timeout, params IWriteChannel<T>[] channels)
		{
			return WriteToAnyAsync(null, value, channels.AsEnumerable(), timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IWriteChannel<T>> WriteToAnyAsync<T>(T value, MultiChannelPriority priority, params IWriteChannel<T>[] channels)
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
		public static Task<IWriteChannel<T>> WriteToAnyAsync<T>(T value, params IWriteChannel<T>[] channels)
		{
			return WriteToAnyAsync(null, value, channels.AsEnumerable(), Timeout.Infinite, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IWriteChannel<T>> WriteToAnyAsync<T>(T value, IEnumerable<IWriteChannel<T>> channels, TimeSpan timeout)
		{
			return WriteToAnyAsync(null, value, channels, timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IWriteChannel<T>> WriteToAnyAsync<T>(T value, IEnumerable<IWriteChannel<T>> channels, MultiChannelPriority priority)
		{
			return WriteToAnyAsync(null, value, channels, Timeout.Infinite, priority);
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
		public static Task<IWriteChannel<T>> WriteToAnyAsync<T>(T value, IEnumerable<IWriteChannel<T>> channels, TimeSpan timeout, MultiChannelPriority priority)
		{
			return WriteToAnyAsync(null, value, channels, timeout, priority);
		}
		#endregion

		#region Overloads for setting default parameters in the readorwrite method
		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(TimeSpan timeout, MultiChannelPriority priority, params MultisetRequest<T>[] channels)
		{
			return ReadOrWriteAnyAsync(null, channels.AsEnumerable(), timeout, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(TimeSpan timeout, params MultisetRequest<T>[] channels)
		{
			return ReadOrWriteAnyAsync(null, channels.AsEnumerable(), timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(MultiChannelPriority priority, params MultisetRequest<T>[] channels)
		{
			return ReadOrWriteAnyAsync(null, channels.AsEnumerable(), Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(params MultisetRequest<T>[] channels)
		{
			return ReadOrWriteAnyAsync(null, channels.AsEnumerable(), Timeout.Infinite, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(IEnumerable<MultisetRequest<T>> channels, TimeSpan timeout)
		{
			return ReadOrWriteAnyAsync(null, channels, timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(IEnumerable<MultisetRequest<T>> channels, MultiChannelPriority priority)
		{
			return ReadOrWriteAnyAsync(null, channels, Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(IEnumerable<MultisetRequest<T>> channels, TimeSpan timeout, MultiChannelPriority priority)
		{
			return ReadOrWriteAnyAsync(null, channels, timeout, priority);
		}
		#endregion
		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call when the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static async Task<MultisetResult<T>> ReadFromAnyAsync<T>(Action<object> callback, IEnumerable<IReadChannel<T>> channels, TimeSpan timeout, MultiChannelPriority priority)
		{
			var res = await ReadOrWriteAnyAsync<T>(callback, channels.Select(x => MultisetRequest<T>.Read(x)), timeout, priority);
			return new MultisetResult<T>(res.Value, res.ReadChannel);
		}
			
		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call when the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static async Task<IWriteChannel<T>> WriteToAnyAsync<T>(Action<object> callback, T value, IEnumerable<IWriteChannel<T>> channels, TimeSpan timeout, MultiChannelPriority priority)
		{
			return (await ReadOrWriteAnyAsync<T>(callback, channels.Select(x => MultisetRequest<T>.Write(value, x)), timeout, priority)).WriteChannel;
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call when the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(Action<object> callback, IEnumerable<MultisetRequest<T>> requests, TimeSpan timeout, MultiChannelPriority priority)
		{
			var tcs = new TaskCompletionSource<MultisetRequest<T>>();

			// We only accept the first offer
			var offer = new SingleOffer<MultisetRequest<T>>(tcs, timeout == Timeout.Infinite ? Timeout.InfiniteDateTime : DateTime.Now + timeout);
			offer.SetCommitCallback(callback);

			switch (priority)
			{
				case MultiChannelPriority.Fair:
					throw new Exception(string.Format("Construct a {0} object to use fair multichannel operations", typeof(MultiChannelSet<>).Name));
				case MultiChannelPriority.Random:
					requests = ShuffleList(requests);
					break;
				default:
					// Use the order the input has
					break;
			}

			// Keep a map of awaitable items
			var tasks = new Dictionary<Task, MultisetRequest<T>>();

			// Then we register the intent to read from a channel in order
			foreach (var c in requests)
			{
				// Timeout is handled by offer instance
				if (c.IsRead)
					tasks[c.ReadChannel.ReadAsync(offer, Timeout.Infinite)] = c;
				else
					tasks[c.WriteChannel.WriteAsync(offer, c.Value, Timeout.Infinite)] = c;

				// Fast exit to avoid littering the channels if we are done
				if (offer.IsTaken)
					break;
			}

			offer.ProbePhaseComplete();

			Task.WhenAny(tasks.Keys).ContinueWith((Task<Task> item) => Task.Run(() =>
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
						{
							var orig = tasks[n];
							if (orig.IsRead)
								tcs.SetResult(new MultisetRequest<T>(((Task<T>)n).Result, orig.ReadChannel, null, true));
							else
								tcs.SetResult(new MultisetRequest<T>(default(T), null, orig.WriteChannel, false));
						}
					}
				}));

			return tcs.Task;
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call when the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequestUntyped> ReadOrWriteFromAnyUntypedAsync(Action<object> callback, IEnumerable<MultisetRequestUntyped> requests, TimeSpan timeout, MultiChannelPriority priority)
		{
			var tcs = new TaskCompletionSource<MultisetRequestUntyped>();

			// We only accept the first offer
			var offer = new SingleOffer<MultisetRequestUntyped>(tcs, timeout == Timeout.Infinite ? Timeout.InfiniteDateTime : DateTime.Now + timeout);
			offer.SetCommitCallback(callback);

			switch (priority)
			{
				case MultiChannelPriority.Fair:
					throw new Exception(string.Format("Construct a {0} object to use fair multichannel operations", typeof(MultiChannelSet<>).Name));
				case MultiChannelPriority.Random:
					requests = ShuffleList(requests);
					break;
				default:
					// Use the order the input has
					break;
			}

			// Keep a map of awaitable items
			var tasks = new Dictionary<Task, MultisetRequestUntyped>();

			// Then we register the intent to read from a channel in order
			foreach (var c in requests)
			{
				// Timeout is handled by offer instance
				if (c.IsRead)
					tasks[c.Channel.ReadAsync(offer, Timeout.Infinite)] = c;
				else
					tasks[c.Channel.WriteAsync(offer, c.Value, Timeout.Infinite)] = c;

				// Fast exit to avoid littering the channels if we are done
				if (offer.IsTaken)
					break;
			}

			offer.ProbePhaseComplete();

			Task.WhenAny(tasks.Keys).ContinueWith((Task<Task> item) => Task.Run(() =>
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
						{
							var orig = tasks[n];
							if (orig.IsRead)
								tcs.SetResult(new MultisetRequestUntyped(((Task<object>)n).Result, orig.Channel, true));
							else
								tcs.SetResult(new MultisetRequestUntyped(null, orig.Channel, false));
						}
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

