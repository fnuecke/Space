using System;
using Engine.ComponentSystem.Components;
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

        /// <summary>
        /// The dynamic physics component we're controlling.
        /// </summary>
        public DynamicPhysics DynamicPhysicsComponent { get; private set; }

        /// <summary>
        /// Movement properties that apply to the way we move.
        /// </summary>
        public MovementProperties MovementPropertiesComponent { get; private set; }

        /// <summary>
        /// The component handling the weapons systems.
        /// </summary>
        public Armament ArmamentComponent { get; private set; }

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

        public ShipControl(DynamicPhysics dynamicPhysicsComponent,
            MovementProperties movementPropertiesComponent, Armament armamentComponent)
        {
            this.DynamicPhysicsComponent = dynamicPhysicsComponent;
            this.MovementPropertiesComponent = movementPropertiesComponent;
            this.ArmamentComponent = armamentComponent;
            _previousRotation = dynamicPhysicsComponent.StaticPhysicsComponent.Rotation;
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
            // Only do this expensive check in debug mode, as this should not happen anyway.
            if (!SupportsParameterization(parameterization))
            {
                throw new System.ArgumentException("parameterization");
            }
#endif
            var p = (InputParameterization)parameterization;

            // Update acceleration.
            DynamicPhysicsComponent.Acceleration = DirectionConversion.DirectionToFPoint(AccelerationDirection) * MovementPropertiesComponent.Acceleration;

            // Update rotation / spin.
            if (DynamicPhysicsComponent.Spin != Fixed.Zero)
            {
                // Currently rotating, check if we passed our target rotation.
                var previousDelta = Angle.MinAngle(_previousRotation, TargetRotation);
                var currentDelta = Angle.MinAngle(DynamicPhysicsComponent.StaticPhysicsComponent.Rotation, TargetRotation);
                if ((currentDelta <= Fixed.Zero && previousDelta >= Fixed.Zero) ||
                    (currentDelta >= Fixed.Zero && previousDelta <= Fixed.Zero))
                {
                    // Yes, set to that rotation and stop spinning.
                    DynamicPhysicsComponent.StaticPhysicsComponent.Rotation = TargetRotation;
                    DynamicPhysicsComponent.Spin = Fixed.Zero;
                }
                _previousRotation = DynamicPhysicsComponent.StaticPhysicsComponent.Rotation;
            }
            else if (_targetRotationChanged)
            {
                _targetRotationChanged = false;
                var rotationDelta = Angle.MinAngle(DynamicPhysicsComponent.StaticPhysicsComponent.Rotation, TargetRotation);
                if (Fixed.Abs(rotationDelta) > MovementPropertiesComponent.RotationSpeed)
                {
                    DynamicPhysicsComponent.Spin = (rotationDelta > 0
                        ? DirectionConversion.DirectionToFixed(Directions.Right)
                        : DirectionConversion.DirectionToFixed(Directions.Left))
                        * MovementPropertiesComponent.RotationSpeed;
                }
                else
                {
                    DynamicPhysicsComponent.StaticPhysicsComponent.Rotation = TargetRotation;
                    DynamicPhysicsComponent.Spin = Fixed.Zero;
                    _previousRotation = DynamicPhysicsComponent.StaticPhysicsComponent.Rotation;
                }
            }

            // Fire?
            ArmamentComponent.IsShooting = IsShooting;
        }

        public override bool SupportsParameterization(object parameterization)
        {
            return parameterization is InputParameterization;
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
