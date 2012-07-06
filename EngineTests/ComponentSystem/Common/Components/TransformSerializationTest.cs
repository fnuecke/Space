using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.Tests.ComponentSystem.Common.Components
{
    public sealed class TransformSerializationTest : AbstractComponentSerializationTest<Transform>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<Transform> NewInstances()
        {
            return new[]
                   {
                       new Transform(), 
                       new Transform().Initialize(new Vector2(1, 0)),
                       new Transform().Initialize(2),
                       new Transform().Initialize(new Vector2(100, 5), 51)
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
                       instance => instance.AddTranslation(new Vector2(12, 34)),
                       instance => instance.SetTranslation(new Vector2(-10, 34)),
                       instance => instance.AddRotation(5),
                       instance => instance.SetRotation(-2)
                   }.Concat(base.GetValueChangers());
        }
    }
}
