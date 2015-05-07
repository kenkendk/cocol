using System;

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
		public static T Read<T>(this IChannel<T> self)
		{
			return DoRead<T>(self, Timeout.Infinite).Result;
		}

		/// <summary>
		/// Read from the channel in a blocking manner
		/// </summary>
		/// <param name="self">The channel to read from</param>
		/// <param name="timeout">The maximum time to wait for a value</param>
		/// <returns>>The value read from the channel</returns>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static T Read<T>(this IChannel<T> self, TimeSpan timeout)
		{
			return DoRead(self, timeout).Result;
		}

		/// <summary>
		/// Read from the channel in a probing manner
		/// </summary>
		/// <param name="self">The channel to read from</param>
		/// <param name="result">The read result</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>True if the read succeeded, false otherwise</returns>
		public static bool TryRead<T>(this IChannel<T> self, out T result)
		{
			var res = DoRead(self, Timeout.Immediate);

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
		/// Read from the channel in a blocking manner
		/// </summary>
		/// <param name="self">The channel to read from</param>
		/// <param name="timeout">The maximum time to wait for a value</param>
		/// <returns>>The value read from the channel</returns>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		private static ICallbackResult<T> DoRead<T>(this IChannel<T> self, TimeSpan timeout)
		{
			using (var evt = new System.Threading.ManualResetEventSlim(false))
			{
				ICallbackResult<T> res = null;
				self.RegisterRead(null, (x) =>
					{
						res = x;
						evt.Set();
					}, timeout);

				evt.Wait();
				return res;
			}
		}

		/// <summary>
		/// Write to the channel in a blocking manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <param name="timeout">The maximum time to wait for an available slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static void Write<T>(this IChannel<T> self, T value)
		{
			var ex = DoWrite<T>(self, value, Timeout.Infinite).Exception;
			if (ex != null)
				throw ex;
		}

		/// <summary>
		/// Write to the channel in a blocking manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <param name="timeout">The maximum time to wait for an available slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static void Write<T>(this IChannel<T> self, T value, TimeSpan timeout)
		{
			var ex = DoWrite<T>(self, value, timeout).Exception;
			if (ex != null)
				throw ex;
		}		

		/// <summary>
		/// Write to the channel in a probing manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>True if the write succeeded, false otherwise</returns>
		public static bool TryWrite<T>(this IChannel<T> self, T value)
		{
			return DoWrite(self, value, Timeout.Immediate).Exception == null;
		}

		/// <summary>
		/// Write to the channel in a blocking manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <param name="timeout">The maximum time to wait for an available slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		private static ICallbackResult<T> DoWrite<T>(this IChannel<T> self, T value, TimeSpan timeout)
		{
			using (var evt = new System.Threading.ManualResetEventSlim(false))
			{
				ICallbackResult<T> res = null;
				self.RegisterWrite(null, (x) =>
					{
						res = x;
						evt.Set();
					}, value, timeout);

				evt.Wait();

				return res;
			}
		}	

		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from a channel</returns>
		public static T ReadFromAny<T>(this MultiChannelSet<T> self)
		{
			IChannel<T> dummy;
			return DoReadFromAny<T>(self, Timeout.Infinite).Result;
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
			var res = DoReadFromAny<T>(self, Timeout.Infinite);
			channel = res.Channel;
			return res.Result;
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
			var res = DoReadFromAny<T>(self, timeout);
			channel = res.Channel;
			return res.Result;
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
			var res = DoReadFromAny<T>(self, Timeout.Immediate);
			channel = res.Channel;

			if (res.Exception != null)
			{
				value = default(T);
				return false;
			}
			else
			{
				value = res.Result;
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
			var res = DoReadFromAny<T>(self, Timeout.Immediate);

			if (res.Exception != null)
			{
				value = default(T);
				return false;
			}
			else
			{
				value = res.Result;
				return true;
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
		public static ICallbackResult<T> DoReadFromAny<T>(this MultiChannelSet<T> self, TimeSpan timeout)
		{
			using (var evt = new System.Threading.ManualResetEventSlim(false))
			{
				ICallbackResult<T> res = null;
				self.ReadFromAny((x) =>
					{
						res = x;
						evt.Set();
					}, timeout);

				evt.Wait();

				return res;
			}
		}

		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <param name="value">The value to write into the channel</param>
		public static void WriteToAny<T>(this MultiChannelSet<T> self, T value)
		{
			var res = DoWriteToAny(self, value, Timeout.Infinite);
			if (res.Exception != null)
				throw res.Exception;			
		}

		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <param name="value">The value to write into the channel</param>
		public static void WriteToAny<T>(this MultiChannelSet<T> self, T value, out IChannel<T> channel)
		{
			var res = DoWriteToAny(self, value, Timeout.Infinite);

			channel = res.Channel;
			if (res.Exception != null)
				throw res.Exception;
		}

		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <param name="timeout">The maximum time to wait for a slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <param name="value">The value to write into the channel</param>
		public static void WriteToAny<T>(this MultiChannelSet<T> self, T value, TimeSpan timeout, out IChannel<T> channel)
		{
			var res = DoWriteToAny(self, value, timeout);

			channel = res.Channel;
			if (res.Exception != null)
				throw res.Exception;
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
			return DoWriteToAny(self, value, Timeout.Immediate).Exception == null;
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
			var res = DoWriteToAny(self, value, Timeout.Immediate);

			channel = res.Channel;
			return res.Exception == null;
		}
			
		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <param name="timeout">The maximum time to wait for a slot</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <param name="value">The value to write into the channel</param>
		public static ICallbackResult<T> DoWriteToAny<T>(this MultiChannelSet<T> self, T value, TimeSpan timeout)
		{
			using (var evt = new System.Threading.ManualResetEventSlim(false))
			{
				ICallbackResult<T> res = null;
				self.WriteToAny((x) =>
					{
						res = x;
						evt.Set();
					}, value, timeout);

				evt.Wait();

				return res;
			}
		}

	}
}

