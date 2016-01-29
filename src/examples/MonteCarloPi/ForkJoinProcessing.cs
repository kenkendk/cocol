using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoCoL;
using System.Linq;

namespace MonteCarloPi
{
	/// <summary>
	/// Example template for fork-join style processing
	/// </summary>
	public static class ForkJoinProcessing
	{
		/// <summary>
		/// The name of the worker input channel
		/// </summary>
		private const string WORKERINPUT = "WorkerInput";
		/// <summary>
		/// The name of the worker output channel
		/// </summary>
		private const string WORKEROUTPUT = "WorkerOutput";

		/// <summary>
		/// Emits all values from the enumerable into the network
		/// </summary>
		/// <param name="values">Values.</param>
		/// <typeparam name="TInput">The 1st type parameter.</typeparam>
		private static Task Generator<TInput>(IEnumerable<TInput> values)
		{
			return AutomationExtensions.RunTask(
				new { channel = ChannelMarker.ForWrite<TInput>(WORKERINPUT) },
				async self => {
					foreach (var value in values)
						await self.channel.WriteAsync(value).ConfigureAwait(false);
				}
			);
		}

		/// <summary>
		/// Reads input and applies the method to each input, and emits the output
		/// </summary>
		/// <param name="method">The worker method to apply to each element.</param>
		/// <typeparam name="TInput">The input type parameter.</typeparam>
		/// <typeparam name="TOutput">The output type parameter.</typeparam>
		private static Task Worker<TInput, TOutput>(Func<TInput, TOutput> method)
		{
			return AutomationExtensions.RunTask(
				new { 
					input = ChannelMarker.ForRead<TInput>(WORKERINPUT),
					output = ChannelMarker.ForWrite<TOutput>(WORKEROUTPUT) 
				},
				
				async self => {
					try
					{
						while (true)
						{
							await self.output.WriteAsync(method(await self.input.ReadAsync().ConfigureAwait(false))).ConfigureAwait(false);
						}
					}
					catch(Exception ex)
					{
						if (!(ex is RetiredException))
							Console.WriteLine("ex: {0}", ex);
						throw;
					}
				}
			);
		}

		/// <summary>
		/// Collects input and combines it with the join method
		/// </summary>
		/// <param name="joinmethod">The method used to join results.</param>
		/// <param name="initial">The initial input to the join method, aka. the neutral element.</param>
		/// <typeparam name="TOutput">The type parameter for the data to join.</typeparam>
		/// <typeparam name="TResult">The type parameter for the aggregated data.</typeparam>
		private static async Task<TResult> Collector<TOutput, TResult>(Func<TResult, TOutput, TResult> joinmethod, TResult initial)
		{
			var current = initial;

			await AutomationExtensions.RunTask(
				new { channel = ChannelMarker.ForRead<TOutput>(WORKEROUTPUT) },
				async self => {
					while (true)
						current = joinmethod(current, await self.channel.ReadAsync().ConfigureAwait(false));
				}
			).ConfigureAwait(false);

			return current;
		}

		/// <summary>
		/// Runs a classic fork/join paradigm with concurrent workers and a serialized join process
		/// </summary>
		/// <returns>The computed results.</returns>
		/// <param name="input">The values to compute on.</param>
		/// <param name="workermethod">The method that performs the work.</param>
		/// <param name="joinmethod">The method that combines the results.</param>
		/// <param name="workers">The number of workers to spawn, or -1 for using the number of processors.</param>
		/// <param name="initialvalue">The initial value of the join process.</param>
		public static async Task<TResult> ForkJoinProcessAsync<TInput, TOutput, TResult>(IEnumerable<TInput> input, Func<TInput, TOutput> workermethod, Func<TResult, TOutput, TResult> joinmethod, int workers = -1, TResult initialvalue = default(TResult))
		{
			// Set up a new isolated name scope so we do not pollute the global scope
			using (new ChannelScope(true))
			{
				// Start the collector so we have the task
				var result = Collector(joinmethod, initialvalue);

				// Await all tasks such that we capture any exceptions
				await Task.WhenAll(

					// Spawn the workers
					Task.WhenAll(from n in Enumerable.Range(0, workers)
						           select Worker(workermethod)), 

					// Inject all work items into the network
					Generator(input)

				).ConfigureAwait(false);
					
				// Give back the result
				return await result;
			}
		}
	}
}

