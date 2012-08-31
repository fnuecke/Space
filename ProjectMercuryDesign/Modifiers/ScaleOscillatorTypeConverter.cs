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
    using ProjectMercury.Modifiers;

    public class ScaleOscillatorTypeConverter : ExpandableObjectConverter
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
            var type = typeof(ScaleOscillator);

            return new PropertyDescriptorCollection(new PropertyDescriptor[]
            {
                new SmartMemberDescriptor(type.GetProperty("Frequency"),
                    new DescriptionAttribute("The oscillator frequency (the number of cycles per second).")),
                
                new SmartMemberDescriptor(type.GetProperty("MinimumScale"),
                    new DescriptionAttribute("The minimum scale (the scale of Particles at the negetive peak of the sine wave).")),
                
                new SmartMemberDescriptor(type.GetProperty("MaximumScale"),
                    new DescriptionAttribute("The maximum scale (the scale of Particles at the positive peak of the sine wave)."))
            });
        }
    }
}