using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;
using Engine.Util;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// This class represents a simple index structure for nearest neighbor
    /// queries. It uses a grid structure for indexing, and will return lists
    /// of entities in cells near a query point.
    /// </summary>
    public sealed class IndexSystem : AbstractComponentSystem<IndexParameterization>
    {
        #region Constants

        /// <summary>
        /// Dictates the size of grid cells, where the actual cell size is 2 to
        /// the power of this value.
        /// </summary>
        public const int GridSizeShiftAmount = 7;

        #endregion

        #region Properties

        /// <summary>
        /// The size of a single grid cell in world units (normally: pixels).
        /// </summary>
        public int GridSize { get { return 1 << GridSizeShiftAmount; } }

        #endregion

        #region Fields

        /// <summary>
        /// The actual index we're using, mapping grid cell ids to a list of
        /// entities currently in that cell.
        /// </summary>
        private Dictionary<ulong, HashSet<int>> _index = new Dictionary<ulong, HashSet<int>>();

        /// <summary>
        /// Reusable parameterization.
        /// </summary>
        private IndexParameterization _parameterization = new IndexParameterization();

        #endregion

        #region Entity lookup

        /// <summary>
        /// Get all entities in the same cell as the query entity, or in a
        /// neighboring cell.
        /// </summary>
        /// <param name="query">The entity to use as a query point.</param>
        /// <returns>All entities in range (including the query entity).</returns>
        public List<IEntity> GetNeighbors(IEntity query)
        {
            return GetNeighbors(query.GetComponent<Transform>().Translation, 1);
        }

        /// <summary>
        /// Get all entities in the same cell as the query entity, or in a
        /// neighboring cell in the given cell range (0 = only the same cell,
        /// 1 = the neighboring, and so on).
        /// </summary>
        /// <param name="query">The entity to use as a query point.</param>
        /// <param name="cellRange">The neighborship rank up to which to
        /// include neighboring cells.</param>
        /// <returns>All entities in range (including the query entity).</returns>
        public List<IEntity> GetNeighbors(IEntity query, int cellRange)
        {
            return GetNeighbors(query.GetComponent<Transform>().Translation, cellRange);
        }

        /// <summary>
        /// Get all entities in the same cell as the query point, or in a
        /// neighboring cell.
        /// </summary>
        /// <param name="query">The point to use as a query point.</param>
        /// <returns>All entities in range.</returns>
        public List<IEntity> GetNeighbors(FPoint query)
        {
            return GetNeighbors(query, 1);
        }

        /// <summary>
        /// Get all entities in the same cell as the query point, or in a
        /// neighboring cell in the given cell range (0 = only the same cell,
        /// 1 = the neighboring, and so on).
        /// </summary>
        /// <param name="query">The point to use as a query point.</param>
        /// <param name="cellRange">The neighborship rank up to which to
        /// include neighboring cells.</param>
        /// <returns>All entities in range.</returns>
        public List<IEntity> GetNeighbors(FPoint query, int cellRange)
        {
            var result = new List<IEntity>();

            // Get actual grid cell index.
            int cellX = ((int)query.X) >> GridSizeShiftAmount;
            int cellY = ((int)query.Y) >> GridSizeShiftAmount;

            // Check all cells in range.
            for (int y = cellY - cellRange; y <= cellY + cellRange; ++y)
            {
                for (int x = cellX - cellRange; x <= cellX + cellRange; ++x)
                {
                    // Check if that cell is tracked.
                    var cellId = CoordinateIds.Combine(x, y);
                    if (_index.ContainsKey(cellId))
                    {
                        // Yes, add all entities in that cell.
                        foreach (var entityId in _index[cellId])
                        {
                            result.Add(Manager.EntityManager.GetEntity(entityId));
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the index as necessary.
        /// </summary>
        /// <param name="updateType">The type of update to perform. We only do logic updates.</param>
        /// <param name="frame">The frame in which this update takes place.</param>
        public override void Update(ComponentSystemUpdateType updateType, long frame)
        {
            if (updateType == ComponentSystemUpdateType.Logic)
            {
                // Check all components for changes.
                foreach (var component in Components)
                {
                    _parameterization.PositionChanged = false;
                    component.Update(_parameterization);
                    if (_parameterization.PositionChanged)
                    {
                        // We need to check if this entities position in the
                        // index is still valid. Get new position.
                        var transform = component.Entity.GetComponent<Transform>();

                        // Cannot track objects that don't have a position.
                        if (transform == null)
                        {
                            continue;
                        }

                        // Get cell ids, new and old.
                        int newCellX = ((int)transform.Translation.X) >> GridSizeShiftAmount;
                        int newCellY = ((int)transform.Translation.Y) >> GridSizeShiftAmount;
                        var newCellId = CoordinateIds.Combine(newCellX, newCellY);

                        int oldCellX = ((int)_parameterization.PreviousPosition.X) >> GridSizeShiftAmount;
                        int oldCellY = ((int)_parameterization.PreviousPosition.Y) >> GridSizeShiftAmount;
                        var oldCellId = CoordinateIds.Combine(oldCellX, oldCellY);

                        // Test for change.
                        if (newCellId != oldCellId)
                        {
                            // Cell changed, update index.
                            _index[oldCellId].Remove(component.Entity.UID);
                            if (_index[oldCellId].Count == 0)
                            {
                                _index.Remove(oldCellId);
                            }
                            if (!_index.ContainsKey(newCellId))
                            {
                                _index.Add(newCellId, new HashSet<int>());
                            }
                            _index[newCellId].Add(component.Entity.UID);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Insert entities of added components to our index.
        /// </summary>
        protected override void HandleComponentAdded(IComponent component)
        {
            var transform = component.Entity.GetComponent<Transform>();

            // If we have a position, put it into its grid cell.
            if (transform != null)
            {
                int cellX = ((int)transform.Translation.X) >> GridSizeShiftAmount;
                int cellY = ((int)transform.Translation.Y) >> GridSizeShiftAmount;
                var cellId = CoordinateIds.Combine(cellX, cellY);
                if (!_index.ContainsKey(cellId))
                {
                    _index.Add(cellId, new HashSet<int>());
                }
                _index[cellId].Add(component.Entity.UID);
            }
        }

        /// <summary>
        /// Remove entities of removed components from our index.
        /// </summary>
        protected override void HandleComponentRemoved(IComponent component)
        {
            // Get the position to remove from. This might not be the current
            // translation due to pending updates, so check for that.
            FPoint position;
            var index = component.Entity.GetComponent<Index>();
            if (index.PositionChanged)
            {
                position = index.PreviousPosition;
            }
            else
            {
                var transform = component.Entity.GetComponent<Transform>();
                if (transform == null)
                {
                    return;
                }
                position = transform.Translation;
            }

            // If we have a position, remove it from its grid cell.
            int cellX = ((int)position.X) >> GridSizeShiftAmount;
            int cellY = ((int)position.Y) >> GridSizeShiftAmount;
            var cellId = CoordinateIds.Combine(cellX, cellY);
            if (!_index.ContainsKey(cellId) || !_index[cellId].Remove(component.Entity.UID))
            {
                throw new ArgumentException("component");
            }
            else
            {
                // Removed successfully. If the list is now empty, remove.
                if (_index[cellId].Count == 0)
                {
                    _index.Remove(cellId);
                }
            }
        }

        #endregion

        #region Serialization / Hashing / Cloning
        
        public override object Clone()
        {
            var copy = (IndexSystem)base.Clone();

            // Create own index. Will be filled when re-adding components.
            copy._index = new Dictionary<ulong, HashSet<int>>();

            return copy;
        }

        #endregion
    }
}
