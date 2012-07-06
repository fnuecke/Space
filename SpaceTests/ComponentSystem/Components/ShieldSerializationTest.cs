using System.Collections.Generic;
using Space.ComponentSystem.Components;
using Space.Data;

namespace SpaceTests.ComponentSystem.Components
{
    public sealed class ShieldSerializationTest : AbstractSpaceItemSerializationTest<Shield>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<Shield> NewInstances()
        {
            return new[]
                   {
                       new Shield(),
                       (Shield)new Shield().Initialize("asd", "zxc"),
                       (Shield)new Shield().Initialize("asd", "zxc", ItemQuality.Poor)
                   };
        }
    }
}
