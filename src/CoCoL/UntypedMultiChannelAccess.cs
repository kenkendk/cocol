using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace CoCoL
{
	/// <summary>
	/// A request in an untyped (or multi-type) multi-channel operation
	/// </summary>
	public interface IMultisetRequestUntyped
	{
		/// <summary>
		/// The result value
		/// </summary>
		object Value { get; set; }
		/// <summary>
		/// The channel being read from
		/// </summary>
		IUntypedChannel Channel { get; }
		/// <summary>
		/// Gets a value indicating if this is a read operation
		/// </summary>
		bool IsRead { get; }
	}

	public static class UntypedMultiChannelAccess
	{
		#region Overloads for setting default parameters in the untyped read method
		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(TimeSpan timeout, MultiChannelPriority priority, params IUntypedChannel[] channels)
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
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(TimeSpan timeout, params IUntypedChannel[] channels)
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
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(MultiChannelPriority priority, params IUntypedChannel[] channels)
		{
			return ReadFromAnyAsync(null, channels.AsEnumerable(), Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(params IUntypedChannel[] channels)
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
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(this IEnumerable<IUntypedChannel> channels, TimeSpan timeout)
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
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(this IEnumerable<IUntypedChannel> channels, MultiChannelPriority priority)
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
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(this IEnumerable<IUntypedChannel> channels, TimeSpan timeout, MultiChannelPriority priority)
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
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(Action<object> callback, IEnumerable<IUntypedChannel> channels, TimeSpan timeout, MultiChannelPriority priority)
		{
			return ReadFromAnyAsync(callback, channels.Select(x => x.RequestRead()), timeout, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(TimeSpan timeout, MultiChannelPriority priority, params IMultisetRequestUntyped[] requests)
		{
			return ReadFromAnyAsync(null, requests.AsEnumerable(), timeout, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(TimeSpan timeout, params IMultisetRequestUntyped[] requests)
		{
			return ReadFromAnyAsync(null, requests.AsEnumerable(), timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(MultiChannelPriority priority, params IMultisetRequestUntyped[] requests)
		{
			return ReadFromAnyAsync(null, requests.AsEnumerable(), Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(params IMultisetRequestUntyped[] requests)
		{
			return ReadFromAnyAsync(null, requests.AsEnumerable(), Timeout.Infinite, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(this IEnumerable<IMultisetRequestUntyped> requests, TimeSpan timeout)
		{
			return ReadFromAnyAsync(null, requests, timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(this IEnumerable<IMultisetRequestUntyped> requests, MultiChannelPriority priority)
		{
			return ReadFromAnyAsync(null, requests, Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadFromAnyAsync(this IEnumerable<IMultisetRequestUntyped> requests, TimeSpan timeout, MultiChannelPriority priority)
		{
			return ReadFromAnyAsync(null, requests, timeout, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call when the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static async Task<IMultisetRequestUntyped> ReadFromAnyAsync(Action<object> callback, IEnumerable<IMultisetRequestUntyped> requests, TimeSpan timeout, MultiChannelPriority priority)
		{
			return (await ReadOrWriteAnyAsync(callback, requests, timeout, priority));
		}
		#endregion

		#region Overloads for setting default parameters in the untyped write method
		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IUntypedChannel> WriteToAnyAsync(TimeSpan timeout, MultiChannelPriority priority, params IMultisetRequestUntyped[] requests)
		{
			return WriteToAnyAsync(requests.AsEnumerable(), timeout, priority);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IUntypedChannel> WriteToAnyAsync(TimeSpan timeout, params IMultisetRequestUntyped[] requests)
		{
			return WriteToAnyAsync(requests.AsEnumerable(), timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IUntypedChannel> WriteToAnyAsync(MultiChannelPriority priority, params IMultisetRequestUntyped[] requests)
		{
			return WriteToAnyAsync(requests.AsEnumerable(), Timeout.Infinite, priority);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IUntypedChannel> WriteToAnyAsync(params IMultisetRequestUntyped[] requests)
		{
			return WriteToAnyAsync(requests.AsEnumerable(), Timeout.Infinite, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="timeout">The maximum time to wait for a channel to become ready for writing.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IUntypedChannel> WriteToAnyAsync(this IEnumerable<IMultisetRequestUntyped> requests, TimeSpan timeout)
		{
			return WriteToAnyAsync(requests, timeout, MultiChannelPriority.Any);
		}

		/// <summary>
		/// Writes to any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the write completes, or null.</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channels">The list of channels to attempt to write.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be written.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IUntypedChannel> WriteToAnyAsync(this IEnumerable<IMultisetRequestUntyped> requests, MultiChannelPriority priority)
		{
			return WriteToAnyAsync(requests, Timeout.Infinite, priority);
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
		public static async Task<IUntypedChannel> WriteToAnyAsync(this IEnumerable<IMultisetRequestUntyped> requests, TimeSpan timeout, MultiChannelPriority priority)
		{
			return (await ReadOrWriteAnyAsync(null, requests, timeout, priority)).Channel;
		}
		#endregion

		#region Overloads for setting default parameters in the untyped readorwrite method
		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select channels, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadOrWriteAnyAsync(TimeSpan timeout, MultiChannelPriority priority, params IMultisetRequestUntyped[] requests)
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
		public static Task<IMultisetRequestUntyped> ReadOrWriteAnyAsync(TimeSpan timeout, params IMultisetRequestUntyped[] requests)
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
		public static Task<IMultisetRequestUntyped> ReadOrWriteAnyAsync(MultiChannelPriority priority, params IMultisetRequestUntyped[] requests)
		{
			return ReadOrWriteAnyAsync(null, requests.AsEnumerable(), Timeout.Infinite, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call after the read completes.</param>
		/// <param name="channels">The list of channels to call.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadOrWriteAnyAsync(params IMultisetRequestUntyped[] requests)
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
		public static Task<IMultisetRequestUntyped> ReadOrWriteAnyAsync<T>(this IEnumerable<IMultisetRequestUntyped> requests, TimeSpan timeout)
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
		public static Task<IMultisetRequestUntyped> ReadOrWriteAnyAsync<T>(this IEnumerable<IMultisetRequestUntyped> requests, MultiChannelPriority priority)
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
		public static Task<IMultisetRequestUntyped> ReadOrWriteAnyAsync<T>(this IEnumerable<IMultisetRequestUntyped> requests, TimeSpan timeout, MultiChannelPriority priority)
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
		private static Task<IMultisetRequestUntyped> ReadOrWriteAnyAsync<T>(this IEnumerable<IMultisetRequestUntyped> requests, Action<object> callback, TimeSpan timeout, MultiChannelPriority priority)
		{
			return ReadOrWriteAnyAsync(callback, requests, timeout, priority);
		}

		/// <summary>
		/// Reads from any of the specified channels
		/// </summary>
		/// <param name="callback">The method to call when the read completes, or null.</param>
		/// <param name="channels">The list of channels to attempt to read from.</param>
		/// <param name="timeout">The maximum time to wait for a value to read.</param>
		/// <param name="priority">The priority used to select a channel, if multiple channels have a value that can be read.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		private static Task<IMultisetRequestUntyped> ReadOrWriteAnyAsync<T>(Action<object> callback, IEnumerable<IMultisetRequestUntyped> requests, TimeSpan timeout, MultiChannelPriority priority)
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
		/// <typeparam name="T">The channel data type.</typeparam>
		public static Task<IMultisetRequestUntyped> ReadOrWriteAnyAsync(Action<object> callback, IEnumerable<IMultisetRequestUntyped> requests, TimeSpan timeout, MultiChannelPriority priority)
		{
			var tcs = new TaskCompletionSource<IMultisetRequestUntyped>();

			// We only accept the first offer
			var offer = new SingleOffer<IMultisetRequestUntyped>(tcs, timeout == Timeout.Infinite ? Timeout.InfiniteDateTime : DateTime.Now + timeout);
			offer.SetCommitCallback(callback);

			switch (priority)
			{
				case MultiChannelPriority.Fair:
					throw new Exception(string.Format("Construct a {0} object to use fair multichannel operations", typeof(MultiChannelSet<>).Name));
				case MultiChannelPriority.Random:
					requests = MultiChannelAccess.Shuffle(requests);
					break;
				default:
					// Use the order the input has
					break;
			}

			// Keep a map of awaitable items
			var tasks = new Dictionary<Task, IMultisetRequestUntyped>();

			// Then we register the intent to read from a channel in order
			foreach (var c in requests)
			{
				// Timeout is handled by offer instance
				if (c.IsRead)
					tasks[c.Channel.ReadAsync(Timeout.Infinite, offer)] = c;
				else
					tasks[c.Channel.WriteAsync(c.Value, Timeout.Infinite, offer)] = c;

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
							{
								orig.Value = ((Task<object>)n).Result;
								tcs.SetResult(orig);
							}
							else
							{
								orig.Value = null;
								tcs.SetResult(orig);
							}
						}
					}
				}));

			return tcs.Task;
		}
	}
}

