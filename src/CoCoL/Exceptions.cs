using System;
using System.Runtime.Serialization;

namespace CoCoL
{
	[Serializable]
	public class RetiredException : Exception
	{
		public RetiredException() : base("The channel is retired") {}
		public RetiredException(string message) : base(message) {}
		public RetiredException(string message, Exception ex) : base(message, ex) {}
		public RetiredException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}
}

