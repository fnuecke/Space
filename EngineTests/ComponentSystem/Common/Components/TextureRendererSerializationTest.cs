using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Common.Components;
using Microsoft.Xna.Framework;

namespace Engine.Tests.ComponentSystem.Common.Components
{
    public sealed class TextureRendererSerializationTest : AbstractComponentSerializationTest<TextureRenderer>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<TextureRenderer> NewInstances()
        {
            return new[]
                   {
                       new TextureRenderer(), 
                       new TextureRenderer().Initialize("textureName"),
                       new TextureRenderer().Initialize("tn2", 1),
                       new TextureRenderer().Initialize("tn3", Color.Gray, 2)
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
                       instance => instance.Scale += 10,
                       instance => instance.TextureName += "b",
                       instance => instance.Tint = Color.Pink
                   }.Concat(base.GetValueChangers());
        }
    }
}
