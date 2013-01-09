using System;
using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.FarMath;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Factories.SunSystemFactoryTypes;
using Space.Util;

namespace Space.ComponentSystem.Factories
{
    /// <summary>Generates sun systems based on specific constraints.</summary>
    [DefaultProperty("Name")]
    public sealed class SunSystemFactory : IFactory
    {
        #region Properties

        /// <summary>The unique name of the object type.</summary>
        [Category("General")]
        [Description("The name of this sun system, by which it can be referenced.")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>The type of sun to create in the center of this sun system/</summary>
        [Editor("Space.Tools.DataEditor.SunEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Category("Structure")]
        [Description("The name of the sun to use as the center of this sun system.")]
        public string Sun
        {
            get { return _sun; }
            set { _sun = value; }
        }

        public class ChildrenHaveSharedResourcesAttribute : Attribute {}

        /// <summary>The planets to create.</summary>
        [ContentSerializer(Optional = true)]
        [Category("Structure")]
        [ChildrenHaveSharedResources]
        [Description("The hierarchical list of planets and moons in this sun system.")]
        public Orbit Planets
        {
            get { return _planets; }
            set { _planets = value; }
        }

        #endregion

        #region Backing fields

        private string _name = "";

        private string _sun;

        private Orbit _planets = new Orbit();

        #endregion

        #region Sampling

        /// <summary>Samples the attributes to apply to the item.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="cellCenter">The center of the cell for which the sun system is created.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        public void SampleSunSystem(IManager manager, FarPosition cellCenter, IUniformRandom random)
        {
            var sun = FactoryLibrary.SampleSun(manager, _sun, cellCenter, random);

            if (_planets != null)
            {
                _planets.Sample(manager, sun, random);
            }
        }

        #endregion
    }

    #region Orbitsystem

    namespace SunSystemFactoryTypes
    {
        /// <summary>Describes an orbit system with a dominant axis.</summary>
        [TypeConverter(typeof (ExpandableObjectConverter))]
        public sealed class Orbit
        {
            /// <summary>The angle of generated planets' orbits.</summary>
            [ContentSerializer(Optional = true)]
            [Description("The primary angle of the orbits around this object.")]
            public FloatInterval Angle
            {
                get { return _angle; }
                set { _angle = value; }
            }

            /// <summary>Objects in this orbit system.</summary>
            [ContentSerializer(FlattenContent = true, CollectionItemName = "Orbiter")]
            [SunSystemFactory.ChildrenHaveSharedResourcesAttribute]
            [Description("The list of objects orbiting this object.")]
            public Orbiter[] Orbiters
            {
                get { return _orbiters; }
                set { _orbiters = value; }
            }

            private FloatInterval _angle = new FloatInterval(0, 360);

            private Orbiter[] _orbiters = new Orbiter[0];

            internal void Sample(IManager manager, int center, IUniformRandom random)
            {
                // Get the dominant angle.
                var dominantAngle = SampleAngle(random);

                // Create orbiters.
                foreach (var orbiter in _orbiters)
                {
                    orbiter.Sample(manager, center, dominantAngle, random);
                }
            }

            /// <summary>Samples the angle of this planet's orbit.</summary>
            /// <param name="random">The randomizer to use.</param>
            /// <returns>The sampled angle.</returns>
            private float SampleAngle(IUniformRandom random)
            {
                return
                    MathHelper.ToRadians(
                        (random == null)
                            ? _angle.Low
                            : MathHelper.Lerp(_angle.Low, _angle.High, (float) random.NextDouble()));
            }

            public override string ToString()
            {
                return "Orbit";
            }
        }

        /// <summary>Describes a single orbiting object.</summary>
        [TypeConverter(typeof (ExpandableObjectConverter))]
        public sealed class Orbiter
        {
            /// <summary>The name of the planet type.</summary>
            [Editor("Space.Tools.DataEditor.PlanetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
            [Category("General")]
            [Description("The name of the planet or moon to be sampled.")]
            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            /// <summary>The radius at which this object orbits.</summary>
            [Category("Orbit")]
            [Description("The radius at which this object should orbit its center.")]
            public FloatInterval OrbitRadius
            {
                get { return _orbitRadius; }
                set { _orbitRadius = value; }
            }

            /// <summary>
            ///     The eccentricity of generated planets' orbits. A value of 0 means it's a circle, 1 means it's totally flat (a
            ///     line).
            /// </summary>
            [Category("Orbit")]
            [Description("The eccentricity of the planet's orbit ellipse. A value of 0 leads to a circle, a value of 1 to a line.")]
            public FloatInterval Eccentricity
            {
                get { return _eccentricity; }
                set { _eccentricity = value; }
            }

            /// <summary>The offset from the base orbiting angle of the sun system.</summary>
            [ContentSerializer(Optional = true)]
            [Category("Orbit")]
            [Description("The offset from the general orbit axis of all orbits around the focus, in degrees.")]
            public FloatInterval AngleOffset
            {
                get { return _angleOffset; }
                set { _angleOffset = value; }
            }

            /// <summary>
            ///     The travel speed of generated planets' orbits. This will be used to determine the period, based on the
            ///     orbiting radius.
            /// </summary>
            [Category("Orbit")]
            [Description("The speed at which the planet will travel along its orbit ellipse, in pixels per second.")]
            public FloatInterval TravelSpeed
            {
                get { return _travelSpeed; }
                set { _travelSpeed = value; }
            }

            /// <summary>The probability with which an actual instance is created in a system.</summary>
            [ContentSerializer(Optional = true)]
            [Category("General")]
            [Description("The probability that this object will be sampled when generating the sun system.")]
            public float ChanceToExist
            {
                get { return _chanceToExist; }
                set { _chanceToExist = value; }
            }

            /// <summary>Child orbits, revolving around this object.</summary>
            [ContentSerializer(Optional = true, SharedResource = true)]
            [Category("Orbit")]
            [Description("The list of objects orbiting this object.")]
            public Orbit Moons
            {
                get { return _moons; }
                set { _moons = value; }
            }

            private string _name = "";

            private FloatInterval _orbitRadius = FloatInterval.Zero;

            private FloatInterval _eccentricity = FloatInterval.Zero;

            private FloatInterval _angleOffset = FloatInterval.Zero;

            private FloatInterval _travelSpeed = FloatInterval.Zero;

            private float _chanceToExist = 1f;

            private Orbit _moons = new Orbit();

            /// <summary>Samples a new orbiter of this type.</summary>
            /// <param name="manager">The manager.</param>
            /// <param name="center">The center.</param>
            /// <param name="dominantAngle">The dominant angle.</param>
            /// <param name="random">The random.</param>
            /// <returns></returns>
            internal void Sample(IManager manager, int center, float dominantAngle, IUniformRandom random)
            {
                if (random != null && ChanceToExist <= random.NextDouble())
                {
                    return;
                }

                var radius = SampleOrbitRadius(random);
                var eccentricity = SampleEccentricity(random);
                var angleOffset = SampleAngleOffset(random);
                var travelSpeed = SampleTravelSpeed(random);
                var periodOffset = random == null ? 0f : (float) random.NextDouble();

                // Compute minor and major radius.
                float a, b;
                ComputeRadii(radius, eccentricity, out a, out b);

                // Get period. Figure out circumference using Ramanujan's approximation.
                var circumference = MathHelper.Pi * (3 * (a + b) - (float) Math.Sqrt((3 * a + b) * (a + 3 * b)));
                var period = circumference / travelSpeed * Settings.TicksPerSecond;

                var entity = FactoryLibrary.SamplePlanet(manager, Name, center, dominantAngle, radius, random);

                // Make it move around its center.
                manager.AddComponent<EllipsePath>(entity)
                       .Initialize(center, a, b, dominantAngle + angleOffset, period, MathHelper.TwoPi * periodOffset);

                // Set initial position to center (to avoid generated instances shortly popping up
                // at the origin).
                ((Transform) manager.GetComponent(entity, Transform.TypeId))
                    .SetTranslation(((Transform) manager.GetComponent(center, Transform.TypeId)).Translation);

                // Recurse.
                if (Moons != null)
                {
                    Moons.Sample(manager, entity, random);
                }
            }

            /// <summary>Computes the radii for an ellipse, given the radius and eccentricity.</summary>
            /// <param name="radius">The radius.</param>
            /// <param name="eccentricity">The eccentricity.</param>
            /// <param name="minor">The minor radius.</param>
            /// <param name="major">The major radius.</param>
            public static void ComputeRadii(float radius, float eccentricity, out float minor, out float major)
            {
                // Compute major radius and focus distance for an ellipse fitting
                // inside a unit circle with the focus point as its center.
                // We know that e = f/a and because of the unit circle assumption
                // with the focus as its center it must follow that a + f = 1.
                minor = 1 / (1 + eccentricity);
                var f = 1 - minor;

                // We can the compute the minor radius for that very ellipse. We
                // know that f = sqrt(a * a - b * b), which can be transformed to
                // b = sqrt(a * a - f * f).
                major = (float) Math.Sqrt(minor * minor - f * f);

                // We then scale the minor and major radii to fit into our actual
                // orbit radius.
                minor *= radius;
                major *= radius;
            }

            /// <summary>Samples the angle of this planet's orbit.</summary>
            /// <param name="random">The randomizer to use.</param>
            /// <returns>The sampled angle.</returns>
            private float SampleOrbitRadius(IUniformRandom random)
            {
                return (random == null)
                           ? _orbitRadius.Low
                           : MathHelper.Lerp(_orbitRadius.Low, _orbitRadius.High, (float) random.NextDouble());
            }

            /// <summary>Samples the eccentricity of this planet's orbit.</summary>
            /// <param name="random">The randomizer to use.</param>
            /// <returns>The sampled major radius.</returns>
            private float SampleEccentricity(IUniformRandom random)
            {
                if (_eccentricity != null)
                {
                    return (random == null)
                               ? _eccentricity.Low
                               : MathHelper.Lerp(_eccentricity.Low, _eccentricity.High, (float) random.NextDouble());
                }
                return 0f;
            }

            /// <summary>Samples the travel speed of this planet.</summary>
            /// <param name="random">The randomizer to use.</param>
            /// <returns>The sampled travel speed.</returns>
            private float SampleTravelSpeed(IUniformRandom random)
            {
                if (_travelSpeed != null)
                {
                    return (random == null)
                               ? _travelSpeed.Low
                               : MathHelper.Lerp(_travelSpeed.Low, _travelSpeed.High, (float) random.NextDouble());
                }
                return 0f;
            }

            /// <summary>Samples the angle offset of this planet.</summary>
            /// <param name="random">The randomizer to use.</param>
            /// <returns>The sampled angle offset.</returns>
            private float SampleAngleOffset(IUniformRandom random)
            {
                if (_angleOffset != null)
                {
                    return MathHelper.ToRadians(
                        (random == null)
                            ? _angleOffset.Low
                            : MathHelper.Lerp(_angleOffset.Low, _angleOffset.High, (float) random.NextDouble()));
                }
                return 0f;
            }

            /// <summary>
            ///     Returns a <see cref="System.String"/> that represents this instance.
            /// </summary>
            /// <returns>
            ///     A <see cref="System.String"/> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return Name +
                       (Moons != null && Moons.Orbiters.Length > 0 ? (" (" + Moons.Orbiters.Length + " moons)") : "");
            }
        }
    }

    #endregion
}