using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Data
{
    /// <summary>
    /// Contains data about a single projectile fired by a weapon.
    /// </summary>
    public sealed class ProjectileConstraints : IPacketizable
    {
        #region Fields

        /// <summary>
        /// The texture to use to render the projectile type.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string Texture = string.Empty;

        /// <summary>
        /// Name of the particle effect to use for this projectile type.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string Effect = string.Empty;

        /// <summary>
        /// The collision radius of the projectile.
        /// </summary>
        public float CollisionRadius;

        /// <summary>
        /// Whether this projectile type can be hit by other projectiles (e.g.
        /// missiles may be shot down, but normal projectiles should not
        /// interact).
        /// </summary>
        [ContentSerializer(Optional = true)]
        public bool CanBeShot = false;

        /// <summary>
        /// The minimal initial velocity of the projectile. This is
        /// rotated according to the emitters rotation. The set value applies
        /// directly if the emitter is facing to the right (i.e. is at zero
        /// rotation).
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float MinInitialVelocity = 0;

        /// <summary>
        /// The maximal initial velocity of the projectile.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float MaxInitialVelocity = 0;

        /// <summary>
        /// The minimum angle to the emitter used as the direction of the
        /// initial velocity.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float MinInitialDirection = 0;

        /// <summary>
        /// The maximum angle to the emitter used as the direction of the
        /// initial velocity.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float MaxInitialDirection = 0;

        /// <summary>
        /// Minimum initial orientation of the projectile. As with the initial
        /// velocity, this is rotated by the emitters rotation, and the
        /// rotation applies directly if the emitter is facing to the right,
        /// i.e. its own rotation is zero.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float MinInitialRotation = 0;

        /// <summary>
        /// Minimum initial orientation of the projectile. As with the initial
        /// velocity, this is rotated by the emitters rotation, and the
        /// rotation applies directly if the emitter is facing to the right,
        /// i.e. its own rotation is zero.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float MaxInitialRotation = 0;

        /// <summary>
        /// Minimum acceleration force applied to this projectile.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float MinAccelerationForce = 0;

        /// <summary>
        /// Maximum acceleration force applied to this projectile.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float MaxAccelerationForce = 0;

        /// <summary>
        /// The friction used to slow the projectile down.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float Friction = 0;

        /// <summary>
        /// The time this projectile will stay alive before disappearing,
        /// in seconds.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float TimeToLive = 5;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples a new projectile.
        /// </summary>
        /// <param name="emitter">The emitter that the projectile comes from.</param>
        /// <param name="faction">The faction the projectile belongs to.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>A new projectile.</returns>
        public Entity SampleProjectile(Weapon emitter, Factions faction, IUniformRandom random)
        {
            var entity = new Entity();

            var emitterTransform = emitter.Entity.GetComponent<Transform>();
            var emitterVelocity = emitter.Entity.GetComponent<Velocity>();

            var initialRotation = emitterTransform.Rotation + SampleInitialRotation(random);
            var transform = new Transform(emitterTransform.Translation, initialRotation);
            entity.AddComponent(transform);

            var velocity = new Velocity(SampleInitialDirectedVelocity(transform.Rotation, random));
            if (emitterVelocity != null)
            {
                velocity.Value += emitterVelocity.Value;
            }
            entity.AddComponent(velocity);
            var accelerationForce = SampleAccelerationForce(initialRotation, random);
            if (accelerationForce != Vector2.Zero)
            {
                entity.AddComponent(new Acceleration(accelerationForce));
            }
            if (Friction > 0)
            {
                entity.AddComponent(new Friction(Friction));
            }
            if (TimeToLive > 0)
            {
                entity.AddComponent(new Expiration((int)(TimeToLive * 60f)));
            }
            if (emitter.Damage != 0)
            {
                entity.AddComponent(new CollisionDamage(emitter.Damage));
            }

            ulong collisionIndexGroup = 0;
            if (!CanBeShot)
            {
                collisionIndexGroup = Factions.Projectiles.ToCollisionIndexGroup();
            }
            if (emitter.Damage >= 0)
            {
                collisionIndexGroup |= faction.ToCollisionIndexGroup();
            }
            else if (emitter.Damage < 0)
            {
                // Negative damage = healing -> collide will all our allies.
                collisionIndexGroup |= faction.Inverse().ToCollisionIndexGroup();
            }
            entity.AddComponent(new Index(collisionIndexGroup));
            uint collisionGroup = faction.ToCollisionGroup();
            if (!CanBeShot)
            {
                collisionGroup |= Factions.Projectiles.ToCollisionGroup();
            }
            entity.AddComponent(new CollidableSphere(CollisionRadius, collisionGroup));
            if (!string.IsNullOrWhiteSpace(Texture))
            {
                entity.AddComponent(new TransformedRenderer(Texture));
            }
            if (!string.IsNullOrWhiteSpace(Effect))
            {
                // TODO
            }

            return entity;
        }

        /// <summary>
        /// Samples the initial rotation.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled rotation.</returns>
        private float SampleInitialRotation(IUniformRandom random)
        {
            return MathHelper.Lerp(MinInitialRotation, MaxInitialRotation, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the initial directed velocity.
        /// </summary>
        /// <param name="baseRotation">The base rotation.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled velocity.</returns>
        private Vector2 SampleInitialDirectedVelocity(float baseRotation, IUniformRandom random)
        {
            Vector2 velocity = Vector2.UnitX;
            Matrix rotation = Matrix.CreateRotationZ(baseRotation + MathHelper.Lerp(MinInitialDirection, MaxInitialDirection, (float)random.NextDouble()));
            Vector2.Transform(ref velocity, ref rotation, out velocity);
            velocity.Normalize();
            velocity *= MathHelper.Lerp(MinInitialVelocity, MaxInitialVelocity, (float)random.NextDouble());
            return velocity;
        }

        /// <summary>
        /// Samples the acceleration force.
        /// </summary>
        /// <param name="baseRotation">The base rotation.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled acceleration force.</returns>
        private Vector2 SampleAccelerationForce(float baseRotation, IUniformRandom random)
        {
            Vector2 acceleration = Vector2.UnitX;
            Matrix rotation = Matrix.CreateRotationZ(baseRotation);
            Vector2.Transform(ref acceleration, ref rotation, out acceleration);
            acceleration.Normalize();
            acceleration *= MathHelper.Lerp(MinAccelerationForce, MaxAccelerationForce, (float)random.NextDouble());
            return acceleration;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public Packet Packetize(Packet packet)
        {
            return packet
                .Write(Texture)
                .Write(Effect)
                .Write(CollisionRadius)
                .Write(CanBeShot)
                .Write(MinInitialVelocity)
                .Write(MaxInitialVelocity)
                .Write(MinInitialDirection)
                .Write(MaxInitialDirection)
                .Write(MinInitialRotation)
                .Write(MaxInitialRotation)
                .Write(MinAccelerationForce)
                .Write(MaxAccelerationForce)
                .Write(Friction)
                .Write(TimeToLive);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            Texture = packet.ReadString();
            Effect = packet.ReadString();
            CollisionRadius = packet.ReadSingle();
            CanBeShot = packet.ReadBoolean();
            MinInitialVelocity = packet.ReadSingle();
            MaxInitialVelocity = packet.ReadSingle();
            MinInitialDirection = packet.ReadSingle();
            MaxInitialDirection = packet.ReadSingle();
            MinInitialRotation = packet.ReadSingle();
            MaxInitialRotation = packet.ReadSingle();
            MinAccelerationForce = packet.ReadSingle();
            MaxAccelerationForce = packet.ReadSingle();
            Friction = packet.ReadSingle();
            TimeToLive = packet.ReadSingle();
        }

        #endregion
    }
}
