using System;
namespace UnitTest
{
    [Serializable]
    public class UnittestException : Exception
    {
        public UnittestException(string message)
            : base(message)
        {
        }
    }

    public static class TestAssert
    {
        public static void Throws<T>(Action m)
            where T : Exception
        {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<T>(m);
        }

        public static void IsInstanceOf<T>(object o)
        {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsInstanceOfType(o, typeof(T));
        }

    }
}
