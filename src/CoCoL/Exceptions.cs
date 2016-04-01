using System;
using System.Runtime.Serialization;

namespace CoCoL
{
	/// <summary>
	/// Exception which is thrown when attempting to access a retired channel
	/// </summary>
#if !PCL_BUILD
	[Serializable]
#endif
	public class RetiredException : Exception
	{
		public RetiredException() : base("The channel is retired") {}
		public RetiredException(string message) : base(message) {}
		public RetiredException(string message, Exception ex) : base(message, ex) {}
#if !PCL_BUILD
		public RetiredException(SerializationInfo info, StreamingContext context) : base(info, context) {}
#endif
	}

	/// <summary>
	/// Exception which is thrown when a channel attempt is discarded from overflow
	/// </summary>
#if !PCL_BUILD
	[Serializable]
#endif
	public class ChannelOverflowException : Exception
	{
		public ChannelOverflowException() : base("The channel has too many pending operations") {}
		public ChannelOverflowException(string message) : base(message) {}
		public ChannelOverflowException(string message, Exception ex) : base(message, ex) {}
#if !PCL_BUILD
		public ChannelOverflowException(SerializationInfo info, StreamingContext context) : base(info, context) {}
#endif
	}
}

