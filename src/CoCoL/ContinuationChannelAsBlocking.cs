using System;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// Helper for treating a continuation channel as a blocking channel
	/// </summary>
	public static class ContinuationChannelAsBlocking
	{
		/// <summary>
		/// Read from the channel in a blocking manner
		/// </summary>
		/// <param name="self">The channel to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from the channel</returns>
		public static T Read<T>(this IReadChannel<T> self)
		{
			return self.ReadAsync(Timeout.Infinite).Result;
		}

		/// <summary>
		/// Read from the channel in a blocking manner
		/// </summary>
		/// <param name="self">The channel to read from</param>
		/// <param name="timeout">The maximum time to wait for a value</param>
		/// <returns>>The value read from the channel</returns>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static T Read<T>(this IReadChannel<T> self, TimeSpan timeout)
		{
			return self.ReadAsync(timeout).Result;
		}

		/// <summary>
		/// Read from the channel in a probing manner
		/// </summary>
		/// <param name="self">The channel to read from</param>
		/// <param name="result">The read result</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>True if the read succeeded, false otherwise</returns>
		public static bool TryRead<T>(this IReadChannel<T> self, out T result)
		{
			var res = self.ReadAsync(Timeout.Immediate);

			if (res.Exception != null)
			{
				result = default(T);
				return false;
			}
			else
			{
				result = res.Result;
				return true;
			}
		}

		/// <summary>
		/// Write to the channel in a blocking manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <param name="timeout">The maximum time to wait for an available slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static void Write<T>(this IWriteChannel<T> self, T value)
		{
			var res = self.WriteAsync(value, Timeout.Infinite);
			res.Wait();

			if (res.Exception != null)
				throw res.Exception;
		}

		/// <summary>
		/// Write to the channel in a blocking manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <param name="timeout">The maximum time to wait for an available slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static void Write<T>(this IWriteChannel<T> self, T value, TimeSpan timeout)
		{
			var res = self.WriteAsync(value, timeout);
			res.Wait();

			if (res.Exception != null)
				throw res.Exception;
		}		

		/// <summary>
		/// Write to the channel in a probing manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>True if the write succeeded, false otherwise</returns>
		public static bool TryWrite<T>(this IWriteChannel<T> self, T value)
		{
			return self.WriteAsync(value, Timeout.Immediate).Exception == null;
		}

		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from a channel</returns>
		public static MultisetResult<T> ReadFromAny<T>(this MultiChannelSet<T> self)
		{
			return self.ReadFromAnyAsync(Timeout.Infinite).Result;
		}

		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from a channel</returns>
		public static T ReadFromAny<T>(this MultiChannelSet<T> self, out IChannel<T> channel)
		{
			var res = self.ReadFromAnyAsync(Timeout.Infinite).Result;
			channel = res.Channel;
			return res.Value;
		}

		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <param name="timeout">The maximum time to wait for a value</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from a channel</returns>
		public static T ReadFromAny<T>(this MultiChannelSet<T> self, out IChannel<T> channel, TimeSpan timeout)
		{
			var res = self.ReadFromAnyAsync(timeout).Result;
			channel = res.Channel;
			return res.Value;
		}

		/// <summary>
		/// Read from the channel set in a probing manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from a channel</returns>
		public static bool TryReadFromAny<T>(this MultiChannelSet<T> self, out T value, out IChannel<T> channel)
		{
			var res = self.ReadFromAnyAsync(Timeout.Immediate);

			// Make sure all is good
			res.Wait();

			if (res.Exception != null)
			{
				channel = null;
				value = default(T);
				return false;
			}
			else
			{
				channel = res.Result.Channel;
				value = res.Result.Value;
				return true;
			}
		}

		/// <summary>
		/// Read from the channel set in a probing manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from a channel</returns>
		public static bool TryReadFromAny<T>(this MultiChannelSet<T> self, out T value)
		{
			IChannel<T> dummy;
			return TryReadFromAny<T>(self, out value, out dummy);
		}

		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <param name="timeout">The maximum time to wait for a value</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from a channel</returns>
		public static MultisetResult<T> ReadFromAny<T>(this MultiChannelSet<T> self, TimeSpan timeout)
		{
			return self.ReadFromAnyAsync(timeout).Result;
		}

		/// <summary>
		/// Write to the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <param name="value">The value to write into the channel</param>
		public static IChannel<T> WriteToAny<T>(this MultiChannelSet<T> self, T value)
		{
			return WriteToAny(self, value, Timeout.Infinite);
		}

		/// <summary>
		/// Write to the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <param name="timeout">The maximum time to wait for a slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <param name="value">The value to write into the channel</param>
		public static IChannel<T> WriteToAny<T>(this MultiChannelSet<T> self, T value, TimeSpan timeout)
		{
			return self.WriteToAnyAsync(value, timeout).Result;
		}

		/// <summary>
		/// Read from the channel set in a probing manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <param name="timeout">The maximum time to wait for a slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <param name="value">The value to write into the channel</param>
		public static bool TryWriteToAny<T>(this MultiChannelSet<T> self, T value)
		{
			IChannel<T> dummy;
			return TryWriteToAny(self, value, out dummy);
		}

		/// <summary>
		/// Read from the channel set in a probing manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <param name="timeout">The maximum time to wait for a slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <param name="value">The value to write into the channel</param>
		public static bool TryWriteToAny<T>(this MultiChannelSet<T> self, T value, out IChannel<T> channel)
		{
			var res = self.WriteToAnyAsync(value, Timeout.Immediate);
			res.Wait();

			if (res.Exception == null)
			{
				channel = res.Result;
				return true;
			}
			else
			{
				channel = null;
				return false;
			}
		}
	}
}

