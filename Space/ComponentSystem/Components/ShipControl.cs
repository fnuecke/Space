using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Modules;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Handles control of a single ship, represented by its relevant components.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>, <c>Spin</c>, <c>Acceleration</c>, <c>MovementProperties</c>.
    /// </para>
    /// </summary>
    public class ShipControl : AbstractComponent
    {
        #region Fields

        /// <summary>
        /// Whether we're currently stabilizing our position or not.
        /// </summary>
        public bool Stabilizing;

        /// <summary>
        /// The current acceleration of the ship.
        /// </summary>
        private Vector2 _directedAcceleration;

        /// <summary>
        /// The current target rotation (used to check if the public one
        /// changed since the last update).
        /// </summary>
        private float _targetRotation;

        /// <summary>
        /// Flag whether the rotation changed since the last update.
        /// </summary>
        private bool _targetRotationChanged;

        /// <summary>
        /// The rotation we had in the previous update.
        /// </summary>
        private float _previousRotation;

        /// <summary>
        /// Whether to shoot or not.
        /// </summary>
        private bool _shooting;

        #endregion

        #region Utility Accessors

        /// <summary>
        /// Begin or continue accelerating in the specified direction, or stop
        /// accelerating if <c>Vector2.Zero</c> is given.
        /// </summary>
        /// <param name="direction">The new directed acceleration.</param>
        public void SetAcceleration(Vector2 direction)
        {
            _directedAcceleration = direction;
            if (_directedAcceleration != Vector2.Zero)
            {
                // Make sure we have a unit vector of our direction.
                if (_directedAcceleration.LengthSquared() > 1)
                {
                    _directedAcceleration.Normalize();
                }
            }
        }

        /// <summary>
        /// Set a new target rotation for this ship.
        /// </summary>
        /// <param name="rotation">The rotation to rotate to.</param>
        public void SetTargetRotation(float rotation)
        {
            if (_targetRotation != rotation)
            {
                _targetRotation = rotation;
                _targetRotationChanged = true;
            }
        }

        /// <summary>
        /// Set whether to fire our weapons or not.
        /// </summary>
        /// <param name="shooting"></param>
        public void SetShooting(bool shooting)
        {
            _shooting = shooting;
        }

        #endregion

        #region Logic

        public override void Update(object parameterization)
        {
            // Get components we depend upon / modify.
            var transform = Entity.GetComponent<Transform>();
            var spin = Entity.GetComponent<Spin>();
            var modules = Entity.GetComponent<ModuleManager<SpaceModifier>>();

            if (modules != null)
            {
                // Get the mass of the ship.
                float mass;

                var info = Entity.GetComponent<ShipInfo>();
                if (info != null)
                {
                    mass = info.Mass;
                }
                else
                {
                    mass = modules.GetValue(SpaceModifier.Mass);
                }

                // Get our acceleration direction, based on whether we're
                // currently stabilizing or not.
                Vector2 accelerationDirection;
                float desiredAcceleration = float.MaxValue;
                if (_directedAcceleration == Vector2.Zero && Stabilizing)
                {
                    accelerationDirection = -Entity.GetComponent<Velocity>().Value;
                    desiredAcceleration = accelerationDirection.Length();
                    // If it's zero, normalize will make it {NaN, NaN}. Avoid that.
                    if (accelerationDirection != Vector2.Zero)
                    {
                        accelerationDirection.Normalize();
                    }
                }
                else
                {
                    accelerationDirection = _directedAcceleration;
                }

                // Check if we're accelerating at all.
                if (accelerationDirection != Vector2.Zero && desiredAcceleration != 0)
                {
                    // Yes, accumulate the needed energy and thruster power.
                    float energyConsumption = 0;
                    float accelerationForce = 0;
                    foreach (var thruster in modules.GetModules<Thruster>())
                    {
                        // Get the needed energy and thruster power.
                        energyConsumption += modules.GetValue(SpaceModifier.ThrusterEnergyConsumption, thruster.EnergyConsumption);
                        accelerationForce += thruster.AccelerationForce;
                    }

                    // Get the percentage of the overall thrusters' power we
                    // still need to fulfill our quota.
                    float load = System.Math.Min(1, desiredAcceleration * mass / accelerationForce);
                    // And apply it to our energy and power values.
                    energyConsumption *= load;
                    accelerationForce *= load;

                    // If we have energy, make sure we have enough.
                    var energy = Entity.GetComponent<Energy>();
                    if (energy != null)
                    {
                        if (energy.Value < energyConsumption)
                        {
                            // Not enough energy, adjust our output.
                            accelerationForce *= energy.Value / energyConsumption;
                            energyConsumption = energy.Value;
                        }

                        // Consume it.
                        energy.Value -= energyConsumption;
                    }

                    // Apply modifiers.
                    float acceleration = modules.GetValue(SpaceModifier.AccelerationForce, accelerationForce) / mass;

                    // Apply our acceleration. Use the min to our desired
                    // acceleration so we don't exceed our target.
                    Entity.GetComponent<Acceleration>().Value = accelerationDirection * System.Math.Min(desiredAcceleration, acceleration);
                }
                else
                {
                    // Update acceleration.
                    Entity.GetComponent<Acceleration>().Value = Vector2.Zero;
                }

                // Compute its rotation speed. Yes, this is actually the rotation acceleration,
                // but whatever...
                var rotation = modules.GetValue(SpaceModifier.RotationForce) / mass;

                // Update rotation / spin.
                var currentDelta = Angle.MinAngle(transform.Rotation, _targetRotation);
                var requiredSpin = (currentDelta > 0
                            ? DirectionConversion.DirectionToScalar(Directions.Right)
                            : DirectionConversion.DirectionToScalar(Directions.Left))
                            * rotation;

                // If the target rotation changed and we're either not spinning, or spinning the wrong way.
                if (_targetRotationChanged && spin != null &&
                    System.Math.Sign(spin.Value) != System.Math.Sign(requiredSpin))
                {
                    // Is it worth starting to spin, or should we just jump to the position?
                    // If the distance we need to spin is lower than what we spin in one tick,
                    // just set it.
                    if (System.Math.Abs(currentDelta) > rotation)
                    {
                        // Spin, the angle takes multiple frames to rotate.
                        spin.Value = requiredSpin;
                    }
                    else
                    {
                        // Set, only one frame (this one) required.
                        transform.Rotation = _targetRotation;
                        spin.Value = 0;
                    }
                }
            }

            // Check if we're spinning.
            if (spin != null && spin.Value != 0)
            {
                // Yes, check if we passed our target rotation. This is the case when the distance
                // to the target in the last step was smaller than our rotational speed.
                if (System.Math.Abs(Angle.MinAngle(_previousRotation, _targetRotation)) < System.Math.Abs(spin.Value))
                {
                    // Yes, set to that rotation and stop spinning.
                    transform.Rotation = _targetRotation;
                    spin.Value = 0;
                }
            }

            // Set firing state for weapon systems.
            var weapons = Entity.GetComponent<WeaponControl>();
            if (weapons != null)
            {
                weapons.Shooting = _shooting;
            }

            // Remember rotation in this update for the next.
            _previousRotation = transform.Rotation;

            // We handled this change, if there was one.
            _targetRotationChanged = false;
        }

        /// <summary>
        /// Accepts <c>InputParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Serialization / Hashing

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
                .Write(_directedAcceleration)
                .Write(Stabilizing)
                .Write(_targetRotation)
                .Write(_targetRotationChanged)
                .Write(_previousRotation)
                .Write(_shooting);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _directedAcceleration = packet.ReadVector2();
            Stabilizing = packet.ReadBoolean();
            _targetRotation = packet.ReadSingle();
            _targetRotationChanged = packet.ReadBoolean();
            _previousRotation = packet.ReadSingle();
            _shooting = packet.ReadBoolean();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(_directedAcceleration.X));
            hasher.Put(BitConverter.GetBytes(_directedAcceleration.Y));
            hasher.Put(BitConverter.GetBytes(Stabilizing));
            hasher.Put(BitConverter.GetBytes(_targetRotation));
            hasher.Put(BitConverter.GetBytes(_targetRotationChanged));
            hasher.Put(BitConverter.GetBytes(_previousRotation));
            hasher.Put(BitConverter.GetBytes(_shooting));
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
        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (ShipControl)base.DeepCopy(into);

            if (copy == into)
            {
                copy._directedAcceleration = _directedAcceleration;
                copy.Stabilizing = Stabilizing;
                copy._targetRotation = _targetRotation;
                copy._targetRotationChanged = _targetRotationChanged;
                copy._previousRotation = _previousRotation;
                copy._shooting = _shooting;
            }

            return copy;
        }

        #endregion
    }
}
