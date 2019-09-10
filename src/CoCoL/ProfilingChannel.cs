using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoCoL
{
    /// <summary>
    /// Implements profiling on a channel
    /// </summary>
	public class ProfilingChannel<T> : IChannel<T>
    {
        /// <summary>
        /// The channel instance that we are wrapping
        /// </summary>
        private readonly IChannel<T> m_channel;

        /// <summary>
        /// The number of reads performed on the channel
        /// </summary>
        private long m_reads;
        /// <summary>
        /// The number of writes performed on the channel
        /// </summary>
        private long m_writes;

        /// <summary>
        /// The maximum delay for a read
        /// </summary>
        private long m_maxreaddelayticks;
        /// <summary>
        /// The maximum delay for a write
        /// </summary>
        private long m_maxwritedelayticks;

        /// <summary>
        /// The minimum delay for a read
        /// </summary>
        private long m_minreaddelayticks;
        /// <summary>
        /// The minimum delay for a write
        /// </summary>
        private long m_minwritedelayticks;

        /// <summary>
        /// The total number of ticks waited for writing
        /// </summary>
        private long m_writedelayticks;

        /// <summary>
        /// The total number of ticks waited for reading
        /// </summary>
        private long m_readdelayticks;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SlimDHT.ProfilerChannel`1"/> class wrapping an existing channel.
        /// </summary>
        /// <param name="channel">The channel to wrap.</param>
        public ProfilingChannel(IChannel<T> channel)
        {
            m_channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        /// <summary>
        /// Registers a desire to read from the channel
        /// </summary>
        /// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
        public async Task<T> ReadAsync(ITwoPhaseOffer offer)
        {
            var start = DateTime.Now.Ticks;
            var res = await m_channel.ReadAsync(offer);
            var waitticks = DateTime.Now.Ticks - start;

            m_minreaddelayticks = Math.Min(m_minreaddelayticks, waitticks);
            m_maxreaddelayticks = Math.Max(m_maxreaddelayticks, waitticks);
            m_readdelayticks += waitticks;
            m_reads++;

            return res;
        }

        /// <summary>
        /// Registers a desire to read from the channel
        /// </summary>
        public Task<T> ReadAsync()
        {
            return ReadAsync(null);
        }

        /// <summary>
        /// Registers a desire to write to the channel
        /// </summary>
        /// <param name="value">The value to write to the channel.</param>
        public Task WriteAsync(T value)
        {
            return WriteAsync(value, null);
        }

        /// <summary>
        /// Registers a desire to write to the channel
        /// </summary>
        /// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
        /// <param name="value">The value to write to the channel.</param>
        public async Task WriteAsync(T value, ITwoPhaseOffer offer)
        {
            var start = DateTime.Now.Ticks;
            await m_channel.WriteAsync(value, offer);
            var waitticks = DateTime.Now.Ticks - start;

            m_minwritedelayticks = Math.Min(m_minwritedelayticks, waitticks);
            m_maxwritedelayticks = Math.Max(m_maxwritedelayticks, waitticks);
            m_writedelayticks += waitticks;
            m_writes++;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="CoCoL.IRetireAbleChannel"/> is retired.
        /// </summary>
        /// <value><c>true</c> if is retired; otherwise, <c>false</c>.</value>
        public Task<bool> IsRetiredAsync => m_channel.IsRetiredAsync;

        /// <summary>
        /// Stops this channel from processing messages
        /// </summary>
        /// <returns>An awaitable task</returns>
        public Task RetireAsync()
        {
            return m_channel.RetireAsync();
        }

        /// <summary>
        /// Stops this channel from processing messages
        /// </summary>
        /// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
        /// <returns>An awaitable task</returns>
        public Task RetireAsync(bool immediate)
        {
            return m_channel.RetireAsync(immediate);
        }

        /// <summary>
        /// Returns the current channel stats
        /// </summary>
        public string ReportStats()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("Stats for channel {0}", (m_channel is INamedItem && !(string.IsNullOrWhiteSpace(((INamedItem)m_channel).Name))) ? ((INamedItem)m_channel).Name : string.Format("Unnamed channel of type: {0}", typeof(T))));
            sb.AppendLine(string.Format("\tRead : {0}, {1} / {2} / {3}", m_reads, m_minreaddelayticks, m_reads / new TimeSpan(Math.Max(1, m_readdelayticks)).TotalSeconds, m_maxreaddelayticks));
            sb.AppendLine(string.Format("\tWrite: {0}, {1} / {2} / {3}", m_writes, m_minwritedelayticks, m_writes / new TimeSpan(Math.Max(1, m_writedelayticks)).TotalSeconds, m_maxwritedelayticks));
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
