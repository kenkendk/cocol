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
		/// Gets or creates a named channel.
		/// </summary>
		/// <returns>The named channel.</returns>
		/// <param name="name">The name of the channel to find.</param>
		/// <param name="buffersize">The number of buffers in the channel.</param>
		/// <param name="scope">The scope to create a named channel in, defaults to null which means the current scope</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> GetChannel<T>(string name, int buffersize = 0, ChannelScope scope = null) 
		{ 
			return (scope ?? ChannelScope.Current).GetOrCreate<T>(name, buffersize); 
		}

		/// <summary>
		/// Creates a channel, possibly unnamed.
		/// If a channel name is provided, the channel is created in the supplied scope.
		/// If a channel with the given name is already found in the supplied scope, the named channel is returned.
		/// </summary>
		/// <returns>The channel.</returns>
		/// <param name="name">The name of the channel, or null.</param>
		/// <param name="buffersize">The number of buffers in the channel.</param>
		/// <param name="scope">The scope to create a named channel in, defaults to null which means the current scope</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> CreateChannel<T>(string name = null, int buffersize = 0, ChannelScope scope = null) 
		{
			if (name == null)
				return CreateChannel<T>(buffersize);
			else
				return GetChannel<T>(name, buffersize, scope);
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

