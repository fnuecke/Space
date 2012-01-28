using System;
using Engine.ComponentSystem.RPG.Components;
using Microsoft.Xna.Framework.Content;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading data in binary format written with the <c>EntityAttributeWriter</c>.
    /// </summary>
    public sealed class AttributeModifierReader<TAttribute> : ContentTypeReader<AttributeModifier<TAttribute>>
        where TAttribute : struct
    {
        protected override AttributeModifier<TAttribute> Read(ContentReader input, AttributeModifier<TAttribute> existingInstance)
        {
            existingInstance = existingInstance ?? new AttributeModifier<TAttribute>();

            existingInstance.Type = (TAttribute)Enum.Parse(typeof(TAttribute), input.ReadString());
            existingInstance.ComputationType = (AttributeComputationType)input.ReadByte();
            existingInstance.Value = input.ReadSingle();

            return existingInstance;
        }
    }
}
