using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Engine.Collections;
using Engine.FarCollections;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Graphics;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// This class represents a simple index structure for nearest neighbor
    /// queries. It uses a grid structure for indexing, and will return lists
    /// of entities in cells near a query point.
    /// </summary>
    public sealed class IndexSystem : AbstractComponentSystem<Index>, IUpdatingSystem, IMessagingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

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

        #region Properties

        /// <summary>
        /// Total number of index structures currently in use.
        /// </summary>
        public int NumIndexes
        {
            get
            {
                var count = 0;
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

        /// <summary>
        /// Total number of entries over all index structures.
        /// </summary>
        public int Count
        {
            get
            {
                var count = 0;
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

        /// <summary>
        /// Gets the list of entites for which the index entry changed.
        /// </summary>
        public IEnumerable<int> ChangedEntites
        {
            get { return _changed; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The number of items in a single cell allowed before we try splitting it.
        /// </summary>
        private int _maxEntriesPerNode;

        /// <summary>
        /// The minimum bounds size of a node along an axis, used to stop splitting
        /// at a defined accuracy.
        /// </summary>
        private int _minNodeBounds;

        /// <summary>
        /// The actual indexes we're using, mapping entity positions to the
        /// entities, allowing faster range queries.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        private FarCollections.SpatialHashedQuadTree<int>[] _trees = new FarCollections.SpatialHashedQuadTree<int>[sizeof(ulong) * 8];

        /// <summary>
        /// List of entities for which the index entry changed in the last update.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        private HashSet<int> _changed = new HashSet<int>();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new index system using the specified constraints for indexes.
        /// </summary>
        /// <param name="maxEntriesPerNode">The maximum number of entries per
        /// node before the node will be split.</param>
        /// <param name="minNodeBounds">The minimum bounds size of a node, i.e.
        /// nodes of this size or smaller won't be split regardless of the
        /// number of entries in them.</param>
        public IndexSystem(int maxEntriesPerNode, int minNodeBounds)
        {
            _maxEntriesPerNode = maxEntriesPerNode;
            _minNodeBounds = minNodeBounds;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Adds entities that got an index component to all their indexes.
        /// </summary>
        /// <param name="component">The component that was added.</param>
        public override void OnComponentAdded(Component component)
        {
            base.OnComponentAdded(component);

            if (component is Index)
            {
                AddEntity(component.Entity, ((Index)component).IndexGroupsMask);
            }
        }

        /// <summary>
        /// Remove entities that had their index component removed from all
        /// indexes.
        /// </summary>
        /// <param name="component">The component.</param>
        public override void OnComponentRemoved(Component component)
        {
            base.OnComponentRemoved(component);

            if (component is Index)
            {
                // Remove from any indexes the entity was part of.
                foreach (var tree in TreesForGroups(((Index)component).IndexGroupsMask))
                {
                    tree.Remove(component.Entity);
                }

                // Remove from changed list.
                _changed.Remove(component.Entity);
            }
        }

        /// <summary>
        /// Updates the index based on translations that happened this frame.
        /// </summary>
        /// <param name="frame">The current simulation frame.</param>
        public void Update(long frame)
        {
            // Reset for next update cycle.
            _changed.Clear();

            // Reset query count until next run.
            _numQueriesSinceLastUpdate = 0;
        }

        /// <summary>
        /// Handles position changes of indexed components.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as IndexGroupsChanged?;
                if (cm != null)
                {
                    var m = cm.Value;

                    // Do we have new groups?
                    if (m.AddedIndexGroups != 0)
                    {
                        AddEntity(m.Entity, m.AddedIndexGroups);
                    }

                    // Do we have deprecated groups?
                    if (m.RemovedIndexGroups != 0)
                    {
                        // Remove from each old group.
                        foreach (var tree in TreesForGroups(m.RemovedIndexGroups))
                        {
                            tree.Remove(m.Entity);
                        }
                    }
                    return;
                }
            }
            {
                var cm = message as IndexBoundsChanged?;
                if (cm != null)
                {
                    var m = cm.Value;

                    // Check if the entity is indexable.
                    var index = ((Index)Manager.GetComponent(m.Entity, Index.TypeId));
                    if (index == null)
                    {
                        return;
                    }

                    var bounds = m.Bounds;
                    var transform = ((Transform)Manager.GetComponent(m.Entity, Transform.TypeId));
                    if (transform != null)
                    {
                        bounds.X = (int)transform.Translation.X - bounds.Width / 2;
                        bounds.Y = (int)transform.Translation.Y - bounds.Height / 2;
                    }

                    // Update all indexes the entity is part of.
                    var changed = false;
                    foreach (var tree in TreesForGroups(index.IndexGroupsMask))
                    {
                        if (tree.Update(bounds, Vector2.Zero, m.Entity))
                        {
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        // Mark as changed.
                        _changed.Add(m.Entity);
                    }
                    return;
                }
            }
            {
                var cm = message as TranslationChanged?;
                if (cm != null)
                {
                    var m = cm.Value;

                    // Check if the entity is indexable.
                    var index = ((Index)Manager.GetComponent(m.Entity, Index.TypeId));
                    if (index == null)
                    {
                        return;
                    }

                    var bounds = index.Bounds;
                    bounds.X = (int)m.CurrentPosition.X - bounds.Width / 2;
                    bounds.Y = (int)m.CurrentPosition.Y - bounds.Height / 2;

                    var velocity = ((Velocity)Manager.GetComponent(m.Entity, Velocity.TypeId));
                    var delta = velocity != null ? velocity.Value : Vector2.Zero;

                    var changed = false;
                    foreach (var tree in TreesForGroups(index.IndexGroupsMask))
                    {
                        if (tree.Update(bounds, delta, m.Entity))
                        {
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        // Mark as changed.
                        _changed.Add(m.Entity);
                    }
                }
            }
        }

        #endregion

        #region Entity lookup

        /// <summary>
        /// Get all entities in the specified range of the query point.
        /// </summary>
        /// <param name="center">The point to use as a query point.</param>
        /// <param name="radius">The distance up to which to get neighbors.</param>
        /// <param name="results">The list to use for storing the results.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <returns>All entities in range.</returns>
        public void Find(FarPosition center, float radius, ref ISet<int> results, ulong groups)
        {
            foreach (var tree in TreesForGroups(groups))
            {
                Interlocked.Add(ref _numQueriesSinceLastUpdate, 1);
                tree.Find(center, radius, results);
            }
        }

        /// <summary>
        /// Get all entities contained in the specified rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="results">The list to use for storing the results.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <returns>All entities in range.</returns>
        public void Find(ref FarRectangle rectangle, ref ISet<int> results, ulong groups)
        {
            foreach (var tree in TreesForGroups(groups))
            {
                Interlocked.Add(ref _numQueriesSinceLastUpdate, 1);
                tree.Find(rectangle, results);
            }
        }

        /// <summary>
        /// Gets the bounds for the specified entity in the first of the specified
        /// groups containing the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="groups">The groups.</param>
        /// <returns></returns>
        public FarRectangle GetBounds(int entity, ulong groups)
        {
            foreach (var tree in TreesForGroups(groups))
            {
                if (tree.Contains(entity))
                {
                    return tree[entity];
                }
            }
            return FarRectangle.Empty;
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Utility method used to create indexes flagged in the specified bit mask
        /// if they don't already exist.
        /// </summary>
        /// <param name="groups">The groups to create index structures for.</param>
        private void EnsureIndexesExist(ulong groups)
        {
            var index = 0;
            while (groups > 0)
            {
                if ((groups & 1) == 1 && _trees[index] == null)
                {
                    _trees[index] = new FarCollections.SpatialHashedQuadTree<int>(_maxEntriesPerNode, _minNodeBounds);
                }
                groups = groups >> 1;
                ++index;
            }
        }

        /// <summary>
        /// Utility method that returns a list of all trees flagged in the
        /// specified bit mask. Calling this a second time invalidates the
        /// reference to a list returned by the previous call.
        /// </summary>
        /// <param name="groups">The groups to get the indexes for.</param>
        /// <returns>A list of the specified indexes.</returns>
        private IEnumerable<IIndex<int, FarRectangle, FarPosition>> TreesForGroups(ulong groups)
        {
            byte index = 0;
            while (groups > 0)
            {
                if ((groups & 1) == 1 && _trees[index] != null)
                {
                    yield return _trees[index];
                }
                groups = groups >> 1;
                ++index;
            }
        }

        /// <summary>
        /// Adds the specified entity to all indexes specified in groups.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <param name="groups">The indexes to add to.</param>
        private void AddEntity(int entity, ulong groups)
        {
            // Make sure the indexes exists.
            EnsureIndexesExist(groups);

            // Compute the bounds for the indexable as well as possible.
            var bounds = new FarRectangle();
            var collidable = ((Collidable)Manager.GetComponent(entity, Collidable.TypeId));
            if (collidable != null)
            {
                bounds = collidable.ComputeBounds();
            }
            var transform = ((Transform)Manager.GetComponent(entity, Transform.TypeId));
            if (transform != null)
            {
                bounds.X = (int)transform.Translation.X - bounds.Width / 2;
                bounds.Y = (int)transform.Translation.Y - bounds.Height / 2;
            }

            // Add the entity to all its indexes.
            foreach (var tree in TreesForGroups(groups))
            {
                // Add to each group.
                tree.Add(bounds, entity);
            }

            // Mark as changed.
            _changed.Add(entity);
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <remarks>
        /// Must be overridden in subclasses setting <c>ShouldSynchronize</c>
        /// to true.
        /// </remarks>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);

            for (var i = 0; i < _trees.Length; ++i)
            {
                var tree = _trees[i];
                if (tree == null)
                {
                    packet.Write(0);
                    continue;
                }

                packet.Write(tree.Count);
                foreach (var tuple in tree)
                {
                    packet.Write(tuple.Item1);
                    packet.Write(tuple.Item2);
                }
            }
            packet.Write(_changed.Count);
            foreach (var entity in _changed)
            {
                packet.Write(entity);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <remarks>
        /// Must be overridden in subclasses setting <c>ShouldSynchronize</c>
        /// to true.
        /// </remarks>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            for (var i = 0; i < _trees.Length; ++i)
            {
                if (_trees[i] != null)
                {
                    _trees[i].Clear();
                }
                var count = packet.ReadInt32();
                if (count <= 0)
                {
                    continue;
                }
                if (_trees[i] == null)
                {
                    _trees[i] = new FarCollections.SpatialHashedQuadTree<int>(_maxEntriesPerNode, _minNodeBounds);
                }
                for (var j = 0; j < count; ++j)
                {
                    var bounds = packet.ReadFarRectangle();
                    var entity = packet.ReadInt32();
                    _trees[i].Add(bounds, entity);
                }
            }

            _changed.Clear();
            var changedCount = packet.ReadInt32();
            for (var i = 0; i < changedCount; i++)
            {
                _changed.Add(packet.ReadInt32());
            }
        }

        #endregion

        #region Copying

        /// <summary>
        /// Servers as a copy constructor that returns a new instance of the same
        /// type that is freshly initialized.
        /// 
        /// <para>
        /// This takes care of duplicating reference types to a new copy of that
        /// type (e.g. collections).
        /// </para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (IndexSystem)base.NewInstance();

            copy._trees = new FarCollections.SpatialHashedQuadTree<int>[sizeof(ulong) * 8];
            copy._changed = new HashSet<int>();

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// 
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <remarks>The manager for the system to copy into must be set to the
        /// manager into which the system is being copied.</remarks>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (IndexSystem)into;

            foreach (var tree in copy._trees)
            {
                if (tree != null)
                {
                    tree.Clear();
                }
            }

            for (var i = 0; i < _trees.Length; i++)
            {
                if (_trees[i] == null)
                {
                    continue;
                }
                if (copy._trees[i] == null)
                {
                    copy._trees[i] = _trees[i].NewInstance();
                }
                _trees[i].CopyInto(copy._trees[i]);
            }

            copy._changed.Clear();
            copy._changed.UnionWith(_changed);
        }

        #endregion

        #region Debug stuff

        /// <summary>
        /// Total number of queries over all index structures since the
        /// last update. This will always be zero when not running in
        /// debug mode.
        /// </summary>
        public int NumQueriesSinceLastUpdate
        {
            get { return _numQueriesSinceLastUpdate; }
        }

        /// <summary>
        /// For ref usage in interlocked update.
        /// </summary>
        private int _numQueriesSinceLastUpdate;

        /// <summary>
        /// Renders all index structures matching the specified index group bit mask
        /// using the specified shape at the specified translation.
        /// </summary>
        /// <param name="groups">Bit mask determining which indexes to draw.</param>
        /// <param name="shape">Shape to use for drawing.</param>
        /// <param name="translation">Translation to apply when drawing.</param>
        [Conditional("DEBUG")]
        public void DrawIndex(ulong groups, AbstractShape shape, FarPosition translation)
        {
            foreach (var tree in TreesForGroups(groups))
            {
                var index = tree as FarCollections.SpatialHashedQuadTree<int>;
                if (index != null)
                {
                    index.Draw(shape, translation);
                }
            }
        }

        #endregion
    }
}
