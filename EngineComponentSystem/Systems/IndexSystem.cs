using System.Collections.Generic;
using Engine.Collections;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;

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
        /// Maximum entries per node in our index to use.
        /// </summary>
        private const int maxEntriesPerNode = 10;

        /// <summary>
        /// Minimum size of a node in our index.
        /// </summary>
        private const int minNodeSize = 32;

        #endregion

        #region Fields

        /// <summary>
        /// The actual index we're using, mapping entity positions to the
        /// entities, allowing faster range queries.
        /// </summary>
        private Dictionary<int, QuadTree<int>> _trees = new Dictionary<int, QuadTree<int>>();

        /// <summary>
        /// Reusable parameterization.
        /// </summary>
        private IndexParameterization _parameterization = new IndexParameterization();

        #endregion

        #region Entity lookup

        /// <summary>
        /// Get all entities in the same cell as the query entity, or in a
        /// neighboring cell in the given cell range (0 = only the same cell,
        /// 1 = the neighboring, and so on).
        /// </summary>
        /// <param name="query">The entity to use as a query point.</param>
        /// <param name="range">The distance up to which to get neighbors.</param>
        /// <returns>All entities in range (including the query entity).</returns>
        public List<IEntity> GetNeighbors(IEntity query, float range, int index = 0)
        {
            return GetNeighbors(query.GetComponent<Transform>().Translation, range, index);
        }

        /// <summary>
        /// Get all entities in the same cell as the query point, or in a
        /// neighboring cell in the given cell range (0 = only the same cell,
        /// 1 = the neighboring, and so on).
        /// </summary>
        /// <param name="query">The point to use as a query point.</param>
        /// <param name="range">The distance up to which to get neighbors.</param>
        /// <returns>All entities in range.</returns>
        public List<IEntity> GetNeighbors(Vector2 query, float range, int index = 0)
        {
            var result = new List<IEntity>();

            EnsureIndexExists(index);
            foreach (var neighborId in _trees[index].RangeQuery(query, range))
            {
                result.Add(Manager.EntityManager.GetEntity(neighborId));
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
                var currentComponents = Components;
                foreach (var component in currentComponents)
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

                        _trees[_parameterization.IndexGroup].Update(_parameterization.PreviousPosition, transform.Translation, component.Entity.UID);
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
                var index = component.Entity.GetComponent<Index>();

                // Only support Index components for now.
                if (index != null)
                {
                    EnsureIndexExists(index.IndexGroup);
                    _trees[index.IndexGroup].Add(transform.Translation, component.Entity.UID);
                }
            }
        }

        /// <summary>
        /// Remove entities of removed components from our index.
        /// </summary>
        protected override void HandleComponentRemoved(IComponent component)
        {
            // Get the position to remove from. This might not be the current
            // translation due to pending updates, so check for that.
            var index = component.Entity.GetComponent<Index>();

            // Only support Index components for now.
            if (index != null)
            {
                // Get the position to remove from.
                Vector2 position;
                if (index.PositionChanged)
                {
                    position = index.PreviousPosition;
                }
                else
                {
                    // No previous position, get the current transform.
                    var transform = component.Entity.GetComponent<Transform>();
                    if (transform == null)
                    {
                        return;
                    }
                    position = transform.Translation;
                }

                _trees[index.IndexGroup].Remove(position, component.Entity.UID);
            }
        }

        #endregion

        #region Utility methods

        private void EnsureIndexExists(int indexId)
        {
            if (!_trees.ContainsKey(indexId))
            {
                _trees.Add(indexId, new QuadTree<int>(maxEntriesPerNode, minNodeSize));
            }
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public override object Clone()
        {
            var copy = (IndexSystem)base.Clone();

            // Create own index. Will be filled when re-adding components.
            copy._trees = new Dictionary<int, QuadTree<int>>();

            return copy;
        }

        #endregion
    }
}
