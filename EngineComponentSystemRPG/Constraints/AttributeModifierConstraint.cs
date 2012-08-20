using System;
using System.ComponentModel;
using Engine.ComponentSystem.RPG.Components;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Engine.ComponentSystem.RPG.Constraints
{
    /// <summary>
    /// Constraints for generating attribute modifiers.
    /// </summary>
    /// <typeparam name="TAttribute">The enum with attribute types.</typeparam>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public sealed class AttributeModifierConstraint<TAttribute>
        where TAttribute : struct
    {
        #region Properties
        
        /// <summary>
        /// The actual type of this attribute, which tells the game how to
        /// handle it.
        /// </summary>
        public TAttribute Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// The value range of the attribute.
        /// </summary>
        public Interval<float> Value
        {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// The computation type of this attribute, i.e. how it should be used
        /// in computation.
        /// </summary>
        public AttributeComputationType ComputationType
        {
            get { return _computationType; }
            set { _computationType = value; }
        }

        /// <summary>
        /// Whether the sampled result should be rounded to the next integer.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(false)]
        public bool Round
        {
            get { return _round; }
            set { _round = value; }
        }

        #endregion

        #region Backing fields

        private TAttribute _type;

        private Interval<float> _value;

        private AttributeComputationType _computationType;

        private bool _round;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples an attribute modifier from this constraint.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled attribute modifier.</returns>
        public AttributeModifier<TAttribute> SampleAttributeModifier(IUniformRandom random)
        {
            // Only randomize if necessary.
            var value = (random == null) ? _value.Low
                : MathHelper.Lerp(_value.Low, _value.High, (float)random.NextDouble());
            if (_round)
            {
                value = (float)System.Math.Round(value);
            }
            return new AttributeModifier<TAttribute>(_type, value, _computationType);
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            switch (_computationType)
            {
                case AttributeComputationType.Additive:
                    if (_value.Low == _value.High)
                    {
                        return _value.Low + " " + _type;
                    }
                    else
                    {
                        return _value.Low + " to " + _value.High + " " + _type;
                    }
                case AttributeComputationType.Multiplicative:
                    if (_value.Low == _value.High)
                    {
                        return _value.Low * 100 + "% " + _type;
                    }
                    else
                    {
                        return _value.Low * 100 + " to " + _value.High * 100 + "% " + _type;
                    }
            }
            throw new InvalidOperationException("Unhandled attribute computation type.");
        }

        #endregion
    }
}
