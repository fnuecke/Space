using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Systems.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Keeps track of a global grid of cells which can be either alive or
    /// dead, depending on whether a player avatar is inside the cell or one
    /// of its neighboring cells, or not.
    /// 
    /// <para>
    /// Also keeps a more fine grained index over transform components to allow
    /// relatively quick requests for nearby transform components (nearest
    /// neighbor search).
    /// </para>
    /// </summary>
    public class CellSystem : AbstractComponentSystem<NullParameterization, NullParameterization>
    {
        #region Constants

        /// <summary>
        /// Dictates the size of cells, where the actual cell size is 2 to the
        /// power of this value.
        /// </summary>
        public const int CellSizeShiftAmount = 17;

        /// <summary>
        /// The size of a single cell in world units (normally: pixels).
        /// </summary>
        public const int CellSize = 1 << CellSizeShiftAmount;

        #endregion

        #region Properties

        /// <summary>
        /// A list of the IDs of all cells that are currently active.
        /// </summary>
        public IEnumerator<ulong> ActiveCells { get { return _livingCells.GetEnumerator(); } }

        #endregion

        #region Fields

        /// <summary>
        /// List of cells that are currently marked as alive.
        /// </summary>
        private HashSet<ulong> _livingCells = new HashSet<ulong>();

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused each update, avoids memory re-allocation.
        /// </summary>
        private HashSet<ulong> _newCells = new HashSet<ulong>();

        /// <summary>
        /// Reused each update, avoids memory re-allocation.
        /// </summary>
        private HashSet<ulong> _bornCells = new HashSet<ulong>();

        /// <summary>
        /// Reused each update, avoids memory re-allocation.
        /// </summary>
        private HashSet<ulong> _deceasedCells = new HashSet<ulong>();

        #endregion

        #region Constructor

        public CellSystem()
        {
            this.ShouldSynchronize = true;
        }

        #endregion

        #region Accept avatar components

        protected override bool SupportsComponentUpdate(AbstractComponent component)
        {
            return component.GetType() == typeof(Avatar);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Checks all players' positions to determine which cells are active
        /// and which are not. Sends messages if a cell's state changes.
        /// </summary>
        /// <param name="updateType">The update type.</param>
        /// <param name="frame">The frame the update applies to.</param>
        public override void Update(long frame)
        {
            // Check the positions of all avatars to check which cells
            // should live, and which should die / stay dead.
            foreach (var avatar in UpdateableComponents)
            {
                var transform = avatar.Entity.GetComponent<Transform>();
                if (transform != null)
                {
                    int x = ((int)transform.Translation.X) >> CellSizeShiftAmount;
                    int y = ((int)transform.Translation.Y) >> CellSizeShiftAmount;
                    AddCellAndNeighbors(x, y, _newCells);
                }
            }

            // Get the cells that became alive.
            _bornCells.UnionWith(_newCells);
            _bornCells.ExceptWith(_livingCells);
            foreach (var bornCell in _bornCells)
            {
                var xy = CoordinateIds.Split(bornCell);
                var message = CellStateChanged.Create(xy.Item1, xy.Item2, bornCell, true);
                Manager.SendSystemMessage(ref message);
            }
            _bornCells.Clear();

            // Get the cells that died.
            _deceasedCells.UnionWith(_livingCells);
            _deceasedCells.ExceptWith(_newCells);
            foreach (var deceasedCell in _deceasedCells)
            {
                var xy = CoordinateIds.Split(deceasedCell);
                var message = CellStateChanged.Create(xy.Item1, xy.Item2, deceasedCell, false);
                Manager.SendSystemMessage(ref message);
            }
            _deceasedCells.Clear();

            _livingCells.Clear();
            _livingCells.UnionWith(_newCells);

            _newCells.Clear();
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Tests if the specified cell is currently active.
        /// </summary>
        /// <param name="cellId">The id of the cell to check/</param>
        /// <returns>Whether the cell is active or not.</returns>
        public bool IsCellActive(ulong cellId)
        {
            return _livingCells.Contains(cellId);
        }

        /// <summary>
        /// Gets the cell id for a given coordinate pair.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>The id of the cell containing that coordinate pair.</returns>
        public static ulong GetCellIdFromCoordinates(int x, int y)
        {
            return CoordinateIds.Combine(x >> CellSizeShiftAmount, y >> CellSizeShiftAmount);
        }

        /// <summary>
        /// Gets the cell id for a given position.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>The id of the cell containing that position.</returns>
        public static ulong GetCellIdFromCoordinates(ref Vector2 position)
        {
            return GetCellIdFromCoordinates((int)position.X, (int)position.Y);
        }

        /// <summary>
        /// Adds the combined coordinates for all neighboring cells and the
        /// specified cell itself to the specified set of cells.
        /// </summary>
        /// <param name="x">The x coordinate of the main cell.</param>
        /// <param name="y">The y coordinate of the main cell.</param>
        /// <param name="cells">The set of cells to add to.</param>
        private void AddCellAndNeighbors(int x, int y, HashSet<ulong> cells)
        {
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    cells.Add(CoordinateIds.Combine(nx, ny));
                }
            }
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public override Packet Packetize(Packet packet)
        {
            packet.Write(_livingCells.Count);
            foreach (var cellId in _livingCells)
            {
                packet.Write(cellId);
            }

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            _livingCells.Clear();
            int numCells = packet.ReadInt32();
            for (int i = 0; i < numCells; i++)
            {
                _livingCells.Add(packet.ReadUInt64());
            }
        }

        public override void Hash(Hasher hasher)
        {
            foreach (var cellId in _livingCells)
            {
                hasher.Put(BitConverter.GetBytes(cellId));
            }
        }

        public override IComponentSystem DeepCopy(IComponentSystem into)
        {
            var copy = (CellSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy._livingCells.Clear();
                copy._livingCells.UnionWith(_livingCells);
            }
            else
            {
                copy._livingCells = new HashSet<ulong>(_livingCells);
                copy._newCells = new HashSet<ulong>();
                copy._bornCells = new HashSet<ulong>();
                copy._deceasedCells = new HashSet<ulong>();
            }

            return copy;
        }

        #endregion
    }
}
