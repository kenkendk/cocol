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
		/// <param name="maxPendingReaders">The maximum number of pending readers. A negative value indicates infinite</param>
		/// <param name="maxPendingWriters">The maximum number of pending writers. A negative value indicates infinite</param>
		/// <param name="pendingReadersOverflowStrategy">The strategy for dealing with overflow for read requests</param>
		/// <param name="pendingWritersOverflowStrategy">The strategy for dealing with overflow for write requests</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> GetChannel<T>(string name, int buffersize = 0, ChannelScope scope = null, int maxPendingReaders = -1, int maxPendingWriters = -1, QueueOverflowStrategy pendingReadersOverflowStrategy = QueueOverflowStrategy.Reject, QueueOverflowStrategy pendingWritersOverflowStrategy = QueueOverflowStrategy.Reject) 
		{ 
			return (scope ?? ChannelScope.Current).GetOrCreate<T>(name, buffersize, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy);
		}

		/// <summary>
		/// Gets or creates a named channel from a marker setup
		/// </summary>
		/// <returns>The named channel.</returns>
		/// <param name="marker">The channel marker instance that describes the channel.</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> GetChannel<T>(ChannelMarkerWrapper<T> marker)
		{
			var scope = ChannelScope.Current;
			if (marker.TargetScope == ChannelNameScope.Parent)
				scope = scope.ParentScope;
			else if (marker.TargetScope == ChannelNameScope.Global)
				scope = ChannelScope.Root;

			return GetChannel<T>(marker.Name, marker.BufferSize, scope);
		}

		/// <summary>
		/// Gets a write channel from a marker interface.
		/// </summary>
		/// <returns>The requested channel.</returns>
		/// <param name="channel">The marker interface, or a real channel instance.</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IWriteChannelEnd<T> GetChannel<T>(IWriteChannel<T> channel)
		{
			var rt = channel as WriteMarker<T>;
			if (rt == null)
				return channel.AsWriteOnly();

			var scope = ChannelScope.Current;
			if (rt.Attribute.TargetScope == ChannelNameScope.Parent)
				scope = scope.ParentScope;
			else if (rt.Attribute.TargetScope == ChannelNameScope.Global)
				scope = ChannelScope.Root;
			
			return GetChannel<T>(rt.Attribute.Name, rt.Attribute.BufferSize, scope).AsWriteOnly();
		}

		/// <summary>
		/// Gets a read channel from a marker interface.
		/// </summary>
		/// <returns>The requested channel.</returns>
		/// <param name="channel">The marker interface, or a real channel instance.</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IReadChannelEnd<T> GetChannel<T>(IReadChannel<T> channel)
		{
			var rt = channel as ReadMarker<T>;
			if (rt == null)
				return channel.AsReadOnly();

			var scope = ChannelScope.Current;
			if (rt.Attribute.TargetScope == ChannelNameScope.Parent)
				scope = scope.ParentScope;
			else if (rt.Attribute.TargetScope == ChannelNameScope.Global)
				scope = ChannelScope.Root;

			return GetChannel<T>(rt.Attribute.Name, rt.Attribute.BufferSize, scope).AsReadOnly();
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
		/// <param name="maxPendingReaders">The maximum number of pending readers. A negative value indicates infinite</param>
		/// <param name="maxPendingWriters">The maximum number of pending writers. A negative value indicates infinite</param>
		/// <param name="pendingReadersOverflowStrategy">The strategy for dealing with overflow for read requests</param>
		/// <param name="pendingWritersOverflowStrategy">The strategy for dealing with overflow for write requests</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> CreateChannel<T>(string name = null, int buffersize = 0, ChannelScope scope = null, int maxPendingReaders = -1, int maxPendingWriters = -1, QueueOverflowStrategy pendingReadersOverflowStrategy = QueueOverflowStrategy.Reject, QueueOverflowStrategy pendingWritersOverflowStrategy = QueueOverflowStrategy.Reject)
		{
			return GetChannel<T>(name, buffersize, scope, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy);
		}

		/// <summary>
		/// Creates a channel for use in a scope
		/// </summary>
		/// <returns>The channel.</returns>
		/// <param name="name">The name of the channel, or null.</param>
		/// <param name="buffersize">The number of buffers in the channel.</param>
		/// <param name="maxPendingReaders">The maximum number of pending readers. A negative value indicates infinite</param>
		/// <param name="maxPendingWriters">The maximum number of pending writers. A negative value indicates infinite</param>
		/// <param name="pendingReadersOverflowStrategy">The strategy for dealing with overflow for read requests</param>
		/// <param name="pendingWritersOverflowStrategy">The strategy for dealing with overflow for write requests</param>
		/// <typeparam name="T">The channel type.</typeparam>
		internal static IChannel<T> CreateChannelForScope<T>(string name, int buffersize, int maxPendingReaders, int maxPendingWriters, QueueOverflowStrategy pendingReadersOverflowStrategy, QueueOverflowStrategy pendingWritersOverflowStrategy)
		{
			return new Channel<T>(name, buffersize, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy); 
		}
	}
}

