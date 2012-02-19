using System;
using System.Collections.Generic;
using Engine.Collections;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// This class represents a simple index structure for nearest neighbor
    /// queries. It uses a grid structure for indexing, and will return lists
    /// of entities in cells near a query point.
    /// </summary>
    public sealed class IndexSystem : AbstractComponentSystem<Index>
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
            foreach (var tree in TreesForGroups(groups))
            {
                tree.Draw(shape, translation);   
            }
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
        private const int _maxEntriesPerNode = 50;

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
        /// Reused for iteration.
        /// </summary>
        private List<QuadTree<int>> _reusableTreeList = new List<QuadTree<int>>();

        #endregion

        #region Entity lookup

        /// <summary>
        /// Get all entities in the specified range of the query point.
        /// </summary>
        /// <param name="entity">The entity to use as a query point.</param>
        /// <param name="range">The distance up to which to get neighbors.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <param name="list">The list to use for storing the results.</param>
        /// <returns>All entities in range (including the query entity).</returns>
        public ICollection<int> RangeQuery(int entity, float range,
            ulong groups = DefaultIndexGroupMask, ICollection<int> list = null)
        {
            var position = Manager.GetComponent<Transform>(entity).Translation;
            return RangeQuery(ref position, range, groups, list);
        }

        /// <summary>
        /// Get all entities in the specified range of the query point.
        /// </summary>
        /// <param name="query">The point to use as a query point.</param>
        /// <param name="range">The distance up to which to get neighbors.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <param name="list">The list to use for storing the results.</param>
        /// <returns>All entities in range.</returns>
        public ICollection<int> RangeQuery(ref Vector2 query, float range,
            ulong groups = DefaultIndexGroupMask, ICollection<int> list = null)
        {
            list = list ?? new List<int>();

            foreach (var tree in TreesForGroups(groups))
            {
                tree.RangeQuery(ref query, range, list);
            }

            return list;
        }

        /// <summary>
        /// Get all entities contained in the specified rectangle.
        /// </summary>
        /// <param name="query">The query rectangle.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <param name="list">The list to use for storing the results.</param>
        /// <returns>All entities in range.</returns>
        public ICollection<int> RangeQuery(ref Rectangle query,
            ulong groups = DefaultIndexGroupMask, ICollection<int> list = null)
        {
            list = list ?? new List<int>();

            foreach (var tree in TreesForGroups(groups))
            {
                tree.RangeQuery(ref query, list);
            }

            return list;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Insert entities of added components to our index.
        /// </summary>
        protected override void OnComponentAdded(Index component)
        {
            // Get the position to add at.
            Vector2 position = Manager.GetComponent<Transform>(component.Entity).Translation;

            EnsureIndexesExist(component.IndexGroups);
            foreach (var tree in TreesForGroups(component.IndexGroups))
            {
                tree.Add(ref position, component.Entity);
            }
        }

        /// <summary>
        /// Remove entities of removed components from our index.
        /// </summary>
        protected override void OnComponentRemoved(Index component)
        {
            // Get the position to remove from.
            Vector2 position = Manager.GetComponent<Transform>(component.Entity).Translation;

            foreach (var tree in TreesForGroups(component.IndexGroups))
            {
                tree.Remove(ref position, component.Entity);
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

        private List<QuadTree<int>> TreesForGroups(ulong groups)
        {
            _reusableTreeList.Clear();
            byte index = 0;
            while (groups > 0)
            {
                if ((groups & 1) == 1 && _trees[index] != null)
                {
                    _reusableTreeList.Add(_trees[index]);
                }
                groups = groups >> 1;
                ++index;
            }
            return _reusableTreeList;
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Handles position changes of indexed components.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        public override void Receive<T>(ref T message)
        {
            base.Receive<T>(ref message);

            if (message is TranslationChanged)
            {
                var translationChanged = (TranslationChanged)(ValueType)message;

                // Get the index component of the object that changed its
                // position. If it doesn't have one we have nothing to do.
                var index = Manager.GetComponent<Index>(translationChanged.Entity);
                if (index == null)
                {
                    return;
                }

                // Check if the actual index cell we're in might have changed.
                var previousCellId = CoordinateIds.Combine(
                    (int)translationChanged.PreviousPosition.X >> IndexSystem.MinimumNodeSizeShift,
                    (int)translationChanged.PreviousPosition.Y >> IndexSystem.MinimumNodeSizeShift);

                var newCellId = CoordinateIds.Combine(
                    (int)translationChanged.CurrentPosition.X >> IndexSystem.MinimumNodeSizeShift,
                    (int)translationChanged.CurrentPosition.Y >> IndexSystem.MinimumNodeSizeShift);

                if (newCellId != previousCellId)
                {
                    // Actual cell we might be in in the index has changed.
                    // Update all indexes the component is part of.
                    foreach (var tree in TreesForGroups(index.IndexGroups))
                    {
                        tree.Update(
                            ref translationChanged.PreviousPosition,
                            ref translationChanged.CurrentPosition,
                            translationChanged.Entity);
                    }
                }
            }
        }

        #endregion

        #region Copying

        public override AbstractSystem DeepCopy(AbstractSystem into)
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
                copy._reusableTreeList = new List<QuadTree<int>>();
            }

            return copy;
        }

        #endregion
    }
}
