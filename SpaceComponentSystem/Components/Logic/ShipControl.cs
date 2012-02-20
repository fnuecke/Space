using System;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Handles control of a single ship, represented by its relevant components.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>, <c>Spin</c>, <c>Acceleration</c>, <c>MovementProperties</c>.
    /// </para>
    /// </summary>
    public class ShipControl : Component
    {
        #region Fields

        /// <summary>
        /// Whether we're currently stabilizing our position or not.
        /// </summary>
        public bool Stabilizing;

        /// <summary>
        /// Whether to shoot or not.
        /// </summary>
        public bool Shooting;

        /// <summary>
        /// The current acceleration of the ship.
        /// </summary>
        internal Vector2 DirectedAcceleration;

        /// <summary>
        /// The current target rotation (used to check if the public one
        /// changed since the last update).
        /// </summary>
        internal float TargetRotation;

        /// <summary>
        /// Flag whether the rotation changed since the last update.
        /// </summary>
        internal bool TargetRotationChanged;

        /// <summary>
        /// The rotation we had in the previous update.
        /// </summary>
        internal float PreviousRotation;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override void Initialize(Component other)
        {
            base.Initialize(other);

            var otherControl = (ShipControl)other;
            DirectedAcceleration = otherControl.DirectedAcceleration;
            Stabilizing = otherControl.Stabilizing;
            TargetRotation = otherControl.TargetRotation;
            TargetRotationChanged = otherControl.TargetRotationChanged;
            PreviousRotation = otherControl.PreviousRotation;
            Shooting = otherControl.Shooting;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Stabilizing = false;
            Shooting = false;
            DirectedAcceleration = Vector2.Zero;
            TargetRotation = 0;
            TargetRotationChanged = false;
            PreviousRotation = 0;
        }

        #endregion

        #region Utility Accessors

        /// <summary>
        /// Begin or continue accelerating in the specified direction, or stop
        /// accelerating if <c>Vector2.Zero</c> is given.
        /// </summary>
        /// <param name="direction">The new directed acceleration.</param>
        public void SetAcceleration(Vector2 direction)
        {
            DirectedAcceleration = direction;
            if (DirectedAcceleration != Vector2.Zero)
            {
                // Make sure we have a unit vector of our direction.
                if (DirectedAcceleration.LengthSquared() > 1)
                {
                    DirectedAcceleration.Normalize();
                }
            }
        }

        /// <summary>
        /// Set a new target rotation for this ship.
        /// </summary>
        /// <param name="rotation">The rotation to rotate to.</param>
        public void SetTargetRotation(float rotation)
        {
            if (Math.Abs(TargetRotation - rotation) > 0.001f)
            {
                TargetRotation = rotation;
                TargetRotationChanged = true;
            }
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
                .Write(DirectedAcceleration)
                .Write(Stabilizing)
                .Write(TargetRotation)
                .Write(TargetRotationChanged)
                .Write(PreviousRotation)
                .Write(Shooting);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            DirectedAcceleration = packet.ReadVector2();
            Stabilizing = packet.ReadBoolean();
            TargetRotation = packet.ReadSingle();
            TargetRotationChanged = packet.ReadBoolean();
            PreviousRotation = packet.ReadSingle();
            Shooting = packet.ReadBoolean();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(DirectedAcceleration.X));
            hasher.Put(BitConverter.GetBytes(DirectedAcceleration.Y));
            hasher.Put(BitConverter.GetBytes(Stabilizing));
            hasher.Put(BitConverter.GetBytes(TargetRotation));
            hasher.Put(BitConverter.GetBytes(TargetRotationChanged));
            hasher.Put(BitConverter.GetBytes(PreviousRotation));
            hasher.Put(BitConverter.GetBytes(Shooting));
        }

        #endregion
    }
}
