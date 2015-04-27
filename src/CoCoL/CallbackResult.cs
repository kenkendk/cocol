using System;

namespace CoCoL
{
	/// <summary>
	/// Implementation of a callback result
	/// </summary>
	public class CallbackResult<T> : ICallbackResult<T>
	{
		/// <summary>
		/// The result, if any
		/// </summary>
		private T m_res;

		/// <summary>
		/// Gets the value written to a channel, or throws the exception
		/// </summary>
		/// <value>The result.</value>
		public T Result 
		{ 
			get 
			{
				if (Exception != null)
					throw Exception;
				return m_res;
					
			}
			internal set 
			{
				m_res = value;
			}
		}
		/// <summary>
		/// Gets the exception found on a channel, or null
		/// </summary>
		/// <value>The exception on the channel, or null.</value>
		public Exception Exception { get; internal set; }
		/// <summary>
		/// Gets the channel.
		/// </summary>
		/// <value>The channel that was read or written.</value>
		public IChannel<T> Channel { get; internal set; }
		/// <summary>
		/// Gets or sets the expires.
		/// </summary>
		/// <value>The expires.</value>
		public DateTime Expires { get; internal set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.CallbackResult`1"/> class.
		/// </summary>
		/// <param name="result">The result value.</param>
		/// <param name="exception">The exception on the channel, or null.</param>
		/// <param name="channel">The channel that was read or written.</param>
		public CallbackResult(T result, Exception exception, IChannel<T> channel)
		{
			m_res = result;
			Exception = exception;
			Channel = channel;
		}
	}
}

