using System;
using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Util;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Generates planetes based on set constraints.
    /// </summary>
    [DefaultProperty("Name")]
    public sealed class PlanetFactory : IFactory
    {
        #region Properties

        /// <summary>
        /// The unique name of the object type.
        /// </summary>
        [Category("General")]
        [Description("The name of this planet, by which it can be referenced, e.g. in sun systems.")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Texture to use for generated planets.
        /// </summary>
        [Editor("Space.Tools.DataEditor.TextureAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [DefaultValue("Textures/Ships/default")]
        [Category("Media")]
        [Description("The base image to represent the ship, without any equipment.")]
        public string Texture
        {
            get { return _texture; }
            set { _texture = value; }
        }

        /// <summary>
        /// The color tint for generated planets' surface.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(0xFFFFFFFF)]
        [Category("Media")]
        [Description("The color tint to apply to the surface texture.")]
        public Color SurfaceTint
        {
            get { return _surfaceTint; }
            set { _surfaceTint = value; }
        }

        /// <summary>
        /// The atmospheric color tint for generated planets.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(0x00000000)]
        [Category("Media")]
        [Description("The color tint to apply to the atmosphere.")]
        public Color AtmosphereTint
        {
            get { return _atmosphereTint; }
            set { _atmosphereTint = value; }
        }

        /// <summary>
        /// The radius of generated planets.
        /// </summary>
        [Category("Media")]
        [Description("The radius of the planet.")]
        public FloatInterval Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        /// <summary>
        /// The mass of generated planets.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [Category("Logic")]
        [Description("The mass of the planet, which determines how strong it's gravitational pull is.")]
        public FloatInterval Mass
        {
            get { return _mass; }
            set { _mass = value; }
        }

        /// <summary>
        /// The rotation speed of generated planets.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [Category("Media")]
        [Description("The speed with which the planet revolves around its own axis.")]
        public FloatInterval RotationSpeed
        {
            get { return _rotationSpeed; }
            set { _rotationSpeed = value; }
        }

        /// <summary>
        /// The eccentricity of generated planets' orbits. A value of
        /// 0 means it's a circle, 1 means it's totally flat (a line).
        /// </summary>
        [Category("Orbit")]
        [Description("The eccentricity of the planet's orbit ellipse. A value of 1 leads to a circle, a value of 0 to a line.")]
        public FloatInterval Eccentricity
        {
            get { return _eccentricity; }
            set { _eccentricity = value; }
        }

        /// <summary>
        /// The offset from the base orbiting angle of the sun system.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [Category("Orbit")]
        [Description("The offset from the general orbit axis of all orbits around the focus, in degrees.")]
        public FloatInterval AngleOffset
        {
            get { return _angleOffset; }
            set { _angleOffset = value; }
        }

        /// <summary>
        /// The travel speed of generated planets' orbits. This will be used
        /// to determine the period, based on the orbiting radius.
        /// </summary>
        [Category("Orbit")]
        [Description("The speed at which the planet will travel along its orbit ellipse, in pixels per second.")]
        public FloatInterval TravelSpeed
        {
            get { return _travelSpeed; }
            set { _travelSpeed = value; }
        }

        #endregion

        #region Backing fields

        private string _name = "";

        private string _texture = "Textures/Planets/default";

        private Color _surfaceTint = Color.White;

        private Color _atmosphereTint = Color.Transparent;

        private FloatInterval _radius;

        private FloatInterval _mass;

        private FloatInterval _rotationSpeed;

        private FloatInterval _eccentricity;

        private FloatInterval _angleOffset;

        private FloatInterval _travelSpeed;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples the attributes to apply to the item.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="center">The entity to revolve around.</param>
        /// <param name="angle">The base angle for orbit ellipses.</param>
        /// <param name="radius">The base orbiting radius this planet will have.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        public int SamplePlanet(IManager manager, int center, float angle, float radius, IUniformRandom random)
        {
            var entity = manager.AddEntity();

            // Sample all values in advance, to allow reshuffling component creation
            // order in case we need to, without influencing the 'random' results.
            var planetRadius = SampleRadius(random);
            var rotationSpeed = SampleRotationSpeed(random);
            var mass = SampleMass(random);
            var eccentricity = SampleEccentricity(random);
            var angleOffset = SampleAngleOffset(random);
            var travelSpeed = SampleTravelSpeed(random);
            var periodOffet = (float)random.NextDouble();

            Vector2 surfaceRotation;
            surfaceRotation.X = (float)(random.NextDouble() - 0.5);
            surfaceRotation.Y = (float)(random.NextDouble() - 0.5);
            surfaceRotation.Normalize();
            surfaceRotation *= rotationSpeed;

            // Compute major radius and focus distance for an ellipse fitting
            // inside a unit circle with the focus point as its center.
            // We know that e = f/a and because of the unit circle assumption
            // with the focus as its center it must follow that a + f = 1.
            var a = 1 / (1 + eccentricity);
            var f = 1 - a;

            // We can the compute the minor radius for that very ellipse. We
            // know that f = sqrt(a * a - b * b), which can be transformed to
            // b = sqrt(a * a - f * f).
            var b = (float)Math.Sqrt(a * a - f * f);

            // We then scale the minor and major radii to fit into our actual
            // orbit radius.
            a *= radius;
            b *= radius;

            // Get period. Figure out circumference using Ramanujan's approximation.
            var circumference = MathHelper.Pi * (3 * (a + b) - (float)Math.Sqrt((3 * a + b) * (a + 3 * b)));
            var period = circumference / travelSpeed * Settings.TicksPerSecond;

            // Give it a position and rotation.
            manager.AddComponent<Transform>(entity).Initialize(((Transform)manager.GetComponent(center, Transform.TypeId)).Translation);

            // Make it rotate.
            manager.AddComponent<Spin>(entity).Initialize(MathHelper.ToRadians(rotationSpeed) / Settings.TicksPerSecond);

            // Make it move around its parent.
            manager.AddComponent<EllipsePath>(entity).Initialize(center, a, b, angle + angleOffset, period, MathHelper.TwoPi * periodOffet);

            // Make it attract stuff if it has mass.
            if (mass > 0)
            {
                manager.AddComponent<Gravitation>(entity).Initialize(Gravitation.GravitationTypes.Attractor, mass);
            }

            // Make it detectable.
            manager.AddComponent<Detectable>(entity).Initialize("Textures/Radar/Icons/radar_planet");

            // Make it visible.
            manager.AddComponent<PlanetRenderer>(entity).Initialize(_texture, _surfaceTint, planetRadius, _atmosphereTint, surfaceRotation);

            // Let it rap.
            manager.AddComponent<Sound>(entity).Initialize("Planet");

            // Add to indexes for lookup.
            manager.AddComponent<Index>(entity).Initialize(
                DetectableSystem.IndexGroupMask | // Can be detected.
                SoundSystem.IndexGroupMask | // Can make noise.
                CellSystem.CellDeathAutoRemoveIndexGroupMask | // Will be removed when out of bounds.
                TextureRenderSystem.IndexGroupMask,
                (int)(planetRadius + planetRadius));

            return entity;
        }

        /// <summary>
        /// Samples the radius of this planet.
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
        /// Samples the rotation speed of this planet.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled rotation speed.</returns>
        private float SampleRotationSpeed(IUniformRandom random)
        {
            if (_rotationSpeed != null)
            {
                return (random == null) ? _rotationSpeed.Low
                    : MathHelper.Lerp(_rotationSpeed.Low, _rotationSpeed.High, (float)random.NextDouble());
            }
            else
            {
                return 0f;
            }
        }

        /// <summary>
        /// Samples the eccentricity of this planet's orbit.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled major radius.</returns>
        private float SampleEccentricity(IUniformRandom random)
        {
            if (_eccentricity != null)
            {
                return (random == null) ? _eccentricity.Low
                    : MathHelper.Lerp(_eccentricity.Low, _eccentricity.High, (float)random.NextDouble());
            }
            else
            {
                return 0f;
            }
        }

        /// <summary>
        /// Samples the travel speed of this planet.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled travel speed.</returns>
        private float SampleTravelSpeed(IUniformRandom random)
        {
            if (_travelSpeed != null)
            {
                return (random == null) ? _travelSpeed.Low
                    : MathHelper.Lerp(_travelSpeed.Low, _travelSpeed.High, (float)random.NextDouble());
            }
            else
            {
                return 0f;
            }
        }

        /// <summary>
        /// Samples the angle offset of this planet.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled angle offset.</returns>
        private float SampleAngleOffset(IUniformRandom random)
        {
            if (_angleOffset != null)
            {
                return MathHelper.ToRadians((random == null) ? _angleOffset.Low
                    : MathHelper.Lerp(_angleOffset.Low, _angleOffset.High, (float)random.NextDouble()));
            }
            else
            {
                return 0f;
            }
        }

        /// <summary>
        /// Samples the mass of this planet.
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
