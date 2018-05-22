using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoCoL
{
    /// <summary>
    /// Set of commonly used methods to set up simple connected processes
    /// </summary>
    public static class Skeletons
    {
        /// <summary>
        /// Sends all values from the enumerable to a channel
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="data">The data to send.</param>
        /// <param name="target">The channel to send the data to.</param>
        /// <typeparam name="T">The data type parameter.</typeparam>
        public static Task EmitAsync<T>(IEnumerable<T> data, IWriteChannel<T> target)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return AutomationExtensions.RunTask(
                new { Target = target },
                async self =>
                {
                    foreach (var n in data)
                        await target.WriteAsync(n);
                }
           );
        }

        /// <summary>
        /// Helper method that sets up a simple process that performs the same task for each received request,
        /// optionally performing the handlers in parallel
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="channel">The channel to handle messages for.</param>
        /// <param name="handler">The method to invoke for each message.</param>
        /// <param name="maxparallel">The maximum number of parallel handlers</param>
        /// <typeparam name="T">The channel data type parameter.</typeparam>
        public static Task CollectAsync<T>(IReadChannel<T> channel, Func<T, Task> handler, int maxparallel = 1)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return AutomationExtensions.RunTask(
                new { Source = channel },
                async self =>
                {
                    // List of pending task; task zero is the reader
                    var pending = new List<Task>() { self.Source.ReadAsync() };

                    while (true)
                    {
                        // Wait for something to happen
                        var t = await Task.WhenAny(pending);

                        // If the reader completed
                        if (t == pending[0])
                        {
                            // Did it work?
                            if (t.IsCompleted && !(t.IsCanceled || t.IsFaulted))
                            {
                                if (pending.Count >= maxparallel)
                                    pending[0] = null;
                                else
                                    pending[0] = Task.Run(() => self.Source.ReadAsync());

                                // Unwrap the data
                                var data = await (Task<T>)t;

                                // Add the handler process to the queue
                                pending.Add(Task.Run(() => handler(data)));
                            }
                            // Otherwise we are probably being terminated
                            else
                            {
                                await Task.WhenAll(pending.Skip(1));

                                // Re-throw the error
                                await t;
                            }
                        }

                        // Non-reader completed, move on
                        else
                        {
                            pending.Remove(t);
                            // We have space, so accept a new read, if required

                            if (pending[0] == null)
                                pending[0] = self.Source.ReadAsync();
                        }
                    }
                });
        }

        /// <summary>
        /// Sets up a simple channel transformation process
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="input">The input channel.</param>
        /// <param name="output">The output channel.</param>
        /// <param name="transform">The method transforming the input</param>
        /// <param name="maxparallel">The maximum number of parallel handlers</param>
        /// <typeparam name="TIn">The input type parameter.</typeparam>
        /// <typeparam name="TOut">The output type parameter.</typeparam>
        public static Task TransformAsync<TIn, TOut>(IReadChannel<TIn> input, IWriteChannel<TOut> output, Func<TIn, TOut> transform, int maxparallel = 1)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            return CollectAsync(input, data => output.WriteAsync(transform(data)), maxparallel);
        }

        /// <summary>
        /// Sets up a simple channel transformation process
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="input">The input channel.</param>
        /// <param name="output">The output channel.</param>
        /// <param name="transform">The method transforming the input</param>
        /// <param name="maxparallel">The maximum number of parallel handlers</param>
        /// <typeparam name="TIn">The input type parameter.</typeparam>
        /// <typeparam name="TOut">The output type parameter.</typeparam>
        public static Task TransformAsync<TIn, TOut>(IReadChannel<TIn> input, IWriteChannel<TOut> output, Func<TIn, Task<TOut>> transform, int maxparallel = 1)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));
            
            return CollectAsync(input, async data => await output.WriteAsync(await transform(data)), maxparallel);
        }

        /// <summary>
        /// The options for distribution
        /// </summary>
        public enum ScatterGatherPolicy
        {
            /// <summary>
            /// Uses any valid channel
            /// </summary>
            Any,
            /// <summary>
            /// Ensures equal distribution on the channels
            /// </summary>
            RoundRobin
        }

        /// <summary>
        /// Sets up a simple process that distributes the input to the output channels
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="input">The data source.</param>
        /// <param name="output">The channels to distribute the data to.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static Task ScatterAsync<T>(IReadChannel<T> input, IWriteChannel<T>[] output, ScatterGatherPolicy policy = ScatterGatherPolicy.Any)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (output == null || output.Any(x => x == null))
                throw new ArgumentNullException(nameof(output));

            return AutomationExtensions.RunTask(
                new { Input = input, Output = output },
                async self =>
                {
                    if (policy == ScatterGatherPolicy.Any)
                    {
                        while (true)
                            await MultiChannelAccess.WriteToAnyAsync(await self.Input.ReadAsync(), self.Output);
                    }
                    else
                    {
                        var ix = 0;
                        while (true)
                        {
                            await self.Output[ix].WriteAsync(await self.Input.ReadAsync());
                            ix = (ix + 1) % output.Length;
                        }
                    }
                }
            );
        }

        /// <summary>
        /// Sets up a simple process that forwards the inputs to the output
        /// </summary>
        /// <returns>The gather.</returns>
        /// <param name="input">Input.</param>
        /// <param name="output">Output.</param>
        /// <param name="policy">Policy.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static Task GatherAsync<T>(IReadChannel<T>[] input, IWriteChannel<T> output, ScatterGatherPolicy policy = ScatterGatherPolicy.Any)
        {
            if (input == null || input.Any(x => x == null))
                throw new ArgumentNullException(nameof(input));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            return AutomationExtensions.RunTask(
                new { Input = input, Output = output },
                async self =>
                {
                    var lst = input.ToList();
                    while (true)
                    {
                        if (policy == ScatterGatherPolicy.Any)
                        {
                            try
                            {
                                await self.Output.WriteAsync((await lst.ReadFromAnyAsync()).Value);
                            }
                            catch (Exception ex)
                            {
                                if (ex.IsRetiredException())
                                {
                                    var any = false;
                                    for (var i = lst.Count - 1; i >= 0; i--)
                                        if (any |= await lst[i].IsRetiredAsync)
                                            lst.RemoveAt(i);

                                    if (lst.Count > 0 && any)
                                        continue;
                                }

                                throw;
                            }
                        }
                        else
                        {

                            for (var i = 0; i < lst.Count; i++)
                            {
                                try
                                {
                                    await self.Output.WriteAsync(await lst[i].ReadAsync());
                                }
                                catch (Exception ex)
                                {
                                    if (ex.IsRetiredException() && lst.Count > 1)
                                    {
                                        lst.RemoveAt(i);
                                        i--;
                                        continue;
                                    }
                                    throw;
                                }
                            }
                        }
                    }
                }
            );
        }

        /// <summary>
        /// Combines inputs from <paramref name="inputA"/> and <paramref name="inputB"/> and writes it to <paramref name="output"/>.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="inputA">One input channel.</param>
        /// <param name="inputB">Another input channel.</param>
        /// <param name="output">The output result.</param>
        /// <param name="method">The zip method</param>
        /// <typeparam name="Ta">One data type parameter.</typeparam>
        /// <typeparam name="Tb">Another data type parameter.</typeparam>
        /// <typeparam name="TOut">The result type parameter.</typeparam>
        public static Task ZipAsync<Ta, Tb, TOut>(IReadChannel<Ta> inputA, IReadChannel<Tb> inputB, IWriteChannel<TOut> output, Func<Ta, Tb, TOut> method)
        {
            if (inputA == null)
                throw new ArgumentNullException(nameof(inputA));
            if (inputB == null)
                throw new ArgumentNullException(nameof(inputB));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            return AutomationExtensions.RunTask(
                new
                {
                    InputA = inputA,
                    InputB = inputB,
                    Output = output
                },
                async self =>
                {
                    while (true)
                    {
                        var readA = self.InputA.ReadAsync();
                        var readB = self.InputB.ReadAsync();
                        var vA = await readA;
                        var vB = await readB;
                        await output.WriteAsync(method(vA, vB));
                    }
                }
            );
        }

        /// <summary>
        /// Combines inputs from <paramref name="inputA"/> and <paramref name="inputB"/> and writes it to <paramref name="output"/>.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="inputA">One input channel.</param>
        /// <param name="inputB">Another input channel.</param>
        /// <param name="output">The output result.</param>
        /// <param name="method">The zip method</param>
        /// <typeparam name="Ta">One data type parameter.</typeparam>
        /// <typeparam name="Tb">Another data type parameter.</typeparam>
        /// <typeparam name="TOut">The result type parameter.</typeparam>
        public static Task ZipAsync<Ta, Tb, TOut>(IReadChannel<Ta> inputA, IReadChannel<Tb> inputB, IWriteChannel<TOut> output, Func<Ta, Tb, Task<TOut>> method)
        {
            if (inputA == null)
                throw new ArgumentNullException(nameof(inputA));
            if (inputB == null)
                throw new ArgumentNullException(nameof(inputB));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            return AutomationExtensions.RunTask(
                new
                {
                    InputA = inputA,
                    InputB = inputB,
                    Output = output
                },
                async self =>
                {
                    while (true)
                    {
                        var readA = self.InputA.ReadAsync();
                        var readB = self.InputB.ReadAsync();
                        var vA = await readA;
                        var vB = await readB;
                        await output.WriteAsync(await method(vA, vB));
                    }
                }
            );
        }

        /// <summary>
        /// Splits all values from a channel into two channels
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="input">The channel to read from.</param>
        /// <param name="outputA">One output channel.</param>
        /// <param name="outputB">Another output channel.</param>
        /// <param name="method">The method that performs the split.</param>
        /// <typeparam name="TIn">The input data type parameter.</typeparam>
        /// <typeparam name="Ta">One output data type parameter.</typeparam>
        /// <typeparam name="Tb">Another output data type parameter.</typeparam>
        public static Task SplitAsync<TIn, Ta, Tb>(IReadChannel<TIn> input, IWriteChannel<Ta> outputA, IWriteChannel<Tb> outputB, Func<TIn, Tuple<Ta, Tb>> method)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (outputA == null)
                throw new ArgumentNullException(nameof(outputA));
            if (outputB == null)
                throw new ArgumentNullException(nameof(outputB));
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            return AutomationExtensions.RunTask(
                new
                {
                    Input = input,
                    OutputA = outputA,
                    OutputB = outputB
                },
                async self =>
                {
                    while (true)
                    {
                        var n = method(await self.Input.ReadAsync());
                        var writeA = self.OutputA.WriteAsync(n.Item1);
                        await self.OutputB.WriteAsync(n.Item2);
                        await writeA; 
                    }
                }
            );
        }

        /// <summary>
        /// Splits all values from a channel into two channels
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="input">The channel to read from.</param>
        /// <param name="outputA">One output channel.</param>
        /// <param name="outputB">Another output channel.</param>
        /// <param name="method">The method that performs the split.</param>
        /// <typeparam name="TIn">The input data type parameter.</typeparam>
        /// <typeparam name="Ta">One output data type parameter.</typeparam>
        /// <typeparam name="Tb">Another output data type parameter.</typeparam>
        public static Task SplitAsync<TIn, Ta, Tb>(IReadChannel<TIn> input, IWriteChannel<Ta> outputA, IWriteChannel<Tb> outputB, Func<TIn, Task<Tuple<Ta, Tb>>> method)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (outputA == null)
                throw new ArgumentNullException(nameof(outputA));
            if (outputB == null)
                throw new ArgumentNullException(nameof(outputB));
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            return AutomationExtensions.RunTask(
                new
                {
                    Input = input,
                    OutputA = outputA,
                    OutputB = outputB
                },
                async self =>
                {
                    while (true)
                    {
                        var n = await method(await self.Input.ReadAsync());
                        var writeA = self.OutputA.WriteAsync(n.Item1);
                        await self.OutputB.WriteAsync(n.Item2);
                        await writeA;
                    }
                }
            );
        }

    }
}
