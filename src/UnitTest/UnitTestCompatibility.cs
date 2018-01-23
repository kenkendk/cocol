using System;
namespace UnitTest
{
    public static class TestAssert
    {
        public static void Throws<T>(Action m)
            where T : Exception
        {
#if NETCOREAPP2_0
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<T>(m);
#else
            NUnit.Framework.Assert.Throws<T>(() => m());
#endif
        }

        public static void IsInstanceOf<T>(object o)
        {
#if NETCOREAPP2_0
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsInstanceOfType(o, typeof(T));
#else
            NUnit.Framework.Assert.IsInstanceOf<T>(o);
#endif
        }

    }
}
