using System.Collections.Generic;
using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Constraints;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Factories
{
    /// <summary>Constraints for generating weapons.</summary>
    public sealed class WeaponFactory : ItemFactory
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Properties

        /// <summary>The sound this weapon emits when firing.</summary>
        [Category("Media")]
        [Editor("Space.Tools.DataEditor.SoundAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Description("The cue name of the sound to play when the weapon fires its projectiles.")]
        public string Sound
        {
            get { return _sound; }
            set { _sound = value; }
        }

        /// <summary>
        ///     A list of local attribute modifiers that are guaranteed to be applied to the generated item, just with random
        ///     values.
        /// </summary>
        /// <remarks>These attributes only apply to the weapon itself.</remarks>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Stats")]
        [Description("Local attribute bonuses that a generated weapon of this type is guaranteed to provide when equipped.")]
        public AttributeModifierConstraint<AttributeType>[] GuaranteedLocalAttributes
        {
            get { return _guaranteedLocalAttributes; }
            set { _guaranteedLocalAttributes = value; }
        }

        /// <summary>
        ///     A list of attribute modifiers from which a certain number is randomly sampled, and from the chosen attribute
        ///     modifiers will then be sampled the actual values to be applied to the generated item.
        /// </summary>
        /// <remarks>These attributes only apply to the weapon itself.</remarks>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Stats")]
        [Description("Possible local attribute bonuses items of this type might have. Additional attributes are sampled from these pools. Note that attributes in this list only apply to the weapon itself, and that not all attribute types actually have an effect (only damage, procs, things like that).")]
        public string[] AdditionalLocalAttributes
        {
            get { return _additionalLocalAttributes; }
            set { _additionalLocalAttributes = value; }
        }

        /// <summary>The number of local attribute modifiers to apply to a generated weapon.</summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Stats")]
        [Description("The number of local attributes to sample for a generated weapon of this type.")]
        public IntInterval AdditionalLocalAttributeCount
        {
            get { return _additionalLocalAttributeCount; }
            set { _additionalLocalAttributeCount = value; }
        }

        /// <summary>Possible projectiles this weapon fires.</summary>
        [Category("Logic")]
        [Description("The list of projectiles to emit each time this weapon is fired.")]
        public ProjectileFactory[] Projectiles
        {
            get { return _projectiles; }
            set { _projectiles = value; }
        }

        #endregion

        #region Backing fields

        private string _sound;

        private AttributeModifierConstraint<AttributeType>[] _guaranteedLocalAttributes;

        private string[] _additionalLocalAttributes = new string[0];

        private IntInterval _additionalLocalAttributeCount = IntInterval.Zero;

        private ProjectileFactory[] _projectiles;

        #endregion

        #region Sampling

        /// <summary>Samples a new weapon based on these constraints.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled weapon.</returns>
        public override int Sample(IManager manager, IUniformRandom random)
        {
            var entity = base.Sample(manager, random);

            // Get baked list of attributes.
            Dictionary<AttributeType, float> attributes = null;

            // Iterate over all local attribute modifiers and accumulate the
            // additive base value, store the multiplicative values in an
            // extra list to apply them at the end.
            Dictionary<AttributeType, float> multipliers = null;

            if (_guaranteedLocalAttributes != null)
            {
                foreach (var attribute in _guaranteedLocalAttributes)
                {
                    AccumulateModifier(attribute.SampleAttributeModifier(random), ref attributes, ref multipliers);
                }
            }
            if (_additionalLocalAttributes != null && _additionalLocalAttributes.Length > 0)
            {
                foreach (
                    var attributeModifier in
                        SampleAttributes(SampleLocalAttributeCount(random), _additionalLocalAttributes, random))
                {
                    AccumulateModifier(attributeModifier, ref attributes, ref multipliers);
                }
            }

            // Done checking all local attributes, apply multipliers, if any.
            if (multipliers != null)
            {
                foreach (var multiplier in multipliers)
                {
                    if (attributes != null && attributes.ContainsKey(multiplier.Key))
                    {
                        attributes[multiplier.Key] *= multiplier.Value;
                    }
                    else
                    {
                        Logger.Warn(
                            "Invalid local attribute for weapon {0}: {1} does not have an additive base value.",
                            Name,
                            multiplier.Key);
                    }
                }
            }

            manager.AddComponent<Weapon>(entity)
                   .Initialize(_sound, attributes, _projectiles)
                   .Initialize(Name, Icon, Quality, RequiredSlotSize, ModelOffset, ModelBelowParent);

            return entity;
        }

        /// <summary>Utility method for baking attribute modifiers into final values.</summary>
        /// <param name="attributeModifier">The attribute modifier.</param>
        /// <param name="additives">The additive attribute values.</param>
        /// <param name="multiplicatives">The multiplicative attribute values.</param>
        private static void AccumulateModifier(
            AttributeModifier<AttributeType> attributeModifier,
            ref Dictionary<AttributeType, float> additives,
            ref Dictionary<AttributeType, float> multiplicatives)
        {
            switch (attributeModifier.ComputationType)
            {
                case AttributeComputationType.Additive:
                    if (additives == null)
                    {
                        additives = new Dictionary<AttributeType, float>();
                    }
                    if (additives.ContainsKey(attributeModifier.Type))
                    {
                        additives[attributeModifier.Type] += attributeModifier.Value;
                    }
                    else
                    {
                        additives[attributeModifier.Type] = attributeModifier.Value;
                    }
                    break;
                case AttributeComputationType.Multiplicative:
                    if (multiplicatives == null)
                    {
                        multiplicatives = new Dictionary<AttributeType, float>();
                    }
                    if (multiplicatives.ContainsKey(attributeModifier.Type))
                    {
                        multiplicatives[attributeModifier.Type] *= attributeModifier.Value;
                    }
                    else
                    {
                        multiplicatives[attributeModifier.Type] = attributeModifier.Value;
                    }
                    break;
            }
        }

        /// <summary>Samples the local attribute count.</summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns></returns>
        private int SampleLocalAttributeCount(IUniformRandom random)
        {
            return (_additionalLocalAttributeCount.Low == _additionalLocalAttributeCount.High || random == null)
                       ? _additionalLocalAttributeCount.Low
                       : random.NextInt32(_additionalLocalAttributeCount.Low, _additionalLocalAttributeCount.High);
        }

        #endregion
    }
}