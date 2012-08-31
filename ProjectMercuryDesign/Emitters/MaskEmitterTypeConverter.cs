/*  
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.Design.Emitters
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing.Design;
    using ProjectMercury.Design.UITypeEditors;
    using ProjectMercury.Emitters;

    public class MaskEmitterTypeConverter : EmitterTypeConverter
    {
        /// <summary>
        /// Adds the descriptors.
        /// </summary>
        /// <param name="descriptors">The descriptors.</param>
        protected override void AddDescriptors(List<SmartMemberDescriptor> descriptors)
        {
            base.AddDescriptors(descriptors);

            var type = typeof(MaskEmitter);

            descriptors.AddRange(new SmartMemberDescriptor[]
            {
                new SmartMemberDescriptor(type.GetProperty("Mask"),
                    new EditorAttribute(typeof(MaskEditor), typeof(UITypeEditor)),
                    new CategoryAttribute("Mask Emitter"),
                    new DescriptionAttribute("An array of bytes representing the mask.")),

                new SmartMemberDescriptor(type.GetProperty("Threshold"),
                    new CategoryAttribute("Mask Emitter"),
                    new DescriptionAttribute("The threshold above which particles will be released from a point within the mask.")),

                new SmartMemberDescriptor(type.GetProperty("MaskTextureContentPath"),
                    new CategoryAttribute("Mask Emitter"),
                    new DescriptionAttribute("The path to a texture in your content project representing the mask.")),

                new SmartMemberDescriptor(type.GetProperty("Width"),
                    new CategoryAttribute("Mask Emitter"),
                    new DescriptionAttribute("The width of the Mask Emitter.")),

                new SmartMemberDescriptor(type.GetProperty("Height"),
                    new CategoryAttribute("Mask Emitter"),
                    new DescriptionAttribute("The height of the Mask Emitter.")),
            });
        }
    }
}