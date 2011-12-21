using Engine.Math;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading actual XML files in the content project.
    /// </summary>
    [ContentTypeSerializer]
    class FixedSerializer : ContentTypeSerializer<Fixed>
    {
        protected override void Serialize(IntermediateWriter output, Fixed value, ContentSerializerAttribute format)
        {
            output.Xml.WriteValue(value.DoubleValue);
        }

        protected override Fixed Deserialize(IntermediateReader input, ContentSerializerAttribute format, Fixed existingInstance)
        {
            return Fixed.Create(input.Xml.ReadContentAsDouble());
        }
    }

    /// <summary>
    /// This is for writing data back in binary format.
    /// </summary>
    [ContentTypeWriter]
    class FixedWriter : ContentTypeWriter<Fixed>
    {
        protected override void Write(ContentWriter output, Fixed value)
        {
            output.Write(value.DoubleValue);
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(Fixed).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FixedReader).AssemblyQualifiedName;
        }
    }
}
