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
    public class EmitterSerializer : AbstractEmitterSerializer<Emitter>
    {
        protected override void Serialize(IntermediateWriter output, Emitter value, ContentSerializerAttribute format)
        {
            base.Serialize(output, value, format);
        }

        protected override Emitter Deserialize(IntermediateReader input, ContentSerializerAttribute format, Emitter existingInstance)
        {
            Emitter value = existingInstance ?? new Emitter();

            base.Deserialize(input, format, value);

            return value;
        }
    }
}