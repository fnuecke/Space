using System;
using System.Collections.Generic;
using Engine.Collections;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// This class represents a simple index structure for nearest neighbor
    /// queries. It uses a grid structure for indexing, and will return lists
    /// of entities in cells near a query point.
    /// </summary>
    public sealed class IndexSystem : AbstractComponentSystem<IndexParameterization, NullParameterization>
    {
        #region Debug stuff
        #if DEBUG

        public int DEBUG_NumIndexes
        {
            get
            {
                int count = 0;
                foreach (var index in _trees)
                {
                    if (index != null)
                    {
                        ++count;
                    }
                }
                return count;
            }
        }

        public int DEBUG_Count
        {
            get
            {
                int count = 0;
                foreach (var index in _trees)
                {
                    if (index != null)
                    {
                        count += index.Count;
                    }
                }
                return count;
            }
        }

        public void DEBUG_DrawIndex(ulong groups, Engine.Graphics.AbstractShape shape, Vector2 translation)
        {
            foreach (var tree in TreesForGroups(groups, _reusableTreeList))
            {
                tree.Draw(shape, translation);   
            }
            _reusableTreeList.Clear();
        }

        #endif
        #endregion

        #region Constants

        /// <summary>
        /// The default group used if none is specified.
        /// </summary>
        public const byte DefaultIndexGroup = 0;

        /// <summary>
        /// The default group used if none is specified.
        /// </summary>
        public const ulong DefaultIndexGroupMask = 1ul;

        /// <summary>
        /// Minimum size of a node in our index as the required bit shift, i.e.
        /// the actual minimum node size is <c>1 &lt;&lt; MinimumNodeSizeShift</c>.
        /// </summary>
        public const int MinimumNodeSizeShift = 7;

        /// <summary>
        /// Minimum size of a node in our index.
        /// </summary>
        public const int MinimumNodeSize = 1 << MinimumNodeSizeShift;

        /// <summary>
        /// Maximum entries per node in our index to use.
        /// </summary>
        private const int _maxEntriesPerNode = 10;

        #endregion

        #region Group number distribution

        /// <summary>
        /// Next group index dealt out.
        /// </summary>
        private static byte _nextGroup = 1;

        /// <summary>
        /// Reserves a group number for use.
        /// </summary>
        /// <returns>The reserved group number.</returns>
        public static byte GetGroup()
        {
            return GetGroups(1);
        }

        /// <summary>
        /// Reserves multiple group numbers for use.
        /// </summary>
        /// <param name="range">The number of group numbers to reserve.</param>
        /// <returns>The start of the range of reserved group numbers.</returns>
        public static byte GetGroups(byte range)
        {
            if ((int)range + (int)_nextGroup > 0xFF)
            {
                throw new InvalidOperationException("No more index groups available.");
            }
            var result = _nextGroup;
            _nextGroup += range;
            return result;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The actual indexes we're using, mapping entity positions to the
        /// entities, allowing faster range queries.
        /// </summary>
        private QuadTree<int>[] _trees = new QuadTree<int>[sizeof(ulong) * 8];

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reusable parameterization.
        /// </summary>
        private IndexParameterization _parameterization = new IndexParameterization();

        /// <summary>
        /// Reused for iteration.
        /// </summary>
        private List<AbstractComponent> _reusableComponentList = new List<AbstractComponent>(1024);

        /// <summary>
        /// Reused for iteration.
        /// </summary>
        private List<QuadTree<int>> _reusableTreeList = new List<QuadTree<int>>(8);

        /// <summary>
        /// Reused for iteration.
        /// </summary>
        private List<int> _reusableEntityIdList = new List<int>(64);

        #endregion

        #region Entity lookup

        /// <summary>
        /// Get all entities in the same cell as the query entity, or in a
        /// neighboring cell in the given cell range (0 = only the same cell,
        /// 1 = the neighboring, and so on).
        /// </summary>
        /// <param name="query">The entity to use as a query point.</param>
        /// <param name="range">The distance up to which to get neighbors.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <param name="list">The list to use for storing the results.</param>
        /// <returns>All entities in range (including the query entity).</returns>
        public ICollection<Entity> GetNeighbors(Entity query, float range,
            ulong groups = DefaultIndexGroupMask, ICollection<Entity> list = null)
        {
            return GetNeighbors(ref query.GetComponent<Transform>().Translation, range, groups);
        }

        /// <summary>
        /// Get all entities in the same cell as the query point, or in a
        /// neighboring cell in the given cell range (0 = only the same cell,
        /// 1 = the neighboring, and so on).
        /// </summary>
        /// <param name="query">The point to use as a query point.</param>
        /// <param name="range">The distance up to which to get neighbors.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <param name="list">The list to use for storing the results.</param>
        /// <returns>All entities in range.</returns>
        public ICollection<Entity> GetNeighbors(ref Vector2 query, float range,
            ulong groups = DefaultIndexGroupMask, ICollection<Entity> list = null)
        {
            list = list ?? new List<Entity>();

            foreach (var tree in TreesForGroups(groups, _reusableTreeList))
            {
                foreach (var neighborId in tree.RangeQuery(ref query, range, _reusableEntityIdList))
                {
                    list.Add(Manager.EntityManager.GetEntity(neighborId));
                }

                _reusableEntityIdList.Clear();
            }

            _reusableTreeList.Clear();

            return list;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the index as necessary.
        /// </summary>
        /// <param name="updateType">The type of update to perform. We only do logic updates.</param>
        /// <param name="frame">The frame in which this update takes place.</param>
        public override void Update(long frame)
        {
            // Check all components for changes.
            _reusableComponentList.AddRange(UpdateableComponents);
            foreach (var component in _reusableComponentList)
            {
                if (component.Enabled)
                {
                    component.Update(_parameterization);
                    if (_parameterization.PositionChanged)
                    {
                        // We need to check if this entities position in the
                        // index is still valid. Get new position.
                        var transform = component.Entity.GetComponent<Transform>();

#if DEBUG
                        // Cannot track objects that don't have a position.
                        if (transform == null)
                        {
                            throw new InvalidOperationException("Indexed objects must have a transform component.");
                        }
#endif

                        // Update all indexes the component is part of.
                        foreach (var tree in TreesForGroups(_parameterization.IndexGroups, _reusableTreeList))
                        {
                            tree.Update(ref _parameterization.PreviousPosition, ref transform.Translation, component.Entity.UID);
                        }

                        _reusableTreeList.Clear();
                    }
                    _parameterization.PositionChanged = false;
                }
            }
            _reusableComponentList.Clear();
        }

        protected override void Clear()
        {
            base.Clear();

            foreach (var tree in _trees)
            {
                if (tree != null)
                {
                    tree.Clear();
                }
            }
        }

        /// <summary>
        /// Insert entities of added components to our index.
        /// </summary>
        protected override void HandleComponentAdded(AbstractComponent component)
        {
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

                EnsureIndexesExist(index.IndexGroups);
                foreach (var tree in TreesForGroups(index.IndexGroups, _reusableTreeList))
                {
                    tree.Add(ref position, component.Entity.UID);
                }

                _reusableTreeList.Clear();
            }
        }

        /// <summary>
        /// Remove entities of removed components from our index.
        /// </summary>
        protected override void HandleComponentRemoved(AbstractComponent component)
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

                foreach (var tree in TreesForGroups(index.IndexGroups, _reusableTreeList))
                {
                    tree.Remove(ref position, component.Entity.UID);
                }

                _reusableTreeList.Clear();
            }
        }

        #endregion

        #region Utility methods

        private void EnsureIndexesExist(ulong groups)
        {
            int index = 0;
            while (groups > 0)
            {
                if ((groups & 1) == 1 && _trees[index] == null)
                {
                    _trees[index] = new QuadTree<int>(_maxEntriesPerNode, MinimumNodeSize);
                }
                groups = groups >> 1;
                ++index;
            }
        }

        private List<QuadTree<int>> TreesForGroups(ulong groups, List<QuadTree<int>> list)
        {
            byte index = 0;
            while (groups > 0)
            {
                if ((groups & 1) == 1 && _trees[index] != null)
                {
                    list.Add(_trees[index]);
                }
                groups = groups >> 1;
                ++index;
            }
            return list;
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public override ISystem DeepCopy(ISystem into)
        {
            var copy = (IndexSystem)base.DeepCopy(into);

            // Create own index. Will be filled when re-adding components.
            if (copy == into)
            {
                foreach (var tree in copy._trees)
                {
                    if (tree != null)
                    {
                        tree.Clear();
                    }
                }
            }
            else
            {
                copy._trees = new QuadTree<int>[sizeof(ulong) * 8];
                copy._parameterization = new IndexParameterization();
                copy._reusableComponentList = new List<AbstractComponent>(1024);
                copy._reusableTreeList = new List<QuadTree<int>>();
                copy._reusableEntityIdList = new List<int>();
            }

            return copy;
        }

        #endregion
    }
}
