CoCoL is a fresh multi-programming approach, leveraging the C# `await` keyword to produce sequential and easily understandable multithreading code. With a shared-nothing approach and explicit communication, programs written with CoCoL are automatically free from race conditions and other threading hazards.

If you are familiar with the [Go Language](https://golang.org/), you can think of CoCoL as providing the Go programming model inside the CLR.

## Hello World

The most basic program with multithreading would be a producer/consumer setup, where one thread produces data, and another consumes it:

```C#
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
```

Output:

```
Hello World: 0
Hello World: 1
Hello World: 2
Hello World: 3
Hello World: 4
```
