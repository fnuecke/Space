using System;
using Engine.Data;
using Engine.Math;
using Microsoft.Xna.Framework.Content;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading data in binary format written with the <c>FixedWriter</c>.
    /// </summary>
    public sealed class EntityAttributeReader<TAttribute> : ContentTypeReader<EntityAttribute<TAttribute>>
        where TAttribute : struct
    {
        protected override EntityAttribute<TAttribute> Read(ContentReader input, EntityAttribute<TAttribute> existingInstance)
        {
            if (existingInstance == null)
            {
                existingInstance = new EntityAttribute<TAttribute>();
            }
            existingInstance.Type = (TAttribute)Enum.Parse(typeof(TAttribute), input.ReadString());
            existingInstance.ComputationType = (EntityAttributeComputationType)input.ReadByte();
            existingInstance.Value = Fixed.Create(input.ReadDouble());
            return existingInstance;
        }
    }
}
