using System;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;

namespace Engine.ComponentSystem.Components
{
    public class DynamicPhysics : AbstractComponent
    {
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

        public DynamicPhysics(IEntity entity)
            : base(entity)
        {
        }

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
            // Apply rotation, keep the value in bounds.
            var sphysics = Entity.GetComponent<StaticPhysics>();
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

            // Apply acceleration and velocity.
            Velocity += Acceleration;
            sphysics.Position += Velocity;

            // Simulate friction.
            if (Damping > 0)
            {
                Velocity *= (1 - Damping);
            }

            // If we're below a certain minimum speed, just stop, otherwise
            // it'd be hard to. We only stop if we were faster than the minimum,
            // before. Otherwise we might have problems getting moving at all, if
            // the acceleration is too low.
            if (previousVelocity > MinVelocity && Velocity.Norm < MinVelocity)
            {
                Velocity = FPoint.Zero;
            }
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

        public override void Packetize(Serialization.Packet packet)
        {
            packet.Write(Velocity);
            packet.Write(Spin);
            packet.Write(Acceleration);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            Velocity = packet.ReadFPoint();
            Spin = packet.ReadFixed();
            Acceleration = packet.ReadFPoint();
        }

        public override void Hash(Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Velocity.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Velocity.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(Spin.RawValue));
            hasher.Put(BitConverter.GetBytes(Acceleration.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Acceleration.Y.RawValue));
        }
    }
}
