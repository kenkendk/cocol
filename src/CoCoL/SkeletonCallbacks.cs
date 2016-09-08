using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// Skeleton methods for the callback syntax
	/// </summary>
	public static class SkeletonCallbacks
	{
		/// <summary>
		/// Method that transforms the input and returns the output
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The method used to transform the input to the output.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static Task WrapperAsync<TInput, TOutput>(Func<TInput, Task<TOutput>> handler, IReadChannel<TInput> input, Func<IReadChannel<TOutput>, Task> output)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan = ChannelManager.CreateChannel<TOutput>();

			return Task.WhenAll(
				output(chan),
				Skeletons.WrapperAsync(handler, input, chan)
			);
		}

		/// <summary>
		/// Wraps an enumerator as a generator
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="data">The data to emit.</param>
		/// <param name="output">The channel to write to.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task DataSourceAsync<T>(IEnumerable<T> data, Func<IReadChannel<T>, Task> output)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan = ChannelManager.CreateChannel<T>();

			return Task.WhenAll(
				output(chan),
				Skeletons.DataSourceAsync(data, chan)
			);
		}

		/// <summary>
		/// Performs an asynchronous broadcast that copies the input value to all outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="input">The input channel.</param>
		/// <param name="count">The number of channels to broadcast to</param>
		/// <param name="output">The output channels.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task BroadcastAsync<T>(IReadChannel<T> input, int count, Func<IReadChannel<T>[], Task> output)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan = Enumerable.Range(0, count).Select(x => ChannelManager.CreateChannel<T>()).ToArray();

			return Task.WhenAll(
				output(chan),
				Skeletons.BroadcastAsync(input, chan)
			);
		}

		/// <summary>
		/// Performs a gather operation that reads one input from each channel and sends all collected values to the output
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="inputs">The input channels.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task GatherAllAsync<T>(IReadChannel<T>[] inputs, Func<IReadChannel<T[]>, Task> output)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan = ChannelManager.CreateChannel<T[]>();

			return Task.WhenAll(
				output(chan),
				Skeletons.GatherAllAsync(inputs, chan)
			);
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The handler function, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="output">The output channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static Task ParallelAsync<TInput, TOutput>(Func<IReadChannel<TInput>, IWriteChannel<TOutput>, Task> handler, IReadChannel<TInput>[] inputs, Func<IReadChannel<TOutput>[], Task> output)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			var chans = inputs.Select(x => ChannelManager.CreateChannel<TOutput>()).ToArray();

			return Task.WhenAll(
				output(chans),
				Skeletons.ParallelAsync(handler, inputs, chans)
			);
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handlers">The handler functions, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="output">The output channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static Task ParallelAsync<TInput, TOutput>(Func<IReadChannel<TInput>, IWriteChannel<TOutput>, Task>[] handlers, IReadChannel<TInput>[] inputs, Func<IReadChannel<TOutput>[], Task> output)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handlers == null)
				throw new ArgumentNullException(nameof(handlers));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			if (handlers.Length != inputs.Length)
				throw new ArgumentOutOfRangeException(nameof(inputs), $"The {nameof(inputs)} and {nameof(output)} arrays must be of the same length");

			var chans = inputs.Select(x => ChannelManager.CreateChannel<TOutput>()).ToArray();

			return Task.WhenAll(
				output(chans),
				Skeletons.ParallelAsync(handlers, inputs, chans)
			);
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handlers">The handler functions, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="output">The output channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static Task ParallelAsync<TInput, TOutput>(Func<TInput, Task<TOutput>>[] handlers, IReadChannel<TInput>[] inputs, Func<IReadChannel<TOutput>[], Task> output)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handlers == null)
				throw new ArgumentNullException(nameof(handlers));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			if (inputs.Length != handlers.Length)
				throw new ArgumentOutOfRangeException(nameof(inputs), $"The {nameof(inputs)} and {nameof(handlers)} arrays must be of the same length");

			var chans = inputs.Select(x => ChannelManager.CreateChannel<TOutput>()).ToArray();

			return Task.WhenAll(
				output(chans),
				Skeletons.ParallelAsync(handlers, inputs, chans)
			);
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The handler function, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="output">The output channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static Task ParallelAsync<TInput, TOutput>(Func<TInput, Task<KeyValuePair<bool, TOutput>>> handler, IReadChannel<TInput>[] inputs, Func<IReadChannel<TOutput>[], Task> output)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			var chans = inputs.Select(x => ChannelManager.CreateChannel<TOutput>()).ToArray();

			return Task.WhenAll(
				output(chans),
				Skeletons.ParallelAsync(handler, inputs, chans)
			);
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handlers">The handler functions, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="output">The output channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static Task ParallelAsync<TInput, TOutput>(Func<TInput, Task<KeyValuePair<bool, TOutput>>>[] handlers, IReadChannel<TInput>[] inputs, Func<IReadChannel<TOutput>[], Task> output)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handlers == null)
				throw new ArgumentNullException(nameof(handlers));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chans = handlers.Select(x => ChannelManager.CreateChannel<TOutput>()).ToArray();

			return Task.WhenAll(
				output(chans),
				Skeletons.ParallelAsync(handlers, inputs, chans)
			);
		}

	}
}

