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
	/// </summary>
	{
		/// <summary>
		/// </summary>

		/// <summary>
		/// </summary>

		/// <summary>
		/// </summary>
		{
		}
	}
}

