using CoCoL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    [TestFixture]
    class ChannelExtensionsTest
    {
        [Test]
        public void TestWaitForTaskOrThrow_propagatesStackTrace()
        {
            Task t = Task.Factory.StartNew(() => ThrowingMethod());

            try
            {
                t.WaitForTaskOrThrow();
                Assert.Fail("Exception expected!");
            } catch (Exception ex)
            {
                StringAssert.Contains("ThrowingMethod", ex.ToString());
            }
        }

        private void ThrowingMethod()
        {
            throw new NotImplementedException("This method is not implemented.");
        }
    }
}
