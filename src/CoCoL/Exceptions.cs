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
}

