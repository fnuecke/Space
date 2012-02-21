﻿using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;
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
                var respawn = Manager.GetComponent<Respawn>(Entity);
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
                var health = Manager.GetComponent<Health>(Entity);
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
                var health = Manager.GetComponent<Health>(Entity);
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
                var health = Manager.GetComponent<Health>(Entity);
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
                var energy = Manager.GetComponent<Energy>(Entity);
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
                var energy = Manager.GetComponent<Energy>(Entity);
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
                var energy = Manager.GetComponent<Energy>(Entity);
                return energy != null ? energy.Value / energy.MaxValue : 0;
            }
        }

        #endregion

        #region Physics

        /// <summary>
        /// Get the ship's current position.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                var transform = Manager.GetComponent<Transform>(Entity);
                return transform != null ? transform.Translation : Vector2.Zero;
            }
        }

        /// <summary>
        /// Get the ship's current rotation, in radians.
        /// </summary>
        public float Rotation
        {
            get
            {
                var transform = Manager.GetComponent<Transform>(Entity);
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
                var acceleration = Manager.GetComponent<Acceleration>(Entity);
                return acceleration != null && acceleration.Value != Vector2.Zero;
            }
        }

        /// <summary>
        /// Tells whether the ship is currently stabilizing its position.
        /// </summary>
        public bool IsStabilizing
        {
            get
            {
                var control = Manager.GetComponent<ShipControl>(Entity);
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
                var velocity = Manager.GetComponent<Velocity>(Entity);
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
                var spin = Manager.GetComponent<Spin>(Entity);
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

        #endregion

        #region Equipment / Inventory

        /// <summary>
        /// The current number of items in the ship's inventory.
        /// </summary>
        public int InventoryCapacity
        {
            get
            {
                var inventory = Manager.GetComponent<Inventory>(Entity);
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
            var inventory = Manager.GetComponent<Inventory>(Entity);
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
            var equipment = Manager.GetComponent<Equipment>(Entity);
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
            var equipment = Manager.GetComponent<Equipment>(Entity);
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
                .Write(RadarRange);
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
        }

        #endregion
    }
}
