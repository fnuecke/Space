/*  
 Copyright © 2010 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.ContentPipeline.Emitters
{
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
    using ProjectMercury.Emitters;
    using System;

    [ContentTypeSerializer]
    public class MaskEmitterSerializer : AbstractEmitterSerializer<MaskEmitter>
    {
        protected override void Serialize(IntermediateWriter output, MaskEmitter value, ContentSerializerAttribute format)
        {
            base.Serialize(output, value, format);

            output.Xml.WriteElementString("MaskTextureContentPath", value.MaskTextureContentPath);

            if (String.IsNullOrEmpty(value.MaskTextureContentPath))
                output.WriteObject<byte[][]>(value.Mask, new ContentSerializerAttribute { ElementName = "Mask" });

            output.Xml.WriteElementString("Threshold", value.Threshold.ToString());
            output.Xml.WriteElementString("Width", value.Width.ToString());
            output.Xml.WriteElementString("Height", value.Height.ToString());
        }

        protected override MaskEmitter Deserialize(IntermediateReader input, ContentSerializerAttribute format, MaskEmitter existingInstance)
        {
            MaskEmitter value = existingInstance ?? new MaskEmitter();

            base.Deserialize(input, format, value);

            value.MaskTextureContentPath = input.Xml.ReadElementString("MaskTextureContentPath");

            if (String.IsNullOrEmpty(value.MaskTextureContentPath))
                value.Mask = input.ReadObject<byte[][]>(new ContentSerializerAttribute { ElementName = "Mask" });

            value.Threshold = Single.Parse(input.Xml.ReadElementString("Threshold"));
            value.Width = Single.Parse(input.Xml.ReadElementString("Width"));
            value.Height = Single.Parse(input.Xml.ReadElementString("Height"));

            return value;
        }
    }
}