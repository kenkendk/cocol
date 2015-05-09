using System;
using CoCoL;

namespace CommsTimeCallback
{
	class TickCollector : IProcess
	{
		public const string TICK_CHANNEL_NAME = "ticks";
		public const string TERM_CHANNEL_NAME = "terminate";

		public const int MEASURE_COUNT = 5;

		private enum States 
		{
			Init,
			Warmup,
			Run
		}

		private States m_state = States.Init;
		private DateTime m_last;
		private long m_tickcount = 0;
		private int m_rounds;

		private readonly long A_SECOND_IN_TICKS = TimeSpan.FromSeconds(1).Ticks;
		private readonly long MEASURE_INTERVAL = TimeSpan.FromSeconds(5).Ticks;

		public void Run() { }

		[OnRead(TICK_CHANNEL_NAME)]
		public void OnData(ICallbackResult<bool> res)
		{
			try
			{
				if (res.Exception != null)
					throw res.Exception;

				switch(m_state)
				{
					case States.Init:
						//Warm up
						Console.WriteLine("Warming up ...");
						m_last = DateTime.Now;
						m_state = States.Warmup;
						break;

					case States.Warmup:
						if ((DateTime.Now - m_last).Ticks > A_SECOND_IN_TICKS)
						{
							Console.WriteLine("Measuring!");
							m_state = States.Run;
							m_last = DateTime.Now;
							m_tickcount = 0;
						}
						break;

					case States.Run:
					default:
						
						m_tickcount++;
						var duration = DateTime.Now - m_last;
						if ((DateTime.Now - m_last).Ticks > MEASURE_INTERVAL)
						{
							Console.WriteLine("Got {0} ticks for {1} processes in {2} seconds, speed is {3} rounds/s ({4} msec/comm)", m_tickcount, CommsTime.PROCESSES, duration, m_tickcount / duration.TotalSeconds, duration.TotalMilliseconds / ((m_tickcount) * (CommsTime.PROCESSES + 1)));
							m_last = DateTime.Now;
							m_tickcount = 0;

							// For shutdown, we retire the initial channel
							if (++m_rounds >= MEASURE_COUNT)
								ChannelManager.GetChannel<bool>("0->1").Retire();

						}
						break;

				}
			}
			catch(RetiredException)
			{
				//Console.WriteLine("Retired tick writer");
				ChannelManager.GetChannel<bool>(TERM_CHANNEL_NAME).Retire();
			}
		}
	}

	[Process(count: PROCESSES)]
	public class CommsTime: IProcess
	{
		public const int PROCESSES = 4;//10000000;

		private static int _index = -1;
		private readonly int m_index = System.Threading.Interlocked.Increment(ref _index);

		private IWriteChannel<bool> m_tickChannel;
		private IReadChannel<bool> m_readChannel;
		private IWriteChannel<bool> m_writeChannel;
		private ChannelCallback<bool> m_onData;

		public CommsTime()
		{
			var next_chan = (m_index + 1) % PROCESSES;
			var prev_chan = m_index == 0 ? PROCESSES - 1 : m_index - 1;

			m_onData = OnData;
			m_writeChannel = ChannelManager.GetChannel<bool>(m_index + "->" + next_chan).AsWrite();
			m_readChannel = ChannelManager.GetChannel<bool>(prev_chan + "->" + m_index).AsRead();
			if (m_index == 0)
				m_tickChannel = ChannelManager.GetChannel<bool>(TickCollector.TICK_CHANNEL_NAME).AsWrite();
		}

		#region IProcess implementation
		public void Run()
		{
			//Console.WriteLine("Started process {0}", m_index);

			// Send initial item out
			if (m_index == 0)
			{
				//Console.WriteLine("process {0} is writing to {1}", m_index, write_chan_name);
				m_writeChannel.RegisterWrite(true);
			}

			m_readChannel.RegisterRead(m_onData);
		}

		private void OnWriteCallback(ICallbackResult<bool> res)
		{
			if (res.Exception is CoCoL.RetiredException)
				m_writeChannel.Retire();
		}

		private void OnData(ICallbackResult<bool> res)
		{
			if (res.Exception != null)
			{
				if (res.Exception is CoCoL.RetiredException)
				{
					//Console.WriteLine("Retired process {0}", m_index);
					m_writeChannel.Retire();

					if (m_index == 0)
						m_tickChannel.Retire();

					return;
				}
				
				throw res.Exception;
			}

			if (m_tickChannel != null)
				m_tickChannel.RegisterWrite(OnWriteCallback, true);
			
			m_writeChannel.RegisterWrite(true);
			m_readChannel.RegisterRead(m_onData);
		}

		#endregion

	}
}


