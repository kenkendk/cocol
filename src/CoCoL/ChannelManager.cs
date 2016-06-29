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
		/// <param name="broadcast"><c>True</c> will create the channel as a broadcast channel, the default <c>false</c> will create a normal channel</param>
		/// <param name="initialBroadcastBarrier">The number of readers required on the channel before sending the first broadcast, can only be used with broadcast channels</param>
		/// <param name="broadcastMinimum">The minimum number of readers required on the channel, before a broadcast can be performed, can only be used with broadcast channels</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> GetChannel<T>(string name, int buffersize = 0, ChannelScope scope = null, int maxPendingReaders = -1, int maxPendingWriters = -1, QueueOverflowStrategy pendingReadersOverflowStrategy = QueueOverflowStrategy.Reject, QueueOverflowStrategy pendingWritersOverflowStrategy = QueueOverflowStrategy.Reject, bool broadcast = false, int initialBroadcastBarrier = -1, int broadcastMinimum = -1) 
		{
			if (!broadcast && (initialBroadcastBarrier >= 0 || broadcastMinimum >= 0))
				throw new ArgumentException(string.Format("Cannot set \"{0}\" or \"{1}\" unless the channel is a broadcast channel", "initialBroadcastBarrier", "broadcastMinimum"));

			var attr =
				broadcast
				? new BroadcastChannelNameAttribute(name, buffersize, ChannelNameScope.Local, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy, initialBroadcastBarrier, broadcastMinimum)
				: new ChannelNameAttribute(name, buffersize, ChannelNameScope.Local, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy);
			
			return GetChannel<T>(attr, scope);
		}

		/// <summary>
		/// Gets or creates a named channel.
		/// </summary>
		/// <returns>The named channel.</returns>
		/// <param name="attr">The attribute describing the channel.</param>
		/// <param name="scope">The scope to create a named channel in, defaults to null which means the current scope</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> GetChannel<T>(ChannelNameAttribute attr, ChannelScope scope = null)
		{
			return (scope ?? ChannelScope.Current).GetOrCreate<T>(attr);
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

			return GetChannel<T>(marker.Attribute, scope);
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
			
			return GetChannel<T>(rt.Attribute, scope).AsWriteOnly();
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

			return GetChannel<T>(rt.Attribute, scope).AsReadOnly();
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
		/// <param name="broadcast"><c>True</c> will create the channel as a broadcast channel, the default <c>false</c> will create a normal channel</param>
		/// <param name="initialBroadcastBarrier">The number of readers required on the channel before sending the first broadcast, can only be used with broadcast channels</param>
		/// <param name="broadcastMinimum">The minimum number of readers required on the channel, before a broadcast can be performed, can only be used with broadcast channels</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> CreateChannel<T>(string name = null, int buffersize = 0, ChannelScope scope = null, int maxPendingReaders = -1, int maxPendingWriters = -1, QueueOverflowStrategy pendingReadersOverflowStrategy = QueueOverflowStrategy.Reject, QueueOverflowStrategy pendingWritersOverflowStrategy = QueueOverflowStrategy.Reject, bool broadcast = false, int initialBroadcastBarrier = -1, int broadcastMinimum = -1)
		{
			return GetChannel<T>(name, buffersize, scope, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy, broadcast, initialBroadcastBarrier, broadcastMinimum);
		}

		/// <summary>
		/// Creates a channel, possibly unnamed.
		/// If a channel name is provided, the channel is created in the supplied scope.
		/// If a channel with the given name is already found in the supplied scope, the named channel is returned.
		/// </summary>
		/// <returns>The named channel.</returns>
		/// <param name="attr">The attribute describing the channel.</param>
		/// <param name="scope">The scope to create a named channel in, defaults to null which means the current scope</param>
		/// <typeparam name="T">The channel type.</typeparam>
		public static IChannel<T> CreateChannel<T>(ChannelNameAttribute attr, ChannelScope scope = null)
		{
			return GetChannel<T>(attr, scope);
		}

		/// <summary>
		/// Creates a channel for use in a scope
		/// </summary>
		/// <returns>The channel.</returns>
		/// <param name="attribute">The attribute describing the channel.</param>
		/// <typeparam name="T">The channel type.</typeparam>
		internal static IChannel<T> CreateChannelForScope<T>(ChannelNameAttribute attribute)
		{
			if (attribute is BroadcastChannelNameAttribute)
				return new BroadcastingChannel<T>(attribute);
			else
				return new Channel<T>(attribute); 
		}
	}
}

