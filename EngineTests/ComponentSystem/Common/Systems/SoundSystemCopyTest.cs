using System.Collections.Generic;
using Engine.ComponentSystem.Systems;
using Engine.Tests.Base.Util;

namespace Engine.Tests.ComponentSystem.Common.Systems
{
    public sealed class SoundSystemCopyTest : AbstractCopyableTest<AbstractSystem>
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
                       new SoundSystem(null)
                   };
        }
    }
}
