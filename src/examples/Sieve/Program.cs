using System;
using CoCoL;

namespace Sieve
{
	/// <summary>
	/// A filter process which ensures that there are no duplicates
	/// </summary>
	class NoMultiples
	{
		public NoMultiples(long number, IChannel<long> input, IChannel<long> output)
		{
			Run(number, input, output);
		}

		private async void Run(long number, IChannel<long> input, IChannel<long> output)
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
			catch(RetiredException)
			{
				input.Retire();
				output.Retire();
			}
		}
	}

	/// <summary>
	/// A generator that creates a stream of numbers
	/// </summary>
	class NumbersFrom
	{
		public NumbersFrom(long from, long increment, IChannel<long> target)
		{
			Run(from, increment, target);
		}

		private async void Run(long from, long increment, IChannel<long> target)
		{
			try
			{
				var n = from;
				while(true)
				{
					await target.WriteAsync(n);
					n += increment;
				}
			}
			catch(RetiredException)
			{
			}
		}
	}

	/// <summary>
	/// The sieve that spawns a new sieve for each prime
	/// </summary>
	class Sieve
	{
		public Sieve(IChannel<long> input, IChannel<long> output)
		{
			Run(input, output);
		}

		private async void Run(IChannel<long> input, IChannel<long> output)
		{
			try
			{
				var n = await input.ReadAsync();
				await output.WriteAsync(n);

				var chan = ChannelManager.CreateChannel<long>();
				new NoMultiples(n, input, chan);
				new Sieve(chan, output);
			}
			catch(RetiredException)
			{
				input.Retire();
				output.Retire();
			}
		}
	}

	/// <summary>
	/// The prime number generator
	/// </summary>
	class Primes
	{
		public Primes(IChannel<long> target)
		{
			Run(target);
		}

		private async void Run(IChannel<long> target)
		{
			try
			{
				await target.WriteAsync(2);
				var chan = ChannelManager.CreateChannel<long>();
				new NumbersFrom(3, 2, chan);
				new Sieve(chan, target);
			}
			catch(RetiredException)
			{
			}
		}
	}

	/// <summary>
	/// The driver class that starts the process and handles printout
	/// </summary>
	class MainClass
	{
		public static void Main(string[] args)
		{
			var chan = ChannelManager.CreateChannel<long>();
			new Primes(chan);

			try
			{
				var prime = 0L;

				while(prime < 50000)
				{
					prime = chan.Read();
					Console.WriteLine(prime);
				}

				chan.Retire();
			}
			catch(RetiredException)
			{
			}

		}
	}
}
