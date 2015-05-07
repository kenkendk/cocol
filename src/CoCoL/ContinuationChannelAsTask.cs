using System;
using System.Threading.Tasks;

namespace CoCoL
{
	public static class ContinuationChannelAsTask
	{

		/// <summary>
		/// Read from the channel with a Task result
		/// </summary>
		/// <returns>The async task result</returns>
		/// <param name="self">The channel to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task<T> ReadAsync<T>(this IChannel<T> self)
		{
			return ReadAsync(self, Timeout.Infinite);
		}

		/// <summary>
		/// Read from the channel with a Task result
		/// </summary>
		/// <returns>The async task result</returns>
		/// <param name="self">The channel to read from</param>
		/// <param name="timeout">The maimum time to wait for a value</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task<T> ReadAsync<T>(this IChannel<T> self, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<T>();
			self.RegisterRead(new ChannelCallback<T>(x => {
				if (x.Exception != null)
					tcs.SetException(x.Exception);
				else
					tcs.SetResult(x.Result);
			}), timeout);
			return tcs.Task;
		}

		/// <summary>
		/// Write to the channel in an async manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <returns>The async task result</returns>
		/// <param name="self">The channel to write to</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task WriteAsync<T>(this IChannel<T> self, T value)
		{
			return WriteAsync(self, value, Timeout.Infinite);
		}

		/// <summary>
		/// Write to the channel in an async manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <returns>The async task result</returns>
		/// <param name="self">The channel to write to</param>
		/// <param name="timeout">The maimum time to wait for a slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task WriteAsync<T>(this IChannel<T> self, T value, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<bool>();
			self.RegisterWrite(new ChannelCallback<T>(x => {
				if (x.Exception != null)
					tcs.SetException(x.Exception);
				else
					tcs.SetResult(true);
			}), value, timeout);
			return tcs.Task;
		}

		/// <summary>
		/// Read from the channel set with a Task result
		/// </summary>
		/// <returns>The async task result</returns>
		/// <param name="self">The channels to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task<T> ReadFromAnyAsync<T>(this MultiChannelSet<T> self)
		{
			return ReadFromAnyAsync(self, Timeout.Infinite);
		}

		/// <summary>
		/// Read from the channel set with a Task result
		/// </summary>
		/// <returns>The async task result</returns>
		/// <param name="self">The channels to read from</param>
		/// <param name="timeout">The maimum time to wait for a value</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task<T> ReadFromAnyAsync<T>(this MultiChannelSet<T> self, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<T>();
			self.ReadFromAny(new ChannelCallback<T>(x => {
				if (x.Exception != null)
					tcs.SetException(x.Exception);
				else
					tcs.SetResult(x.Result);
			}), timeout);
			return tcs.Task;
		}

		/// <summary>
		/// Read from the channel set with a Task result.
		/// The returned data is the full completion data.
		/// </summary>
		/// <returns>The async task result</returns>
		/// <param name="self">The channels to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task<ICallbackResult<T>> ReadFromAnyExtendedAsync<T>(this MultiChannelSet<T> self)
		{
			return ReadFromAnyExtendedAsync(self, Timeout.Infinite);
		}

		/// <summary>
		/// Read from the channel set with a Task result.
		/// The returned data is the full completion data.
		/// </summary>
		/// <returns>The async task result</returns>
		/// <param name="self">The channels to read from</param>
		/// <param name="timeout">The maimum time to wait for a value</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task<ICallbackResult<T>> ReadFromAnyExtendedAsync<T>(this MultiChannelSet<T> self, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<ICallbackResult<T>>();
			self.ReadFromAny(new ChannelCallback<T>(x => {
				tcs.SetResult(x);
			}), timeout);
			return tcs.Task;
		}

		/// <summary>
		/// Write to the channel set in an async manner
		/// </summary>
		/// <returns>The async task result</returns>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channels to write to</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task WriteToAnyAsync<T>(this MultiChannelSet<T> self, T value)
		{
			return WriteToAnyAsync(self, value, Timeout.Infinite);
		}

		/// <summary>
		/// Write to the channel set in an async manner
		/// </summary>
		/// <returns>The async task result</returns>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channels to write to</param>
		/// <param name="timeout">The maimum time to wait for a slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task WriteToAnyAsync<T>(this MultiChannelSet<T> self, T value, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<bool>();
			self.WriteToAny(new ChannelCallback<T>(x => {
				if (x.Exception != null)
					tcs.SetException(x.Exception);
				else
					tcs.SetResult(true);
			}), value, timeout);
			return tcs.Task;
		}

		/// <summary>
		/// Write to the channel set in an async manner
		/// </summary>
		/// <returns>The async task result</returns>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channels to write to</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task<ICallbackResult<T>> WriteToAnyExtendedAsync<T>(this MultiChannelSet<T> self, T value)
		{
			return WriteToAnyExtendedAsync(self, value, Timeout.Infinite);
		}

		/// <summary>
		/// Write to the channel set in an async manner
		/// </summary>
		/// <returns>The async task result</returns>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channels to write to</param>
		/// <param name="timeout">The maimum time to wait for a slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static Task<ICallbackResult<T>> WriteToAnyExtendedAsync<T>(this MultiChannelSet<T> self, T value, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<ICallbackResult<T>>();
			self.WriteToAny(new ChannelCallback<T>(x => {
				tcs.SetResult(x);
			}), value, timeout);
			return tcs.Task;
		}
	}
}

