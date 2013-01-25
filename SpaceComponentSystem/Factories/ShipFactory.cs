using System;
using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Physics;
using Engine.ComponentSystem.Physics.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Constraints;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.FarMath;
using Engine.Random;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Data;

namespace Space.ComponentSystem.Factories
{
    /// <summary>Basic descriptor for a single ship class.</summary>
    [DefaultProperty("Name")]
    public sealed class ShipFactory : IFactory
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        /// <summary>The linear damping applied to ships over a second.</summary>
        public const float LinearDamping = 0.05f * Space.Util.Settings.TicksPerSecond;

        #endregion

        #region General

        /// <summary>The name of the ship class, which serves as a unique type identifier.</summary>
        [Category("General")]
        [Description("The name of this ship, by which it can be referenced.")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>The base texture to use for rendering the ship class.</summary>
        [Editor("Space.Tools.DataEditor.TextureAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Category("Media")]
        [Description("The base image to represent the ship, without any equipment.")]
        public string Texture
        {
            get { return _texture; }
            set { _texture = value; }
        }

        /// <summary>The base collision radius of the ship class.</summary>
        [Category("Logic")]
        [Description("The radius of the circle that is used for collision checks.")]
        public float CollisionRadius
        {
            get { return _collisionRadius; }
            set { _collisionRadius = value; }
        }

        /// <summary>The Item Pool which is used if the Entity is destroyed</summary>
        [Editor("Space.Tools.DataEditor.ItemPoolChooserEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Logic")]
        [Description("The name of the item pool from which items are sampled upon destruction of the ship.")]
        public string ItemPool
        {
            get { return _itemPool; }
            set { _itemPool = value; }
        }

        /// <summary>Gets or sets the experience points the ship is worth when destroyed.</summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(0)]
        [Category("Logic")]
        [Description("The experience the ship is worth when destroyed.")]
        public int ExperiencePoints
        {
            get { return _xp; }
            set { _xp = value; }
        }

        /// <summary>List of basic stats for this ship class.</summary>
        [Category("Stats")]
        [Description("Attribute bonuses provided by this ship.")]
        public AttributeModifierConstraint<AttributeType>[] Attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        /// <summary>Default equipment to generate for the ship.</summary>
        [Category("Equipment")]
        [Description("A hierarchical representation of the default equipment to populate the ship with.")]
        public ItemInfo Items
        {
            get { return _items; }
            set { _items = value; }
        }

        #endregion

        #region Backing fields

        private string _name = "";

        private string _texture = "Textures/Ships/default";

        private float _collisionRadius;

        private string _itemPool;

        private int _xp;

        private AttributeModifierConstraint<AttributeType>[] _attributes;

        private ItemInfo _items = new ItemInfo();

        #endregion

        #region Sampling

        /// <summary>Samples the attributes to apply to the item.</summary>
        /// <param name="manager"> </param>
        /// <param name="faction">The faction the ship belongs to.</param>
        /// <param name="position">The position at which to spawn the ship.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        public int Sample(IManager manager, Factions faction, FarPosition position, IUniformRandom random)
        {
            var entity = CreateShip(manager, faction, position);

            // Create initial equipment.
            var equipment = (ItemSlot) manager.GetComponent(entity, ItemSlot.TypeId);
            equipment.Item = FactoryLibrary.SampleItem(manager, _items.Name, position, random);
            if (equipment.Item > 0)
            {
                foreach (var item in _items.Slots)
                {
                    SampleItems(manager, position, random, equipment.Item, item);
                }
            }

            // Add our attributes.
            var attributes = (Attributes<AttributeType>) manager.GetComponent(entity, Attributes<AttributeType>.TypeId);
            foreach (var attribute in _attributes)
            {
                var modifier = attribute.SampleAttributeModifier(random);
                if (modifier.ComputationType == AttributeComputationType.Multiplicative)
                {
                    throw new InvalidOperationException("Base attributes must be additive.");
                }
                attributes.SetBaseValue(modifier.Type, modifier.Value);
            }

            // Fill up our values.
            var health = ((Health) manager.GetComponent(entity, Health.TypeId));
            var energy = ((Energy) manager.GetComponent(entity, Energy.TypeId));
            health.Value = health.MaxValue;
            energy.Value = energy.MaxValue;

            // Add experience points if we're worth any.
            if (_xp > 0)
            {
                manager.AddComponent<ExperiencePoints>(entity).Initialize(_xp);
            }

            return entity;
        }

        /// <summary>Samples the items for the specified item info and children (recursively).</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="position">The position.</param>
        /// <param name="random">The random.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="itemInfo">The item info.</param>
        private static void SampleItems(
            IManager manager, FarPosition position, IUniformRandom random, int parent, ItemInfo itemInfo)
        {
            // Create the actual item.
            var itemId = FactoryLibrary.SampleItem(manager, itemInfo.Name, position, random);
            if (itemId < 1)
            {
                // No such item.
                return;
            }
            var item = (Item) manager.GetComponent(itemId, Item.TypeId);

            // Then equip it in the parent.
            foreach (ItemSlot slot in manager.GetComponents(parent, ItemSlot.TypeId))
            {
                if (slot.Item == 0 && slot.Validate(item))
                {
                    // Found a suitable empty slot, equip here.
                    slot.Item = itemId;

                    // Recurse to generate children.
                    foreach (var childInfo in itemInfo.Slots)
                    {
                        SampleItems(manager, position, random, itemId, childInfo);
                    }

                    // Done.
                    return;
                }
            }

            // If we get here we couldn't find a slot to equip the item in.
            manager.RemoveEntity(itemId);

            Logger.Warn("Parent item did not have a slot for the requested child item.");
        }

        /// <summary>Sets up the basic ship data that is deterministic.</summary>
        /// <param name="manager"> </param>
        /// <param name="faction"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private int CreateShip(IManager manager, Factions faction, FarPosition position)
        {
            var entity = manager.AddEntity();

            var body = manager.AddBody(entity, type: Body.BodyType.Dynamic, worldPosition: position, allowSleep: false);
            // We need at least one fixture to carry on our index and collision groups.
            manager.AttachCircle(
                body,
                UnitConversion.ToSimulationUnits(_collisionRadius),
                collisionGroups: faction.ToCollisionGroup());

            // These are 'worst-case' bounds, i.e. no ship should get larger than this. We use it as
            // a buffer for the camera and interpolation system, to allow them picking up on objects
            // that are positioned outside the viewport, but reach into it due to their size.
            var bounds = new FarRectangle(-2, -2, 4, 4);

            // Can be detected.
            manager.AddComponent<Indexable>(entity).Initialize(DetectableSystem.IndexId);
            // Can be attracted.
            manager.AddComponent<Indexable>(entity).Initialize(GravitationSystem.IndexId);
            // Can make noise.
            manager.AddComponent<Indexable>(entity).Initialize(SoundSystem.IndexId);
            // Must be detectable by the camera.
            manager.AddComponent<Indexable>(entity).Initialize(bounds, CameraSystem.IndexId);
            // Rendering should be interpolated.
            manager.AddComponent<Indexable>(entity).Initialize(bounds, InterpolationSystem.IndexId);

            // Although 'unrealistic' in space, make ships stop automatically if not accelerating.
            body.LinearDamping = LinearDamping;

            manager.AddComponent<Gravitation>(entity).Initialize();
            manager.AddComponent<ShipControl>(entity);
            manager.AddComponent<WeaponControl>(entity);
            manager.AddComponent<Energy>(entity);
            manager.AddComponent<Health>(entity).Initialize(120);
            manager.AddComponent<ShipDrawable>(entity)
                   .Initialize(_texture, Color.Lerp(Color.White, faction.ToColor(), 0.5f));
            manager.AddComponent<ParticleEffects>(entity);

            // Faction component, which allows checking which group the ship
            // belongs to.
            manager.AddComponent<Faction>(entity).Initialize(faction);

            // Make it detectable by AI and show up on the radar.
            manager.AddComponent<Detectable>(entity).Initialize("Textures/ship", true);

            // Controllers for maneuvering and shooting.
            manager.AddComponent<ShipInfo>(entity);

            // Create equipment slot.
            manager.AddComponent<SpaceItemSlot>(entity).Initialize(Fuselage.TypeId);

            // Give it an inventory as well.
            manager.AddComponent<Inventory>(entity).Initialize(10);

            // Add some character!
            manager.AddComponent<Attributes<AttributeType>>(entity);

            // Do we drop stuff?
            if (_itemPool != null)
            {
                manager.AddComponent<Drops>(entity).Initialize(_itemPool);
            }

            // The the sound component for the thruster sound.
            manager.AddComponent<Sound>(entity).Initialize("Thruster");

            return entity;
        }

        #endregion

        #region Types

        /// <summary>Utility class for serialized representation of items in slots.</summary>
        [TypeConverter(typeof (ExpandableObjectConverter))]
        public sealed class ItemInfo
        {
            #region Properties

            /// <summary>The name of the item (template name).</summary>
            [Editor("Space.Tools.DataEditor.ItemInfoEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
            [TypeConverter(typeof (ReadonlyItemNameConverter))]
            [Description("The name of the item type to sample.")]
            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            /// <summary>Items to be generated and equipped into this item.</summary>
            [ContentSerializer(Optional = true, FlattenContent = true, CollectionItemName = "Item")]
            [Description("Items to generate into the slots available in the sampled item.")]
            public ItemInfo[] Slots
            {
                get { return _slots; }
                set { _slots = value; }
            }

            #endregion

            #region Backing fields

            private string _name;

            private ItemInfo[] _slots = new ItemInfo[0];

            #endregion

            #region ToString

            /// <summary>
            ///     Returns a <see cref="System.String"/> that represents this instance.
            /// </summary>
            /// <returns>
            ///     A <see cref="System.String"/> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return Name + (Slots.Length > 0 ? (" (" + Slots.Length + ")") : "");
            }

            #endregion
        }

        #endregion
    }
}