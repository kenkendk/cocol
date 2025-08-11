using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace CoCoL
{
	/// <summary>
	/// This static class provides various extension methods for
	/// simplifying the use of channels other than async
	/// </summary>
	public static class ChannelExtensions
	{
		/// <summary>
		/// Blocking wait for a task, equivalent to calling Task.Wait(),
		/// but works around a race in Mono that causes Wait() to hang
		/// </summary>
		/// <param name="task">The task to wait for</param>
		/// <returns>The task</returns>
		/// <typeparam name="T">The task data type.</typeparam>
		public static Task<T> WaitForTask<T>(this Task<T> task)
		{
			try { task.ConfigureAwait(false).GetAwaiter().GetResult(); }
			catch
			{
				// Don't throw the exception here
				// let the caller access the task
			}

			return task;
		}

		/// <summary>
		/// Blocking wait for a task, equivalent to calling Task.Wait(),
		/// but works around a race in Mono that causes Wait() to hang
		/// </summary>
		/// <param name="task">The task to wait for</param>
		/// <returns>The task</returns>
		public static Task WaitForTask(this Task task)
		{
			try { task.ConfigureAwait(false).GetAwaiter().GetResult(); }
			catch
			{
				// Don't throw the exception here
				// let the caller access the task
			}

			return task;
		}

		/// <summary>
		/// Blocking wait for a task, equivalent to calling Task.Wait(),
		/// but works around a race in Mono that causes Wait() to hang.
		/// If an error occurs, an exception is thrown
		/// </summary>
		/// <param name="task">The task to wait for</param>
		/// <returns>The task</returns>
		public static Task WaitForTaskOrThrow(this Task task)
		{
			if (task == null)
				throw new ArgumentNullException(nameof(task));

			task.WaitForTask();
			if (task.IsCanceled)
				throw new TaskCanceledException();
			else if (task.IsFaulted)
			{
				var innerExceptions = task.Exception.Flatten().InnerExceptions;
				if (innerExceptions.Count == 1)
				{
					ExceptionDispatchInfo.Capture(innerExceptions.First()).Throw();
				}
				else
				{
					throw task.Exception;
				}
			}

			return task;
		}

		/// <summary>
		/// Leaves the task un-awaited, supressing warnings if the task is not awaited
		/// </summary>
		/// <param name="task">The task to suppress.</param>
		public static void FireAndForget(this Task task)
		{
			// No action needed
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

			Task.WhenAny(lst).ContinueWith(x =>
			{
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

		#region Extra methods for constructing two-phase commit offers
		/// <summary>
		/// Creates a <see cref="ITwoPhaseOffer"/> for a timeout value
		/// </summary>
		/// <returns>The offer.</returns>
		/// <param name="timeout">The timeout value.</param>
		/// <param name="cancelToken">The cancellation token.</param>
		public static ITwoPhaseOffer AsOffer(this TimeSpan timeout, CancellationToken cancelToken = default(CancellationToken))
		{
			return new TimeoutOffer(timeout, cancelToken);
		}
		/// <summary>
		/// Creates a <see cref="ITwoPhaseOffer"/> for a timeout value
		/// </summary>
		/// <returns>The offer.</returns>
		/// <param name="timeout">The timeout value.</param>
		/// <param name="cancelToken">The cancellation token.</param>
		public static ITwoPhaseOffer AsOffer(this DateTime timeout, CancellationToken cancelToken = default(CancellationToken))
		{
			return new TimeoutOffer(timeout, cancelToken);
		}
		/// <summary>
		/// Creates a <see cref="ITwoPhaseOffer"/> for a cancellation token
		/// </summary>
		/// <returns>The offer.</returns>
		/// <param name="cancelToken">The cancellation token.</param>
		public static ITwoPhaseOffer AsOffer(this CancellationToken cancelToken)
		{
			return new CancellationOffer(cancelToken);
		}
		#endregion

		#region Avoid compile warnings when using the write method in fire-n-forget mode
		/// <summary>
		/// Write to the channel in a blocking manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static void WriteNoWait<T>(this IWriteChannel<T> self, T value)
		{
			self.WriteAsync(value, Timeout.Infinite).FireAndForget();
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
			self.WriteAsync(value, timeout).FireAndForget();
		}
		#endregion

		#region Simple channel overload methods
		/// <summary>
		/// Write to the channel in a probing and asynchronous manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>True if the write succeeded, false otherwise</returns>
		public static Task<bool> TryWriteAsync<T>(this IWriteChannel<T> self, T value)
		{
			return TryWriteAsync(self, value, Timeout.Immediate);
		}
		/// <summary>
		/// Write to the channel in a probing and asynchronous manner
		/// </summary>
		/// <param name="value">The value to write into the channel</param>
		/// <param name="self">The channel to read from</param>
		/// <param name="waittime">The time to wait before cancelling</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>True if the write succeeded, false otherwise</returns>
		public static Task<bool> TryWriteAsync<T>(this IWriteChannel<T> self, T value, TimeSpan waittime)
		{
			return self.WriteAsync(value, new TimeoutOffer(waittime)).ContinueWith(x => x.IsCompleted);
		}
		/// <summary>
		/// Reads the channel in a probing and asynchronous manner
		/// </summary>
		/// <param name="self">The channel to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>True if the read succeeded, false otherwise</returns>
		public static Task<KeyValuePair<bool, T>> TryReadAsync<T>(this IReadChannel<T> self)
		{
			return TryReadAsync(self, Timeout.Immediate);
		}
		/// <summary>
		/// Reads the channel in a probing and asynchronous manner
		/// </summary>
		/// <param name="self">The channel to read from</param>
		/// <param name="waittime">The amount of time to wait before cancelling</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>True if the read succeeded, false otherwise</returns>
		public static Task<KeyValuePair<bool, T>> TryReadAsync<T>(this IReadChannel<T> self, TimeSpan waittime)
		{
			return self.ReadAsync(new TimeoutOffer(waittime)).ContinueWith(x =>
			{
				if (x.IsFaulted || x.IsCanceled)
					return new KeyValuePair<bool, T>(false, default(T));

				return new KeyValuePair<bool, T>(true, x.Result);
			});
		}
		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="timeout">The read timeout.</param>
		public static Task<T> ReadAsync<T>(this IReadChannel<T> self, TimeSpan timeout)
		{
			return self.ReadAsync(new TimeoutOffer(timeout));
		}
		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="timeout">The read timeout.</param>
		/// <param name="cancelToken">The cancellation token</param>
		public static Task<T> ReadAsync<T>(this IReadChannel<T> self, TimeSpan timeout, CancellationToken cancelToken)
		{
			return self.ReadAsync(new TimeoutOffer(timeout, cancelToken));
		}
		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="cancelToken">The cancellation token</param>
		public static Task<T> ReadAsync<T>(this IReadChannel<T> self, CancellationToken cancelToken)
		{
			return self.ReadAsync(new CancellationOffer(cancelToken));
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
				return self.ReadAsync(null).WaitForTask().Result;
			}
			catch (AggregateException aex)
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
				return self.ReadAsync(new TimeoutOffer(timeout)).WaitForTask().Result;
			}
			catch (AggregateException aex)
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
			var res = self.ReadAsync(new TimeoutOffer(Timeout.Immediate)).WaitForTask();

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
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static void Write<T>(this IWriteChannel<T> self, T value)
		{
			var res = self.WriteAsync(value, null).WaitForTask();

			if (res.Exception != null)
			{
				if (res.Exception.Flatten().InnerExceptions.Count == 1)
					throw res.Exception.InnerException;

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
			var res = self.WriteAsync(value, new TimeoutOffer(timeout)).WaitForTask();

			if (res.Exception != null)
			{
				if (res.Exception.Flatten().InnerExceptions.Count == 1)
					throw res.Exception.InnerException;

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
			var task = self.WriteAsync(value, new TimeoutOffer(Timeout.Immediate)).WaitForTask();
			return !(task.IsFaulted || task.IsCanceled) && task.IsCompleted;
		}

		/// <summary>
		/// Leave the channel in a blocking manner
		/// </summary>
		/// <param name="self">The channel to leave.</param>
		/// <param name="immediate">Set to <c>true</c> if the channel is retired immediately.</param>
		public static void Retire(this IRetireAbleChannel self, bool immediate = false)
		{
			self.RetireAsync(immediate).WaitForTaskOrThrow();
		}

		/// <summary>
		/// Leave the channel in a blocking manner
		/// </summary>
		/// <param name="self">The channel to leave.</param>
		/// <param name="asReader">Set to <c>true</c> if leaving as reader and <c>false</c> if leaving as writer.</param>
		public static void Leave(this IJoinAbleChannel self, bool asReader)
		{
			self.LeaveAsync(asReader).WaitForTaskOrThrow();
		}

		/// <summary>
		/// Join the channel in a blocking manner
		/// </summary>
		/// <param name="self">The channel to join.</param>
		/// <param name="asReader">Set to <c>true</c> if joining as reader and <c>false</c> if joining as writer.</param>
		public static void Join(this IJoinAbleChannel self, bool asReader)
		{
			self.JoinAsync(asReader).WaitForTaskOrThrow();
		}

		/// <summary>
		/// Leave the channel in a blocking manner
		/// </summary>
		/// <param name="self">The channel to leave.</param>
		public static void Leave(this IJoinAbleChannelEnd self)
		{
			self.LeaveAsync().WaitForTaskOrThrow();
		}

		/// <summary>
		/// Join the channel in a blocking manner
		/// </summary>
		/// <param name="self">The channel to join.</param>
		public static void Join(this IJoinAbleChannelEnd self)
		{
			self.JoinAsync().WaitForTaskOrThrow();
		}
		#endregion

		#region Blocking multi-channel usage
		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from a channel</returns>
		public static MultisetResult<T> ReadFromAny<T>(this MultiChannelSetRead<T> self)
		{
			try
			{
				return self.ReadFromAnyAsync(Timeout.Infinite).WaitForTask().Result;
			}
			catch (AggregateException aex)
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
		public static T ReadFromAny<T>(this MultiChannelSetRead<T> self, out IReadChannel<T> channel)
		{
			try
			{
				var res = self.ReadFromAnyAsync(Timeout.Infinite).WaitForTask().Result;
				channel = res.Channel;
				return res.Value;
			}
			catch (AggregateException aex)
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
		public static T ReadFromAny<T>(this MultiChannelSetRead<T> self, out IReadChannel<T> channel, TimeSpan timeout)
		{
			try
			{
				var res = self.ReadFromAnyAsync(timeout).WaitForTask().Result;
				channel = res.Channel;
				return res.Value;
			}
			catch (AggregateException aex)
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
		/// <param name="value">The value that was read</param>
		/// <param name="channel">The channel read from</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from a channel</returns>
		public static bool TryReadFromAny<T>(this MultiChannelSetRead<T> self, out T value, out IReadChannel<T> channel)
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
		/// <param name="value">The value read</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		/// <returns>The value read from a channel</returns>
		public static bool TryReadFromAny<T>(this MultiChannelSetRead<T> self, out T value)
		{
			IReadChannel<T> dummy;
			return TryReadFromAny<T>(self, out value, out dummy);
		}

		/// <summary>
		/// Read from the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="timeout">The maximum time to wait for a value</param>
		/// <returns>The value read from a channel</returns>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static MultisetResult<T> ReadFromAny<T>(this MultiChannelSetRead<T> self, TimeSpan timeout)
		{
			try
			{
				return self.ReadFromAnyAsync(timeout).WaitForTask().Result;
			}
			catch (AggregateException aex)
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
		/// <param name="value">The value to write into the channel</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static IWriteChannel<T> WriteToAny<T>(this MultiChannelSetWrite<T> self, T value)
		{
			return WriteToAny(self, value, Timeout.Infinite);
		}

		/// <summary>
		/// Write to the channel set in a blocking manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="timeout">The maximum time to wait for a slot</param>
		/// <param name="value">The value to write into the channel</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static IWriteChannel<T> WriteToAny<T>(this MultiChannelSetWrite<T> self, T value, TimeSpan timeout)
		{
			try
			{
				return self.WriteToAnyAsync(value, timeout).WaitForTask().Result;
			}
			catch (AggregateException aex)
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
		/// <param name="value">The value to write into the channel</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static bool TryWriteToAny<T>(this MultiChannelSetWrite<T> self, T value)
		{
			IWriteChannel<T> dummy;
			return TryWriteToAny(self, value, out dummy);
		}

		/// <summary>
		/// Read from the channel set in a probing manner
		/// </summary>
		/// <param name="self">The channels to read from</param>
		/// <param name="channel">The channel written to</param>
		/// <param name="value">The value to write into the channel</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static bool TryWriteToAny<T>(this MultiChannelSetWrite<T> self, T value, out IWriteChannel<T> channel)
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

		/// <summary>
		/// Invokes the retire method on all channels in the set
		/// </summary>
		/// <param name="self">The channels to retire</param>
		/// <param name="immediate">A value indicating if the retire is performed immediately.</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static void Retire<T>(this MultiChannelSetRead<T> self, bool immediate = false)
		{
			self.RetireAsync(immediate).WaitForTaskOrThrow();
		}

		/// <summary>
		/// Invokes the retire method on all channels in the set
		/// </summary>
		/// <param name="self">The channels to retire</param>
		/// <param name="immediate">A value indicating if the retire is performed immediately.</param>
		/// <typeparam name="T">The channel data type parameter.</typeparam>
		public static void Retire<T>(this MultiChannelSetWrite<T> self, bool immediate = false)
		{
			self.RetireAsync(immediate).WaitForTaskOrThrow();
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
		public static async Task RetireAsync<T>(this IEnumerable<IChannel<T>> list, bool immediate = false)
		{
			foreach (var c in list)
				await c.RetireAsync(immediate).ConfigureAwait(false);
		}
		/// <summary>
		/// Retires all channels in the list
		/// </summary>
		/// <param name="list">The list of channels to retire</param>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		/// <typeparam name="T">The channel data type.</typeparam>
		public static void Retire<T>(this IEnumerable<IChannel<T>> list, bool immediate = false)
		{
			list.RetireAsync(immediate).WaitForTaskOrThrow();
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
			return Read(self, null);
		}

		/// <summary>
		/// Reads the channel synchronously.
		/// </summary>
		/// <returns>The value read.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="timeout">The read timeout.</param>
		public static object Read(this IUntypedChannel self, TimeSpan timeout)
		{
			return WaitForTask<object>(ReadAsync(self, new TimeoutOffer(timeout))).Result;
		}

		/// <summary>
		/// Reads the channel synchronously.
		/// </summary>
		/// <returns>The value read.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="cancelToken">The cancellation token.</param>
		public static object Read(this IUntypedChannel self, CancellationToken cancelToken)
		{
			return WaitForTask<object>(ReadAsync(self, new CancellationOffer(cancelToken))).Result;
		}

		/// <summary>
		/// Reads the channel synchronously.
		/// </summary>
		/// <returns>The value read.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="timeout">The read timeout.</param>
		/// <param name="cancelToken">The cancellation token.</param>
		public static object Read(this IUntypedChannel self, TimeSpan timeout, CancellationToken cancelToken)
		{
			return WaitForTask<object>(ReadAsync(self, new TimeoutOffer(timeout, cancelToken))).Result;
		}

		/// <summary>
		/// Reads the channel synchronously.
		/// </summary>
		/// <returns>The value read.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="offer">The two-phase offer token.</param>
		public static object Read(this IUntypedChannel self, ITwoPhaseOffer offer)
		{
			return WaitForTask<object>(ReadAsync(self, offer)).Result;
		}

		/// <summary>
		/// Writes the channel synchronously
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		public static void Write(this IUntypedChannel self, object value)
		{
			Write(self, value, null);
		}

		/// <summary>
		/// Writes the channel synchronously
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="timeout">The write timeout.</param>
		public static void Write(this IUntypedChannel self, object value, TimeSpan timeout)
		{
			Write(self, value, new TimeoutOffer(timeout));
		}

		/// <summary>
		/// Writes the channel synchronously
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="cancelToken">The cancellation token.</param>
		public static void Write(this IUntypedChannel self, object value, CancellationToken cancelToken)
		{
			Write(self, value, new CancellationOffer(cancelToken));
		}

		/// <summary>
		/// Writes the channel synchronously
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="timeout">The write timeout.</param>
		/// <param name="cancelToken">The cancellation token.</param>
		public static void Write(this IUntypedChannel self, object value, TimeSpan timeout, CancellationToken cancelToken)
		{
			Write(self, value, new TimeoutOffer(timeout, cancelToken));
		}

		/// <summary>
		/// Writes the channel synchronously
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="offer">The two-phase offer.</param>
		public static void Write(this IUntypedChannel self, object value, ITwoPhaseOffer offer)
		{
			var res = WriteAsync(self, value, offer).WaitForTask();

			if (res.Exception != null)
			{
				if (res.Exception.Flatten().InnerExceptions.Count == 1)
					throw res.Exception.InnerException;

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
			return WriteAsync(self, value, null);
		}

		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		public static Task<object> ReadAsync(this IUntypedChannel self)
		{
			return ReadAsync(self, null);
		}

		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="timeout">The read timeout.</param>
		public static Task<object> ReadAsync(this IUntypedChannel self, TimeSpan timeout)
		{
			return UntypedAccessMethods.CreateReadAccessor(self).ReadAsync(self, new TimeoutOffer(timeout));
		}

		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="timeout">The read timeout.</param>
		/// <param name="cancelToken">The cancellation token</param>
		public static Task<object> ReadAsync(this IUntypedChannel self, TimeSpan timeout, CancellationToken cancelToken)
		{
			return UntypedAccessMethods.CreateReadAccessor(self).ReadAsync(self, new TimeoutOffer(timeout, cancelToken));
		}

		/// <summary>
		/// Reads the channel asynchronously.
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to read.</param>
		/// <param name="offer">The two-phase offer.</param>
		public static Task<object> ReadAsync(this IUntypedChannel self, ITwoPhaseOffer offer)
		{
			return UntypedAccessMethods.CreateReadAccessor(self).ReadAsync(self, offer);
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
			return UntypedAccessMethods.CreateWriteAccessor(self).WriteAsync(self, value, new TimeoutOffer(timeout));
		}

		/// <summary>
		/// Writes the channel asynchronously with a cancellation token.
		/// </summary>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="cancelToken">The cancellation token</param>
		/// <returns></returns>
		public static Task WriteAsync(this IUntypedChannel self, object value, CancellationToken cancelToken)
		{
			return self.WriteAsync(value, Timeout.Infinite, cancelToken);
		}

		/// <summary>
		/// Writes the channel asynchronously
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to write.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="timeout">The write timeout.</param>
		/// <param name="cancelToken">The cancellation token</param>
		public static Task WriteAsync(this IUntypedChannel self, object value, TimeSpan timeout, CancellationToken cancelToken)
		{
			return UntypedAccessMethods.CreateWriteAccessor(self).WriteAsync(self, value, new TimeoutOffer(timeout, cancelToken));
		}

		/// <summary>
		/// Writes the channel asynchronously
		/// </summary>
		/// <returns>The task for awaiting completion.</returns>
		/// <param name="self">The channel to write.</param>
		/// <param name="offer">The two-phase offer.</param>
		/// <param name="value">The value to write.</param>
		public static Task WriteAsync(this IUntypedChannel self, object value, ITwoPhaseOffer offer)
		{
			return UntypedAccessMethods.CreateWriteAccessor(self).WriteAsync(self, value, offer);
		}

		#endregion
	}
}

