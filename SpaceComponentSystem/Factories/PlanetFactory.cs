using System;
using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Design;
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
        [DefaultValue("Textures/Planets/default")]
        [Category("Surface")]
        [Description("The base image to render the planet surface.")]
        public string Albedo
        {
            get { return _albedo; }
            set { _albedo = value; }
        }

        /// <summary>
        /// Texture to use for surface normals.
        /// </summary>
        [Editor("Space.Tools.DataEditor.TextureAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Surface")]
        [Description("The texture with surface normal information, for surface structure based on light position.")]
        public string Normals
        {
            get { return _normals; }
            set { _normals = value; }
        }

        /// <summary>
        /// Texture to use for surface specularity.
        /// </summary>
        [Editor("Space.Tools.DataEditor.TextureAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Surface")]
        [Description("The texture with surface specularity information, for specular reflections on the surface, e.g. for oceans.")]
        public string Specular
        {
            get { return _specular; }
            set { _specular = value; }
        }

        /// <summary>
        /// Texture to use for surface lights.
        /// </summary>
        [Editor("Space.Tools.DataEditor.TextureAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Surface")]
        [Description("The texture with surface light information, for surface light in the 'night' area.")]
        public string Lights
        {
            get { return _lights; }
            set { _lights = value; }
        }

        /// <summary>
        /// Texture to use for clouds.
        /// </summary>
        [Editor("Space.Tools.DataEditor.TextureAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Surface")]
        [Description("The texture with clouds to render on top of the surface.")]
        public string Clouds
        {
            get { return _clouds; }
            set { _clouds = value; }
        }

        /// <summary>
        /// The color tint for generated planets' surface.
        /// </summary>
        [Editor("Space.Tools.DataEditor.XnaColorEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [TypeConverter(typeof(ColorConverter))]
        [ContentSerializer(Optional = true)]
        [DefaultValue(0xFFFFFFFF)]
        [Category("Surface")]
        [Description("The color tint to apply to the surface texture.")]
        public Color SurfaceTint
        {
            get { return _surfaceTint; }
            set { _surfaceTint = value; }
        }

        /// <summary>
        /// Specular lighting alpha.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(1f)]
        [Category("Surface")]
        [Description("The alpha (intensity) of the specular highlight.")]
        public float SpecularAlpha
        {
            get { return _specularAlpha; }
            set { _specularAlpha = value; }
        }

        /// <summary>
        /// Specular lighting exponent.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(10f)]
        [Category("Surface")]
        [Description("The exponent used for specular lighting. Smaller values mean a smaller, brighter highlight.")]
        public float SpecularExponent
        {
            get { return _specularExponent; }
            set { _specularExponent = value; }
        }

        /// <summary>
        /// Specular lighting exponent.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(1f)]
        [Category("Surface")]
        [Description("The offset used for specular lighting. Higher values result in a highlight closer to the center.")]
        public float SpecularOffset
        {
            get { return _specularOffset; }
            set { _specularOffset = value; }
        }

        /// <summary>
        /// The atmospheric color tint for generated planets.
        /// </summary>
        [Editor("Space.Tools.DataEditor.XnaColorEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [TypeConverter(typeof(ColorConverter))]
        [ContentSerializer(Optional = true)]
        [DefaultValue(0x00000000)]
        [Category("Atmosphere")]
        [Description("The color tint to apply to the atmosphere.")]
        public Color AtmosphereTint
        {
            get { return _atmosphereTint; }
            set { _atmosphereTint = value; }
        }

        /// <summary>
        /// Relative inner atmosphere area.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(0.4f)]
        [Category("Atmosphere")]
        [Description("The relative inner area covered by the atmosphere.")]
        public float AtmosphereInner
        {
            get { return _atmosphereInner; }
            set { _atmosphereInner = value; }
        }

        /// <summary>
        /// Relative outer atmosphere area.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(0.1f)]
        [Category("Atmosphere")]
        [Description("The relative outer area covered by the atmosphere.")]
        public float AtmosphereOuter
        {
            get { return _atmosphereOuter; }
            set { _atmosphereOuter = value; }
        }

        /// <summary>
        /// Relative inner atmosphere alpha.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(0.85f)]
        [Category("Atmosphere")]
        [Description("The alpha multiplier for the inner atmosphere (surface visible underneath).")]
        public float AtmosphereInnerAlpha
        {
            get { return _atmosphereInnerAlpha; }
            set { _atmosphereInnerAlpha = value; }
        }

        /// <summary>
        /// Relative outer atmosphere alpha.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(1f)]
        [Category("Atmosphere")]
        [Description("The alpha multiplier for the outer atmosphere (no surface visible underneath).")]
        public float AtmosphereOuterAlpha
        {
            get { return _atmosphereOuterAlpha; }
            set { _atmosphereOuterAlpha = value; }
        }

        /// <summary>
        /// The radius of generated planets.
        /// </summary>
        [Category("Logic")]
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
        [Category("Logic")]
        [Description("The speed with which the planet revolves around its own axis, in pixels per second.")]
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

        private string _albedo = "Textures/Planets/default";

        private string _normals;

        private string _specular;

        private string _lights;

        private string _clouds;

        private Color _surfaceTint = Color.White;

        private float _specularAlpha = 1;

        private float _specularExponent = 10;

        private float _specularOffset = 1;

        private Color _atmosphereTint = Color.Transparent;

        private float _atmosphereInner = 0.4f;

        private float _atmosphereOuter = 0.1f;

        private float _atmosphereInnerAlpha = 0.85f;

        private float _atmosphereOuterAlpha = 1f;

        private FloatInterval _radius = FloatInterval.Zero;

        private FloatInterval _mass = FloatInterval.Zero;

        private FloatInterval _rotationSpeed = FloatInterval.Zero;

        private FloatInterval _eccentricity = FloatInterval.Zero;

        private FloatInterval _angleOffset = FloatInterval.Zero;

        private FloatInterval _travelSpeed = FloatInterval.Zero;

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
        public int Sample(IManager manager, int center, float angle, float radius, IUniformRandom random)
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
            var periodOffet = random != null ? (float)random.NextDouble() : 0;

            var surfaceRotation = random != null ? (Math.Sign(random.NextDouble() - 0.5) * rotationSpeed) : rotationSpeed;

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
            if (center > 0)
            {
                manager.AddComponent<Transform>(entity).Initialize(((Transform)manager.GetComponent(center, Transform.TypeId)).Translation);
            }
            else
            {
                manager.AddComponent<Transform>(entity);
            }

            // Make it rotate.
            manager.AddComponent<Spin>(entity).Initialize(MathHelper.ToRadians(rotationSpeed) / Settings.TicksPerSecond);

            // Make it move around its parent.
            if (center > 0)
            {
                manager.AddComponent<EllipsePath>(entity).Initialize(center, a, b, angle + angleOffset, period, MathHelper.TwoPi * periodOffet);
            }

            // Make it attract stuff if it has mass.
            if (mass > 0)
            {
                manager.AddComponent<Gravitation>(entity).Initialize(Gravitation.GravitationTypes.Attractor, mass);
            }

            // Make it detectable.
            manager.AddComponent<Detectable>(entity).Initialize("Textures/Radar/Icons/radar_planet");

            // Make it visible.
            manager.AddComponent<PlanetRenderer>(entity).Initialize(this, planetRadius, surfaceRotation);

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
                return MathHelper.ToRadians((random == null) ? _rotationSpeed.Low
                    : MathHelper.Lerp(_rotationSpeed.Low, _rotationSpeed.High, (float)random.NextDouble()));
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
