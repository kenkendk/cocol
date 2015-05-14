using System;
using CoCoL;
using System.Threading.Tasks;

namespace Sieve
{
	/// <summary>
	/// The main class with all functionality
	/// </summary>
	class MainClass
	{
		// The Primes process

		/// <summary>
		/// A process that outputs the number 2, then
		/// spawns the sieve and a number generating process
		/// that produces numbers from 3 and forward
		/// </summary>
		/// <returns>The awaitable task that represents the process</returns>
		/// <param name="target">The channel the numbers are written into.</param>
		private static async Task RunPrimesAsync(IWriteChannel<long> target)
		{
			var chan = ChannelManager.CreateChannel<long>();
			try
			{
				await target.WriteAsync(2);

				await Task.WhenAll(
					RunNumbersFromAsync(3, 2, chan),
					RunSieveAsync(chan, target)
				);
			} 
			catch (RetiredException)
			{
				chan.Retire();
				target.Retire();
			}
		}
			
		/// <summary>
		/// A process that spawns a new set of processes
		/// when a number is received.
		/// By inserting a NoMultiples into the chain,
		/// it is guaranteed that no numbers that are divisible
		/// with any number in the chain can be retrieved by the
		/// Sieve.
		/// </summary>
		/// <returns>The awaitable task that represents the process</returns>
		/// <param name="input">The channel to read numbers from</param>
		/// <param name="output">The channel to write numbers to</param>
		private static async Task RunSieveAsync(IReadChannel<long> input, IWriteChannel<long> output)
		{
			var chan = ChannelManager.CreateChannel<long>();

			try
			{
				var n = await input.ReadAsync();
				await output.WriteAsync(n);

				await Task.WhenAll(
					RunNoMultiplesAsync(n, input, chan),
					RunSieveAsync(chan, output)
				);
			}
			catch (RetiredException)
			{
				chan.Retire();
				input.Retire();
				output.Retire();
			}
		}

		/// <summary>
		/// A process that produces an infinite amount of numbers,
		/// given an offset and an increment
		/// </summary>
		/// <returns>The awaitable task that represents the process</returns>
		/// <param name="from">The number offset.</param>
		/// <param name="increment">The increment.</param>
		/// <param name="target">The channel to which the numbers are written.</param>
		private static async Task RunNumbersFromAsync(long from, long increment, IWriteChannel<long> target)
		{
			var n = from;
			try
			{
				while(true)
				{
					await target.WriteAsync(n);
					n += increment;
				}
			}
			catch (RetiredException)
			{
			}
		}

		/// <summary>
		/// A process that reads numbers and discards those that are
		/// divisible by a certain number and forwards the rest
		/// </summary>
		/// <returns>The awaitable task that represents the process</returns>
		/// <param name="number">The number used to test and filter divisible numbers with.</param>
		/// <param name="input">The channel where data is read from.</param>
		/// <param name="output">The channel where non-multiple values are written to.</param>
		private static async Task RunNoMultiplesAsync(long number, IReadChannel<long> input, IWriteChannel<long> output)
		{
			try
			{
				while(true)
				{
					var v = await input.ReadAsync();
					if (v % number != 0)
						await output.WriteAsync(v);
				}
			}
			catch (RetiredException)
			{
				input.Retire();
				output.Retire();
			}
		}

		/// <summary>
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name="args">The command-line arguments.</param>
		public static void Main(string[] args)
		{
			// Create a result channel
			var chan = ChannelManager.CreateChannel<long>();

			// Start producing numbers as a parallel process
			var primeProcess = RunPrimesAsync(chan);

			// Read primes from the Sieve chain
			var prime = 0L;
			while(prime < 50000)
			{
				prime = chan.Read();
				Console.WriteLine(prime);
			}

			chan.Retire();
				
			// Wait for the retirement to flow through the network
			primeProcess.Wait();
		}
	}
}
