using System;
using CoCoL;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using CoCoL.Network;

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
		/// The number of reads to perform from each channel in each round
		/// </summary>
		private const int MEASURE_ROUNDS = 100;

		/// <summary>
		/// The number of repeated rounds
		/// </summary>
		private const int TOTAL_ROUNDS = 10;

		/// <summary>
		/// The set of channels to read from
		/// </summary>
		private readonly MultiChannelSet<long> m_set;

		/// <summary>
		/// The number of channels
		/// </summary>
		private readonly int m_channelCount;

		/// <summary>
		/// The number of writes pr. channel
		/// </summary>
		private readonly int m_writes_pr_channel;

		/// <summary>
		/// A tracking dictionary to verify correctness
		/// </summary>
		private readonly Dictionary<long, long> m_tracking;

		/// <summary>
		/// The number of tracked reads
		/// </summary>
		private long m_tracked_reads = 0;

		public Reader(IEnumerable<IChannel<long>> channels, int writes_pr_channel)
		{
			m_set = new MultiChannelSet<long>(channels, MultiChannelPriority.Fair);
			m_tracking = new Dictionary<long, long>();
			m_channelCount = m_set.Channels.Count();
			m_writes_pr_channel = writes_pr_channel;
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

				var readcount = m_writes_pr_channel * m_channelCount;

				// Measure the warmup
				var startWarmup = DateTime.Now;

				for (var i = 0; i < WARMUP_ROUNDS; i++)
					for (var j = 0; j < readcount; j++)
						UpdateTracking((await m_set.ReadFromAnyAsync()).Value);

				var expected = ((DateTime.Now - startWarmup).Ticks / WARMUP_ROUNDS) * MEASURE_ROUNDS * TOTAL_ROUNDS;

				Console.WriteLine("Measuring {0} rounds, expected completion around: {1}", MEASURE_ROUNDS, DateTime.Now.AddTicks(expected));

				for(var r = 0; r < TOTAL_ROUNDS; r++)
				{
					var startMeasure = DateTime.Now;

					// Just keep reading
					for (var i = 0; i < MEASURE_ROUNDS * readcount; i++)
						await m_set.ReadFromAnyAsync();
					
					var elapsed = DateTime.Now - startMeasure;

					Console.WriteLine("Performed {0}x{1} priority alternation reads in {2}", MEASURE_ROUNDS, readcount, elapsed);
					Console.WriteLine("Communication time is {0} microseconds", (elapsed.TotalMilliseconds * 1000) / (MEASURE_ROUNDS * readcount));
				}
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

			if ((m_tracked_reads % (m_channelCount * m_writes_pr_channel)) == 0)
			{
				var counts = m_tracking.OrderBy(x => x.Value);
				if (Math.Abs(counts.Last().Value - counts.First().Value) > 1)
					Console.WriteLine("Error in fair alternation, diff: {0}", counts.Last().Value - counts.First().Value);
			}
		}
	}


	public class MainClass
	{

		private static int WRITERS_PR_CHANNEL = 100;
		private static int CHANNELS = 200;

		/// <summary>
		/// Runs the writer process
		/// </summary>
		/// <param name="id">The id to write into the channel.</param>
		/// <param name="channel">The channel to write into.</param>
		private static async void RunWriterAsync(long id, IChannel<long> channel)
		{
			try
			{
				while (true)
					await channel.WriteAsync(id);
			}
			catch(RetiredException)
			{
				channel.Retire();
			}
		}


		public static void Main(string[] args)
		{
			var usenetwork = 0;
			if (args.Length >= 2)
			{
				CHANNELS = int.Parse(args[0]);
				WRITERS_PR_CHANNEL = int.Parse(args[1]);

				if (args.Length > 2)
					usenetwork = int.Parse(args[2]);
			}

			var servertoken = new CancellationTokenSource();
			var server = usenetwork == 0 ? null : NetworkChannelServer.HostServer(servertoken.Token);
			using (usenetwork == 0 ? null : new NetworkChannelScope(redirectunnamed: true))
			{

				Console.WriteLine("Running with {0} channels and {1} writers, a total of {2} communications pr. round", CHANNELS, WRITERS_PR_CHANNEL, CHANNELS * WRITERS_PR_CHANNEL);

				var allchannels = (from n in Enumerable.Range(0, CHANNELS)
				                  select ChannelManager.CreateChannel<long>()).ToArray();

				for (var i = 0; i < allchannels.Length; i++)
					for (var j = 0; j < WRITERS_PR_CHANNEL; j++)
						RunWriterAsync(i, allchannels[i]);

				new Reader(allchannels, WRITERS_PR_CHANNEL).Run();
			}
		}
	}
}
