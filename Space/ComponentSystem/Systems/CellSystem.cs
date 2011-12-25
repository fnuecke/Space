using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
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
    public class CellSystem : AbstractComponentSystem<AvatarParameterization>
    {
        #region Constants

        /// <summary>
        /// Dictates the size of cells, where the actual cell size is 2 to the
        /// power of this value.
        /// </summary>
        private const int _cellSize = 14;

        #endregion

        #region Properties

        /// <summary>
        /// The size of a single cell in world units (normally: pixels).
        /// </summary>
        public int CellSize { get { return 2 << _cellSize; } }

        #endregion

        #region Fields
        
        /// <summary>
        /// List of cells that are currently marked as alive.
        /// </summary>
        private HashSet<ulong> _livingCells = new HashSet<ulong>();

        #endregion

        #region Constructor

        public CellSystem()
        {
            this.ShouldSynchronize = true;
        }

        #endregion

        #region Logic
        
        /// <summary>
        /// Checks all players' positions to determine which cells are active
        /// and which are not. Sends messages if a cell's state changes.
        /// </summary>
        /// <param name="updateType">The update type.</param>
        /// <param name="frame">The frame the update applies to.</param>
        public override void Update(ComponentSystemUpdateType updateType, long frame)
        {
            if (updateType == ComponentSystemUpdateType.Logic)
            {
                // Check the positions of all avatars to check which cells
                // should live, and which should die / stay dead.
                var newCells = new HashSet<ulong>();
                foreach (var avatar in Components)
                {
                    var transform = avatar.Entity.GetComponent<Transform>();
                    if (transform != null)
                    {
                        int x = ((int)transform.Translation.X) >> _cellSize;
                        int y = ((int)transform.Translation.Y) >> _cellSize;
                        AddCellAndNeighbors(x, y, newCells);
                    }
                }

                // Get the cells that became alive.
                var bornCells = new HashSet<ulong>(newCells);
                bornCells.ExceptWith(_livingCells);
                foreach (var bornCell in bornCells)
                {
                    var xy = Split(bornCell);
                    Manager.SendMessage(CellStateChanged.Create(xy.Item1, xy.Item2, bornCell, true));
                }

                // Get the cells that died.
                var deceasedCells = new HashSet<ulong>(_livingCells);
                deceasedCells.ExceptWith(newCells);
                foreach (var deceasedCell in deceasedCells)
                {
                    var xy = Split(deceasedCell);
                    Manager.SendMessage(CellStateChanged.Create(xy.Item1, xy.Item2, deceasedCell, false));
                }

                _livingCells = newCells;
            }
        }

        #endregion

        #region Utility methods
        
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
                    cells.Add(Combine(nx, ny));
                }
            }   
        }

        /// <summary>
        /// Combine two coordinates into one.
        /// </summary>
        /// <param name="x">The coordinates to merge.</param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static ulong Combine(int x, int y)
        {
            return ((ulong)x << 32) | (uint)y;
        }

        private static Tuple<int, int> Split(ulong xy)
        {
            return Tuple.Create((int)(xy >> 32), (int)(xy & 0xFFFFFFFF));
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

        public override object Clone()
        {
            var copy = (CellSystem)base.Clone();

            copy._livingCells = new HashSet<ulong>(_livingCells);

            return copy;
        }

        #endregion
    }
}
