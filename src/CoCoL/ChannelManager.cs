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
		/// Gets a named channel.
		/// </summary>
		/// <returns>The named channel.</returns>
		/// <param name="name">The name of the channel to find.</param>
		/// <param name="buffersize">The number of buffers in the channel.</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> GetChannel<T>(string name, int buffersize = 0) 
		{ 
			return ChannelScope.Root.GetOrCreate<T>(name, buffersize); 
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
			return new Channel<T>(name, buffersize); 
		}

		/// <summary>
		/// Creates an unnamed channel
		/// </summary>
		/// <returns>The channel.</returns>
		/// <param name="buffersize">The number of buffers in the channel.</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> CreateChannel<T>(int buffersize) 
		{ 
			return new Channel<T>(null, buffersize); 
		}
	}
}

