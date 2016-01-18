using System;

namespace CoCoL
{
	/// <summary>
	/// Attribute that indicates that this class is a process, and indicates how many instances should be started
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
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
	/// Attribute for naming a channel in automatic wireup
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class ChannelNameAttribute : Attribute
	{
		/// <summary>
		/// The name of the channel
		/// </summary>
		public string Name;

		/// <summary>
		/// The buffer size of the channel
		/// </summary>
		public int BufferSize;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelNameAttribute"/> class.
		/// </summary>
		/// <param name="name">The name of the channel.</param>
		/// <param name="buffersize">The size of the buffer on the created channel</param>
		public ChannelNameAttribute(string name, int buffersize = 0)
		{
			Name = name;
			BufferSize = buffersize;
		}
	}


}

