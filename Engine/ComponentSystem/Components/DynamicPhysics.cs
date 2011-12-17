using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;

namespace Engine.ComponentSystem.Components
{
    public class DynamicPhysics : AbstractComponent
    {
        /// <summary>
        /// Current position of the object.
        /// </summary>
        public StaticPhysics StaticPhysicsComponent { get; private set; }

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

        public DynamicPhysics(StaticPhysics staticPhysicsComponent)
        {
            StaticPhysicsComponent = staticPhysicsComponent;
        }

        public override void Update(object parameterization)
        {
            // Apply rotation, keep the value in bounds.
            StaticPhysicsComponent.Rotation += Spin;
            if (StaticPhysicsComponent.Rotation < -Fixed.PI)
            {
                StaticPhysicsComponent.Rotation += Fixed.PI * 2;
            }
            else if (StaticPhysicsComponent.Rotation > Fixed.PI)
            {
                StaticPhysicsComponent.Rotation -= Fixed.PI * 2;
            }

            // Save previous velocity for stop check (due to MinVelocity).
            var previousVelocity = Velocity.Norm;

            // Apply acceleration and velocity.
            Velocity += Acceleration;
            StaticPhysicsComponent.Position += Velocity;

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

        public override bool SupportsParameterization(object parameterization)
        {
            return parameterization is PhysicsParameterization;
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
