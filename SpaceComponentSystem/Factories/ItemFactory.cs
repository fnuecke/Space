using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Constraints;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Base class for item constraints.
    /// </summary>
    public abstract class ItemFactory : IFactory
    {
        #region Fields
        
        /// <summary>
        /// Unique name for this item type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Asset name of the texture to use for this item type to render it in
        /// menus and the inventory.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string Icon = "Images/Icons/Buffs/default";

        /// <summary>
        /// The ingame texture to be displayed for items floating around in
        /// space.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string Model = "Textures/Items/default";

        /// <summary>
        /// The scaling to apply to the model texture.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float ModelScale = 1.0f;

        /// <summary>
        /// The quality of the item, to give a rough idea of the value.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public ItemQuality Quality = ItemQuality.Common;

        /// <summary>
        /// The slot size of the item.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public ItemSlotSize SlotSize = ItemSlotSize.Small;

        /// <summary>
        /// Slots this item provides for other items to be equipped into.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public ItemSlotInfo[] Slots = new ItemSlotInfo[0];

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
        /// The number of additional attribute modifiers to apply to a
        /// generated item.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Interval<int> AdditionalAttributeCount = Interval<int>.Zero;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples a new item.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled item.
        /// </returns>
        public virtual int Sample(IManager manager, IUniformRandom random)
        {
            var entity = manager.AddEntity();

            // Add position (when dropped) and renderer (when dropped or equipped).
            manager.AddComponent<Transform>(entity);
            var renderer = manager.AddComponent<TextureRenderer>(entity).Initialize(Model, ModelScale);

            // Do not render initially (only when dropped).
            renderer.Enabled = false;

            // Add to relevant indexes.
            manager.AddComponent<Index>(entity).Initialize(
                Item.IndexGroupMask |
                TextureRenderSystem.IndexGroupMask);

            // Add helper class for info retrieval.
            manager.AddComponent<ItemInfo>(entity);

            // Add slot components.
            for (var i = 0; i < Slots.Length; i++)
            {
                manager.AddComponent<SpaceItemSlot>(entity).
                    Initialize(ItemSlotInfo.TypeMap[Slots[i].Type.ToLowerInvariant()], Slots[i].Size, Slots[i].Offset);
            }

            return entity;
        }

        /// <summary>
        /// Samples the attributes to apply to the item.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="item">The item.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns></returns>
        /// <return>The entity with the attributes applied.</return>
        protected int SampleAttributes(IManager manager, int item, IUniformRandom random)
        {
            foreach (var attribute in GuaranteedAttributes)
            {
                manager.AddComponent<Attribute<AttributeType>>(item).Initialize(attribute.SampleAttributeModifier(random));
            }
            var numAdditionalAttributes = (AdditionalAttributeCount.Low == AdditionalAttributeCount.High) ? AdditionalAttributeCount.Low
                : random.NextInt32(AdditionalAttributeCount.Low, AdditionalAttributeCount.High);
            for (var i = 0; i < numAdditionalAttributes; i++)
            {
                manager.AddComponent<Attribute<AttributeType>>(item).Initialize(AdditionalAttributes[random.NextInt32(AdditionalAttributes.Length)].SampleAttributeModifier(random));
            }
            return item;
        }

        #endregion

        #region Types

        /// <summary>
        /// Utility class for serializing item slots.
        /// </summary>
        public sealed class ItemSlotInfo
        {
            /// <summary>
            /// Maps names used in XML representation to type ids.
            /// </summary>
            /// <remarks>
            /// Use all lowercase here, as the XML input will be forced to
            /// lowercase before the lookup.
            /// </remarks>
            public static readonly Dictionary<string, int> TypeMap = new Dictionary<string, int>
            {
                {"armor", Armor.TypeId},
                {"fuselage", Fuselage.TypeId},
                {"reactor", Reactor.TypeId},
                {"sensor", Sensor.TypeId},
                {"shield", Shield.TypeId},
                {"thruster", Thruster.TypeId},
                {"weapon", Weapon.TypeId},
                {"wing", Wing.TypeId}
            };

            /// <summary>
            /// The supported item type.
            /// </summary>
            public string Type;

            /// <summary>
            /// Size supported by this slot.
            /// </summary>
            [ContentSerializer(Optional = true)]
            public ItemSlotSize Size = ItemSlotSize.Small;

            /// <summary>
            /// The offset of this items origin from its parent slot.
            /// </summary>
            [ContentSerializer(Optional = true)]
            public Vector2 Offset = Vector2.Zero;
        }

        #endregion
    }
}
