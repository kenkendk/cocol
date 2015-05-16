using System;
using System.Collections.Generic;

namespace CoCoL
{
	/// <summary>
	/// Channel manager, responsible for creating named and unnamed channels
	/// </summary>
	public static class ChannelManager
	{
		/// <summary>
		/// The implementation of a channel manager, for easy replacement
		/// </summary>
		private static readonly ChannelManagerImpl _ch = new ChannelManagerImpl();

		/// <summary>
		/// Gets a named channel.
		/// </summary>
		/// <returns>The named channel.</returns>
		/// <param name="name">The name of the channel to find.</param>
		/// <param name="buffersize">The number of buffers in the channel.</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> GetChannel<T>(string name, int buffersize = 0) 
		{ 
			return _ch.GetChannel<T>(name, buffersize); 
		}

		/// <summary>
		/// Creates a channel, possibly unnamed
		/// </summary>
		/// <returns>The channel.</returns>
		/// <param name="name">The name of the channel, or null.</param>
		/// <param name="buffersize">The number of buffers in the channel.</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> CreateChannel<T>(string name = null, int buffersize = 0) 
		{ 
			return new ContinuationChannel<T>(name, buffersize); 
		}
	}

	/// <summary>
	/// The implementation of a channel manager
	/// </summary>
	internal class ChannelManagerImpl
	{
		/// <summary>
		/// A lookup table with all known channels
		/// </summary>
		private Dictionary<string, object> m_channels = new Dictionary<string, object>();

		/// <summary>
		/// A lock object for providing exclusive access to the lookup table
		/// </summary>
		private readonly object m_lock = new object();

		/// <summary>
		/// Gets a named channel.
		/// </summary>
		/// <returns>The named channel.</returns>
		/// <param name="name">The name of the channel to find.</param>
		/// <param name="buffersize">The number of buffers in the channel.</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public IChannel<T> GetChannel<T>(string name, int buffersize)
		{
			object res;

			if (m_channels.TryGetValue(name, out res))
				return (IChannel<T>)res;

			lock (m_lock)
				if (m_channels.TryGetValue(name, out res))
					return (IChannel<T>)res;
				else
				{
					var r = ChannelManager.CreateChannel<T>(name, buffersize);
					m_channels.Add(name, r);
					return r;
				}
				
		}
	}
}

