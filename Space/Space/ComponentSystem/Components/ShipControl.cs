using System;
using Engine.ComponentSystem.Components;
using Engine.Math;
using Engine.Util;
using Space.ComponentSystem.Parameterizations;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Handles control of a single ship, represented by its relevant components.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>, <c>DynamicPhysics</c>, <c>MovementProperties</c>.
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
            // Only do this expensive check (see implementation) in debug mode,
            // as it should not happen that this is of an invalid type anyway.
            base.Update(parameterization);
#endif
            var p = (InputParameterization)parameterization;
            
            // Get components we depend upon / modify.
            var transform = Entity.GetComponent<Transform>();
            var spin = Entity.GetComponent<Spin>();
            var acceleration = Entity.GetComponent<Acceleration>();
            var movement = Entity.GetComponent<MovementProperties>();

            // Fire?
            Entity.GetComponent<Armament>().IsShooting = IsShooting;

            // Update acceleration.
            acceleration.Value = DirectionConversion.DirectionToFPoint(AccelerationDirection) * movement.Acceleration;

            // Update rotation / spin.
            var currentDelta = Angle.MinAngle(transform.Rotation, TargetRotation);
            var requiredSpin = (currentDelta > 0
                        ? DirectionConversion.DirectionToFixed(Directions.Right)
                        : DirectionConversion.DirectionToFixed(Directions.Left))
                        * movement.RotationSpeed;
            // If the target rotation changed and we're either not spinning, or spinning the wrong way.
            if (_targetRotationChanged && Fixed.Sign(spin.Value) != Fixed.Sign(requiredSpin))
            {
                // Is it worth starting to spin, or should we just jump to the position?
                // If the distance we need to spin is lower than what we spin in one tick,
                // just set it.
                if (Fixed.Abs(currentDelta) > movement.RotationSpeed)
                {
                    spin.Value = requiredSpin;
                }
                else
                {
                    transform.Rotation = TargetRotation;
                    spin.Value = Fixed.Zero;
                }
            }

            // Check if we're spinning.
            if (spin.Value != Fixed.Zero)
            {
                // Yes, check if we passed our target rotation.
                var previousDelta = Angle.MinAngle(_previousRotation, TargetRotation);
                //if ((currentDelta <= Fixed.Zero && previousDelta >= Fixed.Zero) ||
                //    (currentDelta >= Fixed.Zero && previousDelta <= Fixed.Zero))
                if (Fixed.Abs(previousDelta) < movement.RotationSpeed)
                {
                    //logger.Debug("prev, target, now: {0}, {1}, {2}", _previousRotation, _targetRotation, sphysics.Rotation);
                    // Yes, set to that rotation and stop spinning.
                    transform.Rotation = TargetRotation;
                    spin.Value = Fixed.Zero;
                }
            }

            // Remember rotation in this update for the next.
            _previousRotation = transform.Rotation;

            // We handled this change.
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

        public override void Packetize(Engine.Serialization.Packet packet)
        {
            packet.Write(IsShooting);
            packet.Write((byte)AccelerationDirection);
            packet.Write(_targetRotationChanged);
            packet.Write(_targetRotation);
            packet.Write(_previousRotation);
        }

        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            IsShooting = packet.ReadBoolean();
            AccelerationDirection = (Directions)packet.ReadByte();
            _targetRotationChanged = packet.ReadBoolean();
            _targetRotation = packet.ReadFixed();
            _previousRotation = packet.ReadFixed();
        }

        public override void Hash(Engine.Util.Hasher hasher)
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
