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

        public int DEBUG_Count { get { return _tree.Count; } }

        #region Fields

        /// <summary>
        /// The actual index we're using, mapping entity positions to the
        /// entities, allowing faster range queries.
        /// </summary>
        private QuadTree<int> _tree = new QuadTree<int>(maxEntriesPerNode, minNodeSize);

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
        /// <param name="cellRange">The neighborship rank up to which to
        /// include neighboring cells.</param>
        /// <returns>All entities in range (including the query entity).</returns>
        public List<IEntity> GetNeighbors(IEntity query, int cellRange)
        {
            return GetNeighbors(query.GetComponent<Transform>().Translation, cellRange);
        }

        /// <summary>
        /// Get all entities in the same cell as the query point, or in a
        /// neighboring cell in the given cell range (0 = only the same cell,
        /// 1 = the neighboring, and so on).
        /// </summary>
        /// <param name="query">The point to use as a query point.</param>
        /// <param name="range">The distance up to which to get neighbors.</param>
        /// <returns>All entities in range.</returns>
        public List<IEntity> GetNeighbors(Vector2 query, float range)
        {
            var result = new List<IEntity>();

            foreach (var neighborId in _tree.RangeQuery(query, range))
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

                        _tree.Update(_parameterization.PreviousPosition, transform.Translation, component.Entity.UID);
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
                _tree.Add(transform.Translation, component.Entity.UID);
            }
        }

        /// <summary>
        /// Remove entities of removed components from our index.
        /// </summary>
        protected override void HandleComponentRemoved(IComponent component)
        {
            // Get the position to remove from. This might not be the current
            // translation due to pending updates, so check for that.
            Vector2 position;
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

            _tree.Remove(position, component.Entity.UID);
        }

        #endregion

        #region Serialization / Hashing / Cloning
        
        public override object Clone()
        {
            var copy = (IndexSystem)base.Clone();

            // Create own index. Will be filled when re-adding components.
            copy._tree = new QuadTree<int>(maxEntriesPerNode, minNodeSize);

            return copy;
        }

        #endregion
    }
}
