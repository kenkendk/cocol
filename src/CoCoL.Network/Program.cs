using System;

namespace CoCoL.Network
{
	/// <summary>
	/// The executable entry class
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name="args">The command-line arguments.</param>
		/// <returns>The exit code that is given to the operating system after the program ends.</returns>
		public static int Main(string[] args)
		{
			var channelserver = new NetworkChannelServer(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8888));
			channelserver.Dispose();

			return 0;
		}
	}
}

