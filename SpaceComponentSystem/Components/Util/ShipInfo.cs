using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// This component has no actual functionality, but serves merely as a
    /// facade to centralize common tasks for retrieving information on
    /// ships.
    /// </summary>
    public sealed class ShipInfo : Component
    {
        #region Fields

        /// <summary>
        /// Cached value for maximum ship acceleration.
        /// </summary>
        private float _maxAcceleration;

        /// <summary>
        /// Cached value for maximum ship speed.
        /// </summary>
        private float _maxSpeed;

        /// <summary>
        /// Cached value for ship mass.
        /// </summary>
        private float _mass;

        /// <summary>
        /// Cached value for ship's radar range.
        /// </summary>
        private float _radarRange;

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
                var respawn = Entity.GetComponent<Respawn>();
                if (respawn != null)
                {
                    return !respawn.IsRespawning;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Gets the ship's current absolute health.
        /// </summary>
        public float Health
        {
            get
            {
                var health = Entity.GetComponent<Health>();
                if (health != null)
                {
                    return health.Value;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the ship's maximum absolute health.
        /// </summary>
        public float MaxHealth
        {
            get
            {
                var health = Entity.GetComponent<Health>();
                if (health != null)
                {
                    return health.MaxValue;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the ship's current relative health.
        /// </summary>
        public float RelativeHealth
        {
            get
            {
                var health = Entity.GetComponent<Health>();
                if (health != null)
                {
                    return health.Value / health.MaxValue;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the ship's current absolute energy.
        /// </summary>
        public float Energy
        {
            get
            {
                var energy = Entity.GetComponent<Energy>();
                if (energy != null)
                {
                    return energy.Value;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the ship's maximum absolute energy.
        /// </summary>
        public float MaxEnergy
        {
            get
            {
                var energy = Entity.GetComponent<Energy>();
                if (energy != null)
                {
                    return energy.MaxValue;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the ship's current relative energy.
        /// </summary>
        public float RelativeEnergy
        {
            get
            {
                var energy = Entity.GetComponent<Energy>();
                if (energy != null)
                {
                    return energy.Value / energy.MaxValue;
                }
                else
                {
                    return 0;
                }
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
                var transform = Entity.GetComponent<Transform>();
                if (transform != null)
                {
                    return transform.Translation;
                }
                else
                {
                    return Vector2.Zero;
                }
            }
        }

        /// <summary>
        /// Get the ship's current rotation, in radians.
        /// </summary>
        public float Rotation
        {
            get
            {
                var transform = Entity.GetComponent<Transform>();
                if (transform != null)
                {
                    return transform.Rotation;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Get whether the ship is currently accelerating.
        /// </summary>
        public bool IsAccelerating
        {
            get
            {
                var acceleration = Entity.GetComponent<Acceleration>();
                if (acceleration != null)
                {
                    return acceleration.Value != Vector2.Zero;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Tells whether the ship is currently stabilizing its position.
        /// </summary>
        public bool IsStabilizing
        {
            get
            {
                var control = Entity.GetComponent<ShipControl>();
                if (control != null)
                {
                    return control.Stabilizing;
                }
                else
                {
                    return false;
                }
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
                var velocity = Entity.GetComponent<Velocity>();
                if (velocity != null)
                {
                    return velocity.Value.Length();
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Get the maximum speed of the ship.
        /// </summary>
        public float MaxSpeed
        {
            get
            {
                return _maxSpeed;
            }
        }

        /// <summary>
        /// Get the maximum acceleration this ship is capable of.
        /// </summary>
        public float MaxAcceleration
        {
            get
            {
                return _maxAcceleration;
            }
        }

        /// <summary>
        /// Get the ship's current rotation speed, in radians per tick.
        /// </summary>
        public float RotationSpeed
        {
            get
            {
                var spin = Entity.GetComponent<Spin>();
                if (spin != null)
                {
                    return spin.Value;
                }
                else
                {
                    return 0;
                }
            }
        }

        #endregion

        #region Modules / Attributes

        /// <summary>
        /// Gets the overall mass of this ship.
        /// </summary>
        public float Mass
        {
            get
            {
                return _mass;
            }
        }

        /// <summary>
        /// Get the ship's overall radar range.
        /// </summary>
        public float RadarRange
        {
            get
            {
                return _radarRange;
            }
        }

        #endregion

        #region Equipment / Inventory

        /// <summary>
        /// The current number of items in the ship's inventory.
        /// </summary>
        public int InventoryCapacity
        {
            get
            {
                var inventory = Entity.GetComponent<Inventory>();
                if (inventory != null)
                {
                    return inventory.Capacity;
                }
                return 0;
            }
        }

        /// <summary>
        /// The item at the specified index in the ship's inventory.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>The item at that index.</returns>
        public Entity InventoryItemAt(int index)
        {
            var inventory = Entity.GetComponent<Inventory>();
            if (inventory != null)
            {
                return inventory[index];
            }
            return null;
        }

        /// <summary>
        /// Get the number of item slots for the specified item type.
        /// </summary>
        /// <typeparam name="TItem">The item type to check for.</typeparam>
        /// <returns></returns>
        public int EquipmentSlotCount<TItem>()
            where TItem : Item
        {
            var equipment = Entity.GetComponent<Equipment>();
            if (equipment != null)
            {
                return equipment.GetSlotCount<TItem>();
            }
            return 0;
        }

        /// <summary>
        /// Get the equipped item of the specified type in the specified slot.
        /// </summary>
        /// <typeparam name="TItem">The type of item to check.</typeparam>
        /// <param name="index">The slot index from which to get the item.</param>
        /// <returns>The item at that slot index.</returns>
        public Entity EquipmentItemAt<TItem>(int index)
            where TItem : Item
        {
            var equipment = Entity.GetComponent<Equipment>();
            if (equipment != null)
            {
                return equipment.GetItem<TItem>(index);
            }
            return null;
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Handles a message. Updates speed and acceleration when modules
        /// change.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is CharacterStatsInvalidated)
            {
                // Get ship modules.
                var character = Entity.GetComponent<Character<AttributeType>>();
                var equipment = Entity.GetComponent<Equipment>();

                // Get the mass of the ship and return it.
                _mass = character.GetValue(AttributeType.Mass);

                // Recompute cached values.
                _maxAcceleration = character.GetValue(AttributeType.AccelerationForce) / _mass;
                _maxSpeed = float.PositiveInfinity;

                // Maximum speed.
                var friction = Entity.GetComponent<Friction>();
                if (friction != null)
                {
                    _maxSpeed = MaxAcceleration / friction.Value;
                }

                // Figure out the overall range of our radar system.
                _radarRange = character.GetValue(AttributeType.SensorRange);
            }
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
                .Write(_maxAcceleration)
                .Write(_maxSpeed)
                .Write(_mass)
                .Write(_radarRange);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _maxAcceleration = packet.ReadSingle();
            _maxSpeed = packet.ReadSingle();
            _mass = packet.ReadSingle();
            _radarRange = packet.ReadSingle();
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
        public override Component DeepCopy(Component into)
        {
            var copy = (ShipInfo)base.DeepCopy(into);

            if (copy == into)
            {
                copy._maxAcceleration = _maxAcceleration;
                copy._maxSpeed = _maxSpeed;
                copy._mass = _mass;
                copy._radarRange = _radarRange;
            }

            return copy;
        }

        #endregion
    }
}
