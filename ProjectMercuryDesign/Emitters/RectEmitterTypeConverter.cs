/*  
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.Design.Emitters
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing.Design;
    using ProjectMercury.Emitters;
    using ProjectMercury.Design.UITypeEditors;

    public class RectEmitterTypeConverter : EmitterTypeConverter
    {
        /// <summary>
        /// Adds the descriptors.
        /// </summary>
        /// <param name="descriptors">The descriptors.</param>
        protected override void AddDescriptors(List<SmartMemberDescriptor> descriptors)
        {
            base.AddDescriptors(descriptors);

            var type = typeof(RectEmitter);

            descriptors.AddRange(new SmartMemberDescriptor[]
            {
                new SmartMemberDescriptor(type.GetProperty("Width"),
                    new CategoryAttribute("Rect Emitter"),
                    new DescriptionAttribute("The width of the rectangle in pixels.")),

                new SmartMemberDescriptor(type.GetProperty("Height"),
                    new CategoryAttribute("Rect Emitter"),
                    new DescriptionAttribute("The height of the rectangle in pixels.")),

                new SmartMemberDescriptor(type.GetField("Frame"),
                    new CategoryAttribute("Rect Emitter"),
                    new DescriptionAttribute("True if the Emitter should release Particles only from the edge of the rectangle.")),

                new SmartMemberDescriptor(type.GetProperty("Rotation"),
                    new EditorAttribute(typeof(AngleEditor), typeof(UITypeEditor)),
                    new CategoryAttribute("Rect Emitter"),
                    new DescriptionAttribute("The Rotation of the rectangle in radians.")),
            });
        }
    }
}