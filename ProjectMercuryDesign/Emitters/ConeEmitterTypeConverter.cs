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

    public class ConeEmitterTypeConverter : EmitterTypeConverter
    {
        /// <summary>
        /// Adds the descriptors.
        /// </summary>
        /// <param name="descriptors">The descriptors.</param>
        protected override void AddDescriptors(List<SmartMemberDescriptor> descriptors)
        {
            base.AddDescriptors(descriptors);

            var type = typeof(ConeEmitter);

            descriptors.AddRange(new SmartMemberDescriptor[]
            {
                new SmartMemberDescriptor(type.GetProperty("Direction"),
                    new EditorAttribute(typeof(AngleEditor), typeof(UITypeEditor)),
                    new DescriptionAttribute("The Angle (in radians) that the SpotEmitters beam is facing.")),

                new SmartMemberDescriptor(type.GetProperty("ConeAngle"),
                    new EditorAttribute(typeof(AngleEditor), typeof(UITypeEditor)),
                    new DescriptionAttribute("The Angle (in radians) from edge to edge of the SpotEmitters beam."))
            });
        }
    }
}