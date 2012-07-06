using System.Collections.Generic;
using Engine.Tests.Base.Serialization;
using Space.ComponentSystem.Components.Logic;

namespace SpaceTests.ComponentSystem.Components
{
    public sealed class CellDeathSerializationTest : AbstractSerializationTest<CellDeath>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<CellDeath> NewInstances()
        {
            return new[]
                   {
                       new CellDeath()
                   };
        }

        /// <summary>
        /// Returns a list of methods that change a value of an instance so
        /// that its new hash value should be different.
        /// </summary>
        protected override IEnumerable<ValueChanger> GetValueChangers()
        {
            return new ValueChanger[] {};
        }
    }
}
