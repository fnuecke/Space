using System.Collections.Generic;
using Engine.Tests.Base.Serialization;
using Space.ComponentSystem.Components;

namespace SpaceTests.ComponentSystem.Components
{
    class ArtificialIntelligenceSerializationTest:AbstractSerializationTest<ArtificialIntelligence>

    {

        protected override IEnumerable<ArtificialIntelligence> NewInstances()
        {
            return new[]
                       {
                           new ArtificialIntelligence()
                       };
        }


        protected override IEnumerable<AbstractSerializationTest<ArtificialIntelligence>.ValueChanger> GetValueChangers()
        {
            return new ValueChanger[]{};
        }
    }
}
