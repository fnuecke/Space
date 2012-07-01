using System;
using System.Collections.Generic;
using Engine.Collections;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
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
        private const int MaxEntriesPerNode = 30;

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
            if (range + _nextGroup > 0xFF)
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
        private IIndex<int>[] _trees = new IIndex<int>[sizeof(ulong) * 8];

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iteration.
        /// </summary>
        private List<IIndex<int>> _reusableTreeList = new List<IIndex<int>>();

        #endregion

        #region Entity lookup

        /// <summary>
        /// Get all entities in the specified range of the query point.
        /// </summary>
        /// <param name="entity">The entity to use as a query point.</param>
        /// <param name="range">The distance up to which to get neighbors.</param>
        /// <param name="list">The list to use for storing the results.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <returns>All entities in range (including the query entity).</returns>
        public void RangeQuery(int entity, float range, ref ICollection<int> list, ulong groups = DefaultIndexGroupMask)
        {
            RangeQuery(Manager.GetComponent<Transform>(entity).Translation, range, ref list, groups);
        }

        /// <summary>
        /// Get all entities in the specified range of the query point.
        /// </summary>
        /// <param name="query">The point to use as a query point.</param>
        /// <param name="range">The distance up to which to get neighbors.</param>
        /// <param name="list">The list to use for storing the results.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <returns>All entities in range.</returns>
        public void RangeQuery(Vector2 query, float range, ref ICollection<int> list, ulong groups = DefaultIndexGroupMask)
        {
            foreach (var tree in TreesForGroups(groups))
            {
#if DEBUG
                ++_numQueriesLastUpdate;
#endif
                tree.RangeQuery(query, range, ref list);
            }
        }

        /// <summary>
        /// Get all entities contained in the specified rectangle.
        /// </summary>
        /// <param name="query">The query rectangle.</param>
        /// <param name="list">The list to use for storing the results.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <returns>All entities in range.</returns>
        public void RangeQuery(ref Rectangle query, ref ICollection<int> list, ulong groups = DefaultIndexGroupMask)
        {
            foreach (var tree in TreesForGroups(groups))
            {
#if DEBUG
                ++_numQueriesLastUpdate;
#endif
                tree.RangeQuery(ref query, ref list);
            }
        }

        #endregion

        #region Utility methods

        private void EnsureIndexesExist(ulong groups)
        {
            var index = 0;
            while (groups > 0)
            {
                if ((groups & 1) == 1 && _trees[index] == null)
                {
                    _trees[index] = new QuadTree<int>(MaxEntriesPerNode, MinimumNodeSize);
                }
                groups = groups >> 1;
                ++index;
            }
        }

        private IEnumerable<IIndex<int>> TreesForGroups(ulong groups)
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

        #region Component removal handling

        /// <summary>
        /// Remove entities that had their index component removed from all
        /// indexes.
        /// </summary>
        /// <param name="component">The component.</param>
        protected override void OnComponentRemoved(Index component)
        {
            foreach (var tree in TreesForGroups(component.IndexGroups))
            {
                tree.Remove(component.Entity);
            }
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
            base.Receive(ref message);

            if (message is IndexGroupsChanged)
            {
                var changedMessage = (IndexGroupsChanged)(ValueType)message;

                // Do we have new groups?
                if (changedMessage.AddedIndexGroups != 0)
                {
                    // Get the position to add at, if we need to add.
                    var position = Manager.GetComponent<Transform>(changedMessage.Entity).Translation;

                    EnsureIndexesExist(changedMessage.AddedIndexGroups);
                    foreach (var tree in TreesForGroups(changedMessage.AddedIndexGroups))
                    {
                        tree.Add(position, changedMessage.Entity);
                    }
                }

                // Do we have deprecated groups?
                if (changedMessage.RemovedIndexGroups != 0)
                {
                    foreach (var tree in TreesForGroups(changedMessage.RemovedIndexGroups))
                    {
                        tree.Remove(changedMessage.Entity);
                    }
                }
            }
            else if (message is TranslationChanged)
            {
                var translationChanged = (TranslationChanged)(ValueType)message;

                // Get the index component of the object that changed its
                // position. If it doesn't have one we have nothing to do.
                var index = Manager.GetComponent<Index>(translationChanged.Entity);
                if (index == null)
                {
                    return;
                }

                // Update all indexes the component is part of.
                foreach (var tree in TreesForGroups(index.IndexGroups))
                {
                    tree.Update(translationChanged.CurrentPosition, translationChanged.Entity);
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
                copy._trees = new IIndex<int>[sizeof(ulong) * 8];
                copy._reusableTreeList = new List<IIndex<int>>();
#if DEBUG
                copy._numQueriesLastUpdate = _numQueriesLastUpdate;
#endif
            }

            for (int i = 0; i < _trees.Length; i++)
            {
                if (_trees[i] != null)
                {
                    if (copy._trees[i] == null)
                    {
                        copy._trees[i]  = new QuadTree<int>(MaxEntriesPerNode, MinimumNodeSize);
                    }
                    foreach (var entry in _trees[i])
                    {
                        copy._trees[i].Add(Manager.GetComponent<Transform>(entry).Translation, entry);
                    }
                }
            }

            return copy;
        }

        #endregion

        #region Debug stuff
#if DEBUG

        private int _numQueriesLastUpdate;

        public int NumQueriesLastUpdate
        {
            get { return _numQueriesLastUpdate; }
        }

        public int NumIndexes
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

        public int Count
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

        public void DrawIndex(ulong groups, Graphics.AbstractShape shape, Vector2 translation)
        {
            foreach (var tree in TreesForGroups(groups))
            {
                var quadTree = tree as QuadTree<int>;
                if (quadTree != null)
                {
                    (quadTree).Draw(shape, translation);
                }
            }
        }

        public override void Update(GameTime gameTime, long frame)
        {
            base.Update(gameTime, frame);

#if DEBUG
            _numQueriesLastUpdate = 0;
#endif
        }

#endif
        #endregion
    }
}
