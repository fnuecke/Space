using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Spatial.Components;

namespace Engine.Tests.ComponentSystem.Common.Components
{
    public sealed class EllipsePathSerializationTest : AbstractComponentSerializationTest<EllipsePath>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<EllipsePath> NewInstances()
        {
            var manager = new Manager();
            return new[]
                   {
                       manager.AddComponent<EllipsePath>(manager.AddEntity()), 
                       manager.AddComponent<EllipsePath>(manager.AddEntity()).Initialize(1, 10, 20, 5, 6, 7)
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
                       instance => instance.CenterEntityId += 1,
                       instance => instance.Angle += 10,
                       instance => instance.MajorRadius += 10,
                       instance => instance.MinorRadius += 10,
                       instance => instance.Period += 10,
                       instance => instance.PeriodOffset += 10
                   }.Concat(base.GetValueChangers());
        }
    }
}
