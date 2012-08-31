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
    using ProjectMercury.Emitters;
    using ProjectMercury.Design.UITypeEditors;

    public class LineEmitterTypeConverter : EmitterTypeConverter
    {
        /// <summary>
        /// Adds the descriptors.
        /// </summary>
        /// <param name="descriptors">The descriptors.</param>
        protected override void AddDescriptors(List<SmartMemberDescriptor> descriptors)
        {
            base.AddDescriptors(descriptors);

            var type = typeof(LineEmitter);

            descriptors.AddRange(new SmartMemberDescriptor[]
            {
                new SmartMemberDescriptor(type.GetProperty("Length"),
                    new CategoryAttribute("Line Emitter"),
                    new DescriptionAttribute("The length of the line in pixels.")),

                new SmartMemberDescriptor(type.GetProperty("Angle"),
                    new CategoryAttribute("Line Emitter"),
                    new EditorAttribute(typeof(AngleEditor), typeof(UITypeEditor)),
                    new DescriptionAttribute("The Angle of the line in radians.")),

                new SmartMemberDescriptor(type.GetField("Rectilinear"),
                    new CategoryAttribute("Line Emitter"),
                    new DescriptionAttribute("True if the Emitter should release Particles perpendicular to the Angle of the line.")),

                new SmartMemberDescriptor(type.GetField("EmitBothWays"),
                    new CategoryAttribute("Line Emitter"),
                    new DescriptionAttribute("True if the Emitter should release Particles both ways. Only work when Rectilinear is enabled.")),

            });
        }
    }
}