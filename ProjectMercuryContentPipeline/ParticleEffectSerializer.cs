/*  
 Copyright © 2010 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.ContentPipeline
{
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
    using ProjectMercury.Controllers;
    using ProjectMercury.Emitters;

    [ContentTypeSerializer]
    public class ParticleEffectSerializer : ContentTypeSerializer<ParticleEffect>
    {
        protected override void Serialize(IntermediateWriter output, ParticleEffect value, ContentSerializerAttribute format)
        {
            foreach (Emitter emitter in value)
            {
                output.WriteObject(emitter, new ContentSerializerAttribute { ElementName = "Item" });
            }

            output.Xml.WriteElementString("Name", value.Name);
            output.Xml.WriteElementString("Author", value.Author);
            output.Xml.WriteElementString("Description", value.Description);

            foreach (Controller controller in value.Controllers)
            {
                // Do not serialize the editor support controller...
                if (controller.GetType().Name != "EditorSupportController")
                    output.WriteObject(controller, new ContentSerializerAttribute { ElementName = "Controller" });
            }
        }

        protected override ParticleEffect Deserialize(IntermediateReader input, ContentSerializerAttribute format, ParticleEffect existingInstance)
        {
            ParticleEffect value = existingInstance ?? new ParticleEffect();

            while (input.MoveToElement("Item"))
                value.Add(input.ReadObject<Emitter>(new ContentSerializerAttribute { ElementName = "Item" }));

            value.Name = input.Xml.ReadElementString("Name");
            value.Author = input.Xml.ReadElementString("Author");
            value.Description = input.Xml.ReadElementString("Description");

            while (input.MoveToElement("Controller"))
                value.Controllers.Add(input.ReadObject<Controller>(new ContentSerializerAttribute { ElementName = "Controller" }));

            return value;
        }
    }
}