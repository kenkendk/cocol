using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoCoL
{
    /// <summary>
    /// A channel wrapper that enforces a rate-limit on reads and/or writes
    /// </summary>
    public class RateLimitingChannel<T> : IChannel<T>
    {
        /// <summary>
        /// The channel instance that we are wrapping
        /// </summary>
        private readonly IChannel<T> m_channel;

        /// <summary>
        /// The number of reads in the last second
        /// </summary>
        private long m_reads;

        /// <summary>
        /// The number of writes in the last second
        /// </summary>
        private long m_writes;

        /// <summary>
        /// The maximum number of reads pr. second
        /// </summary>
        private double m_maxreads;

        /// <summary>
        /// The maximum number of writes pr. second
        /// </summary>
        private double m_maxwrites;

        /// <summary>
        /// The ticks for the last read clearing
        /// </summary>
        private long m_last_read_update;

        /// <summary>
        /// The ticks for the last write clearing
        /// </summary>
        private long m_last_write_update;

        /// <summary>
        /// The lock used to limit reading rates
        /// </summary>
        private readonly AsyncLock m_readlock = new AsyncLock();

        /// <summary>
        /// The lock used to limit write rates
        /// </summary>
        private readonly AsyncLock m_writelock = new AsyncLock();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SlimDHT.RateLimitChannel`1"/> class wrapping an existing channel.
        /// </summary>
        /// <param name="channel">The channel to wrap.</param>
        /// <param name="maxreads">The maximum number of reads pr. second</param>
        /// <param name="maxwrites">The maximum number of writes pr. second</param>
        public RateLimitingChannel(IChannel<T> channel, double maxreads, double maxwrites)
        {
            m_channel = channel ?? throw new ArgumentNullException(nameof(channel));
            m_maxreads = maxreads;
            m_maxwrites = maxwrites;
            m_last_read_update = DateTime.Now.Ticks;
            m_last_write_update = DateTime.Now.Ticks;
        }

        /// <summary>
        /// Registers a desire to read from the channel
        /// </summary>
        public Task<T> ReadAsync()
        {
            return ReadAsync(null);
        }

        /// <summary>
        /// Registers a desire to read from the channel
        /// </summary>
        /// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
        public async Task<T> ReadAsync(ITwoPhaseOffer offer)
        {
            if (m_maxreads > 0)
            {
                using (await m_readlock.LockAsync())
                {
                    m_reads++;

                    if (m_last_read_update > TimeSpan.TicksPerSecond)
                    {
                        m_reads = 0;
                        m_last_read_update = DateTime.Now.Ticks;
                    }

                    // Check if there are too many in this period
                    if (m_reads > m_maxreads)
                    {
                        // Prevent others from entering
                        await Task.Delay(new TimeSpan(Math.Max(0, TimeSpan.TicksPerSecond - (DateTime.Now.Ticks - m_last_read_update))));

                        // Clear our attempt
                        m_reads = 1;
                        m_last_read_update = DateTime.Now.Ticks;
                    }
                }
            }

            return await m_channel.ReadAsync(offer);
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
        public async Task WriteAsync(T value, ITwoPhaseOffer offer)
        {
            if (m_maxwrites > 0)
            {
                using (await m_writelock.LockAsync())
                {
                    m_writes++;

                    if (m_last_write_update > TimeSpan.TicksPerSecond)
                    {
                        m_writes = 0;
                        m_last_write_update = DateTime.Now.Ticks;
                    }

                    // Check if there are too many in this period
                    if (m_writes > m_maxwrites)
                    {
                        // Prevent others from entering
                        await Task.Delay(new TimeSpan(Math.Max(0, TimeSpan.TicksPerSecond - (DateTime.Now.Ticks - m_last_write_update))));

                        // Clear our attempt
                        m_writes = 1;
                        m_last_write_update = DateTime.Now.Ticks;
                    }
                }
            }

            await m_channel.WriteAsync(value, offer);
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
    }
}
