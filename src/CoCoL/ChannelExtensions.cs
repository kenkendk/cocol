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

		/// <summary>
		/// Helper method that implements WhenAny with the NotOnCancelled flag
		/// </summary>
		/// <returns>A task that completes when a NonCancelled task returns, or no more tasks are available</returns>
		/// <param name="items">Items.</param>
		/// <param name="source">The task completion source to signal</param>
		public static Task<Task> WhenAnyNonCancelled(this IEnumerable<Task> items, TaskCompletionSource<Task> source = null)
		{
			var tcs = source ?? new TaskCompletionSource<Task>();
			var lst = items is List<Task> ? (List<Task>)items : items.ToList();

			Task.WhenAny(items).ContinueWith(x => {
				if (x.IsCanceled)
					tcs.TrySetCanceled();
				else if (x.IsFaulted)
					tcs.TrySetException(x.Exception);
				else
				{
					var res = x.Result;
					if (!res.IsCanceled)
						tcs.TrySetResult(res);
					else
					{
						lst.Remove(res);
						if (lst.Count == 0)
							tcs.TrySetCanceled();
						else
							WhenAnyNonCancelled(lst, tcs);
					}
				}
			});

			return tcs.Task;
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

		#region Simple channel overload methods
		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="self">The channel to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from the channel</returns>
		public static Task<T> ReadAsync<T>(this IReadChannel<T> self) 
		{
			return self.ReadAsync(Timeout.Infinite, null);
		}
		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="self">The channel to write to</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>An awaitable task</returns>
		public static Task WriteAsync<T>(this IWriteChannel<T> self, T value)
		{
			return self.WriteAsync(value, Timeout.Infinite, null);
		}
		/// <summary>
		/// Write to the channel in a probing manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>True if the write succeeded, false otherwise</returns>
		public static Task<bool> TryWriteAsync<T>(this IWriteChannel<T> self, T value)
		{
			return self.WriteAsync(value, Timeout.Immediate, null).ContinueWith(x => x.IsCompleted);
		}
		/// <summary>
		/// Write to the channel in a probing manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>True if the write succeeded, false otherwise</returns>
		public static Task<KeyValuePair<bool, T>> TryReadAsync<T>(this IReadChannel<T> self)
		{
			return self.ReadAsync(Timeout.Immediate, null).ContinueWith(x => {
				if (x.IsFaulted || x.IsCanceled)
					return new KeyValuePair<bool, T>(false, default(T));

				return new KeyValuePair<bool, T>(true, x.Result);
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
			var res = self.ReadAsync(Timeout.Immediate).WaitForTask();

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
			var task = self.WriteAsync(value, Timeout.Immediate).WaitForTask();
			return !(task.IsFaulted || task.IsCanceled) && task.IsCompleted;
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

		/// <summary>
		/// Returns the channel as an IDisposable read-only channel.
		/// The returned instance must be Disposed.
		/// </summary>
		/// <returns>The channel as a read-only channel</returns>
		/// <param name="channel">The channel to wrap.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static IReadChannelEnd<T> AsReadOnly<T>(this IReadChannel<T> channel)
		{
			return new ChannelReadEnd<T>(channel);
		}

		/// <summary>
		/// Returns the channel as an IDisposable write-only channel.
		/// The returned instance must be Disposed.
		/// </summary>
		/// <returns>The channel as a write-only channel</returns>
		/// <param name="channel">The channel to wrap.</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static IWriteChannelEnd<T> AsWriteOnly<T>(this IWriteChannel<T> channel)
		{
			return new ChannelWriteEnd<T>(channel);
		}

		#endregion

		#region Operations on lists of channels
		/// <summary>
		/// Retires all channels in the list
		/// </summary>
		/// <param name="list">The list of channels to retire</param>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void Retire<T>(this IEnumerable<IChannel<T>> list, bool immediate = false)
		{
			foreach (var c in list)
				c.Retire(immediate);
		}

		/// <summary>
		/// Retires all channels in the list
		/// </summary>
		/// <param name="list">The list of channels to retire</param>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void Retire<T>(this IEnumerable<IBlockingChannel<T>> list, bool immediate = false)
		{
			foreach (var c in list)
				c.Retire(immediate);
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
			return Read(self, Timeout.Infinite, null);
		}

		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The value read.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="timeout">The read timeout.</param>
		public static object Read(this IUntypedChannel self, TimeSpan timeout, ITwoPhaseOffer offer = null)
		{
			return WaitForTask<object>(ReadAsync(self, timeout, offer)).Result;
		}

		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		public static Task<object> ReadAsync(this IUntypedChannel self)
		{
			return ReadAsync(self, Timeout.Infinite, null);
		}
			
		/// <summary>
		/// Writes the channel synchronously
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		public static void Write(this IUntypedChannel self, object value)
		{
			Write(self, value, Timeout.Infinite, null);
		}
			
		/// <summary>
		/// Writes the channel synchronously
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="timeout">The write timeout.</param>
		public static void Write(this IUntypedChannel self, object value, TimeSpan timeout, ITwoPhaseOffer offer = null)
		{
			var res = WriteAsync(self, value, timeout, offer).WaitForTask();

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
			return WriteAsync(self, value, Timeout.Infinite, null);
		}

		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="timeout">The read timeout.</param>
		public static Task<object> ReadAsync(this IUntypedChannel self, TimeSpan timeout,  ITwoPhaseOffer offer = null)
		{
			return UntypedAccessMethods.CreateReadAccessor(self).ReadAsync(self, timeout, offer);
		}

		/// <summary>
		/// Writes the channel asynchronously
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to write.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="timeout">The write timeout.</param>
		public static Task WriteAsync(this IUntypedChannel self, object value, TimeSpan timeout, ITwoPhaseOffer offer = null)
		{
			return UntypedAccessMethods.CreateWriteAccessor(self).WriteAsync(self, value, timeout, offer);
		}

		#endregion
	}
}

