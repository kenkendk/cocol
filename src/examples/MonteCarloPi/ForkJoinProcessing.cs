using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoCoL;
using System.Linq;

namespace MonteCarloPi
{
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
		/// Generates points based on the count input
		/// </summary>
		private class Generator<TInput> : ProcessHelper
		{
			/// <summary>
			/// The number of points to create
			/// </summary>
			private IEnumerable<TInput> m_values;

			/// <summary>
			/// Initializes a new instance of the <see cref="MonteCarloPi.Generator"/> class.
			/// </summary>
			/// <param name="values">The values to create.</param>
			public Generator(IEnumerable<TInput> values)
				: base()
			{
				m_values = values;
			}

			/// <summary>
			/// The channel where the points are written to
			/// </summary>
			[ChannelName(WORKERINPUT)]
			private IWriteChannel<TInput> m_target;

			/// <summary>
			/// The method that implements this process
			/// </summary>
			protected override async Task Start()
			{
				foreach (var value in m_values)
					await m_target.WriteAsync(value).ConfigureAwait(false);
			}
		}		

		/// <summary>
		/// Work class that computes if a point is within the unit circle
		/// </summary>
		private class Worker<TInput, TOutput> : ProcessHelper
		{
			/// <summary>
			/// The method that performs the forked work
			/// </summary>
			private Func<TInput, TOutput> m_workermethod;

			/// <summary>
			/// The channel where points are read from
			/// </summary>
			[ChannelName(WORKERINPUT)]
			private IReadChannel<TInput> m_source;
			/// <summary>
			/// The channel where results are written to
			/// </summary>
			[ChannelName(WORKEROUTPUT)]
			private IWriteChannel<TOutput> m_target;

			/// <summary>
			/// Initializes a new instance of the <see cref="MonteCarloPi.ForkJoinProcessing`3+Worker"/> class.
			/// </summary>
			/// <param name="workermethod">The worker method.</param>
			public Worker(Func<TInput, TOutput> workermethod)
			{
				m_workermethod = workermethod;
			}

			/// <summary>
			/// The method that implements this process
			/// </summary>
			protected override async Task Start()
			{
				try
				{
					while (true)
						await m_target.WriteAsync(m_workermethod(await m_source.ReadAsync().ConfigureAwait(false))).ConfigureAwait(false);
				}
				catch(Exception ex)
				{
					if (!(ex is RetiredException))
						Console.WriteLine("ex: {0}", ex);
					throw;
				}
			}
		}

		/// <summary>
		/// The collector reads and counts all results
		/// </summary>
		private class Collector<TOutput, TResult> : ProcessHelper
		{
			/// <summary>
			/// The method for joining the results
			/// </summary>
			private Func<TResult, TOutput, TResult> m_joinmethod;

			/// <summary>
			/// The current results
			/// </summary>
			public TResult Current { get; private set; }

			/// <summary>
			/// The channel where computed results are input
			/// </summary>
			[ChannelName(WORKEROUTPUT)]
			private IReadChannel<TOutput> m_source;

			/// <summary>
			/// Initializes a new instance of the <see cref="MonteCarloPi.ForkJoinProcessing`3+Collector"/> class.
			/// </summary>
			/// <param name="joinmethod">The method for joining the values.</param>
			/// <param name="initial">The initial value.</param>
			public Collector(Func<TResult, TOutput, TResult> joinmethod, TResult initial)
			{
				m_joinmethod = joinmethod;
				Current = initial;
			}

			/// <summary>
			/// The method that implements this process
			/// </summary>
			protected override async Task Start()
			{
				while (true)
					Current = m_joinmethod(Current, await m_source.ReadAsync().ConfigureAwait(false));
			}
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
			// Set up a new name scope so we do not pollute the global scope
			using (new ChannelScope())
			{
				// Keep a reference to the collector so we can grab the result
				var collector = new Collector<TOutput, TResult>(joinmethod, initialvalue);

				// Await all tasks such that we can capture any exceptions
				await Task.WhenAll(

					// Spawn the workers
					Task.WhenAll(from n in Enumerable.Range(0, workers)
						           select new Worker<TInput, TOutput>(workermethod).RunAsync()), 

					// Inject all work items into the network
					new Generator<TInput>(input).RunAsync(),

					collector.RunAsync()

				).ConfigureAwait(false);
					
				// Give back the result
				return collector.Current;
			}
		}
	}
}

