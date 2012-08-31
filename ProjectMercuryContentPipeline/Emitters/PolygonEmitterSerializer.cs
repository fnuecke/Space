namespace ProjectMercury.ContentPipeline.Emitters
{
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
    using ProjectMercury.Emitters;
    using System;

    //[ContentTypeSerializer]
    //public class PolygonEmitterSerializer : AbstractEmitterSerializer<PolygonEmitter>
    //{
    //    protected override void Serialize(IntermediateWriter output, PolygonEmitter value, ContentSerializerAttribute format)
    //    {
    //        base.Serialize(output, value, format);

    //        output.Xml.WriteElementString("Close", value.Close.ToString());
    //        output.WriteObject(value.Points, new ContentSerializerAttribute { ElementName = "Points" });
    //        output.Xml.WriteElementString("Origin", value.Origin.ToString());
    //        output.Xml.WriteElementString("Rotation", value.Rotation.ToString());
    //        output.Xml.WriteElementString("Scale", value.Scale.ToString());
    //    }

    //    protected override PolygonEmitter Deserialize(IntermediateReader input, ContentSerializerAttribute format, PolygonEmitter existingInstance)
    //    {
    //        PolygonEmitter value = existingInstance ?? new PolygonEmitter();

    //        base.Deserialize(input, format, value);

    //        value.Close = Boolean.Parse(input.Xml.ReadElementString("Close"));
    //        value.Points = input.ReadObject<PolygonPointCollection>(new ContentSerializerAttribute { ElementName = "Points" });
    //        value.Origin = (PolygonOrigin)Enum.Parse(typeof(PolygonOrigin), input.Xml.ReadElementString("Origin"));
    //        value.Rotation = Single.Parse(input.Xml.ReadElementString("Rotation"));
    //        value.Scale = Single.Parse(input.Xml.ReadElementString("Scale"));

    //        return value;
    //    }
    //}
}