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
	public struct MultisetRequest<T> : IMultisetRequestUntyped
	{
		/// <summary>
		/// The result value
		/// </summary>
		public T Value;
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

		#region IMultisetRequestUntyped implementation

		/// <summary>
		/// Gets the boxed value.
		/// </summary>
		/// <value>The value.</value>
		object IMultisetRequestUntyped.Value 
		{
			get { return this.Value; }
			set 
			{ 
				if (typeof(T).IsValueType && value == null)
					this.Value = default(T);
				else
					this.Value = (T)value; 
			}
		}

		/// <summary>
		/// Gets the channel.
		/// </summary>
		/// <value>The channel.</value>
		IUntypedChannel IMultisetRequestUntyped.Channel
		{
			get { return this.ReadChannel == null ? (IUntypedChannel)this.WriteChannel : (IUntypedChannel)this.ReadChannel; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is a read request.
		/// </summary>
		/// <value><c>true</c> if this instance is read request; otherwise, <c>false</c>.</value>
		bool IMultisetRequestUntyped.IsRead
		{
			get { return this.IsRead; }
		}

		#endregion
	}

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

	/// <summary>
	/// Helper class for performing multi-channel access
	/// </summary>
	public static class MultiChannelAccess
	{
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
		/// <param name="singleOperationType">True if there is are only reads or writes in the queue</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		private static Task<MultisetRequest<T>> ReadOrWriteAnyAsync<T>(Action<object> callback, IEnumerable<MultisetRequest<T>> requests, TimeSpan timeout, MultiChannelPriority priority)
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
					requests = ShuffleList(requests);
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

