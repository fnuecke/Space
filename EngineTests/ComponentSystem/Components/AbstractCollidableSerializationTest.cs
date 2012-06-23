using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.Tests.ComponentSystem.Components
{
    public abstract class AbstractCollidableSerializationTest<T> : AbstractComponentSerializationTest<T>
        where T : Collidable, new()
    {
        /// <summary>
        /// Returns a list of methods that change a value of an instance so
        /// that its new hash value should be different.
        /// </summary>
        protected override IEnumerable<ValueChanger> GetValueChangers()
        {
            return new ValueChanger[]
                   {
                       instance => instance.CollisionGroups += 1,
                       instance => instance.PreviousPosition += new Vector2(1, 2)
                   }.Concat(base.GetValueChangers());
        }
    }
}
