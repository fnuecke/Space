using System;
using Engine.ComponentSystem.Modules;
using Microsoft.Xna.Framework.Content;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading data in binary format written with the <c>FixedWriter</c>.
    /// </summary>
    public sealed class EntityAttributeReader<TModifier> : ContentTypeReader<Modifier<TModifier>>
        where TModifier : struct
    {
        protected override Modifier<TModifier> Read(ContentReader input, Modifier<TModifier> existingInstance)
        {
            if (existingInstance == null)
            {
                existingInstance = new Modifier<TModifier>();
            }
            existingInstance.Type = (TModifier)Enum.Parse(typeof(TModifier), input.ReadString());
            existingInstance.ComputationType = (ModifierComputationType)input.ReadByte();
            existingInstance.Value = (float)input.ReadSingle();
            return existingInstance;
        }
    }
}
