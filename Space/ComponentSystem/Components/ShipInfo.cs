using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;
using Space.Data;
using Space.Data.Modules;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// This component has no actual functionality, but serves merely as a
    /// facade to centralize common tasks for retrieving information on
    /// ships.
    /// </summary>
    public sealed class ShipInfo : AbstractComponent
    {
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
                return health.Value;
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
                return health.MaxValue;
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
                return health.Value / health.MaxValue;
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
                return energy.Value;
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
                return energy.MaxValue;
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
                return energy.Value / energy.MaxValue;
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
                return transform.Translation;
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
                return transform.Rotation;
            }
        }

        /// <summary>
        /// Get whether the ship is currently accelerating.
        /// </summary>
        public bool IsAccelerating
        {
            get
            {
                var acceleartion = Entity.GetComponent<Acceleration>();
                return acceleartion.Value != Vector2.Zero;
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
                return velocity.Value.Length();
            }
        }

        /// <summary>
        /// Get the maximum speed of the ship.
        /// </summary>
        /// <remarks>
        /// Performance note: store this value if you use it more than once.
        /// </remarks>
        public float MaxSpeed
        {
            get
            {
                // Apply modifiers and return.
                return MaxAcceleration / Entity.GetComponent<Friction>().Value;
            }
        }

        /// <summary>
        /// Get the maximum acceleration this ship is capable of.
        /// </summary>
        /// <remarks>
        /// Performance note: store this value if you use it more than once.
        /// </remarks>
        public float MaxAcceleration
        {
            get
            {
                // Get ship modules.
                var modules = Entity.GetComponent<EntityModules<EntityAttributeType>>();

                // Get acceleration from thrusters.
                float maxAcceleration = 0;
                foreach (var thruster in modules.GetModules<ThrusterModule>())
                {
                    maxAcceleration += thruster.AccelerationForce;
                }

                // Divide by mass and return.
                return maxAcceleration / modules.GetValue(EntityAttributeType.Mass);
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
                return spin.Value;
            }
        }

        #endregion

        #region Modules / Attributes

        /// <summary>
        /// Gets the overall mass of this ship.
        /// </summary>
        /// <remarks>
        /// Performance note: store this value if you use it more than once.
        /// </remarks>
        public float Mass
        {
            get
            {
                // Get ship modules.
                var modules = Entity.GetComponent<EntityModules<EntityAttributeType>>();

                // Get the mass of the ship and return it.
                return modules.GetValue(EntityAttributeType.Mass);
            }
        }

        /// <summary>
        /// Get the ship's overall radar range.
        /// </summary>
        /// <remarks>
        /// Performance note: store this value if you use it more than once.
        /// </remarks>
        public float RadarRange
        {
            get
            {
                // Get ship modules.
                var modules = Entity.GetComponent<EntityModules<EntityAttributeType>>();

                // Figure out the overall range of our radar system.
                float radarRange = 0;
                foreach (var sensor in modules.GetModules<SensorModule>())
                {
                    // TODO in case we're adding sensor types (anti-cloaking, ...) check this one's actually a radar.
                    radarRange += sensor.Range;
                }

                // Apply modifiers, compute max speed and return.
                return modules.GetValue(EntityAttributeType.SensorRange, radarRange);
            }
        }

        #endregion
    }
}
