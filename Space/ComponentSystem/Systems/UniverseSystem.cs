using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Entities;
using Space.ComponentSystem.Systems.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    public class UniverseSystem : AbstractComponentSystem<NullParameterization, NullParameterization>
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #region Properties

        public ulong WorldSeed { get; set; }

        #endregion

        #region Fields

        private WorldConstraints _constaints;

        private Dictionary<ulong, List<int>> _entities = new Dictionary<ulong, List<int>>();

        #endregion

        #region Constructor

        public UniverseSystem(WorldConstraints constaits)
        {
            _constaints = constaits;
            ShouldSynchronize = true;
        }

        #endregion

        #region Logic

        public override void HandleMessage(ValueType message)
        {
            if (message is CellStateChanged)
            {
                var info = (CellStateChanged)message;

                if (info.State)
                {
                    // Get random generator based on world seed and cell location.
                    var random = new MersenneTwister((ulong)new Hasher().Put(BitConverter.GetBytes(info.Id)).Put(BitConverter.GetBytes(WorldSeed)).Value);

                    // List to accumulate entities we created for this system in.
                    List<int> list = new List<int>();

                    // Get center of our cell.
                    var cellSize = CellSystem.CellSize;
                    var center = new Vector2(cellSize * info.X + (cellSize >> 1), cellSize * info.Y + (cellSize >> 1));

                    if (info.X == 0 && info.Y == 0)
                    {
                        CreateSystem(random, ref center, list, 7, new[] {
                            0, 0, 1, 2, 4, 2, 1
                        });
                    }
                    else
                    {
                        CreateSystem(random, ref center, list);
                    }

                    _entities.Add(info.Id, list);
                }
                else
                {
                    if (_entities.ContainsKey(info.Id))
                    {
                        foreach (int id in _entities[info.Id])
                        {
                            Manager.EntityManager.RemoveEntity(id);
                        }

                        _entities.Remove(info.Id);
                    }
                }
            }
        }

        #endregion

        #region Cloning

        public override Packet Packetize(Packet packet)
        {
            packet.Write(WorldSeed);

            packet.Write(_entities.Count);
            foreach (var item in _entities)
            {
                packet.Write(item.Key);
                packet.Write(item.Value.Count);
                foreach (var entityId in item.Value)
                {
                    packet.Write(entityId);
                }
            }

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            WorldSeed = packet.ReadUInt64();

            _entities.Clear();
            int numCells = packet.ReadInt32();
            for (int i = 0; i < numCells; i++)
            {
                var key = packet.ReadUInt64();
                var list = new List<int>();
                int numEntities = packet.ReadInt32();
                for (int j = 0; j < numEntities; j++)
                {
                    list.Add(packet.ReadInt32());
                }
                _entities.Add(key, list);
            }
        }

        public override void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(WorldSeed));
            foreach (var entities in _entities.Values)
            {
                foreach (var entity in entities)
                {
                    hasher.Put(BitConverter.GetBytes(entity));
                }
            }
        }

        public override IComponentSystem DeepCopy(IComponentSystem into)
        {
            var copy = (UniverseSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy.WorldSeed = WorldSeed;
                copy._entities.Clear();
            }
            else
            {
                copy._entities = new Dictionary<ulong, List<int>>();
            }

            foreach (var item in _entities)
            {
                copy._entities.Add(item.Key, new List<int>(item.Value));
            }

            return copy;
        }

        #endregion

        #region Utility methods

        private void CreateSystem(
            IUniformRandom random,
            ref Vector2 center,
            List<int> list,
            int numPlanets = -1,
            int[] numsMoons = null)
        {
            // Get our gaussian distributed randomizer.
            var gaussian = new Ziggurat(random);

            // Create our sun.
            float sunMass = _constaints.SampleSunMass(gaussian);
            Entity sun = EntityFactory.CreateSun(
                radius: 512,
                position: center,
                type: AstronomicBodyType.Sun,
                mass: sunMass);
            list.Add(Manager.EntityManager.AddEntity(sun));

            // Get a dominant angle for orbits in our system. This is to avoid
            // planets' orbits to intersect, because we don't want to handle
            // planet collisions ;)
            float dominantPlanetOrbitAngle = (float)(2 * System.Math.PI * random.NextDouble());

            // Keep track of the last orbit major radius, to incrementally
            // increase the radii.
            float previousPlanetOrbit = _constaints.PlanetOrbitMean - _constaints.PlanetOrbitStdDev;

            // Generate as many as we sample.
            if (numPlanets == -1)
            {
                numPlanets = _constaints.SamplePlanets(gaussian);
            }
            for (int i = 0; i < numPlanets; i++)
            {
                int numMoons;
                if (numsMoons != null && numsMoons.Length > i)
                {
                    numMoons = numsMoons[i];
                }
                else
                {
                    numMoons = _constaints.SampleMoons(gaussian);
                }
                previousPlanetOrbit = CreatePlanet(random, gaussian, sun, sunMass, previousPlanetOrbit, dominantPlanetOrbitAngle, numMoons, list);
            }
        }

        private float CreatePlanet(
            IUniformRandom random,
            IGaussianRandom gaussian,
            Entity sun,
            float sunMass,
            float previousOrbitRadius,
            float dominantOrbitAngle,
            int numMoons,
            List<int> list)
        {
            // Sample planet properties.
            float planetRadius = _constaints.SamplePlanetRadius(gaussian);
            float planetOrbitMajorRadius = previousOrbitRadius + _constaints.SamplePlanetOrbit(gaussian);
            float planetOrbitEccentricity = _constaints.SamplePlanetOrbitEccentricity(gaussian);
            float planetOrbitMinorRadius = (float)System.Math.Sqrt(planetOrbitMajorRadius * planetOrbitMajorRadius * (1 - planetOrbitEccentricity * planetOrbitEccentricity));
            float planetOrbitAngle = dominantOrbitAngle + MathHelper.ToRadians(_constaints.SamplePlanetOrbitAngleDeviation(gaussian));
            float planetPeriod = (float)(2 * System.Math.PI * System.Math.Sqrt(planetOrbitMajorRadius * planetOrbitMajorRadius * planetOrbitMajorRadius * 3 /* < slowing factor */ / sunMass));
            float planetMass = _constaints.MassPerVolume * 4f / 3f * (float)System.Math.PI * planetRadius * planetRadius * planetRadius;

            var planet = EntityFactory.CreateOrbitingAstronomicalObject(
                texture: "Textures/rock_07",
                planetTint: Color.Lerp(Color.DarkOliveGreen, Color.White, 0.5f),
                radius: planetRadius,
                rotationDirection: (float)(2 * System.Math.PI * random.NextDouble()),
                atmosphereTint: Color.LightSkyBlue,
                center: sun,
                majorRadius: planetOrbitMajorRadius,
                minorRadius: planetOrbitMinorRadius,
                angle: planetOrbitAngle,
                period: planetPeriod,
                periodOffset: (float)(2 * System.Math.PI * random.NextDouble()),
                type: AstronomicBodyType.Planet,
                mass: planetMass);

            var spin = planet.GetComponent<Spin>();
            spin.Value = 0.001f + (float)random.NextDouble() * 0.0005f;

            list.Add(Manager.EntityManager.AddEntity(planet));

            // Create some moons, so our planet doesn't feel so ronery.

            // Again, fetch a dominant angle.
            float dominantMoonOrbitAngle = (float)(random.NextDouble() * Math.PI * 2);

            // And track the radii. Start outside our planet.
            float previousMoonOrbit = (_constaints.PlanetRadiusMean + _constaints.PlanetRadiusStdDev) * 1.5f;
            if (_constaints.SampleStation(random))
                CreateStation(gaussian, planet, planetMass, planetRadius, list);
                // The create as many as we sample.
            for (int j = 0; j < numMoons; j++)
            {
                previousMoonOrbit = CreateMoon(random, gaussian, planet, planetMass, previousMoonOrbit, dominantMoonOrbitAngle, list);
            }

            return planetOrbitMajorRadius;
        }

        private void CreateStation(
            IGaussianRandom gaussian,
            Entity planet,
            float planetMass,
            float planetRadius,
            List<int> list)
        {
            var faction = Factions.Player5;
            var StationOrbit = planetRadius + _constaints.SampleStationOrbit(gaussian);
            var stationPeriod = (float)(2 * System.Math.PI * System.Math.Sqrt(StationOrbit * StationOrbit * StationOrbit / planetMass));
            var station = EntityFactory.CreateStation(StationOrbit, faction, "Textures/Stolen/Ships/sensor_array", planet, stationPeriod);
            var spin = station.GetComponent<Spin>();
            spin.Value = ( (float)Math.PI)/stationPeriod  ;

            list.Add(Manager.EntityManager.AddEntity(station));
        }
        private float CreateMoon(
            IUniformRandom random,
            IGaussianRandom gaussian,
            Entity planet,
            float planetMass,
            float previousOrbitRadius,
            float dominantOrbitAngle,
            List<int> list)
        {
            // Sample moon properties.
            float moonRadius = _constaints.SampleMoonRadius(gaussian);
            float moonOrbitMajorRadius = previousOrbitRadius + _constaints.SampleMoonOrbit(gaussian);
            float moonOrbitEccentricity = _constaints.SampleMoonOrbitEccentricity(gaussian);
            float moonOrbitMinorRadius = (float)System.Math.Sqrt(moonOrbitMajorRadius * moonOrbitMajorRadius * (1 - moonOrbitEccentricity * moonOrbitEccentricity));
            float moonOrbitAngle = dominantOrbitAngle + MathHelper.ToRadians(_constaints.SampleMoonOrbitAngleDeviation(gaussian));
            float moonPeriod = (float)(2 * System.Math.PI * System.Math.Sqrt(moonOrbitMajorRadius * moonOrbitMajorRadius * moonOrbitMajorRadius / planetMass));
            float moonMass = _constaints.MassPerVolume * 4f / 3f * (float)System.Math.PI * moonRadius * moonRadius * moonRadius;

            var moon = EntityFactory.CreateOrbitingAstronomicalObject(
                texture: "Textures/rock_02",
                planetTint: Color.White,
                radius: moonRadius,
                rotationDirection: (float)(2 * System.Math.PI * random.NextDouble()),
                atmosphereTint: Color.Transparent,
                center: planet,
                majorRadius: moonOrbitMajorRadius,
                minorRadius: moonOrbitMinorRadius,
                angle: moonOrbitAngle,
                period: moonPeriod,
                periodOffset: (float)(2 * System.Math.PI * random.NextDouble()),
                type: AstronomicBodyType.Moon,
                mass: moonMass);

            var spin = moon.GetComponent<Spin>();
            spin.Value = 0.001f + (float)random.NextDouble() * 0.0005f;

            list.Add(Manager.EntityManager.AddEntity(moon));

            return moonOrbitMajorRadius;
        }

        private List<int> CreateAsteroidBelt()
        {
            List<int> list = new List<int>();

            return list;
        }

        private List<int> CreateNebula()
        {
            List<int> list = new List<int>();

            return list;
        }

        private List<int> CreateSpecialSystem()
        {
            List<int> list = new List<int>();

            return list;
        }

        public List<int> GetSystemList(ulong id)
        {
            return _entities[id];
        }

        #endregion
    }
}
