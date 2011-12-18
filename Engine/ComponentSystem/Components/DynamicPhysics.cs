using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents dynamic physical properties such as speed and acceleration.
    /// 
    /// <para>
    /// Requires: <c>StaticPhysics</c>.
    /// </para>
    /// </summary>
    public class DynamicPhysics : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The directed speed of the object.
        /// </summary>
        public FPoint Velocity { get; set; }

        /// <summary>
        /// The current rotation speed of the object.
        /// </summary>
        public Fixed Spin { get; set; }

        /// <summary>
        /// The directed acceleration of the object.
        /// </summary>
        public FPoint Acceleration { get; set; }

        /// <summary>
        /// The damping to apply to this objects movement (simulates friction).
        /// </summary>
        public Fixed Damping { get; set; }

        /// <summary>
        /// Minimum velocity of an object before it is stopped (avoids jitter
        /// for very low velocity, due to which objects with damping never
        /// stop, even though they should).
        /// </summary>
        public Fixed MinVelocity { get; set; }

        #endregion

        #region Logic

        /// <summary>
        /// Updates an objects position/rotation and speed according to its acceleration
        /// and speeds.
        /// </summary>
        /// <param name="parameterization">the parameterization to use.</param>
        public override void Update(object parameterization)
        {
#if DEBUG
            // Only do this expensive check (see implementation) in debug mode,
            // as it should not happen that this is of an invalid type anyway.
            base.Update(parameterization);
#endif
            var sphysics = Entity.GetComponent<StaticPhysics>();

            // Apply rotation, keep the value in bounds.
            sphysics.Rotation += Spin;
            if (sphysics.Rotation < -Fixed.PI)
            {
                sphysics.Rotation += Fixed.PI * 2;
            }
            else if (sphysics.Rotation > Fixed.PI)
            {
                sphysics.Rotation -= Fixed.PI * 2;
            }

            // Save previous velocity for stop check (due to MinVelocity).
            var previousVelocity = Velocity.Norm;

            // Apply acceleration and damping.
            Velocity += Acceleration;
            if (Damping > (Fixed)0)
            {
                Velocity = Velocity * ((Fixed)1 - Damping);
            }

            // If we're below a certain minimum speed, just stop, otherwise
            // it'd be hard to. We only stop if we were faster than the minimum,
            // before. Otherwise we might have problems getting moving at all, if
            // the acceleration is too low.
            if (previousVelocity > Velocity.Norm && Velocity.Norm < MinVelocity)
            {
                Velocity = FPoint.Zero;
            }

            // Apply velocity.
            sphysics.Position += Velocity;
        }

        /// <summary>
        /// Accepts <c>PhysicsParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType.Equals(typeof(PhysicsParameterization));
        }

        #endregion

        #region Serialization / Hashing

        public override void Packetize(Serialization.Packet packet)
        {
            packet.Write(Velocity);
            packet.Write(Spin);
            packet.Write(Acceleration);
            packet.Write(Damping);
            packet.Write(MinVelocity);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            Velocity = packet.ReadFPoint();
            Spin = packet.ReadFixed();
            Acceleration = packet.ReadFPoint();
            Damping = packet.ReadFixed();
            MinVelocity = packet.ReadFixed();
        }

        public override void Hash(Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Velocity.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Velocity.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(Spin.RawValue));
            hasher.Put(BitConverter.GetBytes(Acceleration.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Acceleration.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(Damping.RawValue));
            hasher.Put(BitConverter.GetBytes(MinVelocity.RawValue));
        }

        #endregion
    }
}
