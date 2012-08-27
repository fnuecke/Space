using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.FarMath;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Factories.SunSystemFactoryTypes;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Generates sun systems based on specific constraints.
    /// </summary>
    [DefaultProperty("Name")]
    public sealed class SunSystemFactory : IFactory
    {
        #region Properties

        /// <summary>
        /// The unique name of the object type.
        /// </summary>
        [Category("General")]
        [Description("The name of this sun system, by which it can be referenced.")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// The type of sun to create in the center of this sun system/
        /// </summary>
        [Editor("Space.Tools.DataEditor.SunEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Category("Structure")]
        [Description("The name of the sun to use as the center of this sun system.")]
        public string Sun
        {
            get { return _sun; }
            set { _sun = value; }
        }

        /// <summary>
        /// The planets to create.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [Category("Structure")]
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

        /// <summary>
        /// Samples the attributes to apply to the item.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="cellCenter">The center of the cell for which the sun system is created.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        public void SampleSunSystem(IManager manager, FarPosition cellCenter, IUniformRandom random)
        {
            var sun = FactoryLibrary.SampleSun(manager, _sun, cellCenter, random);

            if (_planets != null)
            {
                _planets.SampleOrbiters(manager, sun, random);
            }
        }

        #endregion
    }

    #region Orbitsystem

    namespace SunSystemFactoryTypes
    {
        /// <summary>
        /// Describes an orbit system with a dominant axis.
        /// </summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public sealed class Orbit
        {
            /// <summary>
            /// The angle of generated planets' orbits.
            /// </summary>
            [ContentSerializer(Optional = true)]
            [Description("The primary angle of the orbits around this object.")]
            public FloatInterval Angle
            {
                get { return _angle; }
                set { _angle = value; }
            }

            /// <summary>
            /// Objects in this orbit system.
            /// </summary>
            [ContentSerializer(FlattenContent = true, CollectionItemName = "Orbiter")]
            [Description("The list of objects orbiting this object.")]
            public Orbiter[] Orbiters
            {
                get { return _orbiters; }
                set { _orbiters = value; }
            }

            private FloatInterval _angle = new FloatInterval(0, 360);

            private Orbiter[] _orbiters = new Orbiter[0];

            internal void SampleOrbiters(IManager manager, int center, IUniformRandom random)
            {
                // Get the dominant angle.
                var dominantAngle = SampleAngle(random);

                // Create orbiters.
                foreach (var orbiter in _orbiters)
                {
                    if (orbiter.ChanceToExist > random.NextDouble())
                    {
                        var entity = FactoryLibrary.SamplePlanet(manager, orbiter.Name, center, dominantAngle,
                                                                    orbiter.SampleOrbitRadius(random), random);

                        // Recurse.
                        if (orbiter.Moons != null)
                        {
                            orbiter.Moons.SampleOrbiters(manager, entity, random);
                        }
                    }
                }
            }

            /// <summary>
            /// Samples the angle of this planet's orbit.
            /// </summary>
            /// <param name="random">The randomizer to use.</param>
            /// <returns>The sampled angle.</returns>
            private float SampleAngle(IUniformRandom random)
            {
                return MathHelper.ToRadians((random == null) ? _angle.Low : MathHelper.Lerp(_angle.Low, _angle.High, (float)random.NextDouble()));
            }

            public override string ToString()
            {
                return "Orbit";
            }
        }

        /// <summary>
        /// Describes a single orbiting object.
        /// </summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public sealed class Orbiter
        {
            /// <summary>
            /// The name of the planet type.
            /// </summary>
            [Editor("Space.Tools.DataEditor.PlanetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
            [Category("General")]
            [Description("The name of the planet or moon to be sampled.")]
            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            /// <summary>
            /// The radius at which this object orbits.
            /// </summary>
            [Category("Orbit")]
            [Description("The radius at which this object should orbit its center.")]
            public FloatInterval OrbitRadius
            {
                get { return _orbitRadius; }
                set { _orbitRadius = value; }
            }

            /// <summary>
            /// The probability with which an actual instance is created in a system.
            /// </summary>
            [ContentSerializer(Optional = true)]
            [Category("General")]
            [Description("The probability that this object will be sampled when generating the sun system.")]
            public float ChanceToExist
            {
                get { return _chanceToExist; }
                set { _chanceToExist = value; }
            }

            /// <summary>
            /// Child orbits, revolving around this object.
            /// </summary>
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

            private float _chanceToExist = 1f;

            private Orbit _moons = new Orbit();

            /// <summary>
            /// Samples the angle of this planet's orbit.
            /// </summary>
            /// <param name="random">The randomizer to use.</param>
            /// <returns>The sampled angle.</returns>
            internal float SampleOrbitRadius(IUniformRandom random)
            {
                return (random == null) ? _orbitRadius.Low : MathHelper.Lerp(_orbitRadius.Low, _orbitRadius.High, (float)random.NextDouble());
            }

            public override string ToString()
            {
                return Name + (Moons != null && Moons.Orbiters.Length > 0 ? (" (" + Moons.Orbiters.Length + " moons)") : "");
            }
        }

    }

    #endregion
}
