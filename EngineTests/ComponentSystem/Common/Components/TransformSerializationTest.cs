using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Spatial.Components;
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
            var manager = new Manager();
            return new[]
                   {
                       manager.AddComponent<Transform>(manager.AddEntity()), 
                       manager.AddComponent<Transform>(manager.AddEntity()).Initialize(new Vector2(1, 0)),
                       manager.AddComponent<Transform>(manager.AddEntity()).Initialize(2),
                       manager.AddComponent<Transform>(manager.AddEntity()).Initialize(new Vector2(100, 5), 51)
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
                       instance =>
                       {
                           instance.Translation += new Vector2(12, 34);
                           instance.Update();
                       },
                       instance =>
                       {
                           instance.Translation = new Vector2(-10, 34);
                           instance.Update();
                       },
                       instance => instance.Rotation += 5,
                       instance => instance.Rotation = -2
                   }.Concat(base.GetValueChangers());
        }
    }
}
