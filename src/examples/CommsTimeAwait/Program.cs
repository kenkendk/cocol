using System;
using CoCoL;

namespace CommsTimeAwait
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			CoCoL.Loader.StartFromTypes(typeof(TickCollector), typeof(CommsTime));

			Console.WriteLine("Running, press CTRL+C to stop");

			var terminateChannel = CoCoL.ChannelManager.GetChannel<bool>(TickCollector.TERM_CHANNEL_NAME);

			try 
			{
				// Blocking read
				terminateChannel.Read();
			}
			catch 
			{
			}



		}
	}
}
