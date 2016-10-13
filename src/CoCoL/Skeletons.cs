using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// Implementations of skeleton methods, as found in RISC-pb2l
	/// </summary>
	public static class Skeletons
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
		public static Task WrapperAsync<TInput, TOutput>(Func<TInput, Task<TOutput>> handler, IReadChannel<TInput> input, IWriteChannel<TOutput> output)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			return AutomationExtensions.RunTask(new { input = input, output = output }, async (self) => {
				while (true)
					await self.output.WriteAsync(await handler(await self.input.ReadAsync()));
			});
		}

		/// <summary>
		/// Wraps an enumerator as a generator
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="data">The data to emit.</param>
		/// <param name="output">The channel to write to.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task EmitAsync<T>(IEnumerable<T> data, IWriteChannel<T> output)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			return AutomationExtensions.RunTask(
				new { output = output },
				async (self) =>
				{
					foreach (var entry in data)
						await output.WriteAsync(entry);
				}
			);
		}

		/// <summary>
		/// Returns an enumerator from a channel
		/// </summary>
		/// <returns>The enumerator.</returns>
		/// <param name="input">The data to read from.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static IEnumerable<T> ChannelEnumerator<T>(IReadChannel<T> input)
		{
			while (true)
				yield return input.Read();
		}

		/// <summary>
		/// Wraps an input as an enumerator
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The handler function operating on the data.</param>
		/// <param name="input">The channel to read from.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task CollectAsync<T>(Func<IEnumerable<T>, Task> handler, IReadChannel<T> input)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			return AutomationExtensions.RunTask(
				new { input = input },
				(self) => handler(ChannelEnumerator(self.input))
			);
		}

		/// <summary>
		/// Wraps an input to a method
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The data to emit.</param>
		/// <param name="input">The channel to read from.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task CollectAsync<T>(Func<T, Task> handler, IReadChannel<T> input)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			return AutomationExtensions.RunTask(
				new { input = input },
				async (self) =>
				{
					while (true)
						await handler(await self.input.ReadAsync());
				}
			);
		}

		/// <summary>
		/// Forwards values from the input to the output, but allows filtering the input values
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="predicate">The function used to determine if the value should be forwarded or not.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task FilterAsync<TInput, TOutput>(Func<TInput, Task<KeyValuePair<bool, TOutput>>> predicate, IReadChannel<TInput> input, IWriteChannel<TOutput> output)
		{
			return AutomationExtensions.RunTask(
				new { input = input, output = output },
				async (self) => {
					while (true)
					{
						var data = await self.input.ReadAsync();
						var res = await predicate(data);
						if (res.Key)
							await self.output.WriteAsync(res.Value);
					}
				}
			);
		}


		/// <summary>
		/// Performs an asynchronous broadcast that copies the input value to all outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="input">The input channel.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task BroadcastAsync<T>(IReadChannel<T> input, IWriteChannel<T>[] outputs)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));
			
			return AutomationExtensions.RunTask(new { input = input, outputs = outputs }, async (self) => {
				while (true)
				{
					var data = await self.input.ReadAsync();
					await Task.WhenAll(self.outputs.Select(x => x.WriteAsync(data)));
				}
			});
		}

		/// <summary>
		/// Performs a scatter operation by reading in an array and scattering the data to the outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="input">The input channel.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task ScatterAsync<T>(IReadChannel<T[]> input, IWriteChannel<T>[] outputs)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));
			
			return AutomationExtensions.RunTask(new { input = input, outputs = outputs }, async (self) => {
				while (true)
				{
					var data = await self.input.ReadAsync();
					await Task.WhenAll(
						Enumerable.Range(0, data.Length).Select(i => self.outputs[i % outputs.Length].WriteAsync(data[i]))
					);
				}
			});
		}

		/// <summary>
		/// Performs a distribution of input values to the outputs, in a round-robin fashion
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="input">The input channel.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task UnicastRoundRobinAsync<T>(IReadChannel<T> input, IWriteChannel<T>[] outputs)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));
			
			return AutomationExtensions.RunTask(new { input = input, outputs = outputs }, async (self) =>
			{
				while (true)
					foreach (var c in self.outputs)
						await c.WriteAsync(await self.input.ReadAsync());
			});
		}

		/// <summary>
		/// Performs a controlled distribution of input values, allowing control of the distribution through the request channel 
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="input">The input channel.</param>
		/// <param name="request">The channel used to select which output to use.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task UnicastAutoAsync<T>(IReadChannel<T> input, IReadChannel<int> request, IWriteChannel<T>[] outputs)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (request == null)
				throw new ArgumentNullException(nameof(request));
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));
			
			return AutomationExtensions.RunTask(new { input = input, request = request, outputs = outputs }, async (self) =>
			{
				while (true)
					await self.outputs[await self.request.ReadAsync() % outputs.Length].WriteAsync(await self.input.ReadAsync());
			});
		}

		/// <summary>
		/// Performs a unicast that is guaranteed to not block if at least one output is available.
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="input">The input channel.</param>
		/// <param name="outputs">The output channels.</param>
		/// <param name="priority">The priority used to choose an output when multiple are available.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task UnicastAutoGuardedAsync<T>(IReadChannel<T> input, IWriteChannel<T>[] outputs, MultiChannelPriority priority = MultiChannelPriority.First)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));
			
			return AutomationExtensions.RunTask(new { input = input, outputs = outputs }, async (self) =>
			{
				var multiset = new MultiChannelSetWrite<T>(priority, self.outputs);

				while (true)					
					await multiset.WriteToAnyAsync(await self.input.ReadAsync());
			});
		}

		/// <summary>
		/// Performs a gather operation that reads one input from each channel and sends it to the output
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="inputs">The input channels.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task GatherAsync<T>(IReadChannel<T>[] inputs, IWriteChannel<T> output)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			
			return AutomationExtensions.RunTask(new { inputs = inputs, output = output }, async (self) =>
			{
				while (true)
					foreach (var c in self.inputs)
						await self.output.WriteAsync(await c.ReadAsync());
			});
		}

		/// <summary>
		/// Performs a gather operation that reads one input from each channel and sends all collected values to the output
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="inputs">The input channels.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task GatherAllAsync<T>(IReadChannel<T>[] inputs, IWriteChannel<T[]> output)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			
			return AutomationExtensions.RunTask(new { inputs = inputs, output = output }, async (self) =>
			{
				while (true)
				{
					var res = new T[self.inputs.Length];
					await Task.WhenAll(Enumerable.Range(0, res.Length).Select(async x => res[x] = await self.inputs[x].ReadAsync()));
					await self.output.WriteAsync(res);
				}
			});
		}

		/// <summary>
		/// Performs a merge operation by sending the input to the block channel and then reading it back, repeating until the conditional operation returns false
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="input">The input channel.</param>
		/// <param name="to_block">The block channel to write to.</param>
		/// <param name="from_block">The block channel to read from.</param>
		/// <param name="output">The output channel.</param>
		/// <param name="conditional">The conditional for merging.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task MergeAsync<T>(IReadChannel<T> input, IWriteChannel<T> to_block, IReadChannel<T> from_block, IWriteChannel<T> output, Func<T, Task<bool>> conditional)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (to_block == null)
				throw new ArgumentNullException(nameof(to_block));
			if (from_block == null)
				throw new ArgumentNullException(nameof(from_block));
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (conditional == null)
				throw new ArgumentNullException(nameof(conditional));

			return AutomationExtensions.RunTask(new { input = input, output = output, to_block = to_block, from_block = from_block }, async (self) =>
			{
				while (true)
				{
					var data = await self.input.ReadAsync();
					do
					{
						await self.to_block.WriteAsync(data);
						data = await self.from_block.ReadAsync();
					} while (await conditional(data));

					await self.output.WriteAsync(data);
				}
			});
		}

		/// <summary>
		/// Perform a feedback function using a combination function that refines a value, and a conditional that determines when the value is completed
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The method used to process values in the feedback process.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <param name="conditional">The conditional for merging.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task FeedbackAsync<T>(Func<IReadChannel<T>, IWriteChannel<T>, Task> handler, IReadChannel<T> input, IWriteChannel<T> output, Func<T, bool> conditional)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (conditional == null)
				throw new ArgumentNullException(nameof(conditional));
			
			return FeedbackAsync(handler, input, output, (x) => Task.FromResult(conditional(x)));
		}

		/// <summary>
		/// Perform a feedback function using a combination function that refines a value, and a conditional that determines when the value is completed
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The method used to process values in the feedback process.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <param name="conditional">The conditional for merging.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task FeedbackAsync<T>(Func<IReadChannel<T>, IWriteChannel<T>, Task> handler, IReadChannel<T> input, IWriteChannel<T> output, Func<T, Task<bool>> conditional)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (conditional == null)
				throw new ArgumentNullException(nameof(conditional));
			
			//Setup two anonymous channels
			var to_block = ChannelManager.CreateChannel<T>();
			var from_block = ChannelManager.CreateChannel<T>();

			return Task.WhenAll(
				handler(to_block, from_block),
				MergeAsync(input, to_block, from_block, output, conditional)
			);
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The handler function, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static Task ParallelAsync<TInput, TOutput>(Func<IReadChannel<TInput>, IWriteChannel<TOutput>, Task> handler, IReadChannel<TInput>[] inputs, IWriteChannel<TOutput>[] outputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return ParallelAsync(Enumerable.Range(0, inputs.Length).Select(x => handler).ToArray(), inputs, outputs);
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handlers">The handler functions, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static Task ParallelAsync<TInput, TOutput>(Func<IReadChannel<TInput>, IWriteChannel<TOutput>, Task>[] handlers, IReadChannel<TInput>[] inputs, IWriteChannel<TOutput>[] outputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handlers == null)
				throw new ArgumentNullException(nameof(handlers));
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));
			
			if (inputs.Length != outputs.Length || handlers.Length != inputs.Length)
				throw new ArgumentOutOfRangeException(nameof(inputs), $"The {nameof(inputs)} and {nameof(outputs)} arrays must be of the same length");

			return Task.WhenAll(
				Enumerable.Range(0, inputs.Length).Select(
					x => AutomationExtensions.RunTask(
						new { input = inputs[x], output = outputs[x] },
						async (self) =>
						{
							while(true)
								await handlers[x](self.input, self.output);
						}
					)
				)
			);
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The handler function, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static Task ParallelAsync<TInput, TOutput>(Func<TInput, Task<TOutput>> handler, IReadChannel<TInput>[] inputs, IWriteChannel<TOutput>[] outputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return ParallelAsync(Enumerable.Range(0, inputs.Length).Select(x => handler).ToArray(), inputs, outputs);
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
		public static Task ParallelAsync<TInput, TOutput>(Func<TInput, Task<TOutput>> handler, IReadChannel<TInput>[] inputs, Func<IReadChannel<TOutput>[], Task> output)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			var chans = inputs.Select(x => ChannelManager.CreateChannel<TOutput>()).ToArray();

			return Task.WhenAll(
				output(chans),
				ParallelAsync(handler, inputs, chans)
	       );
		}


		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handlers">The handler functions, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static Task ParallelAsync<TInput, TOutput>(Func<TInput, Task<TOutput>>[] handlers, IReadChannel<TInput>[] inputs, IWriteChannel<TOutput>[] outputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handlers == null)
				throw new ArgumentNullException(nameof(handlers));
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));

			if (inputs.Length != outputs.Length)
				throw new ArgumentOutOfRangeException(nameof(inputs), $"The {nameof(inputs)} and {nameof(outputs)} arrays must be of the same length");

			return Task.WhenAll(
				Enumerable.Range(0, inputs.Length).Select(
					x => AutomationExtensions.RunTask(
						new { input = inputs[x], output = outputs[x] },
						async (self) =>
						{
							while(true)
								await self.output.WriteAsync(await handlers[x](await self.input.ReadAsync()));
						}
					)
				)
			);
		}


		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handler">The handler function, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static Task ParallelAsync<TInput, TOutput>(Func<TInput, Task<KeyValuePair<bool, TOutput>>> handler, IReadChannel<TInput>[] inputs, IWriteChannel<TOutput>[] outputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return ParallelAsync(Enumerable.Range(0, inputs.Length).Select(x => handler).ToArray(), inputs, outputs);
		}

		/// <summary>
		/// Performs a parallel transformation of all inputs to their outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handlers">The handler functions, transforming input to output.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		public static Task ParallelAsync<TInput, TOutput>(Func<TInput, Task<KeyValuePair<bool, TOutput>>>[] handlers, IReadChannel<TInput>[] inputs, IWriteChannel<TOutput>[] outputs)
		{
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (handlers == null)
				throw new ArgumentNullException(nameof(handlers));
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));

			if (inputs.Length != outputs.Length)
				throw new ArgumentOutOfRangeException(nameof(inputs), $"The {nameof(inputs)} and {nameof(outputs)} arrays must be of the same length");

			return Task.WhenAll(
				Enumerable.Range(0, inputs.Length).Select(
					x => AutomationExtensions.RunTask(
						new { input = inputs[x], output = outputs[x] },
						async (self) =>
						{
							while (true)
							{
								var res = await handlers[x](await self.input.ReadAsync());
								if (res.Key)
									await self.output.WriteAsync(res.Value);
							}
						}
					)
				)
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
		public static Task PipelineAsync<TInput, TOutput>(Func<TInput, Task<TOutput>> handler, IReadChannel<TInput> input, IWriteChannel<TOutput> output)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			return WrapperAsync(handler, input, output);
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
		/// <typeparam name="TIntermediate1">The data type between the handlers.</typeparam>
		/// <typeparam name="TOutput">The output data type parameter.</typeparam>
		public static Task PipelineAsync<TInput, TIntermediate1, TOutput>(
			Func<TInput, Task<TIntermediate1>> handler1,
			Func<TIntermediate1, Task<TOutput>> handler2,
			IReadChannel<TInput> input, IWriteChannel<TOutput> output)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (handler1 == null)
				throw new ArgumentNullException(nameof(handler1));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();

			return Task.WhenAll(
				WrapperAsync(handler1, input, chan1),
				WrapperAsync(handler2, chan1, output)
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
			IReadChannel<TInput> input, IWriteChannel<TOutput> output)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (handler1 == null)
				throw new ArgumentNullException(nameof(handler1));
			if (handler2 == null)
				throw new ArgumentNullException(nameof(handler2));
			if (handler3 == null)
				throw new ArgumentNullException(nameof(handler3));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chan2 = ChannelManager.CreateChannel<TIntermediate2>();

			return Task.WhenAll(
				WrapperAsync(handler1, input, chan1),
				WrapperAsync(handler2, chan1, chan2),
				WrapperAsync(handler3, chan2, output)
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
			IReadChannel<TInput> input, IWriteChannel<TOutput> output)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (handler1 == null)
				throw new ArgumentNullException(nameof(handler1));
			if (handler2 == null)
				throw new ArgumentNullException(nameof(handler2));
			if (handler3 == null)
				throw new ArgumentNullException(nameof(handler3));
			if (handler4 == null)
				throw new ArgumentNullException(nameof(handler4));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chan2 = ChannelManager.CreateChannel<TIntermediate2>();
			var chan3 = ChannelManager.CreateChannel<TIntermediate3>();

			return Task.WhenAll(
				WrapperAsync(handler1, input, chan1),
				WrapperAsync(handler2, chan1, chan2),
				WrapperAsync(handler3, chan2, chan3),
				WrapperAsync(handler4, chan3, output)
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
			IReadChannel<TInput> input, IWriteChannel<TOutput> output)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (handler1 == null)
				throw new ArgumentNullException(nameof(handler1));
			if (handler2 == null)
				throw new ArgumentNullException(nameof(handler2));
			if (handler3 == null)
				throw new ArgumentNullException(nameof(handler3));
			if (handler4 == null)
				throw new ArgumentNullException(nameof(handler4));
			if (handler5 == null)
				throw new ArgumentNullException(nameof(handler5));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chan2 = ChannelManager.CreateChannel<TIntermediate2>();
			var chan3 = ChannelManager.CreateChannel<TIntermediate3>();
			var chan4 = ChannelManager.CreateChannel<TIntermediate4>();

			return Task.WhenAll(
				WrapperAsync(handler1, input, chan1),
				WrapperAsync(handler2, chan1, chan2),
				WrapperAsync(handler3, chan2, chan3),
				WrapperAsync(handler4, chan3, chan4),
				WrapperAsync(handler5, chan4, output)
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
			IReadChannel<TInput> input, IWriteChannel<TOutput> output)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (handler1 == null)
				throw new ArgumentNullException(nameof(handler1));
			if (handler2 == null)
				throw new ArgumentNullException(nameof(handler2));
			if (handler3 == null)
				throw new ArgumentNullException(nameof(handler3));
			if (handler4 == null)
				throw new ArgumentNullException(nameof(handler4));
			if (handler5 == null)
				throw new ArgumentNullException(nameof(handler5));
			if (handler6 == null)
				throw new ArgumentNullException(nameof(handler6));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chan2 = ChannelManager.CreateChannel<TIntermediate2>();
			var chan3 = ChannelManager.CreateChannel<TIntermediate3>();
			var chan4 = ChannelManager.CreateChannel<TIntermediate4>();
			var chan5 = ChannelManager.CreateChannel<TIntermediate5>();

			return Task.WhenAll(
				WrapperAsync(handler1, input, chan1),
				WrapperAsync(handler2, chan1, chan2),
				WrapperAsync(handler3, chan2, chan3),
				WrapperAsync(handler4, chan3, chan4),
				WrapperAsync(handler5, chan4, chan5),
				WrapperAsync(handler6, chan5, output)
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
			IReadChannel<TInput> input, IWriteChannel<TOutput> output)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (handler1 == null)
				throw new ArgumentNullException(nameof(handler1));
			if (handler2 == null)
				throw new ArgumentNullException(nameof(handler2));
			if (handler3 == null)
				throw new ArgumentNullException(nameof(handler3));
			if (handler4 == null)
				throw new ArgumentNullException(nameof(handler4));
			if (handler5 == null)
				throw new ArgumentNullException(nameof(handler5));
			if (handler6 == null)
				throw new ArgumentNullException(nameof(handler6));
			if (handler7 == null)
				throw new ArgumentNullException(nameof(handler7));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chan2 = ChannelManager.CreateChannel<TIntermediate2>();
			var chan3 = ChannelManager.CreateChannel<TIntermediate3>();
			var chan4 = ChannelManager.CreateChannel<TIntermediate4>();
			var chan5 = ChannelManager.CreateChannel<TIntermediate5>();
			var chan6 = ChannelManager.CreateChannel<TIntermediate6>();

			return Task.WhenAll(
				WrapperAsync(handler1, input, chan1),
				WrapperAsync(handler2, chan1, chan2),
				WrapperAsync(handler3, chan2, chan3),
				WrapperAsync(handler4, chan3, chan4),
				WrapperAsync(handler5, chan4, chan5),
				WrapperAsync(handler6, chan5, chan6),
				WrapperAsync(handler7, chan6, output)
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
			IReadChannel<TInput> input, IWriteChannel<TOutput> output)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (handler1 == null)
				throw new ArgumentNullException(nameof(handler1));
			if (handler2 == null)
				throw new ArgumentNullException(nameof(handler2));
			if (handler3 == null)
				throw new ArgumentNullException(nameof(handler3));
			if (handler4 == null)
				throw new ArgumentNullException(nameof(handler4));
			if (handler5 == null)
				throw new ArgumentNullException(nameof(handler5));
			if (handler6 == null)
				throw new ArgumentNullException(nameof(handler6));
			if (handler7 == null)
				throw new ArgumentNullException(nameof(handler7));
			if (handler8 == null)
				throw new ArgumentNullException(nameof(handler8));
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			var chan1 = ChannelManager.CreateChannel<TIntermediate1>();
			var chan2 = ChannelManager.CreateChannel<TIntermediate2>();
			var chan3 = ChannelManager.CreateChannel<TIntermediate3>();
			var chan4 = ChannelManager.CreateChannel<TIntermediate4>();
			var chan5 = ChannelManager.CreateChannel<TIntermediate5>();
			var chan6 = ChannelManager.CreateChannel<TIntermediate6>();
			var chan7 = ChannelManager.CreateChannel<TIntermediate7>();

			return Task.WhenAll(
				WrapperAsync(handler1, input, chan1),
				WrapperAsync(handler2, chan1, chan2),
				WrapperAsync(handler3, chan2, chan3),
				WrapperAsync(handler4, chan3, chan4),
				WrapperAsync(handler5, chan4, chan5),
				WrapperAsync(handler6, chan5, chan6),
				WrapperAsync(handler7, chan6, chan7),
				WrapperAsync(handler8, chan7, output)
			);
		}

		/// <summary>
		/// Spreads a value over the outputs
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="mapper">The mapping function.</param>
		/// <param name="param">The parameter being spread.</param>
		/// <param name="spreadfactor">The degree for which the outputs are spread.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static async Task SpreaderAsync<T>(Func<T, int, Task<T[]>> mapper, T param, int spreadfactor, IEnumerable<IWriteChannel<T>> outputs)
		{
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));

			var outputlength = outputs.Count();
			var value = await mapper(param, outputlength);

			if (spreadfactor == outputlength)
			{
				await Task.WhenAll(
					Enumerable.Range(0, outputlength).Select(
						x => outputs.Skip(x).First().WriteAsync(value[x])
					)
				);
			}
			else
			{
				var ratio = outputlength / spreadfactor;
				await Task.WhenAll(
					Enumerable.Range(0, ratio).Select(
						x => SpreaderAsync(mapper, value[x], spreadfactor, outputs.Skip(ratio * x).Take(ratio))
					)
				);
			}
		}

		/// <summary>
		/// Reads the input, then spreads the value across the outputs using a mapping function to build a tree
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="mapper">The mapping function.</param>
		/// <param name="spreadfactor">The degree for which the outputs are spread.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="outputs">The output channels.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task SpreadAsync<T>(Func<T, int, Task<T[]>> mapper, int spreadfactor, IReadChannel<T> input, IWriteChannel<T>[] outputs)
		{
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));
			if ((int)Math.Sqrt(outputs.Length) != spreadfactor)
				throw new ArgumentOutOfRangeException(nameof(spreadfactor), $"The {nameof(spreadfactor)} must be the square root of the {nameof(outputs)} array length");

			return AutomationExtensions.RunTask(
				new { input = input, outputs = outputs },
				async (self) =>
				{
					while (true)
					{
						var value = await self.input.ReadAsync();
						await SpreaderAsync(mapper, value, spreadfactor, self.outputs);
					}
				}
			);
		}

		/// <summary>
		/// Performs a reduction using the mapper function and the spread factor
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="mapper">The mapping function.</param>
		/// <param name="spreadfactor">The degree for which the outputs are spread.</param>
		/// <param name="param">The values from the input.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static async Task<T> ReducerAsync<T>(Func<T[], Task<T>> mapper, int spreadfactor, IEnumerable<T> param)
		{
			var n = param.Count();
			if (spreadfactor == n)
				return await mapper(param.ToArray());

			var ratio = n / spreadfactor;
			var values = new T[ratio];

			await Task.WhenAll(
				Enumerable.Range(0, ratio).Select(
					async x => values[x] = await ReducerAsync(mapper, spreadfactor, param.Skip(ratio * x).Take(ratio))
				)
			);

			return await mapper(values);
		}

		/// <summary>
		/// Reduces data from the inputs through the mapper function to the output.
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="mapper">The mapping function.</param>
		/// <param name="spreadfactor">The degree for which the outputs are spread.</param>
		/// <param name="inputs">The input channels.</param>
		/// <param name="output">The output channels.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task ReduceAsync<T>(Func<T[], Task<T>> mapper, int spreadfactor, IReadChannel<T>[] inputs, IWriteChannel<T> output)
		{
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			
			if ((int)Math.Sqrt(inputs.Length) != spreadfactor)
				throw new ArgumentOutOfRangeException(nameof(spreadfactor), $"The {nameof(spreadfactor)} must be the square root of the {nameof(output)} array length");
			
			return AutomationExtensions.RunTask(
				new { inputs = inputs, output = output },
				async (self) =>
				{
					var values = new T[inputs.Length];
					while (true)
					{
						await Task.WhenAll(
							Enumerable.Range(0, values.Length)
							.Select(
								async (x) => values[x] = await self.inputs[x].ReadAsync()
							)
						);

						await self.output.WriteAsync(await ReducerAsync(mapper, spreadfactor, values));
					}
				}
			);
		}
	}
}

