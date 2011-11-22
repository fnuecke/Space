using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
using Engine.Math;

namespace Engine.Serialization
{

    /// <summary>
    /// This is for reading actual XML files in the content project.
    /// </summary>
    [ContentTypeSerializer]
    class FIntSerializer : ContentTypeSerializer<Fixed>
    {
        protected override void Serialize(IntermediateWriter output, Fixed value, ContentSerializerAttribute format)
        {
            output.Xml.WriteValue(value.DoubleValue);
        }
        protected override Fixed Deserialize(IntermediateReader input, ContentSerializerAttribute format, Fixed existingInstance)
        {
            Fixed result = Fixed.Create(input.Xml.ReadContentAsDouble());
            return result;
        }
    }

    /// <summary>
    /// This is for writing data back in binary format.
    /// </summary>
    [ContentTypeWriter]
    class FIntWriter : ContentTypeWriter<Fixed>
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
            return "Engine.Serialization.FIntReader, Engine," +
                " Version=1.0.0.0, Culture=neutral";
        }
    }

}
