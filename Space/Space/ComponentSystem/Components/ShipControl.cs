using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.Math;
using Engine.Util;
using Space.ComponentSystem.Parameterizations;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Handles control of a single ship, represented by its relevant components.
    /// </summary>
    public class ShipControl : AbstractComponent
    {
        #region Properties
        
        /// <summary>
        /// The direction we're currently accelerating into.
        /// </summary>
        public Directions AccelerationDirection { get; set; }

        /// <summary>
        /// The rotation we're targeting at the moment.
        /// </summary>
        public Fixed TargetRotation { get { return _targetRotation; } set { _targetRotation = value; _targetRotationChanged = true; } }

        /// <summary>
        /// Whether to shoot or not.
        /// </summary>
        public bool IsShooting { get; set; }

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

        public ShipControl(IEntity entity)
            : base(entity)
        {
            _previousRotation = Entity.GetComponent<StaticPhysics>().Rotation;
        }

        public void Accelerate(Directions direction)
        {
            AccelerationDirection |= direction;
        }

        public void StopAccelerate(Directions direction)
        {
            AccelerationDirection &= ~direction;
        }

        public override void Update(object parameterization)
        {
#if DEBUG
            // Only do this expensive check (see implementation) in debug mode,
            // as it should not happen that this is of an invalid type anyway.
            base.Update(parameterization);
#endif
            var p = (InputParameterization)parameterization;

            var sphysics = Entity.GetComponent<StaticPhysics>();
            var dphysics = Entity.GetComponent<DynamicPhysics>();
            var movement = Entity.GetComponent<MovementProperties>();

            // Update acceleration.
            dphysics.Acceleration = DirectionConversion.DirectionToFPoint(AccelerationDirection) * movement.Acceleration;

            // Update rotation / spin.
            if (dphysics.Spin != Fixed.Zero)
            {
                // Currently rotating, check if we passed our target rotation.
                var previousDelta = Angle.MinAngle(_previousRotation, TargetRotation);
                var currentDelta = Angle.MinAngle(sphysics.Rotation, TargetRotation);
                if ((currentDelta <= Fixed.Zero && previousDelta >= Fixed.Zero) ||
                    (currentDelta >= Fixed.Zero && previousDelta <= Fixed.Zero))
                {
                    // Yes, set to that rotation and stop spinning.
                    sphysics.Rotation = TargetRotation;
                    dphysics.Spin = Fixed.Zero;
                }
                _previousRotation = sphysics.Rotation;
            }
            else if (_targetRotationChanged)
            {
                _targetRotationChanged = false;
                var rotationDelta = Angle.MinAngle(sphysics.Rotation, TargetRotation);
                if (Fixed.Abs(rotationDelta) > movement.RotationSpeed)
                {
                    dphysics.Spin = (rotationDelta > 0
                        ? DirectionConversion.DirectionToFixed(Directions.Right)
                        : DirectionConversion.DirectionToFixed(Directions.Left))
                        * movement.RotationSpeed;
                }
                else
                {
                    sphysics.Rotation = TargetRotation;
                    dphysics.Spin = Fixed.Zero;
                    _previousRotation = sphysics.Rotation;
                }
            }

            // Fire?
            Entity.GetComponent<Armament>().IsShooting = IsShooting;
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

        public override void Packetize(Engine.Serialization.Packet packet)
        {
            packet.Write((byte)AccelerationDirection);
            packet.Write(IsShooting);
            packet.Write(_targetRotationChanged);
            packet.Write(_targetRotation);
            packet.Write(_previousRotation);
        }

        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            AccelerationDirection = (Directions)packet.ReadByte();
            IsShooting = packet.ReadBoolean();
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
    }
}
