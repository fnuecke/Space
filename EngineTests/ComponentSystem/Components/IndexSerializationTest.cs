using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Components;

namespace Engine.Tests.ComponentSystem.Components
{
    public class IndexSerializationTest : AbstractComponentSerializationTest<Index>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<Index> NewInstances()
        {
            return new[]
                   {
                       new Index(), 
                       new Index().Initialize(5)
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
                       instance => instance.IndexGroupsMask += 10
                   }.Concat(base.GetValueChangers());
        }
    }
}
