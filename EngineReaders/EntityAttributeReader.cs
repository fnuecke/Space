using System;
using Engine.Data;
using Engine.Math;
using Microsoft.Xna.Framework.Content;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading data in binary format written with the <c>FixedWriter</c>.
    /// </summary>
    public sealed class EntityAttributeReader<TAttribute> : ContentTypeReader<ModuleAttribute<TAttribute>>
        where TAttribute : struct
    {
        protected override ModuleAttribute<TAttribute> Read(ContentReader input, ModuleAttribute<TAttribute> existingInstance)
        {
            if (existingInstance == null)
            {
                existingInstance = new ModuleAttribute<TAttribute>();
            }
            existingInstance.Type = (TAttribute)Enum.Parse(typeof(TAttribute), input.ReadString());
            existingInstance.ComputationType = (ModuleAttributeComputationType)input.ReadByte();
            existingInstance.Value = Fixed.Create(input.ReadDouble());
            return existingInstance;
        }
    }
}
