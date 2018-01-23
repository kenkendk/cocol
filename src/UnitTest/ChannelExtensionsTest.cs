using CoCoL;
using System;
using System.Threading.Tasks;

#if NETCOREAPP2_0
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TOP_LEVEL = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using TEST_METHOD = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#else
using NUnit.Framework;
using TOP_LEVEL = NUnit.Framework.TestFixtureAttribute;
using TEST_METHOD = NUnit.Framework.TestAttribute;
#endif

namespace UnitTest
{
    [TOP_LEVEL]
    public class ChannelExtensionsTest
    {
        [TEST_METHOD]
        public void TestWaitForTaskOrThrow_propagatesStackTrace()
        {
            Task t = Task.Factory.StartNew(() => ThrowingMethod());

            try
            {
                t.WaitForTaskOrThrow();
                Assert.Fail("Exception expected!");
            } catch (Exception ex)
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
