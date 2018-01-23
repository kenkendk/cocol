using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoCoL;

class Example
{
    static async Task Produce(IChannel<int> channel)
    {
        foreach (var i in Enumerable.Range(0, 5))
            await channel.WriteAsync(i);
        channel.Retire();
    }

    static async Task Consume(IEnumerable<IChannel<int>> channels)
    {
        while (true)
            Console.WriteLine("Hello World: {0}", await channels.ReadFromAnyAsync());
    }

    static void Main()
    {
        var channels = Enumerable.Range(0, 5).Select(x => {
            var channel = ChannelManager.CreateChannel<int>();
            // Using .FireAndForget() as we do not want to wait,
            // but not consuming the task gives a compiler warning
            Produce(channel).FireAndForget();
            return channel;
        });

        Consume(channels).Wait();
    }
}