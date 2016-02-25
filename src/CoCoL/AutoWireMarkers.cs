using System;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// Static factory class for creating channel markers
	/// </summary>
	public static class ChannelMarker
	{
		/// <summary>
		/// Creates a marker property for a read channel
		/// </summary>
		/// <returns>The marker instance.</returns>
		/// <param name="name">The name of the channel.</param>
		/// <param name="buffersize">The desired buffersize to use if the channel is created.</param>
		/// <param name="targetScope">The scope to create or locate the name in.</param>
		/// <typeparam name="T">The type of data passed on the channel.</typeparam>
		public static IReadChannel<T> ForRead<T>(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local)
		{
			return new ReadMarker<T>(name, buffersize, targetScope);
		}

		/// <summary>
		/// Creates a marker property for a write channel
		/// </summary>
		/// <returns>The marker instance.</returns>
		/// <param name="name">The name of the channel.</param>
		/// <param name="buffersize">The desired buffersize to use if the channel is created.</param>
		/// <param name="targetScope">The scope to create or locate the name in.</param>
		/// <typeparam name="T">The type of data passed on the channel.</typeparam>
		public static IWriteChannel<T> ForWrite<T>(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local)
		{
			return new WriteMarker<T>(name, buffersize, targetScope);
		}
	}

	/// <summary>
	/// Helper class for creating typed-and-named markers
	/// </summary>
	public class ChannelMarkerWrapper<T> : INamedItem
	{
		/// <summary>
		/// Gets the channel as a write request
		/// </summary>
		public readonly IWriteChannel<T> ForWrite;
		/// <summary>
		/// Gets the channel as a read request
		/// </summary>
		public readonly IReadChannel<T> ForRead;

		/// <summary>
		/// Gets the name of the channel
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The buffer size for the channel
		/// </summary>
		public readonly int BufferSize;

		/// <summary>
		/// The target channel scope
		/// </summary>
		public readonly ChannelNameScope TargetScope;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelMarkerWrapper`1"/> class.
		/// </summary>
		/// <param name="name">The name of the channel.</param>
		/// <param name="buffersize">The desired buffersize to use if the channel is created.</param>
		/// <param name="targetScope">The scope to create or locate the name in.</param>
		public ChannelMarkerWrapper(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local)
		{
			Name = name;
			BufferSize = buffersize;
			TargetScope = targetScope;
			ForWrite = ChannelMarker.ForWrite<T>(name, buffersize, targetScope);
			ForRead = ChannelMarker.ForRead<T>(name, buffersize, targetScope);
		}
	}

	/// <summary>
	/// Marker class for specifying channel attributes in anonymous types
	/// </summary>
	public abstract class ChannelNameMarker : INamedItem
	{
		/// <summary>
		/// Gets the attribute representation of the data.
		/// </summary>
		public ChannelNameAttribute Attribute { get; protected set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelNameMarker"/> class.
		/// </summary>
		/// <param name="name">The name of the channel.</param>
		/// <param name="buffersize">The desired buffersize to use if the channel is created.</param>
		/// <param name="targetScope">The scope to create or locate the name in.</param>
		public ChannelNameMarker(string name, int buffersize, ChannelNameScope targetScope)
		{
			Attribute = new ChannelNameAttribute(name, buffersize, targetScope);
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get { return Attribute.Name; } }
	}

	/// <summary>
	/// Marker class for specifying channel attributes in anonymous types
	/// </summary>
	internal class ReadMarker<T> : ChannelNameMarker, IReadChannel<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ReadMarker`1"/> class.
		/// </summary>
		/// <param name="name">The name of the channel.</param>
		/// <param name="buffersize">The desired buffersize to use if the channel is created.</param>
		/// <param name="targetScope">The scope to create or locate the name in.</param>
		public ReadMarker(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local)
			: base(name, buffersize, targetScope)
		{
		}

		// Since this is just a marker, we do not implement any methods


		#region IReadChannel implementation
		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a
		/// negative span to wait forever.</param>
		/// <returns>The async.</returns>
		public Task<T> ReadAsync(TimeSpan timeout, ITwoPhaseOffer offer = null)
		{
			throw new NotImplementedException();
		}
		#endregion
		#region IRetireAbleChannel implementation
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		public void Retire()
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		public void Retire(bool immediate)
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Gets a value indicating whether this instance is retired.
		/// </summary>
		/// <value><c>true</c> if this instance is retired; otherwise, <c>false</c>.</value>
		public bool IsRetired
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		#endregion
	}

	internal class WriteMarker<T> : ChannelNameMarker, IWriteChannel<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.WriteMarker`1"/> class.
		/// </summary>
		/// <param name="name">The name of the channel.</param>
		/// <param name="buffersize">The desired buffersize to use if the channel is created.</param>
		/// <param name="targetScope">The scope to create or locate the name in.</param>
		public WriteMarker(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local)
			: base(name, buffersize, targetScope)
		{
		}

		// Since this is just a marker, we do not implement any methods

		#region IWriteChannel implementation
		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a
		/// negative span to wait forever.</param>
		/// <returns>The async.</returns>
		public Task WriteAsync(T value, TimeSpan timeout, ITwoPhaseOffer offer = null)
		{
			throw new NotImplementedException();
		}
		#endregion
		#region IRetireAbleChannel implementation
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		public void Retire()
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		public void Retire(bool immediate)
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Gets a value indicating whether this instance is retired.
		/// </summary>
		/// <value><c>true</c> if this instance is retired; otherwise, <c>false</c>.</value>
		public bool IsRetired
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		#endregion
	}
}

