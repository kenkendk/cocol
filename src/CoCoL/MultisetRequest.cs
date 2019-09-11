using System;

namespace CoCoL
{
	/// <summary>
	/// A request for a multi-channel operation
	/// </summary>
	public static class MultisetRequest
	{
		/// <summary>
		/// Constructs a request to read a channel.
		/// </summary>
		/// <param name="channel">The channel to read from.</param>
		public static MultisetRequest<T> Read<T>(IReadChannel<T> channel)
		{
			return new MultisetRequest<T>(default(T), channel, null, true);
		}

		/// <summary>
		/// Constructs a request to write a channel.
		/// </summary>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channel">The channel to write to.</param>
		public static MultisetRequest<T> Write<T>(T value, IWriteChannel<T> channel)
		{
			return new MultisetRequest<T>(value, null, channel, false);
		}
	}

	/// <summary>
	/// A request for a multi-channel operation
	/// </summary>
	public struct MultisetRequest<T> : IMultisetRequestUntyped, IEquatable<MultisetRequest<T>>
	{
		/// <summary>
		/// The result value
		/// </summary>
		public T Value { get; set; }
		/// <summary>
		/// The channel being read from
		/// </summary>
		public readonly IReadChannel<T> ReadChannel;
		/// <summary>
		/// The channel being written to
		/// </summary>
		public readonly IWriteChannel<T> WriteChannel;
		/// <summary>
		/// Gets a value indicating if this is a read operation
		/// </summary>
		public readonly bool IsRead;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.MultisetRequest&lt;T&gt;"/> struct.
		/// </summary>
		/// <param name="value">The value to write</param>
		/// <param name="readChannel">The channel to read.</param>
		/// <param name="writeChannel">The channel to write.</param>
		/// <param name="read"><c>true</c> if this is a read operation, <c>false</c> otherwise</param>
		internal MultisetRequest(T value, IReadChannel<T> readChannel, IWriteChannel<T> writeChannel, bool read)
		{
			Value = value;
			ReadChannel = readChannel;
			WriteChannel = writeChannel;
			IsRead = read;
		}

		/// <summary>
		/// Constructs a request to read a channel.
		/// </summary>
		/// <param name="channel">The channel to read from.</param>
		public static MultisetRequest<T> Read(IReadChannel<T> channel)
		{
			return new MultisetRequest<T>(default(T), channel, null, true);
		}

		/// <summary>
		/// Constructs a request to write a channel.
		/// </summary>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="channel">The channel to write to.</param>
		public static MultisetRequest<T> Write(T value, IWriteChannel<T> channel)
		{
			return new MultisetRequest<T>(value, null, channel, false);
		}

        /// <summary>
        /// Explicit disable compares
        /// </summary>
        /// <param name="other">The item to compare to</param>
        /// <returns>Always throws an exception</returns>
        bool IEquatable<MultisetRequest<T>>.Equals(MultisetRequest<T> other)
        {
            throw new NotImplementedException();
        }

        #region IMultisetRequestUntyped implementation

        /// <summary>
        /// Gets the boxed value.
        /// </summary>
        /// <value>The value.</value>
        object IMultisetRequestUntyped.Value 
		{
			get { return this.Value; }
			set 
			{ 
#if LIMITED_REFLECTION_SUPPORT
				if ( System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(T)).IsValueType && value == null)
#else
				if (typeof(T).IsValueType && value == null)
#endif
					this.Value = default(T);
				else
					this.Value = (T)value; 
			}
		}

		/// <summary>
		/// Gets the channel.
		/// </summary>
		/// <value>The channel.</value>
		IUntypedChannel IMultisetRequestUntyped.Channel
		{
			get { return this.ReadChannel == null ? (IUntypedChannel)this.WriteChannel : (IUntypedChannel)this.ReadChannel; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is a read request.
		/// </summary>
		/// <value><c>true</c> if this instance is read request; otherwise, <c>false</c>.</value>
		bool IMultisetRequestUntyped.IsRead
		{
			get { return this.IsRead; }
		}

		#endregion
	}
}
