using System;
using Engine.FarMath;
using NUnit.Framework;

namespace Engine.Tests.FarMath
{
    [TestFixture]
    public class FarValueTests
    {
        [Test]
        public void TestOperations()
        {
            var random = new Random(0);

            for (var i = 0; i < 1000000; i++)
            {
                // Test initialization from int.
                var intValue = random.Next(int.MinValue, int.MaxValue);
                var farValue = (FarValue)intValue;
                Assert.AreEqual(intValue, (int)farValue);

                // Test initialization from float. Floats are only precise in a very
                // limited range, so we don't want to generate all that large values here.
                var floatValue = (float)(short.MinValue + (random.NextDouble() * (short.MaxValue - (double)short.MinValue)));
                farValue = floatValue;
                Assert.AreEqual(floatValue, (float)farValue, 0.01f);

                // Test unary minus (negation).
                Assert.AreEqual(-floatValue, (float)(-farValue), 0.01f);
                farValue = intValue;
                Assert.AreEqual(-intValue, (int)(-farValue));

                // Test multiplication.
                intValue = random.Next(-FarValue.SegmentSize * 10, FarValue.SegmentSize * 10);
                farValue = intValue;
                Assert.AreEqual(intValue, (int)farValue);

                var mulIntValue = random.Next(-100, 100);
                intValue *= mulIntValue;
                farValue *= mulIntValue;
                Assert.AreEqual(intValue, (int)farValue);

                floatValue = (float)(short.MinValue / 2 + (random.NextDouble() * (short.MaxValue - (double)short.MinValue) / 2));
                farValue = floatValue;
                Assert.AreEqual(floatValue, (float)farValue, 0.01f);

                var mulFloatValue = (float)(random.NextDouble() * 20 - 10);
                floatValue *= mulFloatValue;
                farValue *= mulFloatValue;
                Assert.AreEqual(floatValue, (float)farValue, 0.1f);

                // Test division.
                floatValue = (float)(short.MinValue / 2 + (random.NextDouble() * (short.MaxValue - (double)short.MinValue) / 2));
                farValue = floatValue;
                Assert.AreEqual(floatValue, (float)farValue, 0.01f);

                var divFloatValue = (float)(random.NextDouble() * FarValue.SegmentSize * 4 - FarValue.SegmentSize * 2);
                floatValue /= divFloatValue;
                farValue /= divFloatValue;
                Assert.AreEqual(floatValue, (float)farValue, 0.01f);

                // Test modulo.
                intValue = random.Next(-FarValue.SegmentSize * 10, FarValue.SegmentSize * 10);
                farValue = intValue;
                Assert.AreEqual(intValue, (int)farValue);

                var modValue = random.Next(1, FarValue.SegmentSize * 5);
                intValue %= modValue;
                farValue %= modValue;
                Assert.AreEqual(intValue, (int)farValue);

                // Test addition/subtraction.
                intValue = random.Next(-FarValue.SegmentSize * 10, FarValue.SegmentSize * 10);
                farValue = intValue;
                Assert.AreEqual(intValue, (int)farValue);

                var addValue = random.Next(-FarValue.SegmentSize * 5, FarValue.SegmentSize * 5);
                intValue += addValue;
                farValue += addValue;
                Assert.AreEqual(intValue, (int)farValue);

                var subValue = random.Next(-FarValue.SegmentSize * 5, FarValue.SegmentSize * 5);
                intValue -= subValue;
                farValue -= subValue;
                Assert.AreEqual(intValue, (int)farValue);

                // Test comparisons.
                intValue = random.Next(-FarValue.SegmentSize * 10, FarValue.SegmentSize * 10);
                farValue = intValue;
                Assert.AreEqual(intValue, (int)farValue);

                addValue = random.Next(1, FarValue.SegmentSize * 5);
                farValue += addValue;
                Assert.IsTrue(intValue < farValue);
                Assert.IsTrue(intValue <= farValue);

                farValue -= 2 * addValue;
                Assert.IsTrue(intValue > farValue);
                Assert.IsTrue(intValue >= farValue);

                var farValueTwo = farValue;
                Assert.IsTrue(farValue == farValueTwo);

                farValueTwo += addValue;
                Assert.IsTrue(farValue != farValueTwo);

                farValue += addValue;
                Assert.IsTrue(farValue == farValueTwo);
            }
        }
    }
}
