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

        private void ThrowingMethod()
        {
            throw new NotImplementedException("This method is not implemented.");
        }
    }
}
