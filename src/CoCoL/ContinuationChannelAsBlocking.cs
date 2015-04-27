using System;

namespace CoCoL
{
	/// <summary>
	/// Helper for treating a continuation channel as a blocking channel
	/// </summary>
	public static class ContinuationChannelAsBlocking
	{

		//TODO: There could be more overloads here ...


		/// <summary>
		/// Read from the channel in a blocking manner
		/// </summary>
		public static T Read<T>(this IChannel<T> self)
		{
			using (var evt = new System.Threading.ManualResetEventSlim(false))
			{
				ICallbackResult<T> res = null;
				self.RegisterRead(null, (x) =>
					{
						res = x;
						evt.Set();
					}, Timeout.Infinite);

				evt.Wait();
				return res.Result;
			}
		}

		/// <summary>
		/// Write to the channel in a blocking manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		public static void Write<T>(this IChannel<T> self, T value)
		{
			using (var evt = new System.Threading.ManualResetEventSlim(false))
			{
				ICallbackResult<T> res = null;
				self.RegisterWrite(null, (x) =>
					{
						res = x;
						evt.Set();
					}, value, Timeout.Infinite);

				evt.Wait();

				if (res.Exception != null)
					throw res.Exception;
			}
		}		

		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		public static T ReadFromAny<T>(this MultiChannelSet<T> self)
		{
			using (var evt = new System.Threading.ManualResetEventSlim(false))
			{
				ICallbackResult<T> res = null;
				self.ReadFromAny((x) =>
					{
						res = x;
						evt.Set();
					}, Timeout.Infinite);

				evt.Wait();
				return res.Result;
			}
		}

		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		public static void WriteToAny<T>(this MultiChannelSet<T> self, T value)
		{
			using (var evt = new System.Threading.ManualResetEventSlim(false))
			{
				ICallbackResult<T> res = null;
				self.WriteToAny((x) =>
					{
						res = x;
						evt.Set();
					}, value, Timeout.Infinite);

				evt.Wait();

				if (res.Exception != null)
					throw res.Exception;
			}
		}

	}
}

