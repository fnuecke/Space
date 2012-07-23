using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Common.Components;
using Microsoft.Xna.Framework;

namespace Engine.Tests.ComponentSystem.Common.Components
{
    public sealed class AccelerationSerializationTest : AbstractComponentSerializationTest<Acceleration>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<Acceleration> NewInstances()
        {
            return new[]
                   {
                       new Acceleration(), 
                       new Acceleration().Initialize(new Vector2(1, 0))
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
                       instance => instance.Value += new Vector2(0, 1)
                   }.Concat(base.GetValueChangers());
        }
    }
}
