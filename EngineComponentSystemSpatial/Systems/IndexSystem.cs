using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Engine.Collections;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Messages;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Graphics;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

#if FARMATH
using Engine.FarCollections;
using WorldPoint = Engine.FarMath.FarPosition;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.Math.RectangleF;
#endif

namespace Engine.ComponentSystem.Spatial.Systems
{
    /// <summary>
    ///     This class represents a simple index structure for nearest neighbor queries.
    /// </summary>
    public sealed class IndexSystem : AbstractSystem, IUpdatingSystem, IMessagingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Group number distribution

        /// <summary>Next group index dealt out.</summary>
        private static byte _nextGroup = 1;

        /// <summary>Reserves a group number for use.</summary>
        /// <returns>The reserved group number.</returns>
        public static byte GetGroup()
        {
            return GetGroups(1);
        }

        /// <summary>Reserves multiple group numbers for use.</summary>
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

        /// <summary>Total number of index structures currently in use.</summary>
        public int IndexCount
        {
            get { return _trees.Count(index => index != null); }
        }

        /// <summary>Total number of entries over all index structures.</summary>
        public int Count
        {
            get { return _trees.Where(index => index != null).Sum(index => index.Count); }
        }

        /// <summary>
        ///     Gets the <em>first</em> index of the specified groups. This is a convenient shortcut when it is known that the
        ///     group mask only represents a single index.
        /// </summary>
        public IIndex<int, WorldBounds, WorldPoint> this[ulong groups]
        {
            get { return IndexesForGroups(groups).Select(index => _trees[index]).FirstOrDefault(); }
        }

        #endregion

        #region Fields

        /// <summary>The number of items in a single cell allowed before we try splitting it.</summary>
        private readonly int _maxEntriesPerNode;

        /// <summary>The minimum bounds size of a node along an axis, used to stop splitting at a defined accuracy.</summary>
        private readonly float _minNodeBounds;

        /// <summary>The actual indexes we're using, mapping component positions to the components, allowing faster range queries.</summary>
        [CopyIgnore, PacketizerIgnore]
#if FARMATH
        private FarCollections.SpatialHashedQuadTree<int>[] _trees = new FarCollections.SpatialHashedQuadTree<int>[sizeof (ulong) * 8];
#else
        private DynamicQuadTree<int>[] _trees = new DynamicQuadTree<int>[sizeof (ulong) * 8];
#endif

        /// <summary>List of components for which the index entry changed in the last update.</summary>
        [CopyIgnore, PacketizerIgnore]
        private HashSet<int>[] _changed = new HashSet<int>[sizeof (ulong) * 8];

        #endregion

        #region Constructor

        /// <summary>Creates a new index system using the specified constraints for indexes.</summary>
        /// <param name="maxEntriesPerNode">The maximum number of entries per node before the node will be split.</param>
        /// <param name="minNodeBounds">
        ///     The minimum bounds size of a node, i.e. nodes of this size or smaller won't be split
        ///     regardless of the number of entries in them.
        /// </param>
        public IndexSystem(int maxEntriesPerNode, float minNodeBounds)
        {
            _maxEntriesPerNode = maxEntriesPerNode;
            _minNodeBounds = minNodeBounds;
        }

        #endregion

        #region Logic

        /// <summary>Updates the index based on translations that happened this frame.</summary>
        /// <param name="frame">The current simulation frame.</param>
        public void Update(long frame)
        {
            // Reset for next update cycle.
            //foreach (var changed in _changed.Where(changed => changed != null))
            //{
            //    changed.Clear();
            //}

            // Reset query count until next run.
            _queryCountSinceLastUpdate = 0;
        }

        /// <summary>Adds index components to all their indexes.</summary>
        /// <param name="component">The component that was added.</param>
        public override void OnComponentAdded(IComponent component)
        {
            base.OnComponentAdded(component);

            var index = component as IIndexable;
            if (index != null && index.Enabled)
            {
                AddToGroups(index, index.IndexGroupsMask);
            }
        }

        /// <summary>Remove index components from all indexes.</summary>
        /// <param name="component">The component.</param>
        public override void OnComponentRemoved(IComponent component)
        {
            base.OnComponentRemoved(component);

            var index = component as IIndexable;
            if (index != null && index.Enabled)
            {
                // Remove from any indexes the component was part of.
                RemoveFromGroups(index, index.IndexGroupsMask);
            }
        }

        /// <summary>Handles position changes of indexed components.</summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            // Handle group changes (moving components from one index group to another).
            var groupsChanged = message as IndexGroupsChanged?;
            if (groupsChanged != null)
            {
                var m = groupsChanged.Value;

                Debug.Assert(m.Component.Enabled);

                AddToGroups(m.Component, m.AddedIndexGroups);
                RemoveFromGroups(m.Component, m.RemovedIndexGroups);

                return;
            }

            // Handle bound changes (size of actual bounds of simple index components).
            var boundsChanged = message as IndexBoundsChanged?;
            if (boundsChanged != null)
            {
                var m = boundsChanged.Value;

                Debug.Assert(m.Component.Enabled);

                var bounds = m.Bounds;
                var delta = m.Delta;
                var component = m.Component;

                // Update all indexes the component is part of.
                foreach (var index in IndexesForGroups(component.IndexGroupsMask)
                    .Where(index => _trees[index].Update(bounds, delta, component.Id)))
                {
                    // Mark as changed.
                    _changed[index].Add(component.Id);
                }
                return;
            }

            // Handle position changes (moving components around in the world).
            var translationChanged = message as TranslationChanged?;
            if (translationChanged != null)
            {
                var m = translationChanged.Value;

                Debug.Assert(m.Component.Enabled);

                var component = m.Component;
                var bounds = component.ComputeWorldBounds();
                var velocity = (IVelocity) Manager.GetComponent(component.Entity, ComponentSystem.Manager.GetComponentTypeId<IVelocity>());
                var delta = velocity != null ? velocity.LinearVelocity : Vector2.Zero;

                // Update all indexes the component is part of.
                foreach (var index in IndexesForGroups(component.IndexGroupsMask)
                    .Where(index => _trees[index].Update(bounds, delta, component.Id)))
                {
                    // Mark as changed.
                    _changed[index].Add(component.Id);
                }
            }
        }

        #endregion

        #region Component lookup

        /// <summary>Get all components in the specified range of the query point.</summary>
        /// <param name="center">The point to use as a query point.</param>
        /// <param name="radius">The distance up to which to get neighbors.</param>
        /// <param name="results">The list to use for storing the results.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <returns>All components in range.</returns>
        public void Find(WorldPoint center, float radius, ISet<int> results, ulong groups)
        {
            foreach (var tree in IndexesForGroups(groups).Select(index => _trees[index]))
            {
                Interlocked.Add(ref _queryCountSinceLastUpdate, 1);
                tree.Find(center, radius, results);
            }
        }

        /// <summary>Get all components contained in the specified rectangle.</summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="results">The list to use for storing the results.</param>
        /// <param name="groups">The bitmask representing the groups to check in.</param>
        /// <returns>All components in range.</returns>
        public void Find(WorldBounds rectangle, ISet<int> results, ulong groups)
        {
            foreach (var tree in IndexesForGroups(groups).Select(index => _trees[index]))
            {
                Interlocked.Add(ref _queryCountSinceLastUpdate, 1);
                tree.Find(rectangle, results);
            }
        }

        /// <summary>Gets the list of components for which the index entry changed.</summary>
        public IEnumerable<int> GetChanged(ulong groups)
        {
            return IndexesForGroups(groups)
                .SelectMany(index => _changed[index] ?? Enumerable.Empty<int>())
                .Distinct();
        }

        /// <summary>Gets the bounds for the specified component in the first of the specified groups.</summary>
        /// <param name="component">The component.</param>
        /// <param name="groups">The groups.</param>
        /// <returns></returns>
        public WorldBounds GetBounds(int component, ulong groups)
        {
            return IndexesForGroups(groups)
                .Select(index => _trees[index])
                .Where(tree => tree.Contains(component))
                .Select(tree => tree[component])
                .FirstOrDefault();
        }

        /// <summary>Marks the component as changed in the specified groups.</summary>
        /// <param name="component">The component.</param>
        /// <param name="groups">The index group mask.</param>
        public void Touch(int component, ulong groups)
        {
            foreach (var index in IndexesForGroups(groups))
            {
                _changed[index].Add(component);
            }
        }
        
        /// <summary>Marks the component as unchanged in the specified groups.</summary>
        /// <param name="component">The component.</param>
        /// <param name="groups">The index group mask.</param>
        public void Untouch(int component, ulong groups)
        {
            foreach (var index in IndexesForGroups(groups))
            {
                _changed[index].Remove(component);
            }
        }

        /// <summary>Clears the list of changed components for the specified groups.</summary>
        /// <param name="groups">The index group mask.</param>
        public void ClearTouched(ulong groups)
        {
            foreach (var changed in IndexesForGroups(groups)
                .Select(index => _changed[index])
                .Where(changed => changed != null))
            {
                changed.Clear();
            }
        }

        #endregion

        #region Utility methods

        /// <summary>Utility method used to create indexes flagged in the specified bit mask if they don't already exist.</summary>
        /// <param name="groups">The groups to create index structures for.</param>
        private void EnsureIndexesExist(ulong groups)
        {
            foreach (var index in IndexesForGroups(groups).Where(index => _trees[index] == null))
            {
#if FARMATH
                _trees[index] = new FarCollections.SpatialHashedQuadTree<int>(_maxEntriesPerNode, _minNodeBounds, 0.1f, 2f, (p, v) => p.Write(v), p => p.ReadInt32());
#else
                _trees[index] = new DynamicQuadTree<int>(_maxEntriesPerNode, _minNodeBounds, 0.1f, 2f, (p, v) => p.Write(v), p => p.ReadInt32());
#endif
                _changed[index] = new HashSet<int>();
            }
        }

        /// <summary>
        ///     Utility method that returns a list of all indexes flagged in the specified bit mask.
        /// </summary>
        /// <param name="groups">The groups to get the indexes for.</param>
        /// <returns>A list of the specified indexes.</returns>
        private static IEnumerable<int> IndexesForGroups(ulong groups)
        {
            byte index = 0;
            while (groups > 0)
            {
                if ((groups & 1) == 1)
                {
                    yield return index;
                }
                groups = groups >> 1;
                ++index;
            }
        }

        /// <summary>Adds the specified component to all indexes specified in groups.</summary>
        /// <param name="component">The component to add.</param>
        /// <param name="groups">The indexes to add to.</param>
        private void AddToGroups(IIndexable component, ulong groups)
        {
            // Make sure the indexes exists.
            EnsureIndexesExist(groups);

            // Add the component to all its indexes.
            foreach (var index in IndexesForGroups(groups))
            {
                // Add to each group.
                _trees[index].Add(component.ComputeWorldBounds(), component.Id);
                // Mark as changed.
                _changed[index].Add(component.Id);
            }
        }

        /// <summary>Removes the specified component from the specified indexes.</summary>
        /// <param name="component">The index to remove.</param>
        /// <param name="groups">The groups to remove from.</param>
        private void RemoveFromGroups(IComponent component, ulong groups)
        {
            // Remove from each group.
            foreach (var index in IndexesForGroups(groups))
            {
                _trees[index].Remove(component.Id);
                _changed[index].Remove(component.Id);
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

            packet.Write(IndexCount);
            for (var index = 0; index < _trees.Length; ++index)
            {
                if (_trees[index] == null)
                {
                    continue;
                }
                packet.Write(index);

                packet.Write(_trees[index]);

                packet.Write(_changed[index].Count);
                foreach (var component in _changed[index])
                {
                    packet.Write(component);
                }
            }

            return packet;
        }

        /// <summary>Bring the object to the state in the given packet.</summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            foreach (var tree in _trees.Where(tree => tree != null))
            {
                tree.Clear();
            }
            foreach (var changed in _changed.Where(changed => changed != null))
            {
                changed.Clear();
            }

            var indexCount = packet.ReadInt32();
            for (var i = 0; i < indexCount; ++i)
            {
                var index = packet.ReadInt32();

                if (_trees[index] == null)
                {
#if FARMATH
                    _trees[index] = new FarCollections.SpatialHashedQuadTree<int>(_maxEntriesPerNode, _minNodeBounds, 0.1f, 2f, (p, v) => p.Write(v), p => p.ReadInt32());
#else
                    _trees[index] = new DynamicQuadTree<int>(_maxEntriesPerNode, _minNodeBounds, 0.1f, 2f, (p, v) => p.Write(v), p => p.ReadInt32());
#endif
                }
                if (_changed[index] == null)
                {
                    _changed[index] = new HashSet<int>();
                }

                packet.ReadPacketizableInto(_trees[index]);

                var changedCount = packet.ReadInt32();
                for (var j = 0; j < changedCount; ++j)
                {
                    _changed[index].Add(packet.ReadInt32());
                }
            }
        }

        public override StreamWriter Dump(StreamWriter w, int indent)
        {
            base.Dump(w, indent);

            w.AppendIndent(indent).Write("Trees = {");
            for (var i = 0; i < _trees.Length; ++i)
            {
                var tree = _trees[i];
                if (tree == null)
                {
                    continue;
                }
                w.AppendIndent(indent + 1).Write(i);
                w.Write(" = ");
                w.Dump(tree, indent + 1);
            }
            w.AppendIndent(indent).Write("}");

            w.AppendIndent(indent).Write("Changed = {");
            var first = true;
            foreach (var component in _changed)
            {
                if (!first)
                {
                    w.Write(", ");
                }
                first = false;
                w.Write(component);
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
            var copy = (IndexSystem) base.NewInstance();

#if FARMATH
            copy._trees = new FarCollections.SpatialHashedQuadTree<int>[sizeof (ulong) * 8];
#else
            copy._trees = new DynamicQuadTree<int>[sizeof (ulong) * 8];
#endif
            copy._changed = new HashSet<int>[sizeof (ulong) * 8];

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

            var copy = (IndexSystem) into;

            foreach (var tree in copy._trees.Where(tree => tree != null))
            {
                tree.Clear();
            }
            foreach (var changed in copy._changed.Where(changed => changed != null))
            {
                changed.Clear();
            }

            for (var index = 0; index < _trees.Length; ++index)
            {
                if (_trees[index] == null)
                {
                    continue;
                }

                if (copy._trees[index] == null)
                {
                    copy._trees[index] = _trees[index].NewInstance();
                }
                if (copy._changed[index] == null)
                {
                    copy._changed[index] = new HashSet<int>();
                }

                _trees[index].CopyInto(copy._trees[index]);
                copy._changed[index].UnionWith(_changed[index]);
            }
        }

        #endregion

        #region Debug stuff

        /// <summary>
        ///     Total number of queries over all index structures since the last update. This will always be zero when not
        ///     running in debug mode.
        /// </summary>
        public int QueryCountSinceLastUpdate
        {
            get { return _queryCountSinceLastUpdate; }
        }

        /// <summary>For ref usage in interlocked update.</summary>
        private int _queryCountSinceLastUpdate;

        /// <summary>
        ///     Renders all index structures matching the specified index group bit mask using the specified shape at the
        ///     specified translation.
        /// </summary>
        /// <param name="groups">Bit mask determining which indexes to draw.</param>
        /// <param name="shape">Shape to use for drawing.</param>
        /// <param name="translation">Translation to apply when drawing.</param>
        [Conditional("DEBUG")]
        public void DrawIndex(ulong groups, AbstractShape shape, WorldPoint translation)
        {
            foreach (var tree in IndexesForGroups(groups)
                .Select(index => _trees[index])
                .Where(tree => tree != null))
            {
                tree.Draw(shape, translation);
            }
        }

        #endregion
    }
}