namespace ProjectMercury.ContentPipeline.Emitters
{
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
    using ProjectMercury.Emitters;
    using System;

    //[ContentTypeSerializer]
    //public class LineEmitterSerializer : AbstractEmitterSerializer<LineEmitter>
    //{
    //    protected override void Serialize(IntermediateWriter output, LineEmitter value, ContentSerializerAttribute format)
    //    {
    //        base.Serialize(output, value, format);

    //        output.Xml.WriteElementString("Length", value.Length.ToString());
    //        output.Xml.WriteElementString("Angle", value.Angle.ToString());
    //        output.Xml.WriteElementString("Rectilinear", value.Rectilinear.ToString());
    //        output.Xml.WriteElementString("EmitBothWays", value.EmitBothWays.ToString());
    //    }

    //    protected override LineEmitter Deserialize(IntermediateReader input, ContentSerializerAttribute format, LineEmitter existingInstance)
    //    {
    //        LineEmitter value = existingInstance ?? new LineEmitter();

    //        base.Deserialize(input, format, value);

    //        value.Length = Single.Parse(input.Xml.ReadElementString("Length"));
    //        value.Angle = Single.Parse(input.Xml.ReadElementString("Angle"));
    //        value.Rectilinear = Boolean.Parse(input.Xml.ReadElementString("Rectilinear"));
    //        value.EmitBothWays = Boolean.Parse(input.Xml.ReadElementString("EmitBothWays"));

    //        return value;
    //    }
    //}
}