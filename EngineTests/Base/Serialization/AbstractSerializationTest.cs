using System.Collections.Generic;
using Engine.Serialization;
using NUnit.Framework;

namespace Engine.Tests.Base.Serialization
{
    /// <summary>
    /// Base class for serialization tests.
    /// </summary>
    /// <typeparam name="T">The class to test.</typeparam>
    [TestFixture]
    public abstract class AbstractSerializationTest<T>
        where T : IPacketizable, IHashable, new()
    {
        [Test]
        public void SerializationAndHashing()
        {
            // Get a list of instances to test.
            var instances = NewInstances();

            foreach (var instance in instances)
            {
                var hash = GetHash(instance);

                // Test different variants of deserialization.

                using (var packet = new Packet())
                {
                    packet.Write(123);
                    packet.Write(instance);
                    packet.Write(456);

                    packet.Reset();

                    Assert.AreEqual(123, packet.ReadInt32());
                    var readInstance = packet.ReadPacketizable<T>();
                    Assert.AreEqual(hash, GetHash(readInstance));
                    Assert.AreEqual(456, packet.ReadInt32());
                }

                using (var packet = new Packet())
                {
                    packet.Write(123);
                    packet.WriteWithTypeInfo(instance);
                    packet.Write(456);

                    packet.Reset();

                    Assert.AreEqual(123, packet.ReadInt32());
                    Assert.AreEqual(hash, GetHash(packet.ReadPacketizableWithTypeInfo<T>()));
                    Assert.AreEqual(456, packet.ReadInt32());
                }

                using (var packet = new Packet())
                {
                    packet.Write(123);
                    packet.Write(instance);
                    packet.Write(456);

                    packet.Reset();

                    Assert.AreEqual(123, packet.ReadInt32());
                    var instanceByRef = instance;
                    packet.ReadPacketizableInto(ref instanceByRef);
                    Assert.AreEqual(hash, GetHash(instance));
                    Assert.AreEqual(456, packet.ReadInt32());
                }

                // Randomize values, make sure the hash is different now.

                foreach (var change in GetValueChangers())
                {
                    change(instance);
                    var changedHash = GetHash(instance);
                    Assert.AreNotEqual(hash, changedHash);
                    hash = changedHash;
                }
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

        /// <summary>
        /// The method signature of methods changing an instance.
        /// </summary>
        /// <param name="instance">The instance to change.</param>
        protected delegate void ValueChanger(T instance);

        /// <summary>
        /// Returns a list of methods that change a value of an instance so
        /// that its new hash value should be different.
        /// </summary>
        protected abstract IEnumerable<ValueChanger> GetValueChangers();
    }
}
