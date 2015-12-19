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
	/// Helper class for performing multi-channel access
	/// </summary>
	public static class MultiChannelAccess
	{
		#region Creating requests from the channel
		/// <summary>
		/// Creates a read request for the given channel.
		/// </summary>
		/// <param name="self">The channel to request the read from.</param>
		/// <typeparam name="T">The type of the channel.</typeparam>
		/// <returns>The created request</returns>
		public static MultisetRequest<T> RequestRead<T>(this IReadChannel<T> self)
		{
			return MultisetRequest<T>.Read(self);
		}

		/// <summary>
		/// Create a write request for the given channel.
		/// </summary>
		/// <param name="self">The channel to request the write to.</param>
		/// <param name="value">The value to write.</param>
		/// <typeparam name="T">The type of the channel.</typeparam>
		/// <returns>The created request</returns>
		public static MultisetRequest<T> RequestWrite<T>(this IWriteChannel<T> self, T value)
		{
			return MultisetRequest<T>.Write(value, self);
		}

		/// <summary>
		/// Creates a read request for the given channel.
		/// </summary>
		/// <param name="self">The channel to request the read from.</param>
		/// <returns>The created request</returns>
		public static IMultisetRequestUntyped RequestRead(this IUntypedChannel self)
		{
			var readinterface = self.ReadInterface();
			var mr = typeof(MultisetRequest<>).MakeGenericType(readinterface.GetGenericArguments());
			return (IMultisetRequestUntyped)mr.GetMethod("Read", new Type[] { readinterface }).Invoke(null, new object[] { self });
		}

		/// <summary>
		/// Create a write request for the given channel.
		/// </summary>
		/// <param name="self">The channel to request the write to.</param>
		/// <param name="value">The value to write.</param>
		/// <returns>The created request</returns>
		public static IMultisetRequestUntyped RequestWrite(this IUntypedChannel self, object value)
		{
			var writeinterface = self.WriteInterface();
			var mr = typeof(MultisetRequest<>).MakeGenericType(writeinterface.GetGenericArguments());
			return (IMultisetRequestUntyped)mr.GetMethod("Write", new Type[] { writeinterface.GetGenericArguments()[0], writeinterface }).Invoke(null, new object[] { value, self });
		}
		#endregion


		#region Creating channel sets from lists of channels
		/// <summary>
		/// Creates a multichannelset from a list of channels
		/// </summary>
		/// <returns>The multichannel set.</returns>
		/// <param name="channels">The channels to make the set from.</param>
		/// <param name="priority">The channel priority.</param>
		/// <typeparam name="T">The type of the channel.</typeparam>
		public static MultiChannelSet<T> CreateSet<T>(this IEnumerable<IChannel<T>> channels, MultiChannelPriority priority = MultiChannelPriority.Any)
		{
			return new MultiChannelSet<T>(channels, priority);
		}
		#endregion

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
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(this IEnumerable<IReadChannel<T>> channels, TimeSpan timeout)
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
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(this IEnumerable<IReadChannel<T>> channels, MultiChannelPriority priority)
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
		public static Task<MultisetResult<T>> ReadFromAnyAsync<T>(this IEnumerable<IReadChannel<T>> channels, TimeSpan timeout, MultiChannelPriority priority)
		{
			return ReadFromAnyAsync(null, channels, timeout, priority);
		}

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
			var res = await ReadOrWriteAnyAsync<T>(callback, channels.Select(x => x.RequestRead()), timeout, priority);
			return new MultisetResult<T>(res.Value, res.ReadChannel);
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

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IWriteChannel<T>> WriteToAnyAsync<T>(this IEnumerable<IWriteChannel<T>> channels, T value, TimeSpan timeout)
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
		public static Task<IWriteChannel<T>> WriteToAnyAsync<T>(this IEnumerable<IWriteChannel<T>> channels, T value, MultiChannelPriority priority)
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
		public static Task<IWriteChannel<T>> WriteToAnyAsync<T>(this IEnumerable<IWriteChannel<T>> channels, T value, TimeSpan timeout, MultiChannelPriority priority)
		{
			return WriteToAnyAsync(null, value, channels, timeout, priority);
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
			return (await ReadOrWriteAnyAsync<T>(callback, channels.Select(x => x.RequestWrite(value)), timeout, priority)).WriteChannel;
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
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(TimeSpan timeout, MultiChannelPriority priority, params MultisetRequest<T>[] requests)
		{
			return ReadOrWriteAnyAsync(null, requests.AsEnumerable(), timeout, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(TimeSpan timeout, params MultisetRequest<T>[] requests)
		{
			return ReadOrWriteAnyAsync(null, requests.AsEnumerable(), timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(MultiChannelPriority priority, params MultisetRequest<T>[] requests)
		{
			return ReadOrWriteAnyAsync(null, requests.AsEnumerable(), Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(params MultisetRequest<T>[] requests)
		{
			return ReadOrWriteAnyAsync(null, requests.AsEnumerable(), Timeout.Infinite, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(this IEnumerable<MultisetRequest<T>> requests, TimeSpan timeout)
		{
			return ReadOrWriteAnyAsync(null, requests, timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(this IEnumerable<MultisetRequest<T>> requests, MultiChannelPriority priority)
		{
			return ReadOrWriteAnyAsync(null, requests, Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(this IEnumerable<MultisetRequest<T>> requests, TimeSpan timeout, MultiChannelPriority priority)
		{
			return ReadOrWriteAnyAsync(null, requests, timeout, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call when the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		private static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(this IEnumerable<MultisetRequest<T>> requests, Action<object> callback, TimeSpan timeout, MultiChannelPriority priority)
		{
			return ReadOrWriteAnyAsync(callback, requests, timeout, priority);
		}
		#endregion


		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call when the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <param name="singleOperationType">True if there is are only reads or writes in the queue</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		private static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(Action<object> callback, IEnumerable<MultisetRequest<T>> requests, TimeSpan timeout, MultiChannelPriority priority)
		{
			// This method could also use the untyped version, 
			// but using the type version is faster as there are no reflection
			// or boxing/typecasting required 

			var tcs = new TaskCompletionSource<MultisetRequest<T>>();

			// We only accept the first offer
			var offer = new SingleOffer<MultisetRequest<T>>(tcs, timeout == Timeout.Infinite ? Timeout.InfiniteDateTime : DateTime.Now + timeout);
			offer.SetCommitCallback(callback);

			switch (priority)
			{
				case MultiChannelPriority.Fair:
					throw new Exception(string.Format("Construct a {0} object to use fair multichannel operations", typeof(MultiChannelSet<>).Name));
				case MultiChannelPriority.Random:
					requests = Shuffle(requests);
					break;
				default:
					// Use the order the input has
					break;
			}

			// Keep a map of awaitable items
			// and register the intent to read from a channel in order
			var tasks = new Dictionary<Task, MultisetRequest<T>>();
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
						{
							// Unwrap aggregate exceptions
							if (n.Exception is AggregateException && (n.Exception as AggregateException).Flatten().InnerExceptions.Count == 1)
								tcs.SetException(n.Exception.InnerException);
							else
								tcs.SetException(n.Exception);
						}
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
		/// Takes an IEnumerable and returns it in random order
		/// </summary>
		/// <returns>The shuffled list</returns>
		/// <param name="source">The input source</param>
		/// <typeparam name="T">The IEnumerable type.</typeparam>
		public static IEnumerable<T> Shuffle<T>(IEnumerable<T> source)
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

