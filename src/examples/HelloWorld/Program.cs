using System;
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

    static async Task Consume(IChannel<int> channel)
    {
        try
        {
            while (true)
                Console.WriteLine("Hello World: {0}", await channel.ReadAsync());
        }
        catch (RetiredException)
        {
        }
    }

    static void Main()
    {
        var channel = ChannelManager.CreateChannel<int>();
        Task.WhenAll(
          Produce(channel),
          Consume(channel)
        ).Wait();
    }
}