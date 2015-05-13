using System;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// This static class provides various extension methods for
	/// simplifying the use of channels other than async
	/// </summary>
	public static class ChannelExtensions
	{
		#region Avoid compile warnings when using the write method in fire-n-forget mode
		/// <summary>
		/// Write to the channel in a blocking manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <param name="timeout">The maximum time to wait for an available slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static void WriteNoWait<T>(this IWriteChannel<T> self, T value)
		{
			self.WriteAsync(value, Timeout.Infinite);
		}

		/// <summary>
		/// Write to the channel in a blocking manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <param name="timeout">The maximum time to wait for an available slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static void WriteNoWait<T>(this IWriteChannel<T> self, T value, TimeSpan timeout)
		{
			self.WriteAsync(value, timeout);
		}
		#endregion

		#region Blocking channel usage
		/// <summary>
		/// Read from the channel in a blocking manner
		/// </summary>
		/// <param name="self">The channel to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from the channel</returns>
		public static T Read<T>(this IReadChannel<T> self)
		{
			try
			{
				return self.ReadAsync(Timeout.Infinite).Result;
			}
			catch(AggregateException aex)
			{
				if (aex.Flatten().InnerExceptions.Count == 1)
					throw aex.InnerException;
				
				throw;
			}
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
			try
			{
				return self.ReadAsync(timeout).Result;
			}
			catch(AggregateException aex)
			{
				if (aex.Flatten().InnerExceptions.Count == 1)
					throw aex.InnerException;

				throw;
			}
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
			{
				if (res.Exception is AggregateException && ((AggregateException)res.Exception).Flatten().InnerExceptions.Count == 1)
					throw ((AggregateException)res.Exception).InnerException;
				
				throw res.Exception;
			}
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
			{
				if (res.Exception is AggregateException && ((AggregateException)res.Exception).Flatten().InnerExceptions.Count == 1)
					throw ((AggregateException)res.Exception).InnerException;

				throw res.Exception;
			}
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
		#endregion

		#region Blocking multi-channel usage
		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from a channel</returns>
		public static MultisetResult<T> ReadFromAny<T>(this MultiChannelSet<T> self)
		{
			try
			{
				return self.ReadFromAnyAsync(Timeout.Infinite).Result;
			}
			catch(AggregateException aex)
			{
				if (aex.Flatten().InnerExceptions.Count == 1)
					throw aex.InnerException;

				throw;
			}
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
			try
			{
				var res = self.ReadFromAnyAsync(Timeout.Infinite).Result;
				channel = res.Channel;
				return res.Value;
			}
			catch(AggregateException aex)
			{
				if (aex.Flatten().InnerExceptions.Count == 1)
					throw aex.InnerException;

				throw;
			}

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
			try
			{
				var res = self.ReadFromAnyAsync(timeout).Result;
				channel = res.Channel;
				return res.Value;
			}
			catch(AggregateException aex)
			{
				if (aex.Flatten().InnerExceptions.Count == 1)
					throw aex.InnerException;

				throw;
			}
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
			try
			{
				return self.ReadFromAnyAsync(timeout).Result;
			}
			catch(AggregateException aex)
			{
				if (aex.Flatten().InnerExceptions.Count == 1)
					throw aex.InnerException;

				throw;
			}
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
			try
			{
				return self.WriteToAnyAsync(value, timeout).Result;
			}
			catch(AggregateException aex)
			{
				if (aex.Flatten().InnerExceptions.Count == 1)
					throw aex.InnerException;

				throw;
			}
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
		#endregion

		#region Readable and Writeable casting
		/// <summary>
		/// Returns the channel as a read channel
		/// </summary>
		/// <returns>The channel as a read channel</returns>
		/// <param name="channel">The channel to cast.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static IReadChannel<T> AsRead<T>(this IChannel<T> channel)
		{
			return channel;
		}

		/// <summary>
		/// Returns the channel as a write channel
		/// </summary>
		/// <returns>The channel as a write channel</returns>
		/// <param name="channel">The channel to cast.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static IWriteChannel<T> AsWrite<T>(this IChannel<T> channel)
		{
			return channel;
		}

		/// <summary>
		/// Returns the channel as a read channel
		/// </summary>
		/// <returns>The channel as a read channel</returns>
		/// <param name="channel">The channel to cast.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static IBlockingReadableChannel<T> AsRead<T>(this IBlockingChannel<T> channel)
		{
			return channel;
		}

		/// <summary>
		/// Returns the channel as a write channel
		/// </summary>
		/// <returns>The channel as a write channel</returns>
		/// <param name="channel">The channel to cast.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static IBlockingWriteableChannel<T> AsWrite<T>(this IBlockingChannel<T> channel)
		{
			return channel;
		}
		#endregion
	}
}

