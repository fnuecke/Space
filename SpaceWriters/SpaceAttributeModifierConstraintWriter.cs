using Engine.Serialization;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
using Space.ComponentSystem;

namespace Space.Serialization
{
    /// <summary>
    /// This is for reading actual XML files in the content project.
    /// </summary>
    [ContentTypeSerializer]
    public sealed class SpaceAttributeModifierConstraintSerializer : AbstractAttributeModifierConstraintSerializer<AttributeType>
    {
    }

    /// <summary>
    /// This is for writing data back in binary format.
    /// </summary>
    [ContentTypeWriter]
    public sealed class SpaceAttributeModifierConstraintWriter : AbstractAttributeModifierConstraintWriter<AttributeType>
    {
    }
}
