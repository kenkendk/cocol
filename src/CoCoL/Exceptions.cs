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
        /// The name of the channel that is retired
        /// </summary>
        public string ChannelName { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.RetiredException"/> class.
		/// </summary>
        /// <param name="channelname">The name of the channel</param>
        public RetiredException(string channelname) : base($"The channel \"{channelname}\" is retired") { ChannelName = channelname; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.RetiredException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
        /// <param name="channelname">The name of the channel</param>
        public RetiredException(string channelname, string message) : base(message) { ChannelName = channelname; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.RetiredException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="ex">The inner exception.</param>
        /// <param name="channelname">The name of the channel</param>
        public RetiredException(string channelname, string message, Exception ex) : base(message, ex) { ChannelName = channelname; }
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
        /// The name of the channel that is overflown
        /// </summary>
        public string ChannelName { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelOverflowException"/> class.
		/// </summary>
        /// <param name="channelname">The name of the channel</param>
        public ChannelOverflowException(string channelname) : base($"The channel \"{channelname}\" has too many pending operations") { ChannelName = channelname; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelOverflowException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
        /// <param name="channelname">The name of the channel</param>
        public ChannelOverflowException(string message, string channelname) : base(message) { ChannelName = channelname; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelOverflowException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="ex">The inner exception.</param>
        /// <param name="channelname">The name of the channel</param>
        public ChannelOverflowException(string channelname, string message, Exception ex) : base(message, ex) { ChannelName = channelname; }
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

