using System;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Constraints;
using Microsoft.Xna.Framework.Content;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading data in binary format written with the <c>EntityAttributeWriter</c>.
    /// </summary>
    public sealed class AttributeModifierConstraintReader<TAttribute> : ContentTypeReader<AttributeModifierConstraint<TAttribute>>
        where TAttribute : struct
    {
        protected override AttributeModifierConstraint<TAttribute> Read(ContentReader input, AttributeModifierConstraint<TAttribute> existingInstance)
        {
            existingInstance = existingInstance ?? new AttributeModifierConstraint<TAttribute>();

            existingInstance.Type = (TAttribute)Enum.Parse(typeof(TAttribute), input.ReadString());
            existingInstance.ComputationType = (AttributeComputationType)input.ReadByte();
            existingInstance.MinValue = input.ReadSingle();
            existingInstance.MaxValue = input.ReadSingle();
            existingInstance.Round = input.ReadBoolean();

            return existingInstance;
        }
    }
}
