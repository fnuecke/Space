using System.Globalization;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.FarMath;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// This component has no actual functionality, but serves merely as a
    /// facade to centralize common tasks for retrieving information on
    /// ships.
    /// </summary>
    public sealed class ShipInfo : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherShipInfo = (ShipInfo)other;
            MaxAcceleration = otherShipInfo.MaxAcceleration;
            MaxSpeed = otherShipInfo.MaxSpeed;
            Mass = otherShipInfo.Mass;
            RadarRange = otherShipInfo.RadarRange;
            WeaponRange = otherShipInfo.WeaponRange;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            MaxAcceleration = 0;
            MaxSpeed = 0;
            Mass = 0;
            RadarRange = 0;
            WeaponRange = 0;
        }

        #endregion

        #region Health / Energy

        /// <summary>
        /// Tests whether the ship is currently alive.
        /// </summary>
        /// <remarks>
        /// For player ships this checks if they are currently respawning. All
        /// AI controlled ships have only a single life, so if they exist they
        /// are considered to be alive.
        /// </remarks>
        public bool IsAlive
        {
            get
            {
                var respawn = (Respawn)Manager.GetComponent(Entity, Respawn.TypeId);
                return respawn == null || !respawn.IsRespawning;
            }
        }

        /// <summary>
        /// Gets the ship's current absolute health.
        /// </summary>
        public float Health
        {
            get
            {
                var health = (Health)Manager.GetComponent(Entity, Components.Health.TypeId);
                return health != null ? health.Value : 0;
            }
        }

        /// <summary>
        /// Gets the ship's maximum absolute health.
        /// </summary>
        public float MaxHealth
        {
            get
            {
                var health = (Health)Manager.GetComponent(Entity, Components.Health.TypeId);
                return health != null ? health.MaxValue : 0;
            }
        }

        /// <summary>
        /// Gets the ship's current relative health.
        /// </summary>
        public float RelativeHealth
        {
            get
            {
                var health = (Health)Manager.GetComponent(Entity, Components.Health.TypeId);
                return health != null ? health.Value / health.MaxValue : 0;
            }
        }

        /// <summary>
        /// Gets the ship's current absolute energy.
        /// </summary>
        public float Energy
        {
            get
            {
                var energy = (Energy)Manager.GetComponent(Entity, Components.Energy.TypeId);
                return energy != null ? energy.Value : 0;
            }
        }

        /// <summary>
        /// Gets the ship's maximum absolute energy.
        /// </summary>
        public float MaxEnergy
        {
            get
            {
                var energy = (Energy)Manager.GetComponent(Entity, Components.Energy.TypeId);
                return energy != null ? energy.MaxValue : 0;
            }
        }

        /// <summary>
        /// Gets the ship's current relative energy.
        /// </summary>
        public float RelativeEnergy
        {
            get
            {
                var energy = (Energy)Manager.GetComponent(Entity, Components.Energy.TypeId);
                return energy != null ? energy.Value / energy.MaxValue : 0;
            }
        }

        #endregion

        #region Physics

        /// <summary>
        /// Get the ship's current position.
        /// </summary>
        public FarPosition Position
        {
            get
            {
                var transform = (Transform)Manager.GetComponent(Entity, Transform.TypeId);
                return transform != null ? transform.Translation : FarPosition.Zero;
            }
        }

        /// <summary>
        /// Get the ship's current rotation, in radians.
        /// </summary>
        public float Rotation
        {
            get
            {
                var transform = (Transform)Manager.GetComponent(Entity, Transform.TypeId);
                return transform != null ? transform.Rotation : 0;
            }
        }

        /// <summary>
        /// Get whether the ship is currently accelerating.
        /// </summary>
        public bool IsAccelerating
        {
            get
            {
                var control = (ShipControl)Manager.GetComponent(Entity, ShipControl.TypeId);
                return control != null && control.DirectedAcceleration != Vector2.Zero;
            }
        }

        /// <summary>
        /// Tells whether the ship is currently stabilizing its position.
        /// </summary>
        public bool IsStabilizing
        {
            get
            {
                var control = (ShipControl)Manager.GetComponent(Entity, ShipControl.TypeId);
                return control != null && control.Stabilizing;
            }
        }

        /// <summary>
        /// Get the ship's current speed.
        /// 
        /// <para>
        /// Note: this value max exceed the <c>MaxSpeed</c> if external forces
        /// such as gravitation are involved.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Performance note: store this value if you use it more than once.
        /// </remarks>
        public float Speed
        {
            get
            {
                var velocity = (Velocity)Manager.GetComponent(Entity, Velocity.TypeId);
                return velocity != null ? velocity.Value.Length() : 0;
            }
        }

        /// <summary>
        /// Get the maximum speed of the ship.
        /// </summary>
        public float MaxSpeed { get; internal set; }

        /// <summary>
        /// Get the maximum acceleration this ship is capable of.
        /// </summary>
        public float MaxAcceleration { get; internal set; }

        /// <summary>
        /// Get the ship's current rotation speed, in radians per tick.
        /// </summary>
        public float RotationSpeed
        {
            get
            {
                var spin = (Spin)Manager.GetComponent(Entity, Spin.TypeId);
                return spin != null ? spin.Value : 0;
            }
        }

        #endregion

        #region Modules / Attributes

        /// <summary>
        /// Gets the overall mass of this ship.
        /// </summary>
        public float Mass { get; internal set; }

        /// <summary>
        /// Get the ship's overall radar range.
        /// </summary>
        public float RadarRange { get; internal set; }

        /// <summary>
        /// The distance our highest range weapon can shoot.
        /// </summary>
        public float WeaponRange { get; internal set; }

        #endregion

        #region Equipment / Inventory

        /// <summary>
        /// The current number of items in the ship's inventory.
        /// </summary>
        public int InventoryCapacity
        {
            get
            {
                var inventory = (Inventory)Manager.GetComponent(Entity, Inventory.TypeId);
                return inventory != null ? inventory.Capacity : 0;
            }
        }

        /// <summary>
        /// The item at the specified index in the ship's inventory.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>The item at that index.</returns>
        public int? InventoryItemAt(int index)
        {
            var inventory = (Inventory)Manager.GetComponent(Entity, Inventory.TypeId);
            return inventory != null ? inventory[index] : null;
        }

        /// <summary>
        /// Get the number of item slots for the specified item type.
        /// </summary>
        /// <typeparam name="TItem">The item type to check for.</typeparam>
        /// <returns></returns>
        public int EquipmentSlotCount<TItem>()
            where TItem : Item
        {
            var equipment = (Equipment)Manager.GetComponent(Entity, Equipment.TypeId);
            return equipment != null ? equipment.GetSlotCount<TItem>() : 0;
        }

        /// <summary>
        /// Get the equipped item of the specified type in the specified slot.
        /// </summary>
        /// <typeparam name="TItem">The type of item to check.</typeparam>
        /// <param name="index">The slot index from which to get the item.</param>
        /// <returns>The item at that slot index.</returns>
        public int? EquipmentItemAt<TItem>(int index)
            where TItem : Item
        {
            var equipment = (Equipment)Manager.GetComponent(Entity, Equipment.TypeId);
            return equipment != null ? equipment.GetItem<TItem>(index) : null;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(MaxAcceleration)
                .Write(MaxSpeed)
                .Write(Mass)
                .Write(RadarRange)
                .Write(WeaponRange);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            MaxAcceleration = packet.ReadSingle();
            MaxSpeed = packet.ReadSingle();
            Mass = packet.ReadSingle();
            RadarRange = packet.ReadSingle();
            WeaponRange = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            hasher.Put(MaxAcceleration);
            hasher.Put(MaxSpeed);
            hasher.Put(Mass);
            hasher.Put(RadarRange);
            hasher.Put(WeaponRange);
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
            return base.ToString() + ", IsAlive=" + IsAlive + ", Health=" + Health.ToString(CultureInfo.InvariantCulture) + ", MaxHealth=" + MaxHealth.ToString(CultureInfo.InvariantCulture) + ", RelativeHealth=" + RelativeHealth.ToString(CultureInfo.InvariantCulture) + ", Energy=" + Energy.ToString(CultureInfo.InvariantCulture) + ", MaxEnergy=" + MaxEnergy.ToString(CultureInfo.InvariantCulture) + ", RelativeEnergy=" + RelativeEnergy.ToString(CultureInfo.InvariantCulture) + ", Position=" + Position + ", Rotation=" + Rotation.ToString(CultureInfo.InvariantCulture) + ", IsAccelerating=" + IsAccelerating + ", IsStabilizing=" + IsStabilizing + ", Speed=" + Speed.ToString(CultureInfo.InvariantCulture) + ", MaxSpeed=" + MaxSpeed.ToString(CultureInfo.InvariantCulture) + ", MaxAcceleration=" + MaxAcceleration.ToString(CultureInfo.InvariantCulture) + ", RotationSpeed=" + RotationSpeed.ToString(CultureInfo.InvariantCulture) + ", Mass=" + Mass.ToString(CultureInfo.InvariantCulture) + ", RadarRange=" + RadarRange.ToString(CultureInfo.InvariantCulture) + ", WeaponRange=" + WeaponRange.ToString(CultureInfo.InvariantCulture) + ", InventoryCapacity=" + InventoryCapacity;
        }

        #endregion
    }
}
