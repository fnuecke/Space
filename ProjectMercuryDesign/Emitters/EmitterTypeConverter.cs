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
    using ProjectMercury.Design.Modifiers;
    using ProjectMercury.Design.UITypeEditors;
    using ProjectMercury.Emitters;

    public class EmitterTypeConverter : ExpandableObjectConverter
    {
        /// <summary>
        /// Gets a value indicating whether this object supports properties using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that provides a format context.</param>
        /// <returns>
        /// true because <see cref="M:System.ComponentModel.TypeConverter.GetProperties(System.Object)"/> should be called to find the properties of this object. This method never returns false.
        /// </returns>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>
        /// Adds the descriptors.
        /// </summary>
        /// <param name="descriptors">The descriptors.</param>
        protected virtual void AddDescriptors(List<SmartMemberDescriptor> descriptors)
        {
            var type = typeof(Emitter);

            descriptors.AddRange(new SmartMemberDescriptor[]
            {
                new SmartMemberDescriptor(type.GetProperty("Name"),
                    new CategoryAttribute("Emitter"),
                    new DescriptionAttribute("The name of the Emitter.")),

                new SmartMemberDescriptor(type.GetField("Enabled"),
                    new CategoryAttribute("Emitter"),
                    new DisplayNameAttribute("Enabled"),
                    new DescriptionAttribute("Enables or disables triggering of the Emitter.")),

                new SmartMemberDescriptor(type.GetField("BlendMode"),
                    new CategoryAttribute("Emitter"),
                    new DisplayNameAttribute("Blend Mode"),
                    new DescriptionAttribute("The blending mode used when rendering the Emitter.")),

                new SmartMemberDescriptor(type.GetField("ParticleTextureAssetName"),
                    new CategoryAttribute("Emitter"),
                    new DisplayNameAttribute("Texture Asset Name"),
                    new DescriptionAttribute("The name of the texture asset in your content project")),

                new SmartMemberDescriptor(type.GetField("TriggerOffset"),
                    new CategoryAttribute("Emitter"),
                    new DisplayNameAttribute("Trigger Offset"),
                    new DescriptionAttribute("The Emitters trigger offset in relation to the ParticleEffect.")),

                new SmartMemberDescriptor(type.GetField("MinimumTriggerPeriod"),
                    new CategoryAttribute("Emitter"),
                    new DisplayNameAttribute("Minimum Trigger Period"),
                    new DescriptionAttribute("The minimum amount of time between triggers for the Emitter (in whole and fractional seconds).")),

                new SmartMemberDescriptor(type.GetField("ReleaseColour"),
                    new CategoryAttribute("Emitter"),
                    new EditorAttribute(typeof(VariableColourEditor), typeof(UITypeEditor)),
                    new DisplayNameAttribute("Release Colour"),
                    new DescriptionAttribute("The initial colour of Particles as they are released by the Emitter.")),

                new SmartMemberDescriptor(type.GetField("ReleaseImpulse"),
                    new CategoryAttribute("Emitter"),
                    new DisplayNameAttribute("Release Impulse"),
                    new DescriptionAttribute("The initial impulse applied to Particles as they are relased.")),

                new SmartMemberDescriptor(type.GetField("ReleaseOpacity"),
                    new CategoryAttribute("Emitter"),
                    new DisplayNameAttribute("Release Opacity"),
                    new DescriptionAttribute("The initial opacity of Particles as they are released by the Emitter.")),

                new SmartMemberDescriptor(type.GetProperty("ReleaseQuantity"),
                    new CategoryAttribute("Emitter"),
                    new DisplayNameAttribute("Release Quantity"),
                    new DescriptionAttribute("The number of Particles which are released by the Emitter on each trigger.")),

                new SmartMemberDescriptor(type.GetField("ReleaseRotation"),
                    new CategoryAttribute("Emitter"),
                    //new EditorAttribute(typeof(AngleEditor), typeof(UITypeEditor)),
                    new DisplayNameAttribute("Release Rotation"),
                    new DescriptionAttribute("The initial Rotation (in radians) of Particles as they are released by the Emitter.")),

                new SmartMemberDescriptor(type.GetField("ReleaseSpeed"),
                    new CategoryAttribute("Emitter"),
                    new DisplayNameAttribute("Release Speed"),
                    new DescriptionAttribute("The initial velocity of Particles as they are released by the Emitter.")),
                
                new SmartMemberDescriptor(type.GetField("ReleaseScale"),
                    new CategoryAttribute("Emitter"),
                    new DisplayNameAttribute("Release Scale"),
                    new DescriptionAttribute("The initial scale of Particles as they are released by the Emitter.")),
            });
        }

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
            List<SmartMemberDescriptor> descriptors = new List<SmartMemberDescriptor>();

            this.AddDescriptors(descriptors);

            return new PropertyDescriptorCollection((PropertyDescriptor[])descriptors.ToArray());
        }
    }
}