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
        public async Task TestReadCanBeCancelled()
        {
            var cts = new System.Threading.CancellationTokenSource();
            var channel = ChannelManager.CreateChannel<int>(buffersize: 0);
            var task = channel.ReadAsync(cts.Token);
            cts.Cancel();

            try
            {
                await task;
                Assert.Fail("Exception expected!");
            }
            catch (TaskCanceledException)
            {
                // Expected result.
            }
        }

        [TestMethod]
        public async Task TestWriteCanBeCancelled()
        {
            var cts = new System.Threading.CancellationTokenSource();
            var channel = ChannelManager.CreateChannel<int>(buffersize: 0);

            var task = channel.WriteAsync(1, cts.Token);
            cts.Cancel();

            try
            {
                await task;
                Assert.Fail("Exception expected!");
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
