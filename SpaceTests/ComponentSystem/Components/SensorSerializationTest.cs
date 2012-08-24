using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.Data;

namespace SpaceTests.ComponentSystem.Components
{
    public sealed class SensorSerializationTest : AbstractSpaceItemSerializationTest<Sensor>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<Sensor> NewInstances()
        {
            return new[]
                   {
                       new Sensor(),
                       (Sensor)new Sensor().Initialize("asd", "zxc"),
                       (Sensor)new Sensor().Initialize("asd", "zxc", ItemQuality.Poor, ItemSlotSize.Large, Vector2.Zero, false)
                   };
        }
    }
}
