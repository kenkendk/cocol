using System;
using System.Threading;
using CoCoL;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace CommsTimeBlockingCollection
{
	public static class BlockingCollectionChannelManager
	{
		/// <summary>
		/// A lookup table with all known channels
		/// </summary>
		private static Dictionary<string, object> m_channels = new Dictionary<string, object>();

		/// <summary>
		/// A lock object for providing exclusive access to the lookup table
		/// </summary>
		private static readonly object m_lock = new object();

		/// <summary>
		/// Gets a named channel.
		/// </summary>
		/// <returns>The named channel.</returns>
		/// <param name="name">The name of the channel to find.</param>
		/// <param name="buffersize">The number of buffers in the channel.</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IBlockingChannel<T> GetChannel<T>(string name)
		{
			object res;

			if (m_channels.TryGetValue(name, out res))
				return (IBlockingChannel<T>)res;

			lock (m_lock)
				if (m_channels.TryGetValue(name, out res))
					return (IBlockingChannel<T>)res;
				else
				{
					var r = new BlockingCollectionBlockingChannel<T>();
					m_channels.Add(name, r);
					return r;
				}
		}	
	}




	/// <summary>
	/// Implementation of a channel by wrapping BlockingCollection
	/// </summary>
    public class BlockingCollectionBlockingChannel<T> : IBlockingChannel<T>
    {
		/// <summary>
		/// True if there are items in the queue, false otherwise
		/// </summary>
		private BlockingCollection<T> m_channel = new BlockingCollection<T>(1);


		/// <summary>
		/// Retires the channel
		/// </summary>
		public void Retire()
		{
			m_channel.CompleteAdding();
		}

		/// <summary>
		/// Gets a value indicating whether this instance is retired.
		/// </summary>
		/// <value><c>true</c> if this instance is retired; otherwise, <c>false</c>.</value>
		public bool IsRetired { get { return m_channel.IsCompleted; } }

		/// <summary>
		/// Perform a blocking read
		/// </summary>
        public T Read() 
        {
			return m_channel.Take();
        }

		/// <summary>
		/// Perform a blocking write
		/// </summary>
		/// <param name="value">The value to write</param>
        public void Write(T value) 
        {
			m_channel.Add(value);
        }
    }
}

