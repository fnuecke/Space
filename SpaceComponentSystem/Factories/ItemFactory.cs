using System.Collections.Generic;
using System.ComponentModel;
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
        #region Properties
        
        /// <summary>
        /// Unique name for this item type.
        /// </summary>
        [DefaultValue("")]
        [Category("General")]
        [Description("The name of this item, by which it can be referenced.")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Asset name of the texture to use for this item type to render it in
        /// menus and the inventory.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue("Images/Icons/Buffs/default")]
        [Category("Media")]
        [Description("The icon used to represent the item in the GUI, e.g. in the inventory.")]
        public string Icon
        {
            get { return _icon; }
            set { _icon = value; }
        }

        /// <summary>
        /// The ingame texture to be displayed for items floating around in
        /// space.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue("Textures/Items/default")]
        [Category("Media")]
        [Description("The texture used to represent the item in-game, e.g. when lying on the ground or equipped on a ship.")]
        public string Model
        {
            get { return _model; }
            set { _model = value; }
        }

        /// <summary>
        /// The scaling to apply to the model texture.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(1.0f)]
        [Category("Media")]
        [Description("The relative scale of the texture.")]
        public float ModelScale
        {
            get { return _modelScale; }
            set { _modelScale = value; }
        }

        /// <summary>
        /// The quality of the item, to give a rough idea of the value.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(ItemQuality.Common)]
        [Category("Equipment")]
        [Description("The items quality rating. This is purely to give the player a quick grasp of the potential value of an item and has no influence on game logic.")]
        public ItemQuality Quality
        {
            get { return _quality; }
            set { _quality = value; }
        }

        /// <summary>
        /// The slot size of the item.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(ItemSlotSize.Small)]
        [Category("Equipment")]
        [Description("The minimum size of the slot the item requires to be equpped in.")]
        public ItemSlotSize RequiredSlotSize
        {
            get { return _requiredSlotSize; }
            set { _requiredSlotSize = value; }
        }

        /// <summary>
        /// Slots this item provides for other items to be equipped into.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Equipment")]
        [Description("The slots this item provides, allowing other items to be equipped into this item, e.g. for socketing.")]
        public ItemSlotInfo[] Slots
        {
            get { return _slots; }
            set { _slots = value; }
        }

        /// <summary>
        /// A list of attribute modifiers that are guaranteed to be applied to
        /// the generated item, just with random values.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Stats")]
        [Description("Attribute bonuses that a generated item of this type is guaranteed to provide when equipped.")]
        public AttributeModifierConstraint<AttributeType>[] GuaranteedAttributes
        {
            get { return _guaranteedAttributes; }
            set { _guaranteedAttributes = value; }
        }

        /// <summary>
        /// A list of attribute modifiers from which a certain number is
        /// randomly sampled, and from the chosen attribute modifiers will then
        /// be sampled the actual values to be applied to the generated item.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Stats")]
        [Description("Possible attribute bonuses items of this type might have. Additional attributes are sampled from this pool.")]
        public AttributeModifierConstraint<AttributeType>[] AdditionalAttributes
        {
            get { return _additionalAttributes; }
            set { _additionalAttributes = value; }
        }

        /// <summary>
        /// The number of additional attribute modifiers to apply to a
        /// generated item.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Stats")]
        [Description("The number of additional attributes to sample for a generated item of this type.")]
        public IntInterval AdditionalAttributeCount
        {
            get { return _additionalAttributeCount; }
            set { _additionalAttributeCount = value; }
        }

        #endregion

        #region Backing fields

        private string _name = "";

        private string _icon = "Images/Icons/Buffs/default";

        private string _model = "Textures/Items/default";

        private float _modelScale = 1.0f;

        private ItemQuality _quality = ItemQuality.Common;

        private ItemSlotSize _requiredSlotSize = ItemSlotSize.Small;

        private ItemSlotInfo[] _slots;

        private AttributeModifierConstraint<AttributeType>[] _guaranteedAttributes;

        private AttributeModifierConstraint<AttributeType>[] _additionalAttributes;

        private IntInterval _additionalAttributeCount;

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
            var renderer = manager.AddComponent<TextureRenderer>(entity).Initialize(_model, _modelScale);

            // Do not render initially (only when dropped).
            renderer.Enabled = false;

            // Add to relevant indexes.
            manager.AddComponent<Index>(entity).Initialize(
                Item.IndexGroupMask |
                TextureRenderSystem.IndexGroupMask);

            // Add helper class for info retrieval.
            manager.AddComponent<ItemInfo>(entity);

            // Add slot components.
            if (_slots != null)
            {
                for (var i = 0; i < _slots.Length; i++)
                {
                    manager.AddComponent<SpaceItemSlot>(entity).
                        Initialize(ItemSlotInfo.TypeMap[_slots[i].Type], _slots[i].Size,
                                   _slots[i].Offset.HasValue ? _slots[i].Offset.Value : Vector2.Zero);
                }
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
            if (_guaranteedAttributes != null)
            {
                foreach (var attribute in _guaranteedAttributes)
                {
                    manager.AddComponent<Attribute<AttributeType>>(item).Initialize(
                        attribute.SampleAttributeModifier(random));
                }
            }
            if (_additionalAttributes != null && _additionalAttributeCount != null)
            {
                var numAdditionalAttributes = (_additionalAttributeCount.Low == _additionalAttributeCount.High)
                                                  ? _additionalAttributeCount.Low
                                                  : random.NextInt32(_additionalAttributeCount.Low,
                                                                     _additionalAttributeCount.High);
                for (var i = 0; i < numAdditionalAttributes; i++)
                {
                    manager.AddComponent<Attribute<AttributeType>>(item).Initialize(
                        _additionalAttributes[random.NextInt32(_additionalAttributes.Length)].SampleAttributeModifier(
                            random));
                }
            }
            return item;
        }

        #endregion

        #region Types

        /// <summary>
        /// Utility class for serializing item slots.
        /// </summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public sealed class ItemSlotInfo
        {
            #region Constants

            /// <summary>
            /// Possible item types for slots. This is used for string representation
            /// in the serialized state.
            /// </summary>
            public enum ItemType
            {
                None,
                Armor,
                Fuselage,
                Reactor,
                Sensor,
                Shield,
                Thruster,
                Weapon,
                Wing
            }

            /// <summary>
            /// Maps names used in XML representation to type ids.
            /// </summary>
            public static readonly Dictionary<ItemType, int> TypeMap = new Dictionary<ItemType, int>
            {
                {ItemType.Armor, Armor.TypeId},
                {ItemType.Fuselage, Fuselage.TypeId},
                {ItemType.Reactor, Reactor.TypeId},
                {ItemType.Sensor, Sensor.TypeId},
                {ItemType.Shield, Shield.TypeId},
                {ItemType.Thruster, Thruster.TypeId},
                {ItemType.Weapon, Weapon.TypeId},
                {ItemType.Wing, Wing.TypeId}
            };

            #endregion

            #region Properties

            /// <summary>
            /// The supported item type.
            /// </summary>
            [Description("The type of item that can be equipped in this slot.")]
            public ItemType Type
            {
                get { return _type; }
                set { _type = value; }
            }

            /// <summary>
            /// Size supported by this slot.
            /// </summary>
            [ContentSerializer(Optional = true)]
            [DefaultValue(ItemSlotSize.Small)]
            [Description("The size of the item slot, i.e. the maximum item size this slot supports.")]
            public ItemSlotSize Size
            {
                get { return _size; }
                set { _size = value; }
            }

            /// <summary>
            /// The offset of this items origin from its parent slot.
            /// </summary>
            [ContentSerializer(Optional = true)]
            [DefaultValue(null)]
            [Description("The offset of the slot relative to its parent.")]
            public Vector2? Offset
            {
                get { return _offset; }
                set { _offset = value; }
            }

            #endregion

            #region Backing fields

            private ItemType _type;

            private ItemSlotSize _size = ItemSlotSize.Small;

            private Vector2? _offset;

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
                return Size + " " + Type + (Offset == null ? "" : (" @ " + Offset));
            }

            #endregion
        }

        #endregion
    }
}
