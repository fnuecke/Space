using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.Data;
using Space.Data.Modules;

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
        /// The rotation we're targeting at the moment.
        /// </summary>
        public float TargetRotation;

        /// <summary>
        /// Whether to shoot or not.
        /// </summary>
        public bool Shooting;

        /// <summary>
        /// The direction we're currently accelerating into.
        /// </summary>
        public Directions AccelerationDirection;

        /// <summary>
        /// The current target rotation (used to check if the public one
        /// changed since the last update).
        /// </summary>
        private float _targetRotation;

        /// <summary>
        /// The rotation we had in the previous update.
        /// </summary>
        private float _previousRotation;

        #endregion

        #region Utility Accessors

        /// <summary>
        /// Begin or continue accelerating in the specified direction.
        /// </summary>
        /// <param name="direction">The direction to accelerate into.</param>
        public void Accelerate(Directions direction)
        {
            AccelerationDirection |= direction;
        }

        /// <summary>
        /// Stop accelerating in the specified direction.
        /// </summary>
        /// <param name="direction">The direction in which to stop accelerating.</param>
        public void StopAccelerate(Directions direction)
        {
            AccelerationDirection &= ~direction;
        }

        #endregion

        #region Logic

        public override void Update(object parameterization)
        {
            // Get components we depend upon / modify.
            var transform = Entity.GetComponent<Transform>();
            var spin = Entity.GetComponent<Spin>();
            var modules = Entity.GetComponent<EntityModules<EntityAttributeType>>();

            if (modules != null)
            {
                // Get the mass of the ship.
                float mass = modules.GetValue(EntityAttributeType.Mass);

                // Get the direction we want to accelerate into.
                var directedAcceleration = DirectionConversion.DirectionToVector(AccelerationDirection);
                float desiredAcceleration = float.MaxValue;
                if (directedAcceleration == Vector2.Zero && AccelerationDirection != Directions.None)
                {
                    // If we aren't accelerating but somethings active we're
                    // stabilizing.
                    directedAcceleration = -Entity.GetComponent<Velocity>().Value;
                    desiredAcceleration = directedAcceleration.Length();
                    // If it's zero, normalize will make it {NaN, NaN}. Avoid that.
                    if (directedAcceleration != Vector2.Zero)
                    {
                        directedAcceleration.Normalize();
                    }
                }

                // Check if we're accelerating at all.
                if (directedAcceleration != Vector2.Zero && desiredAcceleration != 0)
                {
                    // Yes, accumulate the needed energy and thruster power.
                    float energyConsumption = 0;
                    float accelerationForce = 0;
                    foreach (var thruster in modules.GetModules<ThrusterModule>())
                    {
                        // Get the needed energy and thruster power.
                        energyConsumption += modules.GetValue(EntityAttributeType.ThrusterEnergyConsumption, thruster.EnergyConsumption);
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
                    float acceleration = modules.GetValue(EntityAttributeType.AccelerationForce, accelerationForce) / mass;

                    // Apply our acceleration. Use the min to our desired
                    // acceleration so we don't exceed our target.
                    Entity.GetComponent<Acceleration>().Value = directedAcceleration * System.Math.Min(desiredAcceleration, acceleration);
                }
                else
                {
                    // Update acceleration.
                    Entity.GetComponent<Acceleration>().Value = Vector2.Zero;
                }

                // Compute its rotation speed. Yes, this is actually the rotation acceleration,
                // but whatever...
                var rotation = modules.GetValue(EntityAttributeType.RotationForce) / mass;

                // Update rotation / spin.
                var currentDelta = Angle.MinAngle(transform.Rotation, TargetRotation);
                var requiredSpin = (currentDelta > 0
                            ? DirectionConversion.DirectionToScalar(Directions.Right)
                            : DirectionConversion.DirectionToScalar(Directions.Left))
                            * rotation;

                // If the target rotation changed and we're either not spinning, or spinning the wrong way.
                if (spin != null && (_targetRotation != TargetRotation) &&
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
                        transform.Rotation = TargetRotation;
                        spin.Value = 0;
                    }
                }
            }

            // Check if we're spinning.
            if (spin != null && spin.Value != 0)
            {
                // Yes, check if we passed our target rotation. This is the case when the distance
                // to the target in the last step was smaller than our rotational speed.
                if (System.Math.Abs(Angle.MinAngle(_previousRotation, TargetRotation)) < System.Math.Abs(spin.Value))
                {
                    // Yes, set to that rotation and stop spinning.
                    transform.Rotation = TargetRotation;
                    spin.Value = 0;
                }
            }

            // Set firing state for weapon systems.
            var weapons = Entity.GetComponent<WeaponControl>();
            if (weapons != null)
            {
                weapons.Shooting = Shooting;
            }

            // Remember rotation in this update for the next.
            _previousRotation = transform.Rotation;

            // We handled this change, if there was one.
            _targetRotation = TargetRotation;
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
                .Write(TargetRotation)
                .Write(Shooting)
                .Write((byte)AccelerationDirection)
                .Write(_targetRotation)
                .Write(_previousRotation);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            TargetRotation = packet.ReadSingle();
            Shooting = packet.ReadBoolean();
            AccelerationDirection = (Directions)packet.ReadByte();
            _targetRotation = packet.ReadSingle();
            _previousRotation = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);
            
            hasher.Put(BitConverter.GetBytes(TargetRotation));
            hasher.Put((byte)AccelerationDirection);
            hasher.Put(BitConverter.GetBytes(Shooting));
            hasher.Put(BitConverter.GetBytes(_targetRotation));
            hasher.Put(BitConverter.GetBytes(_previousRotation));
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
                copy.TargetRotation = TargetRotation;
                copy.Shooting = Shooting;
                copy.AccelerationDirection = AccelerationDirection;
                copy._targetRotation = _targetRotation;
                copy._previousRotation = _previousRotation;
            }

            return copy;
        }

        #endregion
    }
}
