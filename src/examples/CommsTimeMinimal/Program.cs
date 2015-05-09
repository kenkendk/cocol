using System;

namespace CommsTimeMinimal
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			CoCoL.Loader.StartFromTypes(typeof(TickCollector), typeof(CommsTime));

			Console.WriteLine("Running, press CTRL+C to stop");

			var terminateChannel = SimpleBlockingChannelManager.GetChannel<bool>(TickCollector.TERM_CHANNEL_NAME);

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
