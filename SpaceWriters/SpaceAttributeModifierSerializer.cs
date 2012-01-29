using Engine.Serialization;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
using Space.Data;

namespace Space.Serialization
{
    /// <summary>
    /// This is for reading actual XML files in the content project.
    /// </summary>
    [ContentTypeSerializer]
    public sealed class SpaceAttributeModifierSerializer : AbstractAttributeModifierSerializer<AttributeType>
    {
    }
}
