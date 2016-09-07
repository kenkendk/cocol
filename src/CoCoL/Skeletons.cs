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
		public static Task GeneratorAsync<T>(IEnumerable<T> data, IWriteChannel<T> output)
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
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));
			
			if (inputs.Length != outputs.Length)
				throw new ArgumentOutOfRangeException(nameof(inputs), $"The {nameof(inputs)} and {nameof(outputs)} arrays must be of the same length");

			return Task.WhenAll(
				Enumerable.Range(0, inputs.Length).Select(
					x => AutomationExtensions.RunTask(
						new { input = inputs[x], output = outputs[x] },
						(self) => handler(self.input, self.output)
					)
				)
			);
		}

		/// <summary>
		/// Performs a pipeline operation by reading the input and passing it through all the handler functions in turn
		/// </summary>
		/// <returns>An awaitable task</returns>
		/// <param name="handlers">The handler functions, transforming input to output.</param>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		public static Task PipelineAsync<T>(Func<IReadChannel<T>, IWriteChannel<T>, Task>[] handlers, IReadChannel<T> input, IWriteChannel<T> output)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (handlers == null)
				throw new ArgumentNullException(nameof(handlers));
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			
			var channels = Enumerable.Range(0, handlers.Length - 1).Select(x => ChannelManager.CreateChannel<T>()).ToArray();
			return Task.WhenAll(
				handlers[0](input, channels[0]),
				Task.WhenAll(
					Enumerable.Range(1, channels.Length - 1)
					.Select(x => handlers[x](channels[x - 1], channels[x]))
			    ),
				handlers.Last()(channels.Last(), output)
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

