using System;
using Engine.ComponentSystem.Components;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Parameterizations;
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
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        
        #region Properties
        
        /// <summary>
        /// Whether to shoot or not.
        /// </summary>
        public bool IsShooting { get; set; }

        /// <summary>
        /// The direction we're currently accelerating into.
        /// </summary>
        public Directions AccelerationDirection { get; set; }

        /// <summary>
        /// The rotation we're targeting at the moment.
        /// </summary>
        public Fixed TargetRotation
        {
            get
            {
                return _targetRotation;
            }
            set
            {
                _targetRotation = value;
                _targetRotationChanged = true;
            }
        }

        #endregion

        #region Fields
        
        /// <summary>
        /// Whether the target rotation changed since the last update.
        /// </summary>
        private bool _targetRotationChanged;

        /// <summary>
        /// The current target rotation (setting invalidation flag so we need to store this ourselves).
        /// </summary>
        private Fixed _targetRotation;

        /// <summary>
        /// The rotation we had in the previous update.
        /// </summary>
        private Fixed _previousRotation;

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
#if DEBUG
            base.Update(parameterization);
#endif
            var p = (InputParameterization)parameterization;
            
            // Get components we depend upon / modify.
            var transform = Entity.GetComponent<Transform>();
            var spin = Entity.GetComponent<Spin>();
            var modules = Entity.GetComponent<EntityModules<EntityAttributeType>>();

            // Get the mass of the ship.
            var mass = modules.GetValue(EntityAttributeType.Mass);
            // Compute its current acceleration.
            var acceleration = modules.GetValue(EntityAttributeType.AccelerationForce) / mass;
            // Compute its rotation speed. Yes, this is actually the rotation acceleration,
            // but whatever...
            var rotation = modules.GetValue(EntityAttributeType.RotationForce) / mass;

            // Set firing state for weapon systems.
            Entity.GetComponent<WeaponControl>().IsShooting = IsShooting;

            // Update acceleration.
            Entity.GetComponent<Acceleration>().Value = DirectionConversion.
                DirectionToFPoint(AccelerationDirection) * acceleration;

            // Update rotation / spin.
            var currentDelta = Angle.MinAngle(transform.Rotation, TargetRotation);
            var requiredSpin = (currentDelta > 0
                        ? DirectionConversion.DirectionToFixed(Directions.Right)
                        : DirectionConversion.DirectionToFixed(Directions.Left))
                        * rotation;
            // If the target rotation changed and we're either not spinning, or spinning the wrong way.
            if (_targetRotationChanged && Fixed.Sign(spin.Value) != Fixed.Sign(requiredSpin))
            {
                // Is it worth starting to spin, or should we just jump to the position?
                // If the distance we need to spin is lower than what we spin in one tick,
                // just set it.
                if (Fixed.Abs(currentDelta) > rotation)
                {
                    // Spin, the angle takes multiple frames to rotate.
                    spin.Value = requiredSpin;
                }
                else
                {
                    // Set, only one frame (this one) required.
                    transform.Rotation = TargetRotation;
                    spin.Value = Fixed.Zero;
                }
            }

            // Check if we're spinning.
            if (spin.Value != Fixed.Zero)
            {
                // Yes, check if we passed our target rotation. This is the case when the distance
                // to the target in the last step was smaller than our rotational speed.
                if (Fixed.Abs(Angle.MinAngle(_previousRotation, TargetRotation)) < Fixed.Abs(spin.Value))
                {
                    // Yes, set to that rotation and stop spinning.
                    transform.Rotation = TargetRotation;
                    spin.Value = Fixed.Zero;
                }
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
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType.Equals(typeof(InputParameterization));
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return packet
                .Write(IsShooting)
                .Write((byte)AccelerationDirection)
                .Write(_targetRotationChanged)
                .Write(_targetRotation)
                .Write(_previousRotation);
        }

        public override void Depacketize(Packet packet)
        {
            IsShooting = packet.ReadBoolean();
            AccelerationDirection = (Directions)packet.ReadByte();
            _targetRotationChanged = packet.ReadBoolean();
            _targetRotation = packet.ReadFixed();
            _previousRotation = packet.ReadFixed();
        }

        public override void Hash(Hasher hasher)
        {
            hasher.Put((byte)AccelerationDirection);
            hasher.Put(BitConverter.GetBytes(IsShooting));
            hasher.Put(BitConverter.GetBytes(_targetRotationChanged));
            hasher.Put(BitConverter.GetBytes(_targetRotation.RawValue));
            hasher.Put(BitConverter.GetBytes(_previousRotation.RawValue));
        }

        #endregion
    }
}
