using System;
using System.Threading;

namespace CoCoL
{
	/// <summary>
	/// Implementation of a simply channels that blocks the caller until it is avaliable
	/// </summary>
    public class SimpleBlockingChannel<T> : IBlockingChannel<T>
    {
		/// <summary>
		/// True if there are items in the queue, false otherwise
		/// </summary>
        private volatile bool m_any = false;
		/// <summary>
		/// The lock providing exclusive access to the channel
		/// </summary>
        private readonly object m_lock = new object();
		/// <summary>
		/// The event that is used to signal the reader
		/// </summary>
        private readonly AutoResetEvent m_readevent = new AutoResetEvent(false);
		/// <summary>
		/// The event that is used to signal the write
		/// </summary>
        private readonly AutoResetEvent m_writeevent = new AutoResetEvent(false);
		/// <summary>
		/// The value in holding
		/// </summary>
        private T m_hold = default(T);

		/// <summary>
		/// Perform a blocking read
		/// </summary>
        public T Read() 
        {
             while(true) {
                if (m_any)
                    lock(m_lock)
                        if (m_any)
                        {
                            T n = m_hold;
                            m_any = false;
                            m_hold = default(T);
                            m_writeevent.Set();
                            return n;                    
                        }

                m_readevent.WaitOne();
            }
        }

		/// <summary>
		/// Perform a blocking write
		/// </summary>
		/// <param name="value">The value to write</param>
        public void Write(T value) 
        {
            while(true) {
                if (!m_any)
                    lock(m_lock)
                        if (!m_any)
                        {
                            m_hold = value;
                            m_any = true;
                            m_readevent.Set();
                            return;                    
                        }
                m_writeevent.WaitOne();
            }
        }
    }
}

