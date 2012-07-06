using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Tests.Base.Serialization;

namespace Engine.Tests.ComponentSystem.Common.Components
{
    public abstract class AbstractComponentSerializationTest<T> : AbstractSerializationTest<T>
        where T : Component, new()
    {
        /// <summary>
        /// Returns a list of methods that change a value of an instance so
        /// that its new hash value should be different.
        /// </summary>
        protected override IEnumerable<ValueChanger> GetValueChangers()
        {
            return new ValueChanger[]
                   {
                       instance => instance.Enabled = !instance.Enabled
                   };
        }
    }
}
