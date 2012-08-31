namespace ProjectMercury.ContentPipeline.Emitters
{
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
    using ProjectMercury.Emitters;
    using System;

    //[ContentTypeSerializer]
    //public class ConeEmitterSerializer : AbstractEmitterSerializer<ConeEmitter>
    //{
    //    protected override void Serialize(IntermediateWriter output, ConeEmitter value, ContentSerializerAttribute format)
    //    {
    //        base.Serialize(output, value, format);

    //        output.Xml.WriteElementString("Direction", value.Direction.ToString());
    //        output.Xml.WriteElementString("ConeAngle", value.ConeAngle.ToString());
    //    }

    //    protected override ConeEmitter Deserialize(IntermediateReader input, ContentSerializerAttribute format, ConeEmitter existingInstance)
    //    {
    //        ConeEmitter value = existingInstance ?? new ConeEmitter();

    //        base.Deserialize(input, format, value);

    //        value.Direction = Single.Parse(input.Xml.ReadElementString("Direction"));
    //        value.ConeAngle = Single.Parse(input.Xml.ReadElementString("ConeAngle"));

    //        return value;
    //    }
    //}
}