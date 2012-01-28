using System;
using Engine.ComponentSystem.RPG.Components;
using Microsoft.Xna.Framework.Content;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading data in binary format written with the <c>EntityAttributeWriter</c>.
    /// </summary>
    public sealed class EntityAttributeReader<TModifier> : ContentTypeReader<AttributeModifier<TModifier>>
        where TModifier : struct
    {
        protected override AttributeModifier<TModifier> Read(ContentReader input, AttributeModifier<TModifier> existingInstance)
        {
            if (existingInstance == null)
            {
                existingInstance = new AttributeModifier<TModifier>();
            }
            existingInstance.Type = (TModifier)Enum.Parse(typeof(TModifier), input.ReadString());
            existingInstance.ComputationType = (AttributeComputationType)input.ReadByte();
            existingInstance.Value = (float)input.ReadSingle();
            return existingInstance;
        }
    }
}
