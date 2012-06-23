using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.RPG.Components;

namespace Engine.Tests.ComponentSystem.Components
{
    public abstract class AbstractItemSerializationTest<T> : AbstractComponentSerializationTest<T>
        where T : Item, new()
    {
        /// <summary>
        /// Returns a list of methods that change a value of an instance so
        /// that its new hash value should be different.
        /// </summary>
        protected override IEnumerable<ValueChanger> GetValueChangers()
        {
            return new ValueChanger[]
                   {
                       instance => instance.Enabled = !instance.Enabled,
                       instance => instance.Name += "b",
                       instance => instance.IconName += "b",
                   }.Concat(base.GetValueChangers());
        }
    }
}
