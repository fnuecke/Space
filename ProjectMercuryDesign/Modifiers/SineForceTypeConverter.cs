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
    using ProjectMercury.Modifiers;
    using ProjectMercury.Design.UITypeEditors;

    public class SineForceModifierTypeConverter : ExpandableObjectConverter
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
            var type = typeof(SineForceModifier);

            return new PropertyDescriptorCollection(new PropertyDescriptor[]
            {
                new SmartMemberDescriptor(type.GetField("Frequency"),
                    new DisplayNameAttribute("Frequency"),
                    new DescriptionAttribute("The frequency of the sine wave.")),

                new SmartMemberDescriptor(type.GetField("Amplitude"),
                    new DisplayNameAttribute("Amplitude"),
                    new DescriptionAttribute("The amplitude of the sine wave.")),

                new SmartMemberDescriptor(type.GetProperty("Rotation"),
                    new EditorAttribute(typeof(AngleEditor), typeof(UITypeEditor)),
                    new DisplayNameAttribute("Rotation"),
                    new DescriptionAttribute("The Rotation of the sine force in radians.")),
            });

        }
    }
}