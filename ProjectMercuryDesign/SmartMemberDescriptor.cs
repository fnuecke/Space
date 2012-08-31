/*  
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.Design
{
    using System;
    using System.ComponentModel;
    using System.Reflection;

    public class SmartMemberDescriptor : PropertyDescriptor
    {
        /// <summary>
        /// Gets or sets the member.
        /// </summary>
        /// <value>The member.</value>
        protected MemberInfo Member { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartMemberDescriptor"/> class.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="attributes">The attributes.</param>
        public SmartMemberDescriptor(MemberInfo member, params Attribute[] attributes)
            : base(member.Name, attributes)
        {
            if ((member.MemberType != MemberTypes.Field) && (member.MemberType != MemberTypes.Property))
                throw new ArgumentException("Member must be either a field or a property!");

            this.Member = member;
        }

        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        /// <value></value>
        /// <returns>A <see cref="T:System.Type"/> that represents the type of the property.</returns>
        public override Type PropertyType
        {
            get
            {
                if (this.Member is FieldInfo)
                    return ((this.Member as FieldInfo).FieldType);

                if (this.Member is PropertyInfo)
                    return ((this.Member as PropertyInfo).PropertyType);

                return null;
            }
        }

        /// <summary>
        /// Gets the current value of the property on a component.
        /// </summary>
        /// <param name="component">The component with the property for which to retrieve the value.</param>
        /// <returns>
        /// The value of a property for a given component.
        /// </returns>
        public override object GetValue(object component)
        {
            if (this.Member is FieldInfo)
                return ((this.Member as FieldInfo).GetValue(component));

            if (this.Member is PropertyInfo)
                return ((this.Member as PropertyInfo).GetValue(component, null));

            return null;
        }

        /// <summary>
        /// Sets the value of the component to a different value.
        /// </summary>
        /// <param name="component">The component with the property value that is to be set.</param>
        /// <param name="value">The new value.</param>
        public override void SetValue(object component, object value)
        {
            if (this.Member is FieldInfo)
                (this.Member as FieldInfo).SetValue(component, value);

            if (this.Member is PropertyInfo)
                (this.Member as PropertyInfo).SetValue(component, value, null);

            base.OnValueChanged(component, EventArgs.Empty);
        }

        /// <summary>
        /// Returns whether resetting an object changes its value.
        /// </summary>
        /// <param name="component">The component to test for reset capability.</param>
        /// <returns>
        /// true if resetting the component changes its value; otherwise, false.
        /// </returns>
        public override bool CanResetValue(object component)
        {
            return false;
        }

        /// <summary>
        /// Resets the value for this property of the component to the default value.
        /// </summary>
        /// <param name="component">The component with the property value that is to be reset to the default value.</param>
        public override void ResetValue(object component) { }

        /// <summary>
        /// Determines a value indicating whether the value of this property needs to be persisted.
        /// </summary>
        /// <param name="component">The component with the property to be examined for persistence.</param>
        /// <returns>
        /// true if the property should be persisted; otherwise, false.
        /// </returns>
        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }

        /// <summary>
        /// Gets the type of the component this property is bound to.
        /// </summary>
        /// <value></value>
        /// <returns>A <see cref="T:System.Type"/> that represents the type of component this property is bound to. When the <see cref="M:System.ComponentModel.PropertyDescriptor.GetValue(System.Object)"/> or <see cref="M:System.ComponentModel.PropertyDescriptor.SetValue(System.Object,System.Object)"/> methods are invoked, the object specified might be an instance of this type.</returns>
        public override Type ComponentType
        {
            get { return this.Member.DeclaringType; }
        }

        /// <summary>
        /// Gets a value indicating whether this property is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>true if the property is read-only; otherwise, false.</returns>
        public override bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Compares this to another object to see if they are equivalent.
        /// </summary>
        /// <param name="obj">The object to compare to this <see cref="T:System.ComponentModel.PropertyDescriptor"/>.</param>
        /// <returns>
        /// true if the values are equivalent; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            SmartMemberDescriptor descriptor = obj as SmartMemberDescriptor;
            return ((descriptor != null) && descriptor.Member.Equals(this.Member));
        }

        /// <summary>
        /// Returns the hash code for this object.
        /// </summary>
        /// <returns>The hash code for this object.</returns>
        public override int GetHashCode()
        {
            return this.Member.GetHashCode();
        }
    }
}