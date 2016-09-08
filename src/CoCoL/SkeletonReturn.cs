using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// Skeleton methods that return the channels
	/// </summary>
	public static class SkeletonReturn
	{
		/// <summary>
		/// Method that transforms the input and returns the output
		/// </summary>
		/// <returns>The channel with the results</returns>
		/// <param name="handler">The method used to transform the input to the output.</param>
		/// <param name="input">The input channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static IReadChannel<TOutput> WrapperAsync<TInput, TOutput>(Func<TInput, Task<TOutput>> handler, IReadChannel<TInput> input)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			var chan = ChannelManager.CreateChannel<TOutput>();
			Skeletons.WrapperAsync(handler, input, chan);
			return chan;
		}

		/// <summary>
		/// Wraps an enumerator as a generator
		/// </summary>
		/// <returns>The channel with the results</returns>
		/// <param name="data">The data to emit.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static IReadChannel<T> DataSourceAsync<T>(IEnumerable<T> data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			var chan = ChannelManager.CreateChannel<T>();
			Skeletons.DataSourceAsync(data, chan);
			return chan;
		}

		/// <summary>
		/// Performs an asynchronous broadcast that copies the input value to all outputs
		/// </summary>
		/// <returns>The broadcast channels</returns>
		/// <param name="input">The input channel.</param>
		/// <param name="count">The number of channels to broadcast to</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static IReadChannel<T>[] BroadcastAsync<T>(int count, IReadChannel<T> input)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			var chan = Enumerable.Range(0, count).Select(x => ChannelManager.CreateChannel<T>()).ToArray();
			Skeletons.BroadcastAsync(input, chan);
			return chan;
		}

		/// <summary>
		/// Performs a gather operation that reads one input from each channel and sends all collected values to the output
		/// </summary>
		/// <returns>A channel with the results</returns>
		/// <param name="inputs">The input channels.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static IReadChannel<T[]> GatherAllAsync<T>(IReadChannel<T>[] inputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));

			var chan = ChannelManager.CreateChannel<T[]>();
			Skeletons.GatherAllAsync(inputs, chan);
			return chan;
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>The parallel output channels</returns>
		/// <param name="handler">The handler function, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static IReadChannel<TOutput>[] ParallelAsync<TInput, TOutput>(Func<IReadChannel<TInput>, IWriteChannel<TOutput>, Task> handler, IReadChannel<TInput>[] inputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			var chans = inputs.Select(x => ChannelManager.CreateChannel<TOutput>()).ToArray();
			Skeletons.ParallelAsync(handler, inputs, chans);
			return chans;
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>The parallel output channels</returns>
		/// <param name="handlers">The handler functions, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static IReadChannel<TOutput>[] ParallelAsync<TInput, TOutput>(Func<IReadChannel<TInput>, IWriteChannel<TOutput>, Task>[] handlers, IReadChannel<TInput>[] inputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handlers == null)
				throw new ArgumentNullException(nameof(handlers));

			if (handlers.Length != inputs.Length)
				throw new ArgumentOutOfRangeException(nameof(inputs), $"The {nameof(inputs)} and {nameof(handlers)} arrays must be of the same length");

			var chans = inputs.Select(x => ChannelManager.CreateChannel<TOutput>()).ToArray();
			Skeletons.ParallelAsync(handlers, inputs, chans);
			return chans;
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>The parallel output channels</returns>
		/// <param name="handlers">The handler functions, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static IReadChannel<TOutput>[] ParallelAsync<TInput, TOutput>(Func<TInput, Task<TOutput>>[] handlers, IReadChannel<TInput>[] inputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handlers == null)
				throw new ArgumentNullException(nameof(handlers));

			if (inputs.Length != handlers.Length)
				throw new ArgumentOutOfRangeException(nameof(inputs), $"The {nameof(inputs)} and {nameof(handlers)} arrays must be of the same length");

			var chans = inputs.Select(x => ChannelManager.CreateChannel<TOutput>()).ToArray();
			Skeletons.ParallelAsync(handlers, inputs, chans);
			return chans;
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>The parallel output channels</returns>
		/// <param name="handler">The handler function, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static IReadChannel<TOutput>[] ParallelAsync<TInput, TOutput>(Func<TInput, Task<KeyValuePair<bool, TOutput>>> handler, IReadChannel<TInput>[] inputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			var chans = inputs.Select(x => ChannelManager.CreateChannel<TOutput>()).ToArray();
			Skeletons.ParallelAsync(handler, inputs, chans);
			return chans;
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>The parallel output channels</returns>
		/// <param name="handlers">The handler functions, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static IReadChannel<TOutput>[] ParallelAsync<TInput, TOutput>(Func<TInput, Task<KeyValuePair<bool, TOutput>>>[] handlers, IReadChannel<TInput>[] inputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handlers == null)
				throw new ArgumentNullException(nameof(handlers));

			var chans = handlers.Select(x => ChannelManager.CreateChannel<TOutput>()).ToArray();
			Skeletons.ParallelAsync(handlers, inputs, chans);
			return chans;
		}
	}
}

