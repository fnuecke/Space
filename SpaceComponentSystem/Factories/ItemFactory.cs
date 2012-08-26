using System;
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
    [DefaultProperty("Name")]
    public abstract class ItemFactory : IFactory
    {
        #region Properties
        
        /// <summary>
        /// Unique name for this item type.
        /// </summary>
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
        [Editor("Space.Tools.DataEditor.TextureAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
        [Editor("Space.Tools.DataEditor.TextureAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Category("Media")]
        [Description("The texture used to represent the item in-game, e.g. when lying on the ground or equipped on a ship.")]
        public string Model
        {
            get { return _model; }
            set { _model = value; }
        }

        /// <summary>
        /// The offset with which to render the items model texture relative to its mount point.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Media")]
        [Description("The offset relative to the items mount point with which render it when equipped.")]
        public Vector2? ModelOffset
        {
            get { return _modelOffset; }
            set { _modelOffset = value; }
        }

        /// <summary>
        /// Determines whether the model should be rendered below the parent, e.g. for wings and torpedo mounts.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(false)]
        [Category("Media")]
        [Description("Whether to render the item below its parent, e.g. for wings below fuselage and torpedo mounts below wings.")]
        public bool ModelBelowParent
        {
            get { return _modelBelowParent; }
            set { _modelBelowParent = value; }
        }

        /// <summary>
        /// Asset name of the particle effect to trigger when this thruster is
        /// active (accelerating).
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Media")]
        [Description("A list of effects to that can be triggered when the item is equipped.")]
        public EffectInfo[] Effects
        {
            get { return _effects; }
            set { _effects = value; }
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
        [TriggersFullValidation]
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
        [TriggersFullValidation]
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

        private string _itemPool = "";

        private string _icon = "Images/Icons/Buffs/default";

        private string _model = "Textures/Items/default";

        private Vector2? _modelOffset;

        private bool _modelBelowParent;

        private EffectInfo[] _effects;

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
            var renderer = manager.AddComponent<TextureRenderer>(entity).Initialize(_model, _requiredSlotSize.Scale(1f));

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

            // Add effect components.
            if (_effects != null)
            {
                foreach (var info in _effects)
                {
                    if (!string.IsNullOrWhiteSpace(info.Name))
                    {
                        manager.AddComponent<ItemEffect>(entity)
                            .Initialize(info.Group, info.Name, info.Scale, info.Offset, MathHelper.ToRadians(info.Direction));
                    }
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
                var numAdditionalAttributes = (_additionalAttributeCount.Low == _additionalAttributeCount.High || random == null)
                                                  ? _additionalAttributeCount.Low
                                                  : random.NextInt32(_additionalAttributeCount.Low,
                                                                     _additionalAttributeCount.High);
                for (var i = 0; i < numAdditionalAttributes; i++)
                {
                    manager.AddComponent<Attribute<AttributeType>>(item).Initialize(
                        _additionalAttributes[random == null ? (i % _additionalAttributes.Length) : random.NextInt32(_additionalAttributes.Length)].SampleAttributeModifier(random));
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

        /// <summary>
        /// Holds information for a single thruster effect attachment.
        /// </summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public sealed class EffectInfo
        {
            /// <summary>
            /// Gets or sets the group to which this effect belongs. This allows the
            /// game to trigger the effect when appropriate, e.g. it will trigger
            /// weapon effects when a weapon is fired, thruster effects when accelerating.
            /// </summary>
            [ContentSerializer(Optional = true)]
            [DefaultValue(ParticleEffects.EffectGroup.None)]
            [Category("Logic")]
            [Description("The group this effect belongs to, which will allow the game to trigger the effect when appropriate.")]
            public ParticleEffects.EffectGroup Group
            {
                get { return _group; }
                set { _group = value; }
            }

            /// <summary>
            /// Asset name of the particle effect to trigger when this thruster is
            /// active (accelerating).
            /// </summary>
            [Editor("Space.Tools.DataEditor.EffectAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
            [ContentSerializer(Optional = true)]
            [DefaultValue(null)]
            [Category("General")]
            [Description("The asset name of the particle effect to use for this thruster when accelerating.")]
            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            /// <summary>
            /// The scale at which to render the thruster effect.
            /// </summary>
            [ContentSerializer(Optional = true)]
            [DefaultValue(1f)]
            [Category("Media")]
            [Description("The scale at which to render the thruster effect.")]
            public float Scale
            {
                get { return _scale; }
                set { _scale = value; }
            }

            /// <summary>
            /// Offset for the thruster effect relative to the texture.
            /// </summary>
            [ContentSerializer(Optional = true)]
            [Category("Media")]
            [Description("The offset relative to the slot the item is equipped in at which to emit particle effects when accelerating.")]
            public Vector2 Offset
            {
                get { return _offset; }
                set { _offset = value; }
            }

            /// <summary>
            /// Gets or sets the direction in which the effect should be emitted. It will be
            /// triggered when the ship accelerates in the opposite direction.
            /// </summary>
            [ContentSerializer(Optional = true)]
            [Category("Media")]
            [Description("The direction in which to emit the effect, in degrees, relative to the ships rotation. This will be triggered when the ship accelerates in the opposite direction.")]
            public float Direction
            {
                get { return _direction; }
                set { _direction = value; }
            }

            #region Backing fields

            private ParticleEffects.EffectGroup _group;

            private string _name;

            private float _scale = 1f;

            private Vector2 _offset;

            private float _direction;

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
                return (Group != ParticleEffects.EffectGroup.None ? (Group + ": ") : "") + Name;
            }

            #endregion
        }

        #endregion
    }

    /// <summary>
    /// Converter methods for item type enum.
    /// </summary>
    public static class ItemTypeExtensions
    {
        private static readonly Dictionary<ItemFactory.ItemSlotInfo.ItemType, Type> TypeMapping = new Dictionary<ItemFactory.ItemSlotInfo.ItemType, Type>
                {
                    {ItemFactory.ItemSlotInfo.ItemType.Armor, typeof(ArmorFactory)},
                    {ItemFactory.ItemSlotInfo.ItemType.Fuselage, typeof(FuselageFactory)},
                    {ItemFactory.ItemSlotInfo.ItemType.Reactor, typeof(ReactorFactory)},
                    {ItemFactory.ItemSlotInfo.ItemType.Sensor, typeof(SensorFactory)},
                    {ItemFactory.ItemSlotInfo.ItemType.Shield, typeof(ShieldFactory)},
                    {ItemFactory.ItemSlotInfo.ItemType.Thruster, typeof(ThrusterFactory)},
                    {ItemFactory.ItemSlotInfo.ItemType.Weapon, typeof(WeaponFactory)},
                    {ItemFactory.ItemSlotInfo.ItemType.Wing, typeof(WingFactory)}
                };

        private static readonly Dictionary<Type, ItemFactory.ItemSlotInfo.ItemType> EnumMapping = new Dictionary<Type, ItemFactory.ItemSlotInfo.ItemType>
                {
                    {typeof(ArmorFactory), ItemFactory.ItemSlotInfo.ItemType.Armor},
                    {typeof(FuselageFactory), ItemFactory.ItemSlotInfo.ItemType.Fuselage},
                    {typeof(ReactorFactory), ItemFactory.ItemSlotInfo.ItemType.Reactor},
                    {typeof(SensorFactory), ItemFactory.ItemSlotInfo.ItemType.Sensor},
                    {typeof(ShieldFactory), ItemFactory.ItemSlotInfo.ItemType.Shield},
                    {typeof(ThrusterFactory), ItemFactory.ItemSlotInfo.ItemType.Thruster},
                    {typeof(WeaponFactory), ItemFactory.ItemSlotInfo.ItemType.Weapon},
                    {typeof(WingFactory), ItemFactory.ItemSlotInfo.ItemType.Wing}
                };

        public static Type ToFactoryType(this ItemFactory.ItemSlotInfo.ItemType type)
        {
            Type result;
            TypeMapping.TryGetValue(type, out result);
            return result;
        }

        public static ItemFactory.ItemSlotInfo.ItemType ToItemType(this Type type)
        {
            ItemFactory.ItemSlotInfo.ItemType result;
            EnumMapping.TryGetValue(type, out result);
            return result;
        }
    }
}
