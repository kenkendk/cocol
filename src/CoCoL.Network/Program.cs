using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;

namespace CoCoL.Network
{
	/// <summary>
	/// The executable entry class
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// Helper class to configure the channel server
		/// </summary>
		private class Config : SettingsHelper
		{
			/// <summary>
			/// The address used to listen for connections
			/// </summary>
			[CommandlineOption(
				description: "Sets the adapter to use, special values \"any\" or \"loopback\" are accepted, otherwise an IP", 
				defaultvalue: "any", 
				customparser: "ParseIPAddr",
				shortname: "a"
			)]
			public IPAddress Adapter = IPAddress.Any;

			/// <summary>
			/// The port used to listen for connections
			/// </summary>
			[CommandlineOption("Sets the network port to use")]
			public int Port = 8888;

			/// <summary>
			/// Helper method to parse an IP address
			/// </summary>
			/// <returns>The IP address.</returns>
			/// <param name="value">The string to parse.</param>
			private static IPAddress ParseIPAddr(string value)
			{
				if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "*", StringComparison.InvariantCultureIgnoreCase) || string.Equals(value, "any", StringComparison.InvariantCultureIgnoreCase))
					return IPAddress.Any;
				if (string.Equals(value, "loopback", StringComparison.InvariantCultureIgnoreCase) || string.Equals(value, "127.0.0.1", StringComparison.InvariantCultureIgnoreCase))
					return IPAddress.Loopback;

				return IPAddress.Parse(value);
					
			}
		}

		/// <summary>
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name="args">The command-line arguments.</param>
		/// <returns>The exit code that is given to the operating system after the program ends.</returns>
		public static int Main(string[] args)
		{
			var cfg = new Config();
			if (!Config.Parse(args.ToList(), cfg))
				return -1;

			var channelserver = new NetworkChannelServer(new IPEndPoint(cfg.Adapter, cfg.Port));
			Console.WriteLine("Running server ...");
			channelserver.RunAsync().WaitForTaskOrThrow();

			return 0;
		}
	}
}

