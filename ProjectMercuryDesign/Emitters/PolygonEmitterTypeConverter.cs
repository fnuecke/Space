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

    public class PolygonEmitterTypeConverter : EmitterTypeConverter
    {
        /// <summary>
        /// Adds the descriptors.
        /// </summary>
        /// <param name="descriptors">The descriptors.</param>
        protected override void AddDescriptors(List<SmartMemberDescriptor> descriptors)
        {
            base.AddDescriptors(descriptors);

            var type = typeof(PolygonEmitter);

            descriptors.AddRange(new SmartMemberDescriptor[]
            {
                new SmartMemberDescriptor(type.GetField("Close"),
                    new CategoryAttribute("Polygon"),
                    new DescriptionAttribute("True if the poly should be closed.")),

                new SmartMemberDescriptor(type.GetProperty("Points"),
                    new CategoryAttribute("Polygon"),
                    new EditorAttribute(typeof(PolygonPointCollectionEditor), typeof(UITypeEditor)),
                    new DescriptionAttribute("The points which describe the poly.")),

                new SmartMemberDescriptor(type.GetProperty("Rotation"),
                    new EditorAttribute(typeof(AngleEditor), typeof(UITypeEditor)),
                    new CategoryAttribute("Polygon"),
                    new DescriptionAttribute("The Rotation of the poly in radians.")),

                new SmartMemberDescriptor(type.GetProperty("Scale"),
                    new CategoryAttribute("Polygon"),
                    new DescriptionAttribute("The scale factor of the poly.")),

                new SmartMemberDescriptor(type.GetProperty("Origin"),
                    new CategoryAttribute("Polygon"),
                    new DescriptionAttribute("Describes the origin of the polygon."))
            });
        }
    }
}