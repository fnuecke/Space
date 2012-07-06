using System.Collections.Generic;
using Engine.Util;
using NUnit.Framework;

namespace Engine.Tests.Base.Util
{
    /// <summary>
    /// Base class for copy tests.
    /// </summary>
    /// <typeparam name="T">The class to test.</typeparam>
    [TestFixture]
    public abstract class AbstractCopyableTest<T>
        where T : IHashable, ICopyable<T>
    {
        [Test]
        public void DeepCopyViaHashing()
        {
            // Get a list of instances to test.
            var instances = NewInstances();

            foreach (var instance in instances)
            {
                var hash = GetHash(instance);

                // Test different variants of copying.

                var copy = instance.DeepCopy(instance.DeepCopy());

                Assert.AreEqual(hash, GetHash(copy));
            }
        }

        private static int GetHash(IHashable hashable)
        {
            var hasher = new Hasher();
            hashable.Hash(hasher);
            return hasher.Value;
        }

        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected abstract IEnumerable<T> NewInstances();
    }
}
