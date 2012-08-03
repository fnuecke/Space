using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;

namespace Engine.Tests.ComponentSystem.Common.Components
{
    public sealed class AvatarSerializationTest : AbstractComponentSerializationTest<Avatar>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<Avatar> NewInstances()
        {
            var manager = new Manager();
            return new[]
                   {
                       manager.AddComponent<Avatar>(manager.AddEntity()), 
                       manager.AddComponent<Avatar>(manager.AddEntity()).Initialize(1)
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
                       instance => instance.PlayerNumber += 2
                   }.Concat(base.GetValueChangers());
        }
    }
}
