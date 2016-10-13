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
		public static IReadChannel<T> EmitAsync<T>(IEnumerable<T> data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			var chan = ChannelManager.CreateChannel<T>();
			Skeletons.EmitAsync(data, chan);
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

		/// <summary>
		/// Performs a pipeline operation by reading the input and passing it through all the handler functions in turn
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The handler function, transforming input to output.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TIntermediate1">The data type between the first and second handlers.</typeparam>
		/// <typeparam name="TIntermediate2">The data type between the second and third handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static IReadChannel<TOutput> PipelineAsync<TInput, TOutput>(
			Func<TInput, Task<TOutput>> handler,
			IReadChannel<TInput> input)
		{
			var chanout = ChannelManager.CreateChannel<TOutput>();

			Skeletons.PipelineAsync(handler, input, chanout);
			return chanout;
		}

		/// <summary>
		/// Performs a pipeline operation by reading the input and passing it through all the handler functions in turn
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler1">The first handler function.</param>
		/// <param name="handler2">The second handler function.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TIntermediate1">The data type between the first and second handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static IReadChannel<TOutput> PipelineAsync<TInput, TIntermediate1, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TOutput>> handler2,
			IReadChannel<TInput> input)
		{
			var chanout = ChannelManager.CreateChannel<TOutput>();

			Skeletons.PipelineAsync(handler1, handler2, input, chanout);
			return chanout;
		}

		/// <summary>
		/// Performs a pipeline operation by reading the input and passing it through all the handler functions in turn
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler1">The first handler function.</param>
		/// <param name="handler2">The second handler function.</param>
		/// <param name="handler3">The third handler function.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TIntermediate1">The data type between the first and second handlers.</typeparam>
		/// <typeparam name="TIntermediate2">The data type between the second and third handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static IReadChannel<TOutput> PipelineAsync<TInput, TIntermediate1, TIntermediate2, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TOutput>> handler3,
			IReadChannel<TInput> input)
		{
			var chanout = ChannelManager.CreateChannel<TOutput>();

			Skeletons.PipelineAsync(handler1, handler2, handler3, input, chanout);
			return chanout;
		}

		/// <summary>
		/// Performs a pipeline operation by reading the input and passing it through all the handler functions in turn
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler1">The first handler function.</param>
		/// <param name="handler2">The second handler function.</param>
		/// <param name="handler3">The third handler function.</param>
		/// <param name="handler4">The fourth handler function.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TIntermediate1">The data type between the first and second handlers.</typeparam>
		/// <typeparam name="TIntermediate2">The data type between the second and third handlers.</typeparam>
		/// <typeparam name="TIntermediate3">The data type between the third and fourth handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static IReadChannel<TOutput> PipelineAsync<TInput, TIntermediate1, TIntermediate2, TIntermediate3, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TIntermediate3>> handler3,
			Func<TIntermediate3, Task<TOutput>> handler4,
			IReadChannel<TInput> input)
		{
			var chanout = ChannelManager.CreateChannel<TOutput>();

			Skeletons.PipelineAsync(handler1, handler2, handler3, handler4, input, chanout);
			return chanout;
		}

		/// <summary>
		/// Performs a pipeline operation by reading the input and passing it through all the handler functions in turn
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler1">The first handler function.</param>
		/// <param name="handler2">The second handler function.</param>
		/// <param name="handler3">The third handler function.</param>
		/// <param name="handler4">The fourth handler function.</param>
		/// <param name="handler5">The fifth handler function.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TIntermediate1">The data type between the first and second handlers.</typeparam>
		/// <typeparam name="TIntermediate2">The data type between the second and third handlers.</typeparam>
		/// <typeparam name="TIntermediate3">The data type between the third and fourth handlers.</typeparam>
		/// <typeparam name="TIntermediate4">The data type between the fourth and fifth handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static IReadChannel<TOutput> PipelineAsync<TInput, TIntermediate1, TIntermediate2, TIntermediate3, TIntermediate4, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TIntermediate3>> handler3,
			Func<TIntermediate3, Task<TIntermediate4>> handler4,
			Func<TIntermediate4, Task<TOutput>> handler5,
			IReadChannel<TInput> input)
		{
			var chanout = ChannelManager.CreateChannel<TOutput>();

			Skeletons.PipelineAsync(handler1, handler2, handler3, handler4, handler5, input, chanout);
			return chanout;

		}

		/// <summary>
		/// Performs a pipeline operation by reading the input and passing it through all the handler functions in turn
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler1">The first handler function.</param>
		/// <param name="handler2">The second handler function.</param>
		/// <param name="handler3">The third handler function.</param>
		/// <param name="handler4">The fourth handler function.</param>
		/// <param name="handler5">The fifth handler function.</param>
		/// <param name="handler6">The sixth handler function.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TIntermediate1">The data type between the first and second handlers.</typeparam>
		/// <typeparam name="TIntermediate2">The data type between the second and third handlers.</typeparam>
		/// <typeparam name="TIntermediate3">The data type between the third and fourth handlers.</typeparam>
		/// <typeparam name="TIntermediate4">The data type between the fourth and fifth handlers.</typeparam>
		/// <typeparam name="TIntermediate5">The data type between the fifth and sixth handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static IReadChannel<TOutput> PipelineAsync<TInput, TIntermediate1, TIntermediate2, TIntermediate3, TIntermediate4, TIntermediate5, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TIntermediate3>> handler3,
			Func<TIntermediate3, Task<TIntermediate4>> handler4,
			Func<TIntermediate4, Task<TIntermediate5>> handler5,
			Func<TIntermediate5, Task<TOutput>> handler6,
			IReadChannel<TInput> input)
		{
			var chanout = ChannelManager.CreateChannel<TOutput>();

			Skeletons.PipelineAsync(handler1, handler2, handler3, handler4, handler5, handler6, input, chanout);
			return chanout;

		}

		/// <summary>
		/// Performs a pipeline operation by reading the input and passing it through all the handler functions in turn
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler1">The first handler function.</param>
		/// <param name="handler2">The second handler function.</param>
		/// <param name="handler3">The third handler function.</param>
		/// <param name="handler4">The fourth handler function.</param>
		/// <param name="handler5">The fifth handler function.</param>
		/// <param name="handler6">The sixth handler function.</param>
		/// <param name="handler7">The seventh handler function.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TIntermediate1">The data type between the first and second handlers.</typeparam>
		/// <typeparam name="TIntermediate2">The data type between the second and third handlers.</typeparam>
		/// <typeparam name="TIntermediate3">The data type between the third and fourth handlers.</typeparam>
		/// <typeparam name="TIntermediate4">The data type between the fourth and fifth handlers.</typeparam>
		/// <typeparam name="TIntermediate5">The data type between the fifth and sixth handlers.</typeparam>
		/// <typeparam name="TIntermediate6">The data type between the sixth and seventh handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static IReadChannel<TOutput> PipelineAsync<TInput, TIntermediate1, TIntermediate2, TIntermediate3, TIntermediate4, TIntermediate5, TIntermediate6, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TIntermediate3>> handler3,
			Func<TIntermediate3, Task<TIntermediate4>> handler4,
			Func<TIntermediate4, Task<TIntermediate5>> handler5,
			Func<TIntermediate5, Task<TIntermediate6>> handler6,
			Func<TIntermediate6, Task<TOutput>> handler7,
			IReadChannel<TInput> input)
		{
			var chanout = ChannelManager.CreateChannel<TOutput>();

			Skeletons.PipelineAsync(handler1, handler2, handler3, handler4, handler5, handler6, handler7, input, chanout);
			return chanout;
		}

		/// <summary>
		/// Performs a pipeline operation by reading the input and passing it through all the handler functions in turn
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler1">The first handler function.</param>
		/// <param name="handler2">The second handler function.</param>
		/// <param name="handler3">The third handler function.</param>
		/// <param name="handler4">The fourth handler function.</param>
		/// <param name="handler5">The fifth handler function.</param>
		/// <param name="handler6">The sixth handler function.</param>
		/// <param name="handler7">The seventh handler function.</param>
		/// <param name="handler8">The eight handler function.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TIntermediate1">The data type between the first and second handlers.</typeparam>
		/// <typeparam name="TIntermediate2">The data type between the second and third handlers.</typeparam>
		/// <typeparam name="TIntermediate3">The data type between the third and fourth handlers.</typeparam>
		/// <typeparam name="TIntermediate4">The data type between the fourth and fifth handlers.</typeparam>
		/// <typeparam name="TIntermediate5">The data type between the fifth and sixth handlers.</typeparam>
		/// <typeparam name="TIntermediate6">The data type between the sixth and seventh handlers.</typeparam>
		/// <typeparam name="TIntermediate7">The data type between the seventh and final handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static IReadChannel<TOutput> PipelineAsync<TInput, TIntermediate1, TIntermediate2, TIntermediate3, TIntermediate4, TIntermediate5, TIntermediate6, TIntermediate7, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TIntermediate3>> handler3,
			Func<TIntermediate3, Task<TIntermediate4>> handler4,
			Func<TIntermediate4, Task<TIntermediate5>> handler5,
			Func<TIntermediate5, Task<TIntermediate6>> handler6,
			Func<TIntermediate6, Task<TIntermediate7>> handler7,
			Func<TIntermediate7, Task<TOutput>> handler8,
			IReadChannel<TInput> input)
		{
			var chanout = ChannelManager.CreateChannel<TOutput>();

			Skeletons.PipelineAsync(handler1, handler2, handler3, handler4, handler5, handler6, handler7, handler8, input, chanout);
			return chanout;
		}
	}
}

