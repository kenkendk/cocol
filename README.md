# CoCoL - Concurrent Communications Library

[![Build Status on Travis-CI](https://travis-ci.org/kenkendk/cocol.svg?branch=master)](https://travis-ci.org/kenkendk/cocol)
[![Build status on AppVeyor](https://ci.appveyor.com/api/projects/status/v72maima8o12vsn6/branch/master?svg=true)](https://ci.appveyor.com/project/kenkendk/cocol/branch/master)
[![Nuget count](https://img.shields.io/nuget/v/CoCoL.svg)](https://www.nuget.org/packages/CoCoL/)
[![License](https://img.shields.io/github/license/kenkendk/cocol.svg)](https://github.com/kenkendk/cocol/blob/master/LICENSE)
[![Issues open](https://img.shields.io/github/issues-raw/kenkendk/cocol.svg)](https://github.com/kenkendk/cocol/issues/)
[![Coverage Status](https://coveralls.io/repos/github/kenkendk/cocol/badge.svg?branch=HEAD)](https://coveralls.io/github/kenkendk/cocol?branch=HEAD)

CoCoL is a fresh multi-programming approach, leveraging the C# `await` keyword to produce sequential and easily understandable multithreading code. With a shared-nothing approach and explicit communication, programs written with CoCoL are automatically free from race conditions and other threading hazards.

If you are familiar with the [Go Language](https://golang.org/), you can think of CoCoL as providing the Go programming model inside the CLR.

Installation
------------
The [NuGet package](https://www.nuget.org/packages/CoCoL) is the recommended way of installing CoCoL:

```
PM> Install-Package CoCoL
```


Hello World
-----------

The most basic program with multithreading would be a producer/consumer setup, where one thread produces data, and another consumes it:
```C#
using CoCoL;

class Example
{
  async Task Produce(IChannel<int> channel)
  {
    foreach(var i in Enumerable.Range(0, 5))
      await channel.WriteAsync(i);
    channel.Retire();
  }

  async Task Consume(IChannel<int> channel)
  {
    try
    {
      while(true)
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

The *producer* writes a number of values into the channel, and then stops the channel. The *consumer* simply reads whatever is written into the channel.

Since the producer and consumer only shares the channel, and only communicate through the channel, no race conditions are possible.

Sharing a channel
-----------------

A key feature of CoCoL is that it is capable of scaling to a very large number of channels and reader/writers. On a machine with 16GB of memory, it is possible to run more than 10 million reader/writer pairs. Using the above producer and consumer example, it is possible to attach multiple producers and consumers on the same channel, with no ill effects:
```C#
static void Main()
{
  var channel = ChannelManager.CreateChannel<int>();
  
  Task.WhenAll(
    Enumerable.Range(0,4).Select(x => Produce(channel))
    .Union(
      Enumerable.Range(0,2).Select(x => Consume(channel))
    )
).Wait();
}
```

In the above, there are four producers and two consumers sharing the same channel. The runtime adjusts how many threads it deems necessary for running the application.

Multiple channels
-----------------

If the producers each have their own channel, the consumer can choose to read from any of the channels:

```C#
async Task Consume(IEnumerable<IChannel<int>> channels)
{
  while(true)
    Console.WriteLine("Hello World: {0}", await channels.ReadFromAnyAsync());
}

static void Main()
  {
    var channels = Enumerable.Range(0, 5).Select(x => {
      var channel = ChannelManager.CreateChannel<int>();
      Produce(channel);
      return channel;
    });
    
    Consume(channels).Wait();
  }
```

Mixing with existing multithreading
-----------------------------------

If existing code is used, it is possible to use blocking or probing calls as well:

```C#
static void Main()
{
  var channel = ChannelManager.CreateChannel<int>();
  
  var thread = new System.Threading.Thread(x => {
    // Probing write
    while(!channel.TryWrite(1))
    {
      // Do other stuff here
      System.Thread.Thread.Sleep(1000);
    }
  });
  thread.Run();
  
  // Blocking read
  channel.Read();
  
}
```

More examples?
--------------

Look at the [CommsTime](https://github.com/kenkendk/cocol/blob/master/src/examples/CommsTimeAwait/Program.cs), [Sieve](https://github.com/kenkendk/cocol/blob/master/src/examples/Sieve/Program.cs), and [Mandelbrot](https://github.com/kenkendk/cocol/blob/master/src/examples/Mandelbrot/Program.cs) examples.

