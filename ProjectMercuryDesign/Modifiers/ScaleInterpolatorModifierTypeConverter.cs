/*  
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.Design.Modifiers
{
    using System;
    using System.ComponentModel;
    using System.Drawing.Design;
    using ProjectMercury.Design.UITypeEditors;
    using ProjectMercury.Modifiers;

    public class ScaleInterpolatorModifierTypeConverter : ExpandableObjectConverter
    {
        /// <summary>
        /// Gets a collection of properties for the type of object specified by the value parameter.
        /// </summary>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="value">An <see cref="T:System.Object"/> that specifies the type of object to get the properties for.</param>
        /// <param name="attributes">An array of type <see cref="T:System.Attribute"/> that will be used as a filter.</param>
        /// <returns>
        /// A <see cref="T:System.ComponentModel.PropertyDescriptorCollection"/> with the properties that are exposed for the component, or null if there are no properties.
        /// </returns>
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            var type = typeof(ScaleInterpolatorModifier);

            return new PropertyDescriptorCollection(new PropertyDescriptor[]
            {
                new SmartMemberDescriptor(type.GetProperty("InitialScale"),
                    new DisplayNameAttribute("Initial Scale"),
                    new DescriptionAttribute("The initial scale of Particles as they are released from the Emitter.")),

                new SmartMemberDescriptor(type.GetProperty("MiddleScale"),
                    new DisplayNameAttribute("Middle Scale"),
                    new DescriptionAttribute("The scale of Particles as they reach the defined middle position in their lifetime.")),

                new SmartMemberDescriptor(type.GetProperty("MiddlePosition"),
                    new DisplayNameAttribute("MiddlePosition"),
                    new EditorAttribute(typeof(PercentEditor), typeof(UITypeEditor)),
                    new DescriptionAttribute("The point in a Particles life at which it reaches middle scale.")),

                new SmartMemberDescriptor(type.GetProperty("FinalScale"),
                    new DisplayNameAttribute("Final Scale"),
                    new DescriptionAttribute("The final scale of Particles as they are retired."))
            });
        }
    }
}