using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    ///     Keeps track of a global grid of cells which can be either alive or dead, depending on whether a player avatar is
    ///     inside the cell or one of its neighboring cells, or not.
    ///     <para>
    ///         Also keeps a more fine grained index over transform components to allow relatively quick requests for nearby
    ///         transform components (nearest neighbor search).
    ///     </para>
    /// </summary>
    public sealed class CellSystem : AbstractSystem, IUpdatingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Constants

        /// <summary>Dictates the size of cells, where the actual cell size is 2 to the power of this value.</summary>
        public const int CellSizeShiftAmount = 11;

        /// <summary>The size of a single cell in world units.</summary>
        public const int CellSize = 1 << CellSizeShiftAmount;
        
        /// <summary>Dictates the size of sub cells, where the actual cell size is 2 to the power of this value.</summary>
        public const int SubCellSizeShiftAmount = 8;

        /// <summary>The size of a single sub cell in world units.</summary>
        public const int SubCellSize = 1 << SubCellSizeShiftAmount;

        /// <summary>Index used to track entities that should automatically be removed when a cell dies, and they are in that cell.</summary>
        public static readonly ulong CellDeathAutoRemoveIndexGroupMask = 1ul << IndexSystem.GetGroup();

        /// <summary>
        ///     The time to wait before actually killing of a cell after it has gotten out of reach. This is to avoid
        ///     reallocating cells over and over again, if a player flies along a cell border or keeps flying back and forth over
        ///     it. Unit is in game frames.
        /// </summary>
        private const int CellDeathDelay = (int) (5 * Settings.TicksPerSecond);

        #endregion

        #region Properties

        /// <summary>A list of the IDs of all cells that are currently active.</summary>
        public IEnumerator<ulong> ActiveCells
        {
            get { return _livingCells.GetEnumerator(); }
        }

        /// <summary>A list of the IDs of all cells that are currently active.</summary>
        public IEnumerator<ulong> ActiveSubCells
        {
            get { return _livingSubCells.GetEnumerator(); }
        }

        #endregion

        #region Fields

        /// <summary>List of cells that are currently marked as alive.</summary>
        [CopyIgnore, PacketizerIgnore]
        private HashSet<ulong> _livingCells = new HashSet<ulong>();

        /// <summary>Cells awaiting cleanup, with the time when they became invalid.</summary>
        [CopyIgnore, PacketizerIgnore]
        private Dictionary<ulong, long> _pendingCells = new Dictionary<ulong, long>();

        /// <summary>List of cells that are currently marked as alive.</summary>
        [CopyIgnore, PacketizerIgnore]
        private HashSet<ulong> _livingSubCells = new HashSet<ulong>();

        /// <summary>Cells awaiting cleanup, with the time when they became invalid.</summary>
        [CopyIgnore, PacketizerIgnore]
        private Dictionary<ulong, long> _pendingSubCells = new Dictionary<ulong, long>();

        #endregion

        #region Single-Allocation

        /// <summary>Reused each update, avoids memory re-allocation.</summary>
        [CopyIgnore, PacketizerIgnore]
        private HashSet<ulong> _reusableNewCellIds = new HashSet<ulong>();

        /// <summary>Reused each update, avoids memory re-allocation.</summary>
        [CopyIgnore, PacketizerIgnore]
        private HashSet<ulong> _reusableBornCellsIds = new HashSet<ulong>();

        /// <summary>Reused each update, avoids memory re-allocation.</summary>
        [CopyIgnore, PacketizerIgnore]
        private HashSet<ulong> _reusableDeceasedCellsIds = new HashSet<ulong>();

        /// <summary>Reused each update, avoids memory re-allocation.</summary>
        [CopyIgnore, PacketizerIgnore]
        private List<ulong> _reusablePendingList = new List<ulong>();

        /// <summary>Reused for querying entities contained in a dying cell.</summary>
        [CopyIgnore, PacketizerIgnore]
        private ISet<int> _reusableComponentList = new HashSet<int>();

        #endregion

        #region Accessors

        /// <summary>Tests if the specified cell is currently active.</summary>
        /// <param name="cellId">The id of the cell to check/</param>
        /// <returns>Whether the cell is active or not.</returns>
        public bool IsCellActive(ulong cellId)
        {
            return _livingCells.Contains(cellId) || _pendingCells.ContainsKey(cellId);
        }
        
        /// <summary>Tests if the specified cell is currently active.</summary>
        /// <param name="cellId">The id of the cell to check/</param>
        /// <returns>Whether the cell is active or not.</returns>
        public bool IsSubCellActive(ulong cellId)
        {
            return _livingSubCells.Contains(cellId) || _pendingSubCells.ContainsKey(cellId);
        }

        /// <summary>Gets the cell id for a given position.</summary>
        /// <param name="position">The position.</param>
        /// <returns>The id of the cell containing that position.</returns>
        public static ulong GetCellIdFromCoordinates(FarPosition position)
        {
            const float segmentDivisor = (FarValue.SegmentSizeShiftAmount < CellSizeShiftAmount)
                ? 1f / (float) (1 << (CellSizeShiftAmount - FarValue.SegmentSizeShiftAmount))
                : (float) (1 << (FarValue.SegmentSizeShiftAmount - CellSizeShiftAmount));
            const float offsetDivisor = 1f / (float)FarValue.SegmentSize / (float)CellSize;
            return BitwiseMagic.Pack(
                (int) Math.Floor(position.X.Segment * segmentDivisor + position.X.Offset * offsetDivisor),
                (int) Math.Floor(position.Y.Segment * segmentDivisor + position.Y.Offset * offsetDivisor));
        }

        /// <summary>Gets the cell coordinates for the specified cell id.</summary>
        /// <param name="id">The id of the cell.</param>
        public static FarPosition GetCellCoordinatesFromId(ulong id)
        {
            int x, y;
            BitwiseMagic.Unpack(id, out x, out y);
            return new FarPosition(x << CellSizeShiftAmount, y << CellSizeShiftAmount);
        }
        
        /// <summary>Gets the cell id for a given position.</summary>
        /// <param name="position">The position.</param>
        /// <returns>The id of the cell containing that position.</returns>
        public static ulong GetSubCellIdFromCoordinates(FarPosition position)
        {
            const float segmentDivisor = (FarValue.SegmentSizeShiftAmount < SubCellSizeShiftAmount)
                ? 1f / (float) (1 << (SubCellSizeShiftAmount - FarValue.SegmentSizeShiftAmount))
                : (float) (1 << (FarValue.SegmentSizeShiftAmount - SubCellSizeShiftAmount));
            const float offsetDivisor = 1f / (float)FarValue.SegmentSize / (float)SubCellSize;
            return BitwiseMagic.Pack(
                (int) Math.Floor(position.X.Segment * segmentDivisor + position.X.Offset * offsetDivisor),
                (int) Math.Floor(position.Y.Segment * segmentDivisor + position.Y.Offset * offsetDivisor));
        }

        /// <summary>Gets the cell coordinates for the specified cell id.</summary>
        /// <param name="id">The id of the cell.</param>
        public static FarPosition GetSubCellCoordinatesFromId(ulong id)
        {
            int x, y;
            BitwiseMagic.Unpack(id, out x, out y);
            return new FarPosition(x << SubCellSizeShiftAmount, y << SubCellSizeShiftAmount);
        }

        #endregion

        #region Logic
        
        /// <summary>Store for performance.</summary>
        private static readonly int TransformTypeId = Engine.ComponentSystem.Manager.GetComponentTypeId<ITransform>();

        /// <summary>
        ///     Checks all players' positions to determine which cells are active and which are not. Sends messages if a
        ///     cell's state changes.
        /// </summary>
        /// <param name="frame">The frame the update applies to.</param>
        public void Update(long frame)
        {
            // Only check from time to time.
            if (frame % 10 != 0)
            {
                return;
            }

            UpdateCellState(frame, false, CellSize, _livingCells, _pendingCells);
            UpdateCellState(frame, true, SubCellSize, _livingSubCells, _pendingSubCells);
        }

        private void UpdateCellState(long frame, bool isSubCell, int size, ISet<ulong> living, IDictionary<ulong, long> pending)
        {
            // Check the positions of all avatars to check which cells
            // should live, and which should die / stay dead.
            var avatarSystem = (AvatarSystem) Manager.GetSystem(AvatarSystem.TypeId);
            foreach (var avatar in avatarSystem.Avatars)
            {
                var transform = (ITransform) Manager.GetComponent(avatar, TransformTypeId);
                int x, y;
                if (isSubCell)
                {
                    BitwiseMagic.Unpack(GetSubCellIdFromCoordinates(transform.Position), out x, out y);
                }
                else
                {
                    BitwiseMagic.Unpack(GetCellIdFromCoordinates(transform.Position), out x, out y);
                }
                AddCellAndNeighbors(x, y, _reusableNewCellIds);
            }

            // Get the cells that became alive, notify systems and components.
            _reusableBornCellsIds.UnionWith(_reusableNewCellIds);
            _reusableBornCellsIds.ExceptWith(living);
            CellStateChanged changedMessage;
            changedMessage.IsSubCell = isSubCell;
            foreach (var cellId in _reusableBornCellsIds)
            {
                // If its in there, remove it from the pending list.
                if (!pending.Remove(cellId))
                {
                    // Notify if cell wasn't alive already.
                    changedMessage.Id = cellId;
                    BitwiseMagic.Unpack(cellId, out changedMessage.X, out changedMessage.Y);
                    changedMessage.IsActive = true;
                    Manager.SendMessage(changedMessage);
                }
            }
            _reusableBornCellsIds.Clear();

            // Check pending list, kill off old cells, notify systems etc.
            var cellDeathIndex = ((IndexSystem) Manager.GetSystem(IndexSystem.TypeId))[CellDeathAutoRemoveIndexGroupMask];
            _reusablePendingList.AddRange(pending.Keys);
            foreach (var cellId in _reusablePendingList)
            {
                // Are we still delaying?
                if (frame - pending[cellId] <= CellDeathDelay)
                {
                    continue;
                }

                // Timed out, kill it for good.
                pending.Remove(cellId);

                int x, y;
                BitwiseMagic.Unpack(cellId, out x, out y);

                // Notify.
                changedMessage.Id = cellId;
                changedMessage.X = x;
                changedMessage.Y = y;
                changedMessage.IsActive = false;
                Manager.SendMessage(changedMessage);

                // Kill any remaining entities in the area covered by the
                // cell that just died.
                FarRectangle cellBounds;
                cellBounds.X = x * size;
                cellBounds.Y = y * size;
                cellBounds.Width = size;
                cellBounds.Height = size;
                cellDeathIndex.Find(cellBounds, _reusableComponentList);
                foreach (IIndexable neighbor in _reusableComponentList.Select(Manager.GetComponentById))
                {
                    var cellDeath = (CellDeath) Manager.GetComponent(neighbor.Entity, CellDeath.TypeId);
                    if (cellDeath.IsForSubCell == isSubCell)
                    {
                        Manager.RemoveEntity(neighbor.Entity);
                    }
                }
                _reusableComponentList.Clear();
            }
            _reusablePendingList.Clear();

            // Get the cells that died, put to pending list.
            _reusableDeceasedCellsIds.UnionWith(living);
            _reusableDeceasedCellsIds.ExceptWith(_reusableNewCellIds);
            foreach (var cellId in _reusableDeceasedCellsIds)
            {
                // Add it to the pending list.
                pending.Add(cellId, frame);
            }
            _reusableDeceasedCellsIds.Clear();

            living.Clear();
            living.UnionWith(_reusableNewCellIds);

            _reusableNewCellIds.Clear();
        }

        #endregion

        #region Utility methods

        /// <summary>
        ///     Adds the combined coordinates for all neighboring cells and the specified cell itself to the specified set of
        ///     cells.
        /// </summary>
        /// <param name="x">The x coordinate of the main cell.</param>
        /// <param name="y">The y coordinate of the main cell.</param>
        /// <param name="cells">The set of cells to add to.</param>
        private static void AddCellAndNeighbors(int x, int y, ISet<ulong> cells)
        {
            for (var ny = y - 1; ny <= y + 1; ny++)
            {
                for (var nx = x - 1; nx <= x + 1; nx++)
                {
                    cells.Add(BitwiseMagic.Pack(nx, ny));
                }
            }
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>Write the object's state to the given packet.</summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);

            packet.Write(_livingCells.Count);
            foreach (var cellId in _livingCells)
            {
                packet.Write(cellId);
            }
            packet.Write(_pendingCells.Count);
            foreach (var pending in _pendingCells)
            {
                packet.Write(pending.Key);
                packet.Write(pending.Value);
            }
            
            packet.Write(_livingSubCells.Count);
            foreach (var cellId in _livingSubCells)
            {
                packet.Write(cellId);
            }
            packet.Write(_pendingSubCells.Count);
            foreach (var pending in _pendingSubCells)
            {
                packet.Write(pending.Key);
                packet.Write(pending.Value);
            }

            return packet;
        }

        /// <summary>Bring the object to the state in the given packet.</summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            _livingCells.Clear();
            var livingCellCount = packet.ReadInt32();
            for (var i = 0; i < livingCellCount; i++)
            {
                _livingCells.Add(packet.ReadUInt64());
            }
            _pendingCells.Clear();
            var pendingCellCount = packet.ReadInt32();
            for (var i = 0; i < pendingCellCount; i++)
            {
                var cell = packet.ReadUInt64();
                var frame = packet.ReadInt64();
                _pendingCells.Add(cell, frame);
            }
            
            _livingSubCells.Clear();
            var livingSubCellCount = packet.ReadInt32();
            for (var i = 0; i < livingSubCellCount; i++)
            {
                _livingSubCells.Add(packet.ReadUInt64());
            }
            _pendingSubCells.Clear();
            var pendingSubCellCount = packet.ReadInt32();
            for (var i = 0; i < pendingSubCellCount; i++)
            {
                var cell = packet.ReadUInt64();
                var frame = packet.ReadInt64();
                _pendingSubCells.Add(cell, frame);
            }
        }

        public override StreamWriter Dump(StreamWriter w, int indent)
        {
            base.Dump(w, indent);

            w.AppendIndent(indent).Write("LivingCells = {");
            {
                var first = true;
                foreach (var cell in _livingCells)
                {
                    if (!first)
                    {
                        w.Write(", ");
                    }
                    first = false;
                    w.Write(cell);
                }
            }
            string.Join(", ", _livingCells);
            w.Write("}");
            w.AppendIndent(indent).Write("PendingCells {");
            {
                var first = true;
                foreach (var cell in _pendingCells)
                {
                    if (!first)
                    {
                        w.Write(", ");
                    }
                    first = false;
                    w.Write(cell.Key);
                    w.Write("@");
                    w.Write(cell.Value);
                }
            }
            w.Write("}");

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
            var copy = (CellSystem) base.NewInstance();

            copy._livingCells = new HashSet<ulong>();
            copy._pendingCells = new Dictionary<ulong, long>();
            copy._livingSubCells = new HashSet<ulong>();
            copy._pendingSubCells = new Dictionary<ulong, long>();
            copy._reusableNewCellIds = new HashSet<ulong>();
            copy._reusableBornCellsIds = new HashSet<ulong>();
            copy._reusableDeceasedCellsIds = new HashSet<ulong>();
            copy._reusablePendingList = new List<ulong>();
            copy._reusableComponentList = new HashSet<int>();

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

            var copy = (CellSystem) into;

            copy._livingCells.Clear();
            copy._livingCells.UnionWith(_livingCells);
            copy._pendingCells.Clear();
            foreach (var item in _pendingCells)
            {
                copy._pendingCells.Add(item.Key, item.Value);
            }

            copy._livingSubCells.Clear();
            copy._livingSubCells.UnionWith(_livingSubCells);
            copy._pendingSubCells.Clear();
            foreach (var item in _pendingSubCells)
            {
                copy._pendingSubCells.Add(item.Key, item.Value);
            }
        }

        #endregion
    }
}