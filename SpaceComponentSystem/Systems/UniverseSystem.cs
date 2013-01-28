using System;
using System.Collections.Generic;
using System.IO;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Physics;
using Engine.ComponentSystem.Physics.Components;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Graphics.PolygonTools;
using Engine.Random;
using Engine.Serialization;
using Engine.Util;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    ///     System responsible for tracking astronomical objects and relatively stationary objects (such as space stations) in
    ///     active cells.
    ///     <para>
    ///         It also tracks general stats about a cell, such as the predominant faction in the system, state of stations
    ///         and so on. This information is only stored permanently if it changed, however. Otherwise it is re-generated
    ///         procedurally whenever a cell gets re-activated.
    ///     </para>
    /// </summary>
    public sealed class UniverseSystem : AbstractSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>The base seed for the current game world.</summary>
        public ulong WorldSeed { get; set; }

        #endregion

        #region Fields

        /// <summary>
        ///     Tracks cell information for active cells and inactive cells that are in a changed state (deviating from the
        ///     one that would be procedurally generated).
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        private Dictionary<ulong, CellInfo> _cellInfo = new Dictionary<ulong, CellInfo>();

        #endregion

        #region Accessors

        /// <summary>Get the static cell information for the cell with the given id.</summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        public CellInfo GetCellInfo(ulong cellId)
        {
            return _cellInfo[cellId];
        }

        #endregion

        #region Logic

        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<CellStateChanged>(OnCellStateChanged);
        }

        private void OnCellStateChanged(CellStateChanged message)
        {
            // Get random generator based on world seed and cell location.
            var hasher = new Hasher();
            hasher.Write(message.Id).Write(WorldSeed);
            var random = new MersenneTwister(hasher.Value);

            if (message.IsSubCell)
            {
                if (message.IsActive)
                {
                    PopulateSubCell(message, random);
                }
            }
            else
            {
                if (message.IsActive)
                {
                    PopulateCell(message, random);
                }
                else
                {
                    // Remove cell info only if it does not deviate from the
                    // procedural values.
                    if (!_cellInfo[message.Id].Dirty)
                    {
                        _cellInfo.Remove(message.Id);
                    }
                    else
                    {
                        _cellInfo[message.Id].Stations.Clear();
                    }
                }
            }
        }

        private void PopulateCell(CellStateChanged message, IUniformRandom random)
        {
            // Check if we have a changed cell info.
            if (!_cellInfo.ContainsKey(message.Id))
            {
                // No, generate the default one. Get an independent
                // randomizer to avoid different results in other
                // sampling operations from when we have an existing
                // cell info.
                var hasher = new Hasher();
                hasher.Write(message.Id).Write(WorldSeed);
                var independentRandom = new MersenneTwister(hasher.Value);

                // Figure out which faction should own this cell.
                Factions faction;
                switch (independentRandom.NextInt32(3))
                {
                    case 0:
                        faction = Factions.NPCFactionA;
                        break;
                    case 1:
                        faction = Factions.NPCFactionB;
                        break;
                    default:
                        faction = Factions.NPCFactionC;
                        break;
                }

                // Figure out our tech level.
                // TODO make dependent on distance to center / start system.
                var techLevel = independentRandom.NextInt32(3);

                // Create the cell and push it.
                _cellInfo.Add(message.Id, new CellInfo(faction, techLevel));
            }

            // Get center of our cell.
            const int cellSize = CellSystem.CellSize;
            var center = new FarPosition(
                cellSize * message.X + (cellSize >> 1),
                cellSize * message.Y + (cellSize >> 1));

            // Check if it's the start system or not.
            if (message.X == 0 && message.Y == 0)
            {
                // It is, use a predefined number of planets and moons,
                // and make sure it's a solar system.
                FactoryLibrary.SampleSunSystem(Manager, "solar_system", center, random);
            }
            else
            {
                // It isn't, randomize.
                FactoryLibrary.SampleSunSystem(Manager, "sunsystem_1", center, random);
            }

            // Find nearby active cells and the stations in them, mark
            // them as possible targets for all station sin this cell,
            // and let them know about our existence, as well.
            var cellSystem = (CellSystem) Manager.GetSystem(CellSystem.TypeId);
            var stations = _cellInfo[message.Id].Stations;
            for (var ny = message.Y - 1; ny <= message.Y + 1; ny++)
            {
                for (var nx = message.X - 1; nx <= message.X + 1; nx++)
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
                    if (((message.X + message.Y + ny + nx) & 1) == 0)
                    {
                        // Get the id, only mark the station if we have
                        // info on it and it's an enemy cell.
                        var id = BitwiseMagic.Pack(nx, ny);
                        if (cellSystem.IsCellActive(id) &&
                            _cellInfo.ContainsKey(id) &&
                            (_cellInfo[id].Faction.IsAlliedTo(_cellInfo[message.Id].Faction)))
                        {
                            // Tell the other stations.
                            foreach (var stationId in _cellInfo[id].Stations)
                            {
                                var spawn = ((ShipSpawner) Manager.GetComponent(stationId, ShipSpawner.TypeId));
                                foreach (var otherStationId in stations)
                                {
                                    spawn.Targets.Add(otherStationId);
                                }
                            }
                            // Tell our own stations.
                            foreach (var stationId in stations)
                            {
                                var spawner = ((ShipSpawner) Manager.GetComponent(stationId, ShipSpawner.TypeId));
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

        private void PopulateSubCell(CellStateChanged message, IUniformRandom random)
        {
            // We randomly spawn some asteroid fields in sub-cells. These are laid out as follows:
            // - we decide how many fields we want to spawn.
            // - we build a grid inside the cell that has enough entries for our asteroid fields,
            //   i.e. we take the next higher squared number (e.g. if we have 5 fields, we generate
            //   a 3x3 grid).
            // - For each field we randomly pick one such grid cell to determine it's basic position,
            //   and shift it to a random position inside that grid cell.
            // - To position the actual asteroids, we want it to be somewhat circular, but it should
            //   not look too regular. We lay out our asteroids in a spiral, on which we place our
            //   asteroids in a fixed interval. Finally we add some random offset to make the spiral
            //   non-obvious.

            // Number of asteroid fields in this cell?
            var fieldCount = random.NextInt32(8, 12);

            // Determine number of rows and cols for base positions.
            var cols = (int) Math.Ceiling(Math.Sqrt(fieldCount));
            
            // Build sampling list.
            var cells = new List<Tuple<int, int>>(cols * cols);
            for (var x = 0; x < cols; ++x)
            {
                for (var y = 0; y < cols; ++y)
                {
                    cells.Add(Tuple.Create(x, y));
                }
            }

            // Size of a cell in our sub grid.
            var gridSize = CellSystem.SubCellSize / (float) cols;

            // Cell top left corner position.
            var cellPosition = new FarPosition(message.X, message.Y) * CellSystem.SubCellSize;

            // Generate asteroid fields.
            for (var i = 0; i < fieldCount; ++i)
            {
                // Get base position.
                var positionIndex = random.NextInt32(cells.Count);
                var fieldIndex = cells[positionIndex];
                cells.RemoveAt(positionIndex);

                var asteroidCount = random.NextInt32(30, 60);
                var center = cellPosition + new FarPosition(
                    fieldIndex.Item1 * gridSize + (float) random.NextDouble(0, gridSize),
                    fieldIndex.Item2 * gridSize + (float) random.NextDouble(0, gridSize));
                
                // We grow our asteroid fields as spirals, with a little jitter.
                const float jitter = 2.5f;
                const float radiusStep = 0.4f; //< how fast we move outwards.
                const float angleStep = 2.25f; //< the asteroid interval on the spiral.
                var theta = angleStep / radiusStep;

                // Create first one at the center.
                CreateAsteroid(center, random);

                // Generate rest of the spiral.
                for (var j = 1; j < asteroidCount; ++j)
                {
                    // Compute position in our spiral.
                    var radius = radiusStep * theta;
                    var position = center;
                    position.X += (float) Math.Cos(theta) * radius;
                    position.Y += (float) Math.Sin(theta) * radius;
                    position.X += (float) random.NextDouble(-jitter / 2, jitter / 2);
                    position.Y += (float) random.NextDouble(-jitter / 2, jitter / 2);
                    theta += angleStep / radius;

                    CreateAsteroid(position, random);
                }
            }
        }

        // TODO in case we need this somewhere else it might be a good idea to move this to the EntityFactory
        private void CreateAsteroid(FarPosition position, IUniformRandom random)
        {
            var content = ((GraphicsDeviceSystem) Manager.GetSystem(GraphicsDeviceSystem.TypeId)).Content;

            // Randomly scale and rotate it.
            var scale = (float) random.NextDouble(0.5f, 1f);
            var angle = (float) random.NextDouble() * MathHelper.TwoPi;

            // Determine shape for physics system.
            var textureName = "Textures/Asteroids/rock_" + random.NextInt32(1, 14);
            var texture = content.Load<Texture2D>(textureName);
            var hull = new List<Vector2>(TextureConverter.DetectVertices(texture, 8f, textureName: textureName)[0]);
            for (var k = 0; k < hull.Count; ++k)
            {
                hull[k] -= new Vector2(texture.Width / 2f, texture.Height / 2f);
                hull[k] = XnaUnitConversion.ToSimulationUnits(hull[k]) * scale;
            }
            var polygons = EarClipDecomposer.ConvexPartition(hull);

            // Create physical representation.
            var entity = Manager.AddEntity();
            var body = Manager.AddBody(entity, position, angle, Body.BodyType.Dynamic);
            foreach (var polygon in polygons)
            {
                Manager.AttachPolygon(body, polygon, density: 1000f, restitution: 0.2f);
            }
            // Slow down to allow reaching sleep state again.
            body.LinearDamping = 0.05f * Space.Util.Settings.TicksPerSecond;
            body.AngularDamping = 0.025f * Space.Util.Settings.TicksPerSecond;
                    
            // Rendering stuff.
            Manager.AddComponent<Indexable>(entity).Initialize(CameraSystem.IndexId);
            Manager.AddComponent<Indexable>(entity).Initialize(InterpolationSystem.IndexId);
            Manager.AddComponent<SimpleTextureDrawable>(entity).Initialize(textureName, scale);

            // Auto removal.
            Manager.AddComponent<CellDeath>(entity).Initialize(true);
            Manager.AddComponent<Indexable>(entity).Initialize(CellSystem.CellDeathAutoRemoveIndexId);

            // Make it destructible.
            var health = Manager.AddComponent<Health>(entity);
            health.Value = health.MaxValue = 200 * scale;
            health.Regeneration = 0f;

            // As they don't move on their own, start asteroids as sleeping to save performance.
            body.IsAwake = false;
        }

        #endregion

        #region Serialization

        /// <summary>Packetizes the specified packet.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns></returns>
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);

            packet.Write(_cellInfo.Count);
            foreach (var item in _cellInfo)
            {
                packet.Write(item.Key);
                packet.Write(item.Value);
            }

            return packet;
        }

        /// <summary>Depacketizes the specified packet.</summary>
        /// <param name="packet">The packet.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            _cellInfo.Clear();
            var cellCount = packet.ReadInt32();
            for (var i = 0; i < cellCount; i++)
            {
                var key = packet.ReadUInt64();
                var value = packet.ReadPacketizable<CellInfo>();
                _cellInfo.Add(key, value);
            }
        }

        public override StreamWriter Dump(StreamWriter w, int indent)
        {
            base.Dump(w, indent);

            w.AppendIndent(indent).Write("StoredCellCount = ");
            w.Write(_cellInfo.Count);
            w.AppendIndent(indent).Write("Cells = {");
            foreach (var item in _cellInfo)
            {
                w.AppendIndent(indent + 1).Write(item.Key);
                w.Write(" = ");
                w.Dump(item.Value, indent + 1);
            }
            w.AppendIndent(indent).Write("}");

            return w;
        }

        #endregion

        #region Copying

        /// <summary>
        ///     Servers as a copy constructor that returns a new instance of the same type that is freshly initialized.
        ///     <para>This takes care of duplicating reference types to a new copy of that type (e.g. collections).</para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (UniverseSystem) base.NewInstance();

            copy._cellInfo = new Dictionary<ulong, CellInfo>();

            return copy;
        }

        /// <summary>
        ///     Creates a deep copy of the system. The passed system must be of the same type.
        ///     <para>
        ///         This clones any contained data types to return an instance that represents a complete copy of the one passed
        ///         in.
        ///     </para>
        /// </summary>
        /// <remarks>The manager for the system to copy into must be set to the manager into which the system is being copied.</remarks>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (UniverseSystem) into;

            copy._cellInfo.Clear();
            foreach (var cellInfo in _cellInfo)
            {
                copy._cellInfo.Add(cellInfo.Key, cellInfo.Value.DeepCopy());
            }
        }

        #endregion

        #region CellInfo

        /// <summary>Class used to track persistent information on a single cell.</summary>
        public sealed class CellInfo : IPacketizable
        {
            #region Properties

            /// <summary>
            ///     Flag to check if the values deviate from the procedurally generated ones, so that we know whether to
            ///     persistently store the settings of this cell, or not.
            /// </summary>
            public bool Dirty { get; private set; }

            /// <summary>The predominant faction in the system.</summary>
            public Factions Faction
            {
                get { return _faction; }
                set
                {
                    _faction = value;
                    Dirty = true;
                }
            }

            /// <summary>The current tech level of the cell.</summary>
            public int TechLevel
            {
                get { return _techLevel; }
                set
                {
                    _techLevel = value;
                    Dirty = true;
                }
            }

            [PacketizerIgnore]
            public List<int> Stations = new List<int>();

            #endregion

            #region Fields

            /// <summary>Actual value for tech level.</summary>
            private Factions _faction;

            /// <summary>Actual value for tech level.</summary>
            private int _techLevel;

            #endregion

            #region Constructor

            public CellInfo(Factions faction, int techLevel)
            {
                // Assign directly to fields, to avoid setting the dirty flag.
                _faction = faction;
                _techLevel = techLevel;
            }

            public CellInfo() {}

            #endregion

            #region Copying

            internal CellInfo DeepCopy()
            {
                var copy = (CellInfo) MemberwiseClone();

                copy.Stations = new List<int>(Stations);

                return copy;
            }

            #endregion
        }

        #endregion
    }
}