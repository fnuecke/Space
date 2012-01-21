using System;
using System.Collections.Generic;
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

        public Dictionary<ulong, CellInfo> CellInfo = new Dictionary<ulong, CellInfo>();

        #endregion

        #region Constructor

        public UniverseSystem(WorldConstraints constaits)
        {
            _constaints = constaits;
            ShouldSynchronize = true;
        }

        #endregion

        #region Logic

        public override void HandleMessage<T>(ref T message)
        {
            if (message is CellStateChanged)
            {
                var info = (CellStateChanged)(ValueType)message;

                if (info.State)
                {

                    // Get random generator based on world seed and cell location.
                    var random = new MersenneTwister((ulong)new Hasher().Put(BitConverter.GetBytes(info.Id)).Put(BitConverter.GetBytes(WorldSeed)).Value);

                    // List to accumulate entities we created for this system in.
                    List<int> list = new List<int>();

                    if (!CellInfo.ContainsKey(info.Id))
                    {
                        var random2 = new MersenneTwister((ulong)new Hasher().Put(BitConverter.GetBytes(info.Id)).Put(BitConverter.GetBytes(WorldSeed)).Value);
                        var number = random2.NextInt32(3);
                        var cell = new CellInfo();
                        switch (number)
                        {
                                
                            case (0):
                                 cell.Faction = Factions.NpcFractionA;
                                break;
                            case (1):
                                cell.Faction = Factions.NpcFractionB;
                                break;
                            case (2):
                                cell.Faction = Factions.NpcFractionC;
                                break;
                            default:
                                cell.Faction = Factions.NpcFractionC;
                                break;
                        }
                        CellInfo.Add(info.Id, cell);
                    }
                    // Get center of our cell.
                    var cellSize = CellSystem.CellSize;
                    var center = new Vector2(cellSize * info.X + (cellSize >> 1), cellSize * info.Y + (cellSize >> 1));

                    if (info.X == 0 && info.Y == 0)
                    {
                        CreateSystem(random, ref center, list,
                            CellInfo[info.Id].Faction, 
                            7, new[] {
                            0, 0, 1, 2, 4, 2, 1
                        });
                    }
                    else
                    {
                        CreateSystem(random, ref center, list,CellInfo[info.Id].Faction);
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
                    //remove only if not changed
                    if (CellInfo.ContainsKey(info.Id))
                    {
                        if (!CellInfo[info.Id].Changed)
                            CellInfo.Remove(info.Id);
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
            packet.Write(CellInfo.Count);
            foreach (var item in CellInfo)
            {
                packet.Write(item.Key);
                packet.Write(item.Value);
                
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

            CellInfo.Clear();
            numCells = packet.ReadInt32();
            for (int i = 0; i < numCells; i++)
            {
                var key = packet.ReadUInt64();
                var value = packet.ReadPacketizable<CellInfo>();
                CellInfo.Add(key,value);
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
            foreach (var entities in CellInfo.Values)
            {
                
                    entities.Hash(hasher);
            }
        }

        public override IComponentSystem DeepCopy(IComponentSystem into)
        {
            var copy = (UniverseSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy.WorldSeed = WorldSeed;
                copy._entities.Clear();
                copy.CellInfo.Clear();
            }
            else
            {
                copy._entities = new Dictionary<ulong, List<int>>();
                copy.CellInfo = new Dictionary<ulong, CellInfo>();
            }

            foreach (var item in _entities)
            {
                copy._entities.Add(item.Key, new List<int>(item.Value));

            }
            foreach (var factionse in CellInfo)
            {
                copy.CellInfo.Add(factionse.Key, factionse.Value);
            }

            return copy;
        }

        #endregion

        #region Utility methods

        private void CreateSystem(
            IUniformRandom random,
            ref Vector2 center,
            List<int> list,
            Factions faction,
            int numPlanets = -1,
            int[] numsMoons = null)
        {
            // Get our gaussian distributed randomizer.
            var gaussian = new Ziggurat(random);

            // Create our sun.
            float sunRadius = _constaints.SampleSunRadius(gaussian);
            float sunMass = _constaints.SunMassFactor * 4f / 3f * (float)System.Math.PI * sunRadius * sunRadius * sunRadius; ;
            Entity sun = EntityFactory.CreateSun(
                radius: sunRadius,
                position: center,
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
                previousPlanetOrbit = CreatePlanet(random, gaussian, sun, sunMass, previousPlanetOrbit, dominantPlanetOrbitAngle, numMoons, list, faction);
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
            List<int> list
            , Factions faction
            )
        {
            // Sample planet properties.
            float planetRadius = _constaints.SamplePlanetRadius(gaussian);
            float planetOrbitMajorRadius = previousOrbitRadius + _constaints.SamplePlanetOrbit(gaussian);
            float planetOrbitEccentricity = _constaints.SamplePlanetOrbitEccentricity(gaussian);
            float planetOrbitMinorRadius = (float)System.Math.Sqrt(planetOrbitMajorRadius * planetOrbitMajorRadius * (1 - planetOrbitEccentricity * planetOrbitEccentricity));
            float planetOrbitAngle = dominantOrbitAngle + MathHelper.ToRadians(_constaints.SamplePlanetOrbitAngleDeviation(gaussian));
            float planetPeriod = (float)(2 * System.Math.PI * System.Math.Sqrt(planetOrbitMajorRadius * planetOrbitMajorRadius * planetOrbitMajorRadius * 3 /* < slowing factor */ / sunMass));
            float planetMass = _constaints.PlanetMassFactor * 4f / 3f * (float)System.Math.PI * planetRadius * planetRadius * planetRadius;

            var planet = EntityFactory.CreateOrbitingAstronomicalObject(
                texture: "Textures/Planets/rock_07",
                planetTint: Color.Lerp(Color.DarkOliveGreen, Color.White, 0.5f),
                radius: planetRadius,
                atmosphereTint: Color.LightSkyBlue,
                rotationSpeed: (float)gaussian.NextSampleClamped(System.Math.PI / 6000, 0.0000125) * System.Math.Sign(random.NextDouble() - 0.5),
                center: sun,
                majorRadius: planetOrbitMajorRadius,
                minorRadius: planetOrbitMinorRadius,
                angle: planetOrbitAngle,
                period: planetPeriod,
                periodOffset: (float)(2 * System.Math.PI * random.NextDouble()),
                mass: planetMass);

            list.Add(Manager.EntityManager.AddEntity(planet));

            // Create some moons, so our planet doesn't feel so ronery.

            // Again, fetch a dominant angle.
            float dominantMoonOrbitAngle = (float)(random.NextDouble() * Math.PI * 2);

            // And track the radii. Start outside our planet.
            float previousMoonOrbit = (_constaints.PlanetRadiusMean + _constaints.PlanetRadiusStdDev) * 1.5f;
            if (_constaints.SampleStation(random))
            {
                CreateStation(random, gaussian, planet, planetMass, planetRadius, list
                    , faction
                    );
            }
            // The create as many as we sample.
            for (int j = 0; j < numMoons; j++)
            {
                previousMoonOrbit = CreateMoon(random, gaussian, planet, planetMass, previousMoonOrbit, dominantMoonOrbitAngle, list);
            }

            return planetOrbitMajorRadius;
        }

        private void CreateStation(
            IUniformRandom random,
            IGaussianRandom gaussian,
            Entity planet,
            float planetMass,
            float planetRadius,
            List<int> list
            , Factions faction
            )
        {

            var StationOrbit = planetRadius + _constaints.SampleStationOrbit(gaussian);
            var stationPeriod = (float)(2 * System.Math.PI * System.Math.Sqrt(StationOrbit * StationOrbit * StationOrbit / planetMass));
            var station = EntityFactory.CreateStation(
                texture: "Textures/Stolen/Ships/sensor_array",
                center: planet,
                orbitRadius: StationOrbit,
                period: stationPeriod,
                faction: faction);

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
            float moonMass = _constaints.PlanetMassFactor * 4f / 3f * (float)System.Math.PI * moonRadius * moonRadius * moonRadius;

            var moon = EntityFactory.CreateOrbitingAstronomicalObject(
                texture: "Textures/Planets/rock_02",
                planetTint: Color.White,
                radius: moonRadius,
                atmosphereTint: Color.Transparent,
                rotationSpeed: (float)gaussian.NextSampleClamped(System.Math.PI / 20000, 0.0000025) * System.Math.Sign(random.NextDouble() - 0.5),
                center: planet,
                majorRadius: moonOrbitMajorRadius,
                minorRadius: moonOrbitMinorRadius,
                angle: moonOrbitAngle,
                period: moonPeriod,
                periodOffset: (float)(2 * System.Math.PI * random.NextDouble()),
                mass: moonMass);

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

    public class CellInfo:IPacketizable,IHashable
    {
        public Factions Faction;
        public bool Changed;
        public int TechLevel { get { return TechLevel; }
            set { TechLevel = value;
                Changed = true;
            }
        }

        /// <summary>
        /// Changes the Faction of this Cell 
        /// </summary>
        /// <param name="faction"></param>
        public void ChangeFaction(Factions faction)
        {
            Faction = faction;
            Changed = true;
        }
        #region Packetize
        
        public Packet Packetize(Packet packet)
        {
            packet.Write((int)Faction);
            packet.Write(Changed);
            return packet;
        }

        public void Depacketize(Packet packet)
        {
            Faction = (Factions) packet.ReadInt32();
            Changed = packet.ReadBoolean();
        }

        public void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes((int) Faction))
                .Put(BitConverter.GetBytes(Changed));

        }
        #endregion
    }
}
