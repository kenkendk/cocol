using System;
#if !DISABLE_SERIALIZATION
using System.Runtime.Serialization;
#endif

namespace CoCoL
{
	/// <summary>
	/// Exception which is thrown when attempting to access a retired channel
	/// </summary>
#if !DISABLE_SERIALIZATION
	[Serializable]
#endif
	public class RetiredException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.RetiredException"/> class.
		/// </summary>
		public RetiredException() : base("The channel is retired") {}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.RetiredException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		public RetiredException(string message) : base(message) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.RetiredException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="ex">The inner exception.</param>
		public RetiredException(string message, Exception ex) : base(message, ex) {}
#if !DISABLE_SERIALIZATION
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.RetiredException"/> class.
		/// </summary>
		/// <param name="info">The serialization info.</param>
		/// <param name="context">The streaming context.</param>
		public RetiredException(SerializationInfo info, StreamingContext context) : base(info, context) {}
#endif
	}

	/// <summary>
	/// Exception which is thrown when a channel attempt is discarded from overflow
	/// </summary>
#if !DISABLE_SERIALIZATION
	[Serializable]
#endif
	public class ChannelOverflowException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelOverflowException"/> class.
		/// </summary>
		public ChannelOverflowException() : base("The channel has too many pending operations") {}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelOverflowException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		public ChannelOverflowException(string message) : base(message) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelOverflowException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="ex">The inner exception.</param>
		public ChannelOverflowException(string message, Exception ex) : base(message, ex) {}
#if !DISABLE_SERIALIZATION
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelOverflowException"/> class.
		/// </summary>
		/// <param name="info">The serialization info.</param>
		/// <param name="context">The streaming context.</param>
		public ChannelOverflowException(SerializationInfo info, StreamingContext context) : base(info, context) {}
#endif
	}
}

