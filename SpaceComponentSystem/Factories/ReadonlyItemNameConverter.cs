using System;
using System.ComponentModel;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Avoids changing item entries via text entry, to avoid invalid entries.
    /// </summary>
    public sealed class ReadonlyItemNameConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return false;
        }
    }
}