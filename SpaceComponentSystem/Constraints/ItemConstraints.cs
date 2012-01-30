using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Constraints;
using Engine.Util;
using Microsoft.Xna.Framework.Content;
using Space.Data;

namespace Space.ComponentSystem.Constraints
{
    /// <summary>
    /// Base class for item constraints.
    /// </summary>
    public abstract class ItemConstraints : IConstraint
    {
        #region Fields
        
        /// <summary>
        /// Unique name for this item type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The quality of the item, to give a rough idea of the value.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public ItemQuality Quality = ItemQuality.Common;

        /// <summary>
        /// A list of attribute modifiers that are guaranteed to be applied to
        /// the generated item, just with random values.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public AttributeModifierConstraint<AttributeType>[] GuaranteedAttributes = new AttributeModifierConstraint<AttributeType>[0];

        /// <summary>
        /// A list of attribute modifiers from which a certain number is
        /// randomly sampled, and from the chosen attribute modifiers will then
        /// be sampled the actual values to be applied to the generated item.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public AttributeModifierConstraint<AttributeType>[] AdditionalAttributes = new AttributeModifierConstraint<AttributeType>[0];

        /// <summary>
        /// The minimum number of additional attribute modifiers to apply to a
        /// generated item.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public int MinAdditionalAttributes;

        /// <summary>
        /// The maximum number of additional attribute modifiers to apply to a
        /// generated item.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public int MaxAdditionalAttributes;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples a new item.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled item.</returns>
        public abstract Entity Sample(IUniformRandom random);

        /// <summary>
        /// Samples the attributes to apply to the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        protected Entity SampleAttributes(Entity item, IUniformRandom random)
        {
            foreach (var attribute in GuaranteedAttributes)
            {
                item.AddComponent(new Attribute<AttributeType>(attribute.SampleAttributeModifier(random)));
            }
            var numAdditionalAttributes = (MinAdditionalAttributes == MaxAdditionalAttributes) ? MinAdditionalAttributes
                : random.NextInt32(MinAdditionalAttributes, MaxAdditionalAttributes);
            for (int i = 0; i < numAdditionalAttributes; i++)
            {
                item.AddComponent(new Attribute<AttributeType>(AdditionalAttributes[random.NextInt32(AdditionalAttributes.Length)].SampleAttributeModifier(random)));
            }
            return item;
        }

        #endregion
    }
}
