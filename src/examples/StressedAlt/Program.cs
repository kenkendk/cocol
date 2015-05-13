using System;
using CoCoL;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StressedAlt
{
	/// <summary>
	/// The stressed reader performs a fair read from the set of channels
	/// </summary>
	class Reader
	{
		/// <summary>
		/// The number of reads to do before counting
		/// </summary>
		private const int WARMUP_ROUNDS = 10;

		/// <summary>
		/// The number of reads to do before counting
		/// </summary>
		private const int MEASURE_ROUNDS = 100;

		/// <summary>
		/// The set of channels to read from
		/// </summary>
		private readonly MultiChannelSet<long> m_set;

		/// <summary>
		/// The number of channels
		/// </summary>
		private readonly int m_channelCount;

		/// <summary>
		/// A tracking dictionary to verify correctness
		/// </summary>
		private readonly Dictionary<long, long> m_tracking;

		/// <summary>
		/// The number of tracked reads
		/// </summary>
		private long m_tracked_reads = 0;

		public Reader(IEnumerable<IChannel<long>> channels)
		{
			m_set = new MultiChannelSet<long>(channels, MultiChannelPriority.Fair);
			m_tracking = new Dictionary<long, long>();
			m_channelCount = m_set.Channels.Count();
		}

		public void Run()
		{
			RunAsync().Wait();
		}

		public async Task RunAsync()
		{
			try
			{
				Console.WriteLine("Running {0} warmup rounds ...", WARMUP_ROUNDS);

				var startWarmup = DateTime.Now;

				for (var i = 0; i < WARMUP_ROUNDS; i++)
					for (var j = 0; j < m_channelCount; j++)
						UpdateTracking((await m_set.ReadFromAnyAsync()).Value);

				var expected = ((DateTime.Now - startWarmup).Ticks / WARMUP_ROUNDS) * MEASURE_ROUNDS;

				Console.WriteLine("Measuring {0} rounds, expected completion around: {1}", MEASURE_ROUNDS, DateTime.Now.AddTicks(expected));

				var startMeasure = DateTime.Now;

				// Just keep reading
				for (var i = 0; i < MEASURE_ROUNDS * m_channelCount; i++)
					await m_set.ReadFromAnyAsync();
				
				var elapsed = DateTime.Now - startMeasure;

				Console.WriteLine("Performed {0}x{1} priority alternation reads in {2}", MEASURE_ROUNDS, m_channelCount, elapsed);
				Console.WriteLine("Communication time is {0} microseconds", (elapsed.TotalMilliseconds * 1000) / (MEASURE_ROUNDS * m_channelCount));
			}
			catch(RetiredException)
			{				
			}
			finally
			{
				m_set.Retire();
			}
		}

		private void UpdateTracking(long value)
		{
			// Keep track of correctness
			long c;
			if (!m_tracking.TryGetValue(value, out c))
				m_tracking[value] = 1;
			else
				m_tracking[value] = c + 1;

			m_tracked_reads++;

			if ((m_tracked_reads % m_channelCount) == 0)
			{
				var counts = m_tracking.OrderBy(x => x.Value);
				if (counts.First().Value != counts.Last().Value)
					throw new Exception("Error in fair alternation");
			}
		}
	}

	/// <summary>
	/// Each writer writes a message to each of its channels
	/// </summary>
	class Writer
	{
		private readonly long m_id;
		private readonly IChannel<long>[] m_channels;

		public Writer(long id, int channel_count)
		{
			m_id = id;
			m_channels = Enumerable.Range(0, channel_count)
				.Select(x => ChannelManager.CreateChannel<long>()).ToArray();
			
			RunAsync();
		}

		public IChannel<long>[] Channels { get { return m_channels; } }

		private async void RunAsync()
		{
			try
			{
				while (true)
				{
					await Task.WhenAll(
						m_channels.Select(chan => chan.WriteAsync(m_id))
					);	
				}
			}
			catch(RetiredException)
			{
				m_channels.Retire();
			}
		}
	}


	class MainClass
	{
		private const int CHANNELS_PR_WRITER = 100;
		private const int WRITER_PROCCESSES = 200;

		public static void Main(string[] args)
		{
			var allchannels = 
				Enumerable.Range(0, WRITER_PROCCESSES)
					.SelectMany(id => new Writer(id, CHANNELS_PR_WRITER).Channels);

			new Reader(allchannels).Run();
		}
	}
}
