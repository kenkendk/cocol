using System;
using System.Threading.Tasks;

namespace CoCoL
{
	public static class ChannelMarker
	{
		public static IReadChannel<T> ForRead<T>(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local)
		{
			return new ReadMarker<T>(name, buffersize, targetScope);
		}

		public static IWriteChannel<T> ForWrite<T>(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local)
		{
			return new WriteMarker<T>(name, buffersize, targetScope);
		}
	}

	/// <summary>
	/// Marker class for specifying channel attributes in anonymous types
	/// </summary>
	public abstract class ChannelNameMarker
	{
		public ChannelNameAttribute Attribute { get; protected set; }

		public ChannelNameMarker(string name, int buffersize, ChannelNameScope targetScope)
		{
			Attribute = new ChannelNameAttribute(name, buffersize, targetScope);
		}
	}

	/// <summary>
	/// Marker class for specifying channel attributes in anonymous types
	/// </summary>
	internal class ReadMarker<T> : ChannelNameMarker, IReadChannel<T>
	{
		public ReadMarker(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local)
			: base(name, buffersize, targetScope)
		{
		}

		// Since this is just a marker, we do not implement any methods


		#region IReadChannel implementation
		public Task<T> ReadAsync()
		{
			throw new NotImplementedException();
		}
		public Task<T> ReadAsync(TimeSpan timeout)
		{
			throw new NotImplementedException();
		}
		public Task<T> ReadAsync(ITwoPhaseOffer offer)
		{
			throw new NotImplementedException();
		}
		public Task<T> ReadAsync(ITwoPhaseOffer offer, TimeSpan timeout)
		{
			throw new NotImplementedException();
		}
		#endregion
		#region IRetireAbleChannel implementation
		public void Retire()
		{
			throw new NotImplementedException();
		}
		public void Retire(bool immediate)
		{
			throw new NotImplementedException();
		}
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
		public WriteMarker(string name, int buffersize = 0, ChannelNameScope targetScope = ChannelNameScope.Local)
			: base(name, buffersize, targetScope)
		{
		}

		// Since this is just a marker, we do not implement any methods

		#region IWriteChannel implementation
		public Task WriteAsync(T value)
		{
			throw new NotImplementedException();
		}
		public Task WriteAsync(ITwoPhaseOffer offer, T value)
		{
			throw new NotImplementedException();
		}
		public Task WriteAsync(T value, TimeSpan timeout)
		{
			throw new NotImplementedException();
		}
		public Task WriteAsync(ITwoPhaseOffer offer, T value, TimeSpan timeout)
		{
			throw new NotImplementedException();
		}
		#endregion
		#region IRetireAbleChannel implementation
		public void Retire()
		{
			throw new NotImplementedException();
		}
		public void Retire(bool immediate)
		{
			throw new NotImplementedException();
		}
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

