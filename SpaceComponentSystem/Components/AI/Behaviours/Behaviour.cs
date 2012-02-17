using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.Behaviours
{
    abstract class Behaviour : IPacketizable, ICopyable<Behaviour>
    {
        public enum Behaviours
        {
            Attack,
            Move,
            Patrol
        }

        protected Vector2 direction;

        public AiComponent AiComponent;

        private Vector2 EscapeDir;

        private int counter;
        protected Behaviour(AiComponent entity)
        {
            this.AiComponent = entity;
        }

        protected Behaviour()
        {
        }

        public abstract void Update();

        public void TurnToFace(Vector2 facePosition, float turnSpeed)
        {
            //var input = AiComponent.Entity.GetComponent<ShipControl>();

            //Transform transform = AiComponent.Entity.GetComponent<Transform>();
            //float x = facePosition.X - transform.Translation.X;
            //float y = facePosition.Y - transform.Translation.Y;


            //float difference = WrapAngle(desiredAngle - transform.Rotation);

            //difference = MathHelper.Clamp(difference, -turnSpeed, turnSpeed);
        }

        /// <summary>
        /// Returns the angle expressed in radians between -Pi and Pi.
        /// <param name="radians">the angle to wrap, in radians.</param>
        /// <returns>the input value expressed in radians from -Pi to Pi.</returns>
        /// </summary>
        public static float WrapAngle(float radians)
        {
            while (radians < -MathHelper.Pi)
            {
                radians += MathHelper.TwoPi;
            }
            while (radians > MathHelper.Pi)
            {
                radians -= MathHelper.TwoPi;
            }
            return radians;
        }

        protected Vector2 CalculateEscapeDirection()
        {
            //only recalculate every few seconds
            if ((counter %= 10) == 0)
            {
                // Get local player's avatar.
                var info = AiComponent.Entity.GetComponent<ShipInfo>();
                EscapeDir = new Vector2(0, 0);
                // Can't do anything without an avatar.
                if (info == null)
                {
                    return EscapeDir;
                }

                var position = info.Position;
                var mass = info.Mass;
                var index = AiComponent.Entity.Manager.SystemManager.GetSystem<IndexSystem>();
                if (index == null) return EscapeDir;
                foreach (var neighbor in index.
                   RangeQuery(ref position, 7000, Detectable.IndexGroup))
                {
                    var transform = neighbor.GetComponent<Transform>();
                    if (transform == null) continue;

                    var neighborGravitation = neighbor.GetComponent<Gravitation>();
                    var neighborCollisionDamage = neighbor.GetComponent<CollisionDamage>();
                    if (neighborCollisionDamage != null && neighborGravitation != null &&
                        (neighborGravitation.GravitationType & Gravitation.GravitationTypes.Attractor) != 0)
                    {
                        var pointOfNoReturn = (float)System.Math.Sqrt(mass * neighborGravitation.Mass / info.MaxAcceleration);
                        var direction = position - transform.Translation;
                        if (direction.Length() < pointOfNoReturn * 2)
                        {
                            if (direction != Vector2.Zero)
                                direction.Normalize();
                            EscapeDir += direction;
                        }
                    }
                }
            }
            return EscapeDir;
        }

        public virtual Packet Packetize(Packet packet)
        {
            return packet.Write(direction)
                .Write(EscapeDir);
        }

        public virtual void Depacketize(Packet packet)
        {
            direction = packet.ReadVector2();
            EscapeDir = packet.ReadVector2();
        }

        public Behaviour DeepCopy()
        {
            return DeepCopy(null);
        }

        public virtual Behaviour DeepCopy(Behaviour into)
        {
            var copy = (into != null && into.GetType() == this.GetType())
               ? into
               : (Behaviour)MemberwiseClone();

            if (copy == into)
            {
                copy.direction = direction;
                copy.EscapeDir = EscapeDir;
            }

            // Must be re-set from outside.
            AiComponent = null;

            return copy;
        }
    }
}
