/*  
 Copyright © 2010 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.ContentPipeline.Emitters
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
    using ProjectMercury.Emitters;
    using ProjectMercury.Modifiers;

    public abstract class AbstractEmitterSerializer<T> : ContentTypeSerializer<T> where T : Emitter
    {
        protected override void Serialize(IntermediateWriter output, T value, ContentSerializerAttribute format)
        {
            output.Xml.WriteElementString("Name", value.Name);
            output.Xml.WriteElementString("Budget", value.Budget.ToString());
            output.Xml.WriteElementString("Term", value.Term.ToString());
            output.Xml.WriteElementString("ReleaseQuantity", value.ReleaseQuantity.ToString());
            output.Xml.WriteElementString("Enabled", value.Enabled.ToString());
            output.WriteObject(value.ReleaseSpeed, new ContentSerializerAttribute { ElementName = "ReleaseSpeed" });
            output.WriteObject(value.ReleaseColour, new ContentSerializerAttribute { ElementName = "ReleaseColour" });
            output.WriteObject(value.ReleaseOpacity, new ContentSerializerAttribute { ElementName = "ReleaseOpacity" });
            output.WriteObject(value.ReleaseScale, new ContentSerializerAttribute { ElementName = "ReleaseScale" });
            output.WriteObject(value.ReleaseRotation, new ContentSerializerAttribute { ElementName = "ReleaseRotation" });
            output.WriteObject(value.ReleaseImpulse, new ContentSerializerAttribute { ElementName = "ReleaseImpulse" });
            output.Xml.WriteElementString("ParticleTextureAssetName", value.ParticleTextureAssetName);
            output.WriteObject(value.Modifiers, new ContentSerializerAttribute { ElementName = "Modifiers" });
            output.WriteObject(value.BlendMode, new ContentSerializerAttribute { ElementName = "BlendMode" });
            output.WriteObject(value.TriggerOffset, new ContentSerializerAttribute { ElementName = "TriggerOffset" });
            output.Xml.WriteElementString("MinimumTriggerPeriod", value.MinimumTriggerPeriod.ToString());
        }

        protected override T Deserialize(IntermediateReader input, ContentSerializerAttribute format, T existingInstance)
        {
            T value = existingInstance ?? default(T);

            value.Name = input.Xml.ReadElementString("Name");
            value.Budget = Int32.Parse(input.Xml.ReadElementString("Budget"));
            value.Term = Single.Parse(input.Xml.ReadElementString("Term"));
            value.ReleaseQuantity = Int32.Parse(input.Xml.ReadElementString("ReleaseQuantity"));
            value.Enabled = Boolean.Parse(input.Xml.ReadElementString("Enabled"));
            value.ReleaseSpeed = input.ReadObject<VariableFloat>(new ContentSerializerAttribute { ElementName = "ReleaseSpeed" });
            value.ReleaseColour = input.ReadObject<VariableFloat3>(new ContentSerializerAttribute { ElementName = "ReleaseColour" });
            value.ReleaseOpacity = input.ReadObject<VariableFloat>(new ContentSerializerAttribute { ElementName = "ReleaseOpacity" });
            value.ReleaseScale = input.ReadObject<VariableFloat>(new ContentSerializerAttribute { ElementName = "ReleaseScale" });
            value.ReleaseRotation = input.ReadObject<VariableFloat>(new ContentSerializerAttribute { ElementName = "ReleaseRotation" });
            value.ReleaseImpulse = input.ReadObject<Vector2>(new ContentSerializerAttribute { ElementName = "ReleaseImpulse" });
            value.ParticleTextureAssetName = input.Xml.ReadElementString("ParticleTextureAssetName");
            value.Modifiers = input.ReadObject<ModifierCollection>(new ContentSerializerAttribute { ElementName = "Modifiers" });
            value.BlendMode = input.ReadObject<EmitterBlendMode>(new ContentSerializerAttribute { ElementName = "BlendMode" });
            value.TriggerOffset = input.ReadObject<Vector2>(new ContentSerializerAttribute { ElementName = "TriggerOffset" });
            value.MinimumTriggerPeriod = Single.Parse(input.Xml.ReadElementString("MinimumTriggerPeriod"));

            return value;
        }
    }
}