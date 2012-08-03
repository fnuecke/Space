using Engine.FarMath;
using NUnit.Framework;

namespace Engine.Tests.FarMath
{
    [TestFixture]
    public class FarValueTests
    {
        [Test]
        public void TestCreation()
        {
            var a = (FarValue)1f;
            Assert.AreEqual((float)a, 1f, 0.00001f);

            a = 1000f;
            Assert.AreEqual(1000f, (float)a, 0.0001f);

            a = 100000f;
            Assert.AreEqual(100000f, (float)a, 0.001f);

            a = 10000000f;
            Assert.AreEqual(10000000f, (float)a, 0.1f);

            a = 1000000000f;
            Assert.AreEqual(1000000000f, (float)a, 1f);

            a = 100000000;
            Assert.AreEqual(100000000, (int)a);
        }

        [Test]
        public void TestOperations()
        {
            var a = (FarValue)12340000;
            var b = (FarValue)123.456789f;

            var c = a + b;
            Assert.AreEqual(12340123.456789f, (float)c, 0.001f);
            Assert.AreEqual(12340123, (int)c);
        }
    }
}
