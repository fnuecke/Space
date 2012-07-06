using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Components;

namespace Engine.Tests.ComponentSystem.Common.Components
{
    public sealed class SpinSerializationTest : AbstractComponentSerializationTest<Spin>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<Spin> NewInstances()
        {
            return new[]
                   {
                       new Spin(), 
                       new Spin().Initialize(5)
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
                       instance => instance.Value += 5
                   }.Concat(base.GetValueChangers());
        }
    }
}
