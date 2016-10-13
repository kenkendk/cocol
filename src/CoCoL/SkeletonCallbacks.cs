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
		public static Task EmitAsync<T>(IEnumerable<T> data, Func<IReadChannel<T>, Task> output)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan = ChannelManager.CreateChannel<T>();

			return Task.WhenAll(
				output(chan),
				Skeletons.EmitAsync(data, chan)
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

		/// <summary>
		/// Performs a pipeline operation by reading the input and passing it through all the handler functions in turn
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The handler function, transforming input to output.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static Task PipelineAsync<TInput, TOutput>(
			Func<TInput, Task<TOutput>> handler,
			IReadChannel<TInput> input, Func<IReadChannel<TOutput>, Task> output)
		{

			var chanout = ChannelManager.CreateChannel<TOutput>();

			return Task.WhenAll(
				output(chanout),
				Skeletons.PipelineAsync(handler, input, chanout)
			);
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
		/// <param name="handler7">The secenth handler function.</param>
		/// <param name="handler8">The eight handler function.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="TInput">The input data type parameter.</typeparam>
		/// <typeparam name="TIntermediate1">The data type between the handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static Task PipelineAsync<TInput, TIntermediate1, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TOutput>> handler2,
			IReadChannel<TInput> input, Func<IReadChannel<TOutput>, Task> output)
		{

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chanout = ChannelManager.CreateChannel<TOutput>();

			return Task.WhenAll(
				output(chanout),
				Skeletons.PipelineAsync(handler1, handler2, input, chanout)
			);
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
		/// <typeparam name="TIntermediate2">The data type between the second and final handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static Task PipelineAsync<TInput, TIntermediate1, TIntermediate2, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TOutput>> handler3,
			IReadChannel<TInput> input, Func<IReadChannel<TOutput>, Task> output)
		{

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chan2 = ChannelManager.CreateChannel<TIntermediate2>();
			var chanout = ChannelManager.CreateChannel<TOutput>();

			return Task.WhenAll(
				output(chanout),
				Skeletons.PipelineAsync(handler1, handler2, handler3, input, chanout)
			);
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
		/// <typeparam name="TIntermediate3">The data type between the third and final handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static Task PipelineAsync<TInput, TIntermediate1, TIntermediate2, TIntermediate3, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TIntermediate3>> handler3,
			Func<TIntermediate3, Task<TOutput>> handler4,
			IReadChannel<TInput> input, Func<IReadChannel<TOutput>, Task> output)
		{

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chan2 = ChannelManager.CreateChannel<TIntermediate2>();
			var chan3 = ChannelManager.CreateChannel<TIntermediate3>();
			var chanout = ChannelManager.CreateChannel<TOutput>();

			return Task.WhenAll(
				output(chanout),
				Skeletons.PipelineAsync(handler1, handler2, handler3, handler4, input, chanout)
			);
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
		/// <typeparam name="TIntermediate4">The data type between the fourth and final handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static Task PipelineAsync<TInput, TIntermediate1, TIntermediate2, TIntermediate3, TIntermediate4, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TIntermediate3>> handler3,
			Func<TIntermediate3, Task<TIntermediate4>> handler4,
			Func<TIntermediate4, Task<TOutput>> handler5,
			IReadChannel<TInput> input, Func<IReadChannel<TOutput>, Task> output)
		{

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chan2 = ChannelManager.CreateChannel<TIntermediate2>();
			var chan3 = ChannelManager.CreateChannel<TIntermediate3>();
			var chan4 = ChannelManager.CreateChannel<TIntermediate4>();
			var chanout = ChannelManager.CreateChannel<TOutput>();

			return Task.WhenAll(
				output(chanout),
				Skeletons.PipelineAsync(handler1, handler2, handler3, handler4, handler5, input, chanout)
			);
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
		/// <typeparam name="TIntermediate5">The data type between the fifth and final handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static Task PipelineAsync<TInput, TIntermediate1, TIntermediate2, TIntermediate3, TIntermediate4, TIntermediate5, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TIntermediate3>> handler3,
			Func<TIntermediate3, Task<TIntermediate4>> handler4,
			Func<TIntermediate4, Task<TIntermediate5>> handler5,
			Func<TIntermediate5, Task<TOutput>> handler6,
			IReadChannel<TInput> input, Func<IReadChannel<TOutput>, Task> output)
		{

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chan2 = ChannelManager.CreateChannel<TIntermediate2>();
			var chan3 = ChannelManager.CreateChannel<TIntermediate3>();
			var chan4 = ChannelManager.CreateChannel<TIntermediate4>();
			var chan5 = ChannelManager.CreateChannel<TIntermediate5>();
			var chanout = ChannelManager.CreateChannel<TOutput>();

			return Task.WhenAll(
				output(chanout),
				Skeletons.PipelineAsync(handler1, handler2, handler3, handler4, handler5, handler6, input, chanout)
			);
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
		/// <typeparam name="TIntermediate6">The data type between the sixth and final handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static Task PipelineAsync<TInput, TIntermediate1, TIntermediate2, TIntermediate3, TIntermediate4, TIntermediate5, TIntermediate6, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TIntermediate3>> handler3,
			Func<TIntermediate3, Task<TIntermediate4>> handler4,
			Func<TIntermediate4, Task<TIntermediate5>> handler5,
			Func<TIntermediate5, Task<TIntermediate6>> handler6,
			Func<TIntermediate6, Task<TOutput>> handler7,
			IReadChannel<TInput> input, Func<IReadChannel<TOutput>, Task> output)
		{

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chan2 = ChannelManager.CreateChannel<TIntermediate2>();
			var chan3 = ChannelManager.CreateChannel<TIntermediate3>();
			var chan4 = ChannelManager.CreateChannel<TIntermediate4>();
			var chan5 = ChannelManager.CreateChannel<TIntermediate5>();
			var chan6 = ChannelManager.CreateChannel<TIntermediate6>();
			var chanout = ChannelManager.CreateChannel<TOutput>();

			return Task.WhenAll(
				output(chanout),
				Skeletons.PipelineAsync(handler1, handler2, handler3, handler4, handler5, handler6, handler7, input, chanout)
			);
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
		public static Task PipelineAsync<TInput, TIntermediate1, TIntermediate2, TIntermediate3, TIntermediate4, TIntermediate5, TIntermediate6, TIntermediate7, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TIntermediate2>> handler2,
			Func<TIntermediate2, Task<TIntermediate3>> handler3,
			Func<TIntermediate3, Task<TIntermediate4>> handler4,
			Func<TIntermediate4, Task<TIntermediate5>> handler5,
			Func<TIntermediate5, Task<TIntermediate6>> handler6,
			Func<TIntermediate6, Task<TIntermediate7>> handler7,
			Func<TIntermediate7, Task<TOutput>> handler8,
			IReadChannel<TInput> input, Func<IReadChannel<TOutput>, Task> output)
		{

			var chanout = ChannelManager.CreateChannel<TOutput>();

			return Task.WhenAll(
				output(chanout),
				Skeletons.PipelineAsync(handler1, handler2, handler3, handler4, handler5, handler6, handler7, handler8, input, chanout)
			);
		}

	}
}

