using System;

namespace CoCoL
{
	/// <summary>
	/// Helper class for providing standard timeouts
	/// </summary>
    public static class Timeout
    {
        /// <summary>
        /// The timespan used to signal infinite waiting
        /// </summary>
        public static readonly TimeSpan Infinite = TimeSpan.FromMilliseconds(System.Threading.Timeout.Infinite);

        /// <summary>
        /// The timespan used to signal no waiting
        /// </summary>
        public static readonly TimeSpan Immediate = new TimeSpan(0);

		/// <summary>
		/// A marker instance for a wait forever
		/// </summary>
		public static readonly DateTime InfiniteDateTime = new DateTime(0);

    }
}

