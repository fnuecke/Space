using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Engine.Util;
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
    public sealed class UniverseSystem : AbstractSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = Engine.ComponentSystem.Manager.GetSystemTypeId(typeof(UniverseSystem));

        #endregion

        #region Properties

        /// <summary>
        /// The base seed for the current game world.
        /// </summary>
        public ulong WorldSeed { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Tracks cell information for active cells and inactive cells that
        /// are in a changed state (deviating from the one that would be
        /// procedurally generated).
        /// </summary>
        private Dictionary<ulong, CellInfo> _cellInfo = new Dictionary<ulong, CellInfo>();

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
                    var random = new MersenneTwister((ulong)new Hasher().Put(info.Id).Put(WorldSeed).Value);

                    // Check if we have a changed cell info.
                    if (!_cellInfo.ContainsKey(info.Id))
                    {
                        // No, generate the default one. Get an independent
                        // randomizer to avoid different results in other
                        // sampling operations from when we have an existing
                        // cell info.
                        var independentRandom = new MersenneTwister((ulong)new Hasher().Put(info.Id).Put(WorldSeed).Value);

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
                            default:
                                faction = Factions.NpcFactionC;
                                break;
                        }

                        // Figure out our tech level.
                        // TODO make dependent on distance to center / start system.
                        var techLevel = independentRandom.NextInt32(3);

                        // Create the cell and push it.
                        _cellInfo.Add(info.Id, new CellInfo(faction, techLevel));
                    }

                    // Get center of our cell.
                    const int cellSize = CellSystem.CellSize;
                    var center = new FarPosition(cellSize * info.X + (cellSize >> 1), cellSize * info.Y + (cellSize >> 1));

                    // Check if it's the start system or not.
                    if (info.X == 0 && info.Y == 0)
                    {
                        // It is, use a predefined number of planets and moons,
                        // and make sure it's a solar system.
                        FactoryLibrary.SampleSunSystem(Manager, "home_system", center, random);
                    }
                    else
                    {
                        // It isn't, randomize.
                        FactoryLibrary.SampleSunSystem(Manager, "sunsystem_1", center, random);
                    }

                    // Find nearby active cells and the stations in them, mark
                    // them as possible targets for all station sin this cell,
                    // and let them know about our existence, as well.
                    var cellSystem = (CellSystem)Manager.GetSystem(CellSystem.TypeId);
                    var stations = _cellInfo[info.Id].Stations;
                    for (var ny = info.Y - 1; ny <= info.Y + 1; ny++)
                    {
                        for (var nx = info.X - 1; nx <= info.X + 1; nx++)
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
                                        var spawn = ((ShipSpawner)Manager.GetComponent(stationId, ShipSpawner.TypeId));
                                        foreach (var otherStationId in stations)
                                        {
                                            spawn.Targets.Add(otherStationId);
                                        }
                                    }
                                    // Tell our own stations.
                                    foreach (var stationId in stations)
                                    {
                                        var spawner = ((ShipSpawner)Manager.GetComponent(stationId, ShipSpawner.TypeId));
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

        #region Serialization

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
            var numCells = packet.ReadInt32();
            for (var i = 0; i < numCells; i++)
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
            hasher.Put(WorldSeed);
            hasher.Put(_cellInfo.Values);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Servers as a copy constructor that returns a new instance of the same
        /// type that is freshly initialized.
        /// 
        /// <para>
        /// This takes care of duplicating reference types to a new copy of that
        /// type (e.g. collections).
        /// </para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (UniverseSystem)base.NewInstance();

            copy._cellInfo = new Dictionary<ulong, CellInfo>();

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// 
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <remarks>The manager for the system to copy into must be set to the
        /// manager into which the system is being copied.</remarks>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (UniverseSystem)into;

            copy.WorldSeed = WorldSeed;

            copy._cellInfo.Clear();
            foreach (var cellInfo in _cellInfo)
            {
                copy._cellInfo.Add(cellInfo.Key, cellInfo.Value.DeepCopy());
            }
        }

        #endregion

        #region CellInfo

        /// <summary>
        /// Class used to track persistent information on a single cell.
        /// </summary>
        public sealed class CellInfo : IPacketizable, IHashable
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
            private int _techLevel;

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
                    .Put(Dirty)
                    .Put((int)_faction)
                    .Put(_techLevel);
            }

            #endregion

            #region Copying

            internal CellInfo DeepCopy()
            {
                var copy = (CellInfo)MemberwiseClone();
                
                copy.Stations = new List<int>(Stations);

                return copy;
            }

            #endregion
        }

        #endregion
    }
}
