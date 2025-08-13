using CoCoL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace UnitTest
{
    [TestClass]
    public class ChannelExtensionsTest
    {
        [TestMethod]
        public void TestWaitForTaskOrThrow_propagatesStackTrace()
        {
            Task t = Task.Factory.StartNew(() => ThrowingMethod());

            try
            {
                t.WaitForTaskOrThrow();
                Assert.Fail("Exception expected!");
            }
            catch (Exception ex)
            {
                // Unfortunately the method we want is missing in Mono
                if (!(ex is NotImplementedException))
                    StringAssert.Contains("ThrowingMethod", ex.ToString());
            }
        }

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(false, false, true)]
        [DataRow(false, true, false)]
        [DataRow(false, true, true)]
        [DataRow(true, false, false)]
        [DataRow(true, false, true)]
        [DataRow(true, true, false)]
        [DataRow(true, true, true)]
        public async Task TestCanBeCancelled(bool use_read, bool use_try, bool use_timeout)
        {
            var cts = new System.Threading.CancellationTokenSource();
            var channel = ChannelManager.CreateChannel<int>(buffersize: 0);

            Task task;
            if (use_read)
            {
                if (use_try)
                {
                    if (use_timeout)
                    {
                        task = channel.TryReadAsync(TimeSpan.FromSeconds(1), cts.Token);
                    }
                    else // !use_timeout
                    {
                        task = channel.TryReadAsync(cts.Token);
                    }
                }
                else // !use_try
                {
                    if (use_timeout)
                    {
                        task = channel.ReadAsync(TimeSpan.FromSeconds(1), cts.Token);
                    }
                    else // !use_timeout
                    {
                        task = channel.ReadAsync(cts.Token);
                    }
                }
            }
            else // !use_read
            {
                if (use_try)
                {
                    if (use_timeout)
                    {
                        task = channel.TryWriteAsync(1, TimeSpan.FromSeconds(1), cts.Token);
                    }
                    else // !use_timeout
                    {
                        task = channel.TryWriteAsync(1, cts.Token);
                    }
                }
                else // !use_try
                {
                    if (use_timeout)
                    {
                        task = channel.WriteAsync(1, TimeSpan.FromSeconds(1), cts.Token);
                    }
                    else // !use_timeout
                    {
                        task = channel.WriteAsync(1, cts.Token);
                    }
                }
            }

            cts.Cancel();

            try
            {
                if (use_try)
                {
                    if (use_read)
                    {
                        var bt = task as Task<System.Collections.Generic.KeyValuePair<bool, int>>;
                        var ret = await bt;
                        Assert.IsFalse(ret.Key, $"Expected false result for Try operation: {use_read}, {use_try}, {use_timeout}");
                    }
                    else
                    {
                        var bt = task as Task<bool>;
                        var ret = await bt;
                        Assert.IsFalse(ret, $"Expected false result for Try operation: {use_read}, {use_try}, {use_timeout}");
                    }
                }
                else
                {
                    await task;
                    Assert.Fail($"Expected TaskCanceledException: {use_read}, {use_try}, {use_timeout}");
                }
            }
            catch (TaskCanceledException)
            {
                // Expected result.
            }
        }

        private void ThrowingMethod()
        {
            throw new NotImplementedException("This method is not implemented.");
        }
    }
}
