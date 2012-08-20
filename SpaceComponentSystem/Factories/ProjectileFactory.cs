using System;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.FarMath;
using Engine.Math;
using Engine.Random;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Contains data about a single projectile fired by a weapon.
    /// </summary>
    public sealed class ProjectileFactory : IPacketizable, IHashable
    {
        #region Fields

        /// <summary>
        /// The texture to use to render the projectile type.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string Model = string.Empty;

        /// <summary>
        /// Name of the particle effect to use for this projectile type.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string Effect = string.Empty;

        /// <summary>
        /// Offset of the particle effect relative to its center.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Vector2 EffectOffset = Vector2.Zero;

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
        public bool CanBeShot;

        /// <summary>
        /// The range allowed for initial velocity of the projectile. This is
        /// rotated according to the emitters rotation. The set value applies
        /// directly if the emitter is facing to the right (i.e. is at zero
        /// rotation).
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Interval<float> InitialVelocity = Interval<float>.Zero;

        /// <summary>
        /// The allowed range for the angle to the emitter used as the
        /// direction of the initial velocity.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Interval<float> InitialDirection = Interval<float>.Zero;

        /// <summary>
        /// Allowed range for initial orientation of the projectile. As with
        /// the initial velocity, this is rotated by the emitters rotation,
        /// and the rotation applies directly if the emitter is facing to the
        /// right, i.e. its own rotation is zero.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Interval<float> InitialRotation = Interval<float>.Zero;

        /// <summary>
        /// Allowed range for the acceleration force applied to this projectile.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Interval<float> AccelerationForce = Interval<float>.Zero;

        /// <summary>
        /// The friction used to slow the projectile down.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float Friction;

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
        /// <param name="manager">The manager.</param>
        /// <param name="emitter">The emitter that the projectile comes from.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="weapon">The weapon.</param>
        /// <param name="faction">The faction the projectile belongs to.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// A new projectile.
        /// </returns>
        public int SampleProjectile(IManager manager, int emitter, Vector2 offset, Weapon weapon, Factions faction, IUniformRandom random)
        {
            var entity = manager.AddEntity();

            // Get position and velocity of the emitter, to set initial position
            // and additional velocity.
            var emitterTransform = (Transform)manager.GetComponent(emitter, Transform.TypeId);
            var emitterVelocity = (Velocity)manager.GetComponent(emitter, Velocity.TypeId);

            // Get initial rotation via emitter rotation plus sampled delta.
            var initialRotation = emitterTransform.Rotation + SampleInitialRotation(random);

            // Rotate the offset.
            var cosRadians = (float)Math.Cos(initialRotation);
            var sinRadians = (float)Math.Sin(initialRotation);

            FarPosition rotatedOffset;
            rotatedOffset.X = offset.X * cosRadians - offset.Y * sinRadians;
            rotatedOffset.Y = offset.X * sinRadians + offset.Y * cosRadians;

            // Set initial position.
            var transform = manager.AddComponent<Transform>(entity)
                .Initialize(emitterTransform.Translation + rotatedOffset, initialRotation);

            // Set initial velocity.
            var velocity = manager.AddComponent<Velocity>(entity)
                .Initialize(SampleInitialDirectedVelocity(transform.Rotation, random));

            // If our emitter was moving, apply its velocity.
            if (emitterVelocity != null)
            {
                velocity.Value += emitterVelocity.Value;
            }

            // Sample an acceleration for this projectile. If there is any, create the
            // component for it, otherwise disregard.
            var accelerationForce = SampleAccelerationForce(initialRotation, random);
            if (accelerationForce != Vector2.Zero)
            {
                manager.AddComponent<Acceleration>(entity).Initialize(accelerationForce);
            }

            // Apply friction to this projectile if so desired.
            if (Friction > 0)
            {
                manager.AddComponent<Friction>(entity).Initialize(Friction);
            }

            // If this projectile should vanish after some time, make it expire.
            if (TimeToLive > 0)
            {
                manager.AddComponent<Expiration>(entity).Initialize((int)(TimeToLive * Settings.TicksPerSecond));
            }

            // Figure out how much damage we should do, if any.
            if (Math.Abs(weapon.Damage) > 0.001f)
            {
                manager.AddComponent<CollisionDamage>(entity).Initialize(weapon.Damage);
            }

            // Register with indexes that need to be able to find us.
            manager.AddComponent<Index>(entity).Initialize(
                CollisionSystem.IndexGroupMask | // Can collide.
                TextureRenderSystem.IndexGroupMask | // Can be rendered.
                InterpolationSystem.IndexGroupMask, // Rendering should be interpolated.
                (int)(CollisionRadius + CollisionRadius));

            // See what we can bump into.
            var collisionGroup = (weapon.Damage >= 0)
                                     // We do damage, so we want to collide with enemies.
                                     ? faction.ToCollisionGroup()
                                     // We don't do damage, so we collide with friends (heal, e.g.).
                                     : faction.Inverse().ToCollisionGroup();

            // Normally projectiles won't test against each other, but some may be
            // shot down, such as missiles. If that's the case, don't add us to the
            // common projectile group.
            if (!CanBeShot)
            {
                collisionGroup |= Factions.Projectiles.ToCollisionGroup();
            }

            // Give the collision some info on how to handle us.
            manager.AddComponent<CollidableSphere>(entity).Initialize(CollisionRadius, collisionGroup);

            // Make us visible!
            if (!string.IsNullOrWhiteSpace(Model))
            {
                manager.AddComponent<TextureRenderer>(entity).Initialize(Model);
            }

            // And add some particle effects, if so desired.
            if (!string.IsNullOrWhiteSpace(Effect))
            {
                manager.AddComponent<ParticleEffects>(entity).TryAdd(Effect, EffectOffset, ParticleEffects.EffectGroup.None, true);
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
            return MathHelper.ToRadians((random == null) ? InitialRotation.Low : MathHelper.Lerp(InitialRotation.Low, InitialRotation.High, (float)random.NextDouble()));
        }

        /// <summary>
        /// Samples the initial directed velocity.
        /// </summary>
        /// <param name="baseRotation">The base rotation.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled velocity.</returns>
        private Vector2 SampleInitialDirectedVelocity(float baseRotation, IUniformRandom random)
        {
            var velocity = Vector2.UnitX;
            var rotation = Matrix.CreateRotationZ(baseRotation + MathHelper.ToRadians(MathHelper.Lerp(InitialDirection.Low, InitialDirection.High, (random == null) ? 0 : (float)random.NextDouble())));
            Vector2.Transform(ref velocity, ref rotation, out velocity);
            velocity.Normalize();
            velocity *= (random == null) ? InitialVelocity.Low : MathHelper.Lerp(InitialVelocity.Low, InitialVelocity.High, (float)random.NextDouble());
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
            acceleration *= (random == null) ? AccelerationForce.Low : MathHelper.Lerp(AccelerationForce.Low, AccelerationForce.High, (float)random.NextDouble());
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
                .Write(Model)
                .Write(Effect)
                .Write(CollisionRadius)
                .Write(CanBeShot)
                .Write(InitialVelocity.Low)
                .Write(InitialVelocity.High)
                .Write(InitialDirection.Low)
                .Write(InitialDirection.High)
                .Write(InitialRotation.Low)
                .Write(InitialRotation.High)
                .Write(AccelerationForce.Low)
                .Write(AccelerationForce.High)
                .Write(Friction)
                .Write(TimeToLive);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            Model = packet.ReadString();
            Effect = packet.ReadString();
            CollisionRadius = packet.ReadSingle();
            CanBeShot = packet.ReadBoolean();

            var low = packet.ReadSingle();
            var high = packet.ReadSingle();
            InitialVelocity = new Interval<float>(low, high);

            low = packet.ReadSingle();
            high = packet.ReadSingle();
            InitialDirection = new Interval<float>(low, high);

            low = packet.ReadSingle();
            high = packet.ReadSingle();
            InitialRotation = new Interval<float>(low, high);

            low = packet.ReadSingle();
            high = packet.ReadSingle();
            AccelerationForce = new Interval<float>(low, high);

            Friction = packet.ReadSingle();
            TimeToLive = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
            hasher.Put(Model);
            hasher.Put(Effect);
            hasher.Put(CollisionRadius);
            hasher.Put(CanBeShot);
            hasher.Put(InitialVelocity.Low);
            hasher.Put(InitialVelocity.High);
            hasher.Put(InitialDirection.Low);
            hasher.Put(InitialDirection.High);
            hasher.Put(InitialRotation.Low);
            hasher.Put(InitialRotation.High);
            hasher.Put(AccelerationForce.Low);
            hasher.Put(AccelerationForce.High);
            hasher.Put(Friction);
            hasher.Put(TimeToLive);
        }

        #endregion
    }
}
