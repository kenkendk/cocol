using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CoCoL
{
	/// <summary>
	/// This static class provides various extension methods for
	/// simplifying the use of channels other than async
	/// </summary>
	public static class ChannelExtensions
	{
		/// <summary>
		/// Single-shot variable that is set if we are running under the Mono runtime
		/// </summary>
		private static readonly bool IsRunningMono = Type.GetType ("Mono.Runtime") != null;

		/// <summary>
		/// Blocking wait for a task, equivalent to calling Task.Wait(),
		/// but works around a race in Mono that causes Wait() to hang
		/// </summary>
		/// <param name="t">The task to wait for</param>
		/// <returns>The task</returns>
		public static Task<T> WaitForTask<T>(this Task<T> task)
		{
			// Mono has a race when waiting for a
			// task to complete, this workaround
			// ensures that the wait call does not hang
			if (IsRunningMono)
			{
				if (!task.IsCompleted)
					using (var lck = new System.Threading.ManualResetEventSlim(false))
					{
						task.ContinueWith(x => lck.Set());
						// This ensures we never return with 
						// an incomplete task, but may casue
						// some spin waiting
						while (!task.IsCompleted)
							lck.Wait();
					}
			}
			else
			{
				// Don't throw the exception here
				// let the caller access the task
				try { task.Wait(); } 
				catch {	}
			}

			return task;
		}

		/// <summary>
		/// Blocking wait for a task, equivalent to calling Task.Wait(),
		/// but works around a race in Mono that causes Wait() to hang
		/// </summary>
		/// <param name="t">The task to wait for</param>
		/// <returns>The task</returns>
		public static Task WaitForTask(this Task task)
		{
			// Mono has a race when waiting for a
			// task to complete, this workaround
			// ensures that the wait call does not hang
			if (IsRunningMono)
			{
				if (!task.IsCompleted)
					using (var lck = new System.Threading.ManualResetEventSlim(false))
					{
						task.ContinueWith(x => lck.Set());
						// This ensures we never return with 
						// an incomplete task, but may casue
						// some spin waiting
						while (!task.IsCompleted)
							lck.Wait();
					}
			}
			else
			{
				// Don't throw the exception here
				// let the caller access the task
				try { task.Wait(); } 
				catch {	}
			}

			return task;
		}

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
			ThreadPool.QueueItem(() =>
				{
					self.WriteAsync(value, Timeout.Infinite);
				});
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
			ThreadPool.QueueItem(() =>
				{
					self.WriteAsync(value, timeout);
				});
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
				return self.ReadAsync(Timeout.Infinite).WaitForTask().Result;
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
				return self.ReadAsync(timeout).WaitForTask().Result;
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

			if (res.IsFaulted || res.IsCanceled)
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
			var res = self.WriteAsync(value, Timeout.Infinite).WaitForTask();

			if (res.Exception != null)
			{
				if (res.Exception is AggregateException && ((AggregateException)res.Exception).Flatten().InnerExceptions.Count == 1)
					throw ((AggregateException)res.Exception).InnerException;
				
				throw res.Exception;
			}
			else if (res.IsCanceled)
			{
				throw new OperationCanceledException();
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
			var res = self.WriteAsync(value, timeout).WaitForTask();

			if (res.Exception != null)
			{
				if (res.Exception is AggregateException && ((AggregateException)res.Exception).Flatten().InnerExceptions.Count == 1)
					throw ((AggregateException)res.Exception).InnerException;

				throw res.Exception;
			}
			else if (res.IsCanceled)
			{
				throw new OperationCanceledException();
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
			return self.WriteAsync(value, Timeout.Immediate).IsCompleted;
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
				return self.ReadFromAnyAsync(Timeout.Infinite).WaitForTask().Result;
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
		public static T ReadFromAny<T>(this MultiChannelSet<T> self, out IReadChannel<T> channel)
		{
			try
			{
				var res = self.ReadFromAnyAsync(Timeout.Infinite).WaitForTask().Result;
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
		public static T ReadFromAny<T>(this MultiChannelSet<T> self, out IReadChannel<T> channel, TimeSpan timeout)
		{
			try
			{
				var res = self.ReadFromAnyAsync(timeout).WaitForTask().Result;
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
		public static bool TryReadFromAny<T>(this MultiChannelSet<T> self, out T value, out IReadChannel<T> channel)
		{
			var res = self.ReadFromAnyAsync(Timeout.Immediate).WaitForTask();

			if (res.IsFaulted || res.IsCanceled)
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
			IReadChannel<T> dummy;
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
				return self.ReadFromAnyAsync(timeout).WaitForTask().Result;
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
		public static IWriteChannel<T> WriteToAny<T>(this MultiChannelSet<T> self, T value)
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
		public static IWriteChannel<T> WriteToAny<T>(this MultiChannelSet<T> self, T value, TimeSpan timeout)
		{
			try
			{
				return self.WriteToAnyAsync(value, timeout).WaitForTask().Result;
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
			IWriteChannel<T> dummy;
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
		public static bool TryWriteToAny<T>(this MultiChannelSet<T> self, T value, out IWriteChannel<T> channel)
		{
			var res = self.WriteToAnyAsync(value, Timeout.Immediate).WaitForTask();

			if (res.IsFaulted || res.IsCanceled)
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

		#region Readable, Writeable and Untyped casting
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

		/// <summary>
		/// Returns the channel as an untyped channel
		/// </summary>
		/// <returns>The untyped channel.</returns>
		/// <param name="channel">The channel to untype.</param>
		/// <typeparam name="T">The type of the channel.</typeparam>
		public static IUntypedChannel AsUntyped<T>(this IChannel<T> channel)
		{
			return (IUntypedChannel)channel;
		}
		#endregion

		#region Operations on lists of channels
		/// <summary>
		/// Retires all channels in the list
		/// </summary>
		/// <param name="list">The list of channels to retire</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void Retire<T>(this IEnumerable<IChannel<T>> list)
		{
			foreach (var c in list)
				c.Retire();
		}

		/// <summary>
		/// Retires all channels in the list
		/// </summary>
		/// <param name="list">The list of channels to retire</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void Retire<T>(this IEnumerable<IBlockingChannel<T>> list)
		{
			foreach (var c in list)
				c.Retire();
		}
		#endregion

		#region Operations on untyped channels
		/// <summary>
		/// Reads the channel synchronously.
		/// </summary>
		/// <returns>The value read.</returns>
		/// <param name="self">The channel to read.</param>
		public static object Read(this IUntypedChannel self)
		{
			return Read(self, null, Timeout.Infinite);
		}

		/// <summary>
		/// Reads the channel synchronously.
		/// </summary>
		/// <returns>The value read.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="offer">The two-phase offer.</param>
		public static object Read(this IUntypedChannel self, ITwoPhaseOffer offer)
		{
			return Read(self, offer, Timeout.Infinite);
		}

		/// <summary>
		/// Reads the channel synchronously.
		/// </summary>
		/// <returns>The value read.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="timeout">The read timeout.</param>
		public static object Read(this IUntypedChannel self, TimeSpan timeout)
		{
			return Read(self, null, timeout);
		}

		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The value read.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="timeout">The read timeout.</param>
		public static object Read(this IUntypedChannel self, ITwoPhaseOffer offer, TimeSpan timeout)
		{
			return WaitForTask<object>(ReadAsync(self, offer, timeout)).Result;
		}

		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		public static Task<object> ReadAsync(this IUntypedChannel self)
		{
			return ReadAsync(self, null, Timeout.Infinite);
		}

		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="timeout">The read timeout.</param>
		public static Task<object> ReadAsync(this IUntypedChannel self, ITwoPhaseOffer offer)
		{
			return ReadAsync(self, offer, Timeout.Infinite);
		}

		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="timeout">The read timeout.</param>
		public static Task<object> ReadAsync(this IUntypedChannel self, TimeSpan timeout)
		{
			return ReadAsync(self, null, timeout);
		}

		/// <summary>
		/// Writes the channel synchronously
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		public static void Write(this IUntypedChannel self, object value)
		{
			Write(self, null, value, Timeout.Infinite);
		}

		/// <summary>
		/// Writes the channel synchronously
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="value">The value to write.</param>
		public static void Write(this IUntypedChannel self, ITwoPhaseOffer offer, object value)
		{
			Write(self, offer, value, Timeout.Infinite);
		}

		/// <summary>
		/// Writes the channel synchronously
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="timeout">The write timeout.</param>
		public static void Write(this IUntypedChannel self, object value, TimeSpan timeout)
		{
			Write(self, null, value, timeout);
		}

		/// <summary>
		/// Writes the channel synchronously
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="timeout">The write timeout.</param>
		public static void Write(this IUntypedChannel self, ITwoPhaseOffer offer, object value, TimeSpan timeout)
		{
			var res = WriteAsync(self, offer, value, timeout).WaitForTask();

			if (res.Exception != null)
			{
				if (res.Exception is AggregateException && ((AggregateException)res.Exception).Flatten().InnerExceptions.Count == 1)
					throw ((AggregateException)res.Exception).InnerException;

				throw res.Exception;
			}
			else if (res.IsCanceled)
			{
				throw new OperationCanceledException();
			}
		}

		/// <summary>
		/// Writes the channel asynchronously
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		public static Task WriteAsync(this IUntypedChannel self, object value)
		{
			return WriteAsync(self, null, value, Timeout.Infinite);
		}

		/// <summary>
		/// Writes the channel asynchronously
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to write.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="value">The value to write.</param>
		public static Task WriteAsync(this IUntypedChannel self, ITwoPhaseOffer offer, object value)
		{
			return WriteAsync(self, offer, value, Timeout.Infinite);
		}

		/// <summary>
		/// Writes the channel asynchronously
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="timeout">The write timeout.</param>
		public static Task WriteAsync(this IUntypedChannel self, object value, TimeSpan timeout)
		{
			return WriteAsync(self, null, value, timeout);
		}


		/// <summary>
		/// Gets the implemented generic interface from an instance.
		/// </summary>
		/// <returns>The implemented generic interface type.</returns>
		/// <param name="item">The item to examine.</param>
		/// <param name="interface">The interface type definition.</param>
		private static Type GetImplementedGenericInterface(object item, Type @interface)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			var implementedinterface = item.GetType().GetInterfaces().Where(x => x.IsGenericType && !x.IsGenericTypeDefinition).Where(x => x.GetGenericTypeDefinition() == @interface).FirstOrDefault();

			if (implementedinterface == null)
				throw new ArgumentException(string.Format("Given type {0} does not implement interface {1}", item.GetType(), @interface));

			return implementedinterface;
		}

		/// <summary>
		/// Gets the IReadChannel&lt;&gt; interface from an untyped channel instance
		/// </summary>
		/// <returns>The IReadChannel&lt;&gt; interface.</returns>
		/// <param name="self">The channel to get the interface from</param>
		public static Type ReadInterface(this IUntypedChannel self)
		{
			return GetImplementedGenericInterface(self, typeof(IReadChannel<>));
		}

		/// <summary>
		/// Gets the IWriteChannel&lt;&gt; interface from an untyped channel instance
		/// </summary>
		/// <returns>The IWriteChannel&lt;&gt; interface.</returns>
		/// <param name="self">The channel to get the interface from</param>
		public static Type WriteInterface(this IUntypedChannel self)
		{
			return GetImplementedGenericInterface(self, typeof(IWriteChannel<>));
		}
			
		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="timeout">The read timeout.</param>
		public static async Task<object> ReadAsync(this IUntypedChannel self, ITwoPhaseOffer offer, TimeSpan timeout)
		{
			var m = ReadInterface(self).GetMethod("ReadAsync", new Type[] { typeof(ITwoPhaseOffer), typeof(TimeSpan) });
			var t = (Task)m.Invoke(self, new object[] { offer, timeout });
			await t;
			return t.GetType().GetProperty("Result").GetValue(t);
		}
			
		/// <summary>
		/// Writes the channel asynchronously
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to write.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="timeout">The write timeout.</param>
		public static Task WriteAsync(this IUntypedChannel self, ITwoPhaseOffer offer, object value, TimeSpan timeout)
		{
			var m = WriteInterface(self).GetMethod("WriteAsync", new Type[] { typeof(ITwoPhaseOffer), self.GetType().GetGenericArguments()[0], typeof(TimeSpan) });
			return (Task)m.Invoke(self, new object[] { offer, value, timeout });
		}
		#endregion
	}
}

