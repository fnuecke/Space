using System.Collections.Generic;
using Engine.Serialization;
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
        where T : class, IPacketizable, ICopyable<T>
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
                var copy = instance.NewInstance();

                InitCopy(copy);

                instance.CopyInto(copy);

                Assert.AreEqual(hash, GetHash(copy));
            }
        }

        private static uint GetHash(IPacketizable hashable)
        {
            var hasher = new Hasher();
            hasher.Write(hashable);
            return hasher.Value;
        }

        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected abstract IEnumerable<T> NewInstances();

        /// <summary>
        /// Initialize a shallow copy before it is used to copy into.
        /// </summary>
        /// <param name="copy">The shallow copy.</param>
        protected virtual void InitCopy(T copy)
        {
        }
    }
}
