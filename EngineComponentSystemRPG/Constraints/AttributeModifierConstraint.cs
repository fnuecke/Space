using Engine.ComponentSystem.RPG.Components;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.RPG.Constraints
{
    /// <summary>
    /// Constraints for generating attribute modifiers.
    /// </summary>
    /// <typeparam name="TAttribute">The enum with attribute types.</typeparam>
    public sealed class AttributeModifierConstraint<TAttribute>
        where TAttribute : struct
    {
        #region Fields
        
        /// <summary>
        /// The actual type of this attribute, which tells the game how to
        /// handle it.
        /// </summary>
        public TAttribute Type;

        /// <summary>
        /// The value range of the attribute.
        /// </summary>
        public Interval<float> Value;

        /// <summary>
        /// The computation type of this attribute, i.e. how it should be used
        /// in computation.
        /// </summary>
        public AttributeComputationType ComputationType;

        /// <summary>
        /// Whether the sampled result should be rounded to the next integer.
        /// </summary>
        public bool Round;

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
            var value = (random == null) ? Value.Low
                : MathHelper.Lerp(Value.Low, Value.High, (float)random.NextDouble());
            if (Round)
            {
                value = (float)System.Math.Round(value);
            }
            return new AttributeModifier<TAttribute>(Type, value, ComputationType);
        }

        #endregion
    }
}
