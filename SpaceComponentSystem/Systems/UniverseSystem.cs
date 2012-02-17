using System;
using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// System responsible for tracking astronomical objects and relatively
    /// stationary objects (such as space stations) in active cells.
    /// 
    /// <para>
    /// It also tracks general stats about a cell, such as the predominant
    /// faction in the system, state of stations and so on. This information
    /// is only stored permanently if it changed, however. Otherwise it is
    /// re-generated procedurally whenever a cell gets re-activated.
    /// </para>
    /// </summary>
    public sealed class UniverseSystem : System
    {
        #region Properties

        /// <summary>
        /// The base seed for the current game world.
        /// </summary>
        public ulong WorldSeed { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// Some constraints used for generating procedural content.
        /// </summary>
        private WorldConstraints _constraints;

        /// <summary>
        /// Tracks cell information for active cells and inactive cells that
        /// are in a changed state (deviating from the one that would be
        /// procedurally generated).
        /// </summary>
        private Dictionary<ulong, CellInfo> _cellInfo = new Dictionary<ulong, CellInfo>();

        #endregion

        #region Constructor

        public UniverseSystem(WorldConstraints constaits)
        {
            _constraints = constaits;
            ShouldSynchronize = true;
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Get the static cell information for the cell with the given id.
        /// </summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        public CellInfo GetCellInfo(ulong cellId)
        {
            return _cellInfo[cellId];
        }

        #endregion

        #region Logic

        public override void Receive<T>(ref T message)
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

                    // Check if we have a changed cell info.
                    if (!_cellInfo.ContainsKey(info.Id))
                    {
                        // No, generate the default one. Get an independent
                        // randomizer to avoid different results in other
                        // sampling operations from when we have an existing
                        // cell info.
                        var independentRandom = new MersenneTwister((ulong)new Hasher().Put(BitConverter.GetBytes(info.Id)).Put(BitConverter.GetBytes(WorldSeed)).Value);

                        // Figure out which faction should own this cell.
                        Factions faction;
                        switch (independentRandom.NextInt32(3))
                        {
                            case 0:
                                faction = Factions.NpcFactionA;
                                break;
                            case 1:
                                faction = Factions.NpcFactionB;
                                break;
                            case 2:
                            default:
                                faction = Factions.NpcFactionC;
                                break;
                        }

                        // Figure out our tech level.
                        // TODO make dependent on distance to center / start system.
                        int techLevel = independentRandom.NextInt32(3);

                        // Create the cell and push it.
                        _cellInfo.Add(info.Id, new CellInfo(faction, techLevel));
                    }

                    // Get center of our cell.
                    var cellSize = CellSystem.CellSize;
                    var center = new Vector2(cellSize * info.X + (cellSize >> 1), cellSize * info.Y + (cellSize >> 1));

                    // Check if it's the start system or not.
                    if (info.X == 0 && info.Y == 0)
                    {
                        // It is, use a predefined number of planets and moons,
                        // and make sure it's a solar system.
                        CreateSystem(random, ref center, list,
                            _cellInfo[info.Id],
                            // Seven planets, with their respective number of moons.
                            7, new[] { 0, 0, 1, 2, 4, 2, 1 });
                    }
                    else
                    {
                        // It isn't, randomize.
                        CreateSystem(random, ref center, list, _cellInfo[info.Id]);
                    }

                    // Find nearby active cells and the stations in them, mark
                    // them as possible targets for all station sin this cell,
                    // and let them know about our existence, as well.
                    var cellSystem = Manager.GetSystem<CellSystem>();
                    var stations = _cellInfo[info.Id].Stations;
                    for (int ny = info.Y - 1; ny <= info.Y + 1; ny++)
                    {
                        for (int nx = info.X - 1; nx <= info.X + 1; nx++)
                        {
                            // Don't fly to cells that are diagonal to
                            // ourselves, which we do by checking if the
                            // sum of the coordinates is uneven.
                            // This becomes more obvious when considering this:
                            // +-+-+-+-+
                            // |0|1|0|1|
                            // +-+-+-+-+
                            // |1|0|1|0|
                            // +-+-+-+-+
                            // |0|1|0|1|
                            // +-+-+-+-+
                            // |1|0|1|0|
                            // +-+-+-+-+
                            // Where 0 means the sum of the own coordinate is
                            // even, 1 means it is odd. Then we can see that
                            // the sum of diagonal pairs of cells is always
                            // even, and the one of straight neighbors is
                            // always odd.
                            if (((info.X + info.Y + ny + nx) & 1) == 0)
                            {
                                // Get the id, only mark the station if we have
                                // info on it and it's an enemy cell.
                                var id = CoordinateIds.Combine(nx, ny);
                                if (cellSystem.IsCellActive(id) &&
                                    _cellInfo.ContainsKey(id) &&
                                    (_cellInfo[id].Faction.IsAlliedTo(_cellInfo[info.Id].Faction)))
                                {
                                    // Tell the other stations.
                                    foreach (var stationId in _cellInfo[id].Stations)
                                    {
                                        var spawn = Manager.EntityManager.GetEntity(stationId).
                                            GetComponent<ShipSpawner>();
                                        foreach (var otherStationId in stations)
                                        {
                                            spawn.Targets.Add(otherStationId);
                                        }
                                    }
                                    // Tell our own stations.
                                    foreach (var stationId in stations)
                                    {
                                        var spawner = Manager.EntityManager.GetEntity(stationId).
                                            GetComponent<ShipSpawner>();
                                        foreach (var otherStationId in _cellInfo[id].Stations)
                                        {
                                            spawner.Targets.Add(otherStationId);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Remove cell info only if it does not deviate from the
                    // procedural values.
                    if (!_cellInfo[info.Id].Dirty)
                    {
                        _cellInfo.Remove(info.Id);
                    }
                    else
                    {
                        _cellInfo[info.Id].Stations.Clear();
                    }
                }
            }
        }

        #endregion

        #region Cloning

        /// <summary>
        /// Packetizes the specified packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <returns></returns>
        public override Packet Packetize(Packet packet)
        {
            packet.Write(WorldSeed);

            packet.Write(_cellInfo.Count);
            foreach (var item in _cellInfo)
            {
                packet.Write(item.Key);
                packet.Write(item.Value);
            }

            return packet;
        }

        /// <summary>
        /// Depacketizes the specified packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public override void Depacketize(Packet packet)
        {
            WorldSeed = packet.ReadUInt64();

            _cellInfo.Clear();
            int numCells = packet.ReadInt32();
            for (int i = 0; i < numCells; i++)
            {
                var key = packet.ReadUInt64();
                var value = packet.ReadPacketizable<CellInfo>();
                _cellInfo.Add(key, value);
            }
        }

        /// <summary>
        /// Hashes the specified hasher.
        /// </summary>
        /// <param name="hasher">The hasher.</param>
        public override void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(WorldSeed));
            foreach (var cellInfo in _cellInfo.Values)
            {
                cellInfo.Hash(hasher);
            }
        }

        public override ISystem DeepCopy(ISystem into)
        {
            var copy = (UniverseSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy.WorldSeed = WorldSeed;
                copy._cellInfo.Clear();
            }
            else
            {
                copy._cellInfo = new Dictionary<ulong, CellInfo>();
            }

            foreach (var cellInfo in _cellInfo)
            {
                copy._cellInfo.Add(cellInfo.Key, cellInfo.Value.DeepCopy());
            }

            return copy;
        }

        #endregion

        #region Generators

        #region Solar systems

        /// <summary>
        /// Creates a new solar system.
        /// </summary>
        private void CreateSystem(
            IUniformRandom random,
            ref Vector2 center,
            List<int> list,
            CellInfo cellInfo,
            int numPlanets = -1,
            int[] numsMoons = null)
        {
            // Get our gaussian distributed randomizer.
            var gaussian = new Ziggurat(random);

            // Create our sun.
            float sunRadius = _constraints.SampleSunRadius(gaussian);
            float sunMass = _constraints.SunMassFactor * 4f / 3f * (float)System.Math.PI * sunRadius * sunRadius * sunRadius; ;
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
            float previousPlanetOrbit = _constraints.PlanetOrbitMean - _constraints.PlanetOrbitStdDev;

            // Generate as many as we sample.
            if (numPlanets == -1)
            {
                numPlanets = _constraints.SamplePlanets(gaussian);
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
                    numMoons = _constraints.SampleMoons(gaussian);
                }
                previousPlanetOrbit = CreatePlanet(random, gaussian, sun, sunMass, previousPlanetOrbit, dominantPlanetOrbitAngle, numMoons, list, cellInfo);
            }
        }

        /// <summary>
        /// Creates a new planet.
        /// </summary>
        private float CreatePlanet(
            IUniformRandom random,
            IGaussianRandom gaussian,
            Entity sun,
            float sunMass,
            float previousOrbitRadius,
            float dominantOrbitAngle,
            int numMoons,
            List<int> list,
            CellInfo cellInfo)
        {
            // Sample planet properties.
            float planetRadius = _constraints.SamplePlanetRadius(gaussian);
            float planetOrbitMajorRadius = previousOrbitRadius + _constraints.SamplePlanetOrbit(gaussian);
            float planetOrbitEccentricity = _constraints.SamplePlanetOrbitEccentricity(gaussian);
            float planetOrbitMinorRadius = (float)System.Math.Sqrt(planetOrbitMajorRadius * planetOrbitMajorRadius * (1 - planetOrbitEccentricity * planetOrbitEccentricity));
            float planetOrbitAngle = dominantOrbitAngle + MathHelper.ToRadians(_constraints.SamplePlanetOrbitAngleDeviation(gaussian));
            float planetPeriod = (float)(2 * System.Math.PI * System.Math.Sqrt(planetOrbitMajorRadius * planetOrbitMajorRadius * planetOrbitMajorRadius * 3 /* < slowing factor */ / sunMass));
            float planetMass = _constraints.PlanetMassFactor * 4f / 3f * (float)System.Math.PI * planetRadius * planetRadius * planetRadius;

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
            float previousMoonOrbit = (_constraints.PlanetRadiusMean + _constraints.PlanetRadiusStdDev) * 1.5f;
            if (_constraints.SampleStation(random))
            {
                CreateStation(random, gaussian, planet, planetMass, planetRadius, list
                    , cellInfo
                    );
            }
            // The create as many as we sample.
            for (int j = 0; j < numMoons; j++)
            {
                previousMoonOrbit = CreateMoon(random, gaussian, planet, planetMass, previousMoonOrbit, dominantMoonOrbitAngle, list);
            }

            return planetOrbitMajorRadius;
        }

        /// <summary>
        /// Creates a new moon, orbiting a planet.
        /// </summary>
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
            float moonRadius = _constraints.SampleMoonRadius(gaussian);
            float moonOrbitMajorRadius = previousOrbitRadius + _constraints.SampleMoonOrbit(gaussian);
            float moonOrbitEccentricity = _constraints.SampleMoonOrbitEccentricity(gaussian);
            float moonOrbitMinorRadius = (float)System.Math.Sqrt(moonOrbitMajorRadius * moonOrbitMajorRadius * (1 - moonOrbitEccentricity * moonOrbitEccentricity));
            float moonOrbitAngle = dominantOrbitAngle + MathHelper.ToRadians(_constraints.SampleMoonOrbitAngleDeviation(gaussian));
            float moonPeriod = (float)(2 * System.Math.PI * System.Math.Sqrt(moonOrbitMajorRadius * moonOrbitMajorRadius * moonOrbitMajorRadius / planetMass));
            float moonMass = _constraints.PlanetMassFactor * 4f / 3f * (float)System.Math.PI * moonRadius * moonRadius * moonRadius;

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
                mass: /*moonMass*/ 0);

            list.Add(Manager.EntityManager.AddEntity(moon));

            return moonOrbitMajorRadius;
        }

        #endregion

        /// <summary>
        /// Creates a new space station, orbiting another object (planet, moon).
        /// </summary>
        private void CreateStation(
            IUniformRandom random,
            IGaussianRandom gaussian,
            Entity center,
            float centerMass,
            float centerRadius,
            List<int> list,
            CellInfo cellInfo)
        {
            var stationOrbit = centerRadius + _constraints.SampleStationOrbit(gaussian);
            var stationPeriod = (float)(2 * System.Math.PI * System.Math.Sqrt(stationOrbit * stationOrbit * stationOrbit / centerMass));
            var station = EntityFactory.CreateStation(
                texture: "Textures/Stolen/Ships/sensor_array",
                center: center,
                orbitRadius: stationOrbit,
                period: stationPeriod,
                faction: cellInfo.Faction);

            list.Add(Manager.EntityManager.AddEntity(station));
            cellInfo.Stations.Add(station.UID);
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

        #endregion

        #region CellInfo

        /// <summary>
        /// Class used to track persistent information on a single cell.
        /// </summary>
        public class CellInfo : IPacketizable, IHashable, ICopyable<CellInfo>
        {
            #region Properties

            /// <summary>
            /// Flag to check if the values deviate from the procedurally generated
            /// ones, so that we know whether to persistently store the settings of
            /// this cell, or not.
            /// </summary>
            public bool Dirty { get; private set; }

            /// <summary>
            /// The predominant faction in the system.
            /// </summary>
            public Factions Faction
            {
                get
                {
                    return _faction;
                }
                set
                {
                    _faction = value;
                    Dirty = true;
                }
            }

            /// <summary>
            /// The current tech level of the cell.
            /// </summary>
            public int TechLevel
            {
                get
                {
                    return _techLevel;
                }
                set
                {
                    _techLevel = value;
                    Dirty = true;
                }
            }

            public List<int> Stations = new List<int>();

            #endregion

            #region Fields

            /// <summary>
            /// Actual value for tech level.
            /// </summary>
            private Factions _faction;

            /// <summary>
            /// Actual value for tech level.
            /// </summary>
            private int _techLevel = 0;

            #endregion

            #region Constructor

            public CellInfo(Factions faction, int techLevel)
            {
                // Assign directly to fields, to avoid setting the dirty flag.
                _faction = faction;
                _techLevel = techLevel;
            }

            public CellInfo()
            {
            }

            #endregion

            #region Serialization / Hashing

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
                    .Write(Dirty)
                    .Write((int)_faction)
                    .Write(_techLevel);
            }

            /// <summary>
            /// Bring the object to the state in the given packet.
            /// </summary>
            /// <param name="packet">The packet to read from.</param>
            public void Depacketize(Packet packet)
            {
                Dirty = packet.ReadBoolean();
                _faction = (Factions)packet.ReadInt32();
                _techLevel = packet.ReadInt32();
            }

            /// <summary>
            /// Push some unique data of the object to the given hasher,
            /// to contribute to the generated hash.
            /// </summary>
            /// <param name="hasher">The hasher to push data to.</param>
            public void Hash(Hasher hasher)
            {
                hasher
                    .Put(BitConverter.GetBytes(Dirty))
                    .Put(BitConverter.GetBytes((int)_faction))
                    .Put(BitConverter.GetBytes(_techLevel));
            }

            #endregion

            #region Copying

            public CellInfo DeepCopy()
            {
                return DeepCopy(null);
            }

            public CellInfo DeepCopy(CellInfo into)
            {
                var copy = into ?? (CellInfo)MemberwiseClone();

                if (copy == into)
                {
                    copy.Dirty = Dirty;
                    copy._faction = _faction;
                    copy._techLevel = _techLevel;
                }

                return copy;
            }

            #endregion
        }

        #endregion
    }
}
