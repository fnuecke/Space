using System.Collections.Generic;
using System.Linq;
using Engine.Tests.ComponentSystem.Common.Components;
using Space.ComponentSystem.Components;

namespace SpaceTests.ComponentSystem.Components
{
    public abstract class AbstractSpaceItemSerializationTest<T> : AbstractItemSerializationTest<T>
        where T : SpaceItem, new()
    {
        /// <summary>
        /// Returns a list of methods that change a value of an instance so
        /// that its new hash value should be different.
        /// </summary>
        protected override IEnumerable<ValueChanger> GetValueChangers()
        {
            return new ValueChanger[]
                   {
                       instance => instance.Quality += 1
                   }.Concat(base.GetValueChangers());
        }
    }
}
