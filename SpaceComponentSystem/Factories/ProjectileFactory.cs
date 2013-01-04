using System;
using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Components;
using Engine.FarMath;
using Engine.Math;
using Engine.Random;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Contains data about a single projectile fired by a weapon.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public sealed class ProjectileFactory : IPacketizable, IHashable
    {
        #region Properties

        /// <summary>
        /// The texture to use to render the projectile type.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Editor("Space.Tools.DataEditor.TextureAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Category("Media")]
        [Description("The asset name of the texture to use to render this projectile.")]
        public string Model
        {
            get { return _model; }
            set { _model = value; }
        }

        /// <summary>
        /// Name of the particle effect to use for this projectile type.
        /// </summary>
        [Editor("Space.Tools.DataEditor.EffectAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Media")]
        [Description("The asset name of the particle effect to use to render this projectile.")]
        public string Effect
        {
            get { return _effect; }
            set { _effect = value; }
        }

        /// <summary>
        /// Offset of the particle effect relative to its center.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [Category("Media")]
        [Description("The offset relative to a projectile's position to emit the particle effects at.")]
        public Vector2 EffectOffset
        {
            get { return _effectOffset; }
            set { _effectOffset = value; }
        }

        /// <summary>
        /// The collision radius of the projectile.
        /// </summary>
        [Category("Logic")]
        [Description("The radius of the circle that is used for collision checks.")]
        public float CollisionRadius
        {
            get { return _collisionRadius; }
            set { _collisionRadius = value; }
        }

        /// <summary>
        /// Whether this projectile type can be hit by other projectiles (e.g.
        /// missiles may be shot down, but normal projectiles should not
        /// interact).
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(false)]
        [Category("Logic")]
        [Description("Whether this projectile can collide with other projectiles, e.g. for missiles.")]
        public bool CanBeShot
        {
            get { return _canBeShot; }
            set { _canBeShot = value; }
        }

        /// <summary>
        /// The range allowed for initial velocity of the projectile. This is
        /// rotated according to the emitters rotation. The set value applies
        /// directly if the emitter is facing to the right (i.e. is at zero
        /// rotation).
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Logic")]
        [Description("The initial velocity of the projectile, relative to its emitter.")]
        public FloatInterval InitialVelocity
        {
            get { return _initialVelocity; }
            set { _initialVelocity = value; }
        }

        /// <summary>
        /// The allowed range for the angle to the emitter used as the
        /// direction of the initial velocity.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Logic")]
        [Description("The direction of the intial velocity, relative to the projectile's emitter.")]
        public FloatInterval InitialDirection
        {
            get { return _initialDirection; }
            set { _initialDirection = value; }
        }

        /// <summary>
        /// Allowed range for the acceleration force applied to this projectile.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Logic")]
        [Description("The directed acceleration direction of the projectile, relative to its emitter, e.g. for missiles, in degrees.")]
        public FloatInterval AccelerationDirection
        {
            get { return _accelerationDirection; }
            set { _accelerationDirection = value; }
        }

        /// <summary>
        /// Allowed range for the acceleration force applied to this projectile.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Logic")]
        [Description("The strength of the acceleration of the projectile.")]
        public FloatInterval AccelerationForce
        {
            get { return _accelerationForce; }
            set { _accelerationForce = value; }
        }

        /// <summary>
        /// The friction used to slow the projectile down.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(0f)]
        [Category("Logic")]
        [Description("The friction to apply to the projectile, e.g. for missiles.")]
        public float Friction
        {
            get { return _friction; }
            set { _friction = value; }
        }

        /// <summary>
        /// The time this projectile will stay alive before disappearing,
        /// in seconds.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(5f)]
        [Category("Logic")]
        [Description("The time after which an instance of this projectile type is removed from the simulation, even if it didn't hit anything, in seconds.")]
        public float TimeToLive
        {
            get { return _timeToLive; }
            set { _timeToLive = value; }
        }

        #endregion

        #region Backing fields

        private string _model;

        private string _effect;

        private Vector2 _effectOffset;

        private float _collisionRadius;

        private bool _canBeShot;

        private FloatInterval _initialVelocity;

        private FloatInterval _initialDirection;

        private FloatInterval _accelerationDirection;

        private FloatInterval _accelerationForce;

        private float _friction;

        private float _timeToLive = 5f;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples a new projectile.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="emitter">The emitter that the projectile comes from.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="weapon">The weapon.</param>
        /// <param name="faction">The faction the projectile belongs to.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// A new projectile.
        /// </returns>
        public int SampleProjectile(IManager manager, int emitter, Vector2 offset, float angle, Weapon weapon, Factions faction, IUniformRandom random)
        {
            var entity = manager.AddEntity();

            // Get position and velocity of the emitter, to set initial position
            // and additional velocity.
            var emitterTransform = (Transform)manager.GetComponent(emitter, Transform.TypeId);
            var emitterVelocity = (Velocity)manager.GetComponent(emitter, Velocity.TypeId);

            // Rotate the offset.
            var rotation = emitterTransform.Rotation;
            var cosRadians = (float)Math.Cos(rotation);
            var sinRadians = (float)Math.Sin(rotation);

            FarPosition rotatedOffset;
            rotatedOffset.X = -offset.X * cosRadians - offset.Y * sinRadians;
            rotatedOffset.Y = -offset.X * sinRadians + offset.Y * cosRadians;

            // Set initial velocity.
            var velocity = SampleInitialDirectedVelocity(rotation + angle, random);
            var accelerationForce = SampleAccelerationForce(rotation + angle, random);

            // Adjust rotation for projectile based on its own acceleration or speed.
            if (accelerationForce != Vector2.Zero)
            {
                rotation = (float)Math.Atan2(accelerationForce.Y, accelerationForce.X);
            }
            else if (velocity != Vector2.Zero)
            {
                rotation = (float)Math.Atan2(velocity.Y, velocity.X);
            }

            // Set initial position.
            manager.AddComponent<Transform>(entity).Initialize(emitterTransform.Translation + rotatedOffset, rotation);

            // If our emitter was moving, apply its velocity.
            if (emitterVelocity != null)
            {
                velocity += emitterVelocity.Value;
            }

            if (velocity != Vector2.Zero)
            {
                manager.AddComponent<Velocity>(entity).Initialize(velocity);
            }

            // Sample an acceleration for this projectile. If there is any, create the
            // component for it, otherwise disregard.
            if (accelerationForce != Vector2.Zero)
            {
                manager.AddComponent<Acceleration>(entity).Initialize(accelerationForce);
            }

            // Apply friction to this projectile if so desired.
            if (_friction > 0)
            {
                manager.AddComponent<Friction>(entity).Initialize(_friction);
            }

            // If this projectile should vanish after some time, make it expire.
            if (_timeToLive > 0)
            {
                manager.AddComponent<Expiration>(entity).Initialize((int)(_timeToLive * Settings.TicksPerSecond));
            }

            // Mark as applying damage on collision.
            manager.AddComponent<CollisionDamage>(entity).Initialize(true);

            // Apply attributes of the weapon, modified with emitter attribute values,
            // and emitter attributes to the projectile to allow damage calculation if
            // it hits something.
            var emitterAttributes = (Attributes<AttributeType>)manager.GetComponent(emitter, Attributes<AttributeType>.TypeId);
            Attributes<AttributeType> projectileAttributes = null; // Only create if necessary.
            foreach (AttributeType attributeType in Enum.GetValues(typeof(AttributeType)))
            {
                if (attributeType == AttributeType.None)
                {
                    continue;
                }
                // Try to get weapon local attribute as base values.
                var value = 0f;
                if (weapon.Attributes.ContainsKey(attributeType))
                {
                    value = weapon.Attributes[attributeType];
                }
                // Try to modify it with emitter attributes.
                if (emitterAttributes != null)
                {
                    value = emitterAttributes.GetValue(attributeType, value);
                }
                // If we have something, copy it to the projectile.
                if (value != 0f)
                {
                    if (projectileAttributes == null)
                    {
                        projectileAttributes = manager.AddComponent<Attributes<AttributeType>>(entity);
                    }
                    projectileAttributes.SetBaseValue(attributeType, value);
                }
            }

            // Register with indexes that need to be able to find us.
            manager.AddComponent<Index>(entity).Initialize(
                CollisionSystem.IndexGroupMask | // Can collide.
                CameraSystem.IndexGroupMask | // Must be detectable by the camera.
                InterpolationSystem.IndexGroupMask, // Rendering should be interpolated.
                (int)(_collisionRadius + _collisionRadius));

            // See what we can bump into.
            var collisionGroup = faction.ToCollisionGroup();

            // Normally projectiles won't test against each other, but some may be
            // shot down, such as missiles. If that's the case, don't add us to the
            // common projectile group.
            if (!_canBeShot)
            {
                collisionGroup |= Factions.Projectiles.ToCollisionGroup();
            }

            // Give the collision some info on how to handle us.
            manager.AddComponent<CollidableSphere>(entity).Initialize(_collisionRadius, collisionGroup, true);

            // Make us visible!
            if (!string.IsNullOrWhiteSpace(_model))
            {
                manager.AddComponent<TextureRenderer>(entity).Initialize(_model);
            }

            // And add some particle effects, if so desired.
            if (!string.IsNullOrWhiteSpace(_effect))
            {
                manager.AddComponent<ParticleEffects>(entity).TryAdd(0, _effect, 1f, 0, _effectOffset, ParticleEffects.EffectGroup.None, true);
            }

            // Assign owner, to track original cause when they do something (e.g. kill something).
            manager.AddComponent<Owner>(entity).Initialize(emitter);

            return entity;
        }

        /// <summary>
        /// Samples the initial directed velocity.
        /// </summary>
        /// <param name="baseRotation">The base rotation.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled velocity.</returns>
        public Vector2 SampleInitialDirectedVelocity(float baseRotation, IUniformRandom random)
        {
            if (_initialDirection != null && _initialVelocity != null)
            {
                var velocity = Vector2.UnitX;
                var rotation = Matrix.CreateRotationZ(baseRotation + MathHelper.ToRadians(MathHelper.Lerp(_initialDirection.Low, _initialDirection.High, (random == null) ? 0 : (float)random.NextDouble())));
                Vector2.Transform(ref velocity, ref rotation, out velocity);
                velocity.Normalize();
                return velocity * ((random == null) ? _initialVelocity.Low : MathHelper.Lerp(_initialVelocity.Low, _initialVelocity.High, (float)random.NextDouble()));
            }
            else
            {
                return Vector2.Zero;
            }
        }

        /// <summary>
        /// Samples the acceleration force.
        /// </summary>
        /// <param name="baseRotation">The base rotation.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled acceleration force.</returns>
        public Vector2 SampleAccelerationForce(float baseRotation, IUniformRandom random)
        {
            if (_accelerationDirection != null && _accelerationForce != null)
            {
                var acceleration = Vector2.UnitX;
                var rotation = Matrix.CreateRotationZ(baseRotation + MathHelper.ToRadians(MathHelper.Lerp(_accelerationDirection.Low, _accelerationDirection.High, (random == null) ? 0 : (float)random.NextDouble())));
                Vector2.Transform(ref acceleration, ref rotation, out acceleration);
                acceleration.Normalize();
                return acceleration * ((random == null) ? _accelerationForce.Low : MathHelper.Lerp(_accelerationForce.Low, _accelerationForce.High, (float)random.NextDouble()));
            }
            else
            {
                return Vector2.Zero;
            }
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
            hasher.Put(_model);
            hasher.Put(_effect);
            hasher.Put(_collisionRadius);
            hasher.Put(_canBeShot);
            hasher.Put(_initialVelocity);
            hasher.Put(_initialDirection);
            hasher.Put(_accelerationDirection);
            hasher.Put(_accelerationForce);
            hasher.Put(_friction);
            hasher.Put(_timeToLive);
        }

        #endregion
    }
}
