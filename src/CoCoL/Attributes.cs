using System;

namespace CoCoL
{
	/// <summary>
	/// Attribute that indicates that this class is a process, and indicates how many instances should be started
	/// </summary>
	public class ProcessAttribute : Attribute
	{
		/// <summary>
		/// The number of processes
		/// </summary>
		public long ProcessCount = 1;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ProcessAttribute"/> class.
		/// </summary>
		/// <param name="count">The number of processes to launch.</param>
		public ProcessAttribute(long count = 1)
		{
			ProcessCount = count;
		}
	}

	/// <summary>
	/// Attribute that indicates that this method receives a read from any on the specified channels.
	/// The given channels are repeatedly read, and the method called when there is a result.
	/// </summary>
	public class OnReadAttribute : Attribute
	{
		/// <summary>
		/// The list of channels to read from
		/// </summary>
		public string[] Channels;
		/// <summary>
		/// The timeout used when reading the channels
		/// </summary>
		public TimeSpan Timeout;
		/// <summary>
		/// The priority used when reading the channels
		/// </summary>
		public MultiChannelPriority Priority;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.OnReadAttribute"/> class.
		/// </summary>
		/// <param name="channels">The channels to read from.</param>
		public OnReadAttribute(params string[] channels)
		{
			Channels = channels;
			Timeout = CoCoL.Timeout.Infinite;
			Priority = MultiChannelPriority.Any;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.OnReadAttribute"/> class.
		/// </summary>
		/// <param name="timeout">The read timeout.</param>
		/// <param name="channels">The channels to read from.</param>
		public OnReadAttribute(TimeSpan timeout, params string[] channels)
		{
			Channels = channels;
			Timeout = timeout;
			Priority = MultiChannelPriority.Any;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.OnReadAttribute"/> class.
		/// </summary>
		/// <param name="priority">The priority for selecting channels.</param>
		/// <param name="channels">The channels to read from.</param>
		public OnReadAttribute(MultiChannelPriority priority, params string[] channels)
		{
			Channels = channels;
			Timeout = CoCoL.Timeout.Infinite;
			Priority = priority;
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.OnReadAttribute"/> class.
		/// </summary>
		/// <param name="timeout">The read timeout.</param>
		/// <param name="priority">The priority for selecting channels.</param>
		/// <param name="channels">The channels to read from.</param>
		public OnReadAttribute(TimeSpan timeout, MultiChannelPriority priority, params string[] channels)
		{
			Channels = channels;
			Timeout = timeout;
			Priority = priority;
		}
	}
}

