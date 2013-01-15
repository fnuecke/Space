using System.Collections.Generic;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;

namespace Engine.Tests.ComponentSystem.Common.Systems
{
    public sealed class EllipsePathSystemCopyTest : AbstractSystemCopyTest
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<AbstractSystem> NewInstances()
        {
            return new[]
                   {
                       new EllipsePathSystem()
                   };
        }
    }
}
