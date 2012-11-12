using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Components;
using Engine.FarMath;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Design;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Data;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Generates suns based on set constraints.
    /// </summary>
    [DefaultProperty("Name")]
    public sealed class SunFactory : IFactory
    {
        #region Properties

        /// <summary>
        /// The unique name of the object type.
        /// </summary>
        [Category("General")]
        [Description("The name of this sun, by which it can be referenced, e.g. in sun systems.")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Radius for generated suns.
        /// </summary>
        [Category("Media")]
        [Description("The radius of the sun.")]
        public FloatInterval Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        /// <summary>
        /// The color tint for generated planets' surface.
        /// </summary>
        [Editor("Space.Tools.DataEditor.XnaColorEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [TypeConverter(typeof(ColorConverter))]
        [ContentSerializer(Optional = true)]
        [DefaultValue(0xFFFFFFFF)]
        [Category("Media")]
        [Description("The color tint to apply to the surface texture.")]
        public Color Tint
        {
            get { return _tint; }
            set { _tint = value; }
        }

        /// <summary>
        /// Offset from cell center for generated suns.
        /// </summary>
        [Category("Media")]
        [Description("The radius of the area around a cell's center in which the sun may be placed. The actual position is randomly determined, but is guaranteed to lie in this circle.")]
        public FloatInterval OffsetRadius
        {
            get { return _offsetRadius; }
            set { _offsetRadius = value; }
        }

        /// <summary>
        /// Mass of generated suns.
        /// </summary>
        [Category("Logic")]
        [Description("The mass of the sun, which determines how strong it's gravitational pull is.")]
        public FloatInterval Mass
        {
            get { return _mass; }
            set { _mass = value; }
        }

        #endregion

        #region Backing fields

        private string _name = "";

        private FloatInterval _radius = FloatInterval.Zero;

        private Color _tint = Color.White;

        private FloatInterval _offsetRadius = FloatInterval.Zero;

        private FloatInterval _mass = FloatInterval.Zero;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples the attributes to apply to the item.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="cellCenter">The center of the cell the sun will be inserted in.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        public int Sample(IManager manager, FarPosition cellCenter, IUniformRandom random)
        {
            var entity = manager.AddEntity();

            // Sample all values in advance, to allow reshuffling component creation
            // order in case we need to, without influencing the 'random' results.
            var radius = SampleRadius(random);
            var offset = SampleOffset(random);
            var mass = SampleMass(random);

            var surfaceRotation = -Vector2.UnitX;
            if (random != null)
            {
                surfaceRotation.X = (float)(random.NextDouble() - 0.5) * 2;
                surfaceRotation.Y = (float)(random.NextDouble() - 0.5) * 2;
                surfaceRotation.Normalize();
            }

            var primaryTurbulenceRotation = Vector2.UnitX;
            if (random != null)
            {
                primaryTurbulenceRotation.X = (float)(random.NextDouble() - 0.5) * 2;
                primaryTurbulenceRotation.Y = (float)(random.NextDouble() - 0.5) * 2;
                primaryTurbulenceRotation.Normalize();
            }

            var secondaryTurbulenceRotation = Vector2.UnitY;
            if (random != null)
            {
                secondaryTurbulenceRotation.X = (float)(random.NextDouble() - 0.5) * 2;
                secondaryTurbulenceRotation.Y = (float)(random.NextDouble() - 0.5) * 2;
                secondaryTurbulenceRotation.Normalize();
            }

            manager.AddComponent<Transform>(entity).Initialize(offset + cellCenter);

            // Make it attract stuff if it has mass.
            if (mass > 0)
            {
                manager.AddComponent<Gravitation>(entity).Initialize(Gravitation.GravitationTypes.Attractor, mass);
            }

            // Make it collidable.
            manager.AddComponent<CollidableSphere>(entity).Initialize(radius, Factions.Nature.ToCollisionGroup());

            // Damage stuff that touches a sun.
            manager.AddComponent<CollisionDamage>(entity).Initialize(false);
            var attributes = manager.AddComponent<Attributes<AttributeType>>(entity);
            attributes.SetBaseValue(AttributeType.AttackBurnMinDamage, 1000);
            attributes.SetBaseValue(AttributeType.AttackBurnMaxDamage, 2000);
            attributes.SetBaseValue(AttributeType.AttackBurnChance, 1);
            attributes.SetBaseValue(AttributeType.AttackBurnMinDuration, 5);
            attributes.SetBaseValue(AttributeType.AttackBurnMaxDuration, 5);

            // Make it detectable.
            manager.AddComponent<Detectable>(entity).Initialize("Textures/Radar/Icons/radar_sun");

            // Make it glow.
            manager.AddComponent<SunRenderer>(entity).Initialize(radius * 0.95f, surfaceRotation, primaryTurbulenceRotation, secondaryTurbulenceRotation, _tint);

            // Make it go whoooosh.
            manager.AddComponent<Sound>(entity).Initialize("Sun");

            // Add to indexes for lookup.
            manager.AddComponent<Index>(entity).Initialize(
                CollisionSystem.IndexGroupMask | // Can be bumped into.
                DetectableSystem.IndexGroupMask | // Can be detected.
                SoundSystem.IndexGroupMask | // Can make noise.
                CellSystem.CellDeathAutoRemoveIndexGroupMask | // Will be removed when out of bounds.
                TextureRenderSystem.IndexGroupMask,
                (int)(radius + radius));

            return entity;
        }

        /// <summary>
        /// Samples the radius of this sun.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled radius.</returns>
        private float SampleRadius(IUniformRandom random)
        {
            if (_radius != null)
            {
                return (random == null) ? _radius.Low
                    : MathHelper.Lerp(_radius.Low, _radius.High, (float)random.NextDouble());
            }
            else
            {
                return 0f;
            }
        }

        /// <summary>
        /// Samples the offset of this sun.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled offset.</returns>
        private Vector2 SampleOffset(IUniformRandom random)
        {
            if (_offsetRadius != null && random != null)
            {
                Vector2 offset;
                offset.X = (float)(random.NextDouble() - 0.5);
                offset.Y = (float)(random.NextDouble() - 0.5);
                offset.Normalize();
                offset *= MathHelper.Lerp(_offsetRadius.Low, _offsetRadius.High, (float)random.NextDouble());
                return offset;
            }
            else
            {
                return Vector2.Zero;
            }
        }

        /// <summary>
        /// Samples the mass of this sun.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled mass.</returns>
        private float SampleMass(IUniformRandom random)
        {
            if (_mass != null)
            {
                return (random == null) ? _mass.Low
                    : MathHelper.Lerp(_mass.Low, _mass.High, (float)random.NextDouble());
            }
            else
            {
                return 0f;
            }
        }

        #endregion
    }
}
