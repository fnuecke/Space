using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Common.Components;

namespace Engine.Tests.ComponentSystem.Common.Components
{
    public sealed class ExpirationSerializationTest : AbstractComponentSerializationTest<Expiration>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<Expiration> NewInstances()
        {
            return new[]
                   {
                       new Expiration(), 
                       new Expiration().Initialize(10)
                   };
        }

        /// <summary>
        /// Returns a list of methods that change a value of an instance so
        /// that its new hash value should be different.
        /// </summary>
        protected override IEnumerable<ValueChanger> GetValueChangers()
        {
            return new ValueChanger[]
                   {
                       instance => instance.TimeToLive += 10
                   }.Concat(base.GetValueChangers());
        }
    }
}
