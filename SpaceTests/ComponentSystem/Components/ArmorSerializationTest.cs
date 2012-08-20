using System.Collections.Generic;
using Space.ComponentSystem.Components;
using Space.Data;

namespace SpaceTests.ComponentSystem.Components
{
    public sealed class ArmorSerializationTest : AbstractSpaceItemSerializationTest<Armor>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<Armor> NewInstances()
        {
            return new[]
                   {
                       new Armor(),
                       (Armor)new Armor().Initialize("asd", "zxc"),
                       (Armor)new Armor().Initialize("asd", "zxc", ItemQuality.Poor, ItemSlotSize.Huge)
                   };
        }
    }
}
