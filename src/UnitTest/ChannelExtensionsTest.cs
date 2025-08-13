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

        [TestMethod]
        public async Task TestNormalChannelOperationWithCancellationToken()
        {
            var cts = new System.Threading.CancellationTokenSource();
            var channel = ChannelManager.CreateChannel<int>(buffersize: 1);

            // Test normal without timeout
            await channel.WriteAsync(1, cts.Token);
            var res = await channel.ReadAsync(cts.Token);
            Assert.AreEqual(1, res);

            // Test normal with non-triggering timeout
            await channel.WriteAsync(2, TimeSpan.FromSeconds(1), cts.Token);
            var res2 = await channel.ReadAsync(cts.Token);
            Assert.AreEqual(2, res2);

            // Test try methods without timeout
            var tryWriteResult = await channel.TryWriteAsync(3, cts.Token);
            Assert.IsTrue(tryWriteResult, "Expected TryWrite to succeed without timeout");
            var tryReadResult = await channel.TryReadAsync(cts.Token);
            Assert.IsTrue(tryReadResult.Key, "Expected TryRead to succeed without timeout");
            Assert.AreEqual(3, tryReadResult.Value);

            // Test try methods with non-triggering timeout
            var tryWriteResult2 = await channel.TryWriteAsync(4, TimeSpan.FromSeconds(1), cts.Token);
            Assert.IsTrue(tryWriteResult2, "Expected TryWrite to succeed with non-triggering timeout");
            var tryReadResult2 = await channel.TryReadAsync(TimeSpan.FromSeconds(1), cts.Token);
            Assert.IsTrue(tryReadResult2.Key, "Expected TryRead to succeed with non-triggering timeout");
            Assert.AreEqual(4, tryReadResult2.Value);
        }

        private void ThrowingMethod()
        {
            throw new NotImplementedException("This method is not implemented.");
        }
    }
}
