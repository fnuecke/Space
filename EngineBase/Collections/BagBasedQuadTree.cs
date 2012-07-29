using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
using TPoint = Microsoft.Xna.Framework.Point;
using TRectangle = Microsoft.Xna.Framework.Rectangle;

namespace Engine.Collections
{
    /// <summary>
    /// A quad tree that can dynamically grow as needed.
    /// 
    /// <para>
    /// A special restriction is that all nodes will be sized at some power of
    /// two, where every level that power increases:<br/>
    /// <c>node size := minBucketSize &lt;&lt; level</c>.
    /// </para>
    /// 
    /// <para>
    /// This implementation uses one Bag collection per node to store the
    /// node's entires. It appears to be slower than the linked list based
    /// implementation, though.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of the values stored in this tree.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class BagBasedQuadTree<T> : IIndex<T>
    {
        #region Properties

        /// <summary>
        /// The number of values stored in this tree.
        /// </summary>
        public int Count
        {
            get { return _values.Count; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The number of items in a single cell allowed before we try splitting it.
        /// </summary>
        private readonly int _maxEntriesPerNode;

        /// <summary>
        /// The minimum bounds size of a node along an axis, used to stop splitting
        /// at a defined accuracy.
        /// </summary>
        private readonly int _minNodeBounds;

        /// <summary>
        /// The current bounds of the tree. This is a dynamic value, adjusted
        /// based on elements added to the tree.
        /// </summary>
        private TRectangle _bounds;

        /// <summary>
        /// The root node of the tree.
        /// </summary>
        private Node _root;

        /// <summary>
        /// Mapping back from value to entry, for faster value to entry lookup,
        /// when removing items or updating items.
        /// </summary>
        private readonly Dictionary<T, Entry> _values = new Dictionary<T, Entry>();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new, empty quad tree, with the specified parameters.
        /// </summary>
        /// <param name="maxEntriesPerNode">The maximum number of entries per
        /// node before the node will be split.</param>
        /// <param name="minNodeBounds">The minimum bounds size of a node, i.e.
        /// nodes of this size or smaller won't be split regardless of the
        /// number of entries in them.</param>
        public BagBasedQuadTree(int maxEntriesPerNode, int minNodeBounds)
        {
            if (maxEntriesPerNode < 1)
            {
                throw new ArgumentException("Split count must be larger than zero.", "maxEntriesPerNode");
            }
            if (minNodeBounds < 1)
            {
                throw new ArgumentException("Bucket size must be larger than zero.", "minNodeBounds");
            }
            _maxEntriesPerNode = maxEntriesPerNode;
            _minNodeBounds = minNodeBounds;

            Clear();
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Add a new entry to the tree, with the specified bounds, with the
        /// specified associated value.
        /// </summary>
        /// <param name="bounds">The bounds of the entry.</param>
        /// <param name="item">The value associated with the point.</param>
        /// <exception cref="ArgumentException">This value is already stored
        /// in the tree.</exception>
        public void Add(TRectangle bounds, T item)
        {
            if (Contains(item))
            {
                throw new ArgumentException("Entry is already in the tree.", "item");
            }

            // Create the entry to add.
            var entry = new Entry {Bounds = bounds, Value = item};

            // Handle dynamic growth.
            EnsureCapacity(ref bounds);

            // Get the node to insert in.
            var nodeBounds = _bounds;
            var node = FindNode(ref bounds, _root, ref nodeBounds);
            AddToNode(node, ref nodeBounds, entry);

            // Store the entry in the value lookup.
            _values.Add(entry.Value, entry);
        }

        /// <summary>
        /// Update a single entry by changing its bounds. If the entry is not
        /// already in the tree, this will return <code>false</code>.
        /// </summary>
        /// <param name="newBounds">The new bounds of the entry.</param>
        /// <param name="item">The value of the entry.</param>
        /// <returns>
        ///   <code>true</code> if the update was successful.
        /// </returns>
        public bool Update(TRectangle newBounds, T item)
        {
            // Check if we have that entry, if not add it.
            if (!Contains(item))
            {
                return false;
            }

            // Get the old position.
            var entry = _values[item];

            // Update tree.
            UpdateBounds(ref newBounds, entry);

            return true;
        }

        /// <summary>
        /// Remove the specified value from the tree.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            // See if we have that entry.
            if (!Contains(item))
            {
                return false;
            }

            // Yes, get the instance.
            var entry = _values[item];

            // Get the node the entry would be in.
            var nodeBounds = _bounds;
            var node = FindNode(ref entry.Bounds, _root, ref nodeBounds);
            RemoveFromNode(node, entry);

            // Remove the entry from the value lookup.
            _values.Remove(entry.Value);

            // If the tree is empty, restore the bounds to their defaults.
            if (Count == 0)
            {
                _bounds.X = _bounds.Y = -_minNodeBounds;
                _bounds.Width = _bounds.Height = _minNodeBounds << 1;
            }

            return true;
        }

        /// <summary>
        /// Test whether this tree contains the specified value.
        /// </summary>
        /// <param name="item">The value to look for.</param>
        /// <returns>
        ///   <c>true</c> if the tree contains the value at the
        /// specified point.
        /// </returns>
        public bool Contains(T item)
        {
            return _values.ContainsKey(item);
        }

        /// <summary>
        /// Removes all entries from this tree.
        /// </summary>
        public void Clear()
        {
            _root = new Node(_maxEntriesPerNode);
            _bounds.X = _bounds.Y = -_minNodeBounds;
            _bounds.Width = _bounds.Height = _minNodeBounds << 1;
            _values.Clear();
        }

        /// <summary>
        /// Get the bounds at which the specified item is currently stored.
        /// </summary>
        public TRectangle this[T item] { get { return _values[item].Bounds; } }

        /// <summary>
        /// Perform a circular query on this tree. This will return all entries
        /// in the tree that are in the specified range to the specified point,
        /// using a euclidean distance.
        /// </summary>
        /// <param name="point">The query point near which to get entries.</param>
        /// <param name="range">The maximum distance an entry may be away
        /// from the query point to be returned.</param>
        /// <param name="list">The list to put the results into, or null in
        /// which case a new list will be created and returned.</param>
        public void Find(TPoint point, float range, ref ICollection<T> list)
        {
            // Skip if the tree is empty.
            if (Count > 0)
            {
                // Recurse through the tree, starting at the containing node, to find
                // nodes intersecting with the range query.
                Accumulate(_root, ref _bounds, ref point, range, ref list);
            }
        }

        /// <summary>
        /// Perform an area query on this tree. This will return all entries
        /// in the tree that are in contained the specified rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="list">The list to put the results into, or null in
        /// which case a new list will be created and returned.</param>
        public void Find(ref TRectangle rectangle, ref ICollection<T> list)
        {
            // Skip if the tree is empty.
            if (Count > 0)
            {
                // Recurse through the tree, starting at the containing node, to find
                // nodes intersecting with the range query.
                Accumulate(_root, ref _bounds, ref rectangle, ref list);
            }
        }

        #endregion

        #region Internal functionality

        /// <summary>
        /// Find a node at the given query point. If possible, this will return
        /// a leaf node. If there is no leaf node at the query point, it will
        /// return the inner node that would contain the leaf node that would
        /// hold that point, or in which the splits intersect the bounds.
        /// </summary>
        /// <param name="bounds">The bounds to get the node for.</param>
        /// <param name="node">The node to start searching in.</param>
        /// <param name="nodeBounds">Will be the bounds of the node.</param>
        /// <returns>The node for the specified query point.</returns>
        private static Node FindNode(ref TRectangle bounds, Node node, ref TRectangle nodeBounds)
        {
            // We're definitely done when we hit a leaf.
            while (!node.IsLeaf)
            {
                // Get current child size.
                var childSize = nodeBounds.Width >> 1;

                // Into which child node would we descend?
                var cell = ComputeCell(ref nodeBounds, ref bounds);

                // Can we descend and do we have to create that child?
                if (cell < 0 || node.Children[cell] == null)
                {
                    // No, return the current node.
                    return node;
                }

                // Yes, descend into that node.
                node = node.Children[cell];
                nodeBounds.X += (((cell & 1) == 0) ? 0 : childSize);
                nodeBounds.Y += (((cell & 2) == 0) ? 0 : childSize);
                nodeBounds.Width = childSize;
                nodeBounds.Height = childSize;
            }

            return node;
        }

        #region Restructuring

        /// <summary>
        /// Ensures the tree can contain the given point.
        /// </summary>
        /// <param name="bounds">The bounds to ensure tree size for.</param>
        private void EnsureCapacity(ref TRectangle bounds)
        {
            // Inserts a new level on top of the root node, making it the new root
            // node, until the tree bounds completely contain the entry bounds. Will
            // reattach all of the root node's child nodes to the appropriate new
            // child nodes.
            while (_bounds.X >= bounds.X ||
                   _bounds.Y >= bounds.Y ||
                   _bounds.X + _bounds.Width <= bounds.X + bounds.Width ||
                   _bounds.Y + _bounds.Height <= bounds.Y + bounds.Height)
            {
                // Check sectors for relocations. For existing child nodes
                // of root (0, 1, 2, null)
                // +---+---+
                // | 0 | 1 |
                // +---+---+
                // | 2 |
                // +---+
                // this results in the wrappers (a, b, c, d)
                // +-------+-------+
                // |  a    |    b  |
                // |   +---+---+   |
                // |   | 0 | 1 |   |
                // +---+---+---+---+
                // |   | 2 |
                // |   +---+
                // |  c    |
                // +-------+
                // If there was no node before, there won't be one afterwards, either.
                // If the node was a leaf, it will stay as it was (but size will
                // implicitly larger).
                for (var childNumber = 0; childNumber < 4; childNumber++)
                {
                    // Get the old child node that will be attached to the new node.
                    var child = _root.Children[childNumber];

                    // Skip unset ones and leaf nodes (leaf nodes would just have to
                    // be collapsed into the new wrapper anyway, and because nodes
                    // are not explicitly aware of their size, just via their position
                    // in the tree, we can keep it).
                    if (child == null || child.IsLeaf)
                    {
                        continue;
                    }

                    // Allocate new node.
                    var wrapper = new Node(_maxEntriesPerNode)
                                  {
                                      Parent = _root,
                                      ChildEntryCount = child.Entries.Count + child.ChildEntryCount
                                  };

                    // Set opposing corner inside that node to old node in that corner.
                    // The (3 - x) will always yield the diagonally opposite cell to x.
                    wrapper.Children[3 - childNumber] = child;

                    // Replace old child in the root node, and set the old child node's
                    // parent to the inserted node.
                    _root.Children[childNumber] = wrapper;
                    child.Parent = wrapper;
                }

                // Adjust the overall tree bounds.
                _bounds.X = _bounds.X << 1;
                _bounds.Y = _bounds.Y << 1;
                _bounds.Width = _bounds.Width << 1;
                _bounds.Height = _bounds.Height << 1;
            }
        }

        /// <summary>
        /// Adds an entry to a node and handles overflow as necessary.
        /// </summary>
        /// <param name="node">The node to insert in.</param>
        /// <param name="nodeBounds">The bounds of the node.</param>
        /// <param name="entry">The node to insert.</param>
        private void AddToNode(Node node, ref TRectangle nodeBounds, Entry entry)
        {
            // Check what type of node we have.
            if (!node.IsLeaf)
            {
                // Inner node, see if we can create a child node for that entry (there
                // is none yet, because otherwise we would have received that as the
                // parameter).
                var cell = ComputeCell(ref nodeBounds, ref entry.Bounds);
                if (cell >= 0)
                {
                    // Yes we can. Allocate the new child node.
                    Debug.Assert(node.Children[cell] == null);
                    node.Children[cell] = new Node(_maxEntriesPerNode) { Parent = node };

                    // Mark the new node as the current one (the one we're adding to).
                    node = node.Children[cell];

                    // Also adjust the current bounds, for the following split operation.
                    nodeBounds.Width >>= 1;
                    nodeBounds.Height >>= 1;
                    nodeBounds.X += (((cell & 1) == 0) ? 0 : nodeBounds.Width);
                    nodeBounds.Y += (((cell & 2) == 0) ? 0 : nodeBounds.Height);
                }
            }

            // Insert in whatever node we have to.
            node.Entries.Add(entry);

            // Update child node count in parents.
            var parent = node.Parent;
            while (parent != null)
            {
                ++parent.ChildEntryCount;
                parent = parent.Parent;
            }

            // Check whether we need to split the node, and do it if necessary.
            TrySplitNode(node, ref nodeBounds);
        }

        /// <summary>
        /// Check if a node needs to be split, and split it if allowed to.
        /// </summary>
        /// <param name="node">The actual node to split.</param>
        /// <param name="nodeBounds">The bounds of the node.</param>
        private void TrySplitNode(Node node, ref TRectangle nodeBounds)
        {
            // Should we split?
            if (!node.IsLeaf || // Already is split.
                node.Entries.Count <= _maxEntriesPerNode || // No reason to.
                nodeBounds.Width <= _minNodeBounds) // We can't (too small already).
            {
                return;
            }

            // Check each entry to which new cell it'll belong.
            for (var i = node.Entries.Count; i > 0; --i)
            {
                var entry = node.Entries[i - 1];

                // In which child node would we insert?
                var cell = ComputeCell(ref nodeBounds, ref entry.Bounds);
                if (cell < 0)
                {
                    // Stays in this node, skip it.
                    continue;
                }

                // Do we have to create that child? (It might already exist because
                // we created it in a previous iteration for another entry)
                if (node.Children[cell] == null)
                {
                    // Yes, create the node.
                    node.Children[cell] = new Node(_maxEntriesPerNode) { Parent = node };
                }

                // Add to the node.
                node.Children[cell].Entries.Add(entry);

                // Update parent's child counter.
                ++node.ChildEntryCount;

                // Remove from old node.
                node.Entries.RemoveAt(i - 1);
            }

            // Do this recursively if the split resulted in another node that
            // has too many entries.
            var childBounds = new TRectangle {Width = nodeBounds.Width >> 1, Height = nodeBounds.Height >> 1};

            if (node.Children[0] != null)
            {
                childBounds.X = nodeBounds.X;
                childBounds.Y = nodeBounds.Y;
                TrySplitNode(node.Children[0], ref childBounds);
            }
            if (node.Children[1] != null)
            {
                childBounds.X = nodeBounds.X + childBounds.Width;
                childBounds.Y = nodeBounds.Y;
                TrySplitNode(node.Children[1], ref childBounds);
            }
            if (node.Children[2] != null)
            {
                childBounds.X = nodeBounds.X;
                childBounds.Y = nodeBounds.Y + childBounds.Height;
                TrySplitNode(node.Children[2], ref childBounds);
                childBounds.Y -= childBounds.Height;
            }
            if (node.Children[3] != null)
            {
                childBounds.X = nodeBounds.X + childBounds.Width;
                childBounds.Y = nodeBounds.Y + childBounds.Height;
                TrySplitNode(node.Children[3], ref childBounds);
            }
        }

        /// <summary>
        /// Removes an entry from a node.
        /// </summary>
        /// <param name="node">The node to remove from.</param>
        /// <param name="entry">The entry to remove.</param>
        private void RemoveFromNode(Node node, Entry entry)
        {
            // Remove from the node.
            node.Entries.Remove(entry);

            // Update child node count in parents.
            var parent = node.Parent;
            while (parent != null)
            {
                --parent.ChildEntryCount;
                parent = parent.Parent;
            }

            // See if we can collapse the branch.
            CollapseBranch(node);
        }

        /// <summary>
        /// Try to collapse up a branch starting with the specified child node.
        /// This walks the tree towards the root, removing child nodes while possible.
        /// </summary>
        /// <param name="node">The node to start cleaning at.</param>
        private void CollapseBranch(Node node)
        {
            // Walk up the tree.
            while (node != null)
            {
                // Skip leaf nodes.
                if (!node.IsLeaf)
                {
                    // Check if child nodes are unnecessary for the parent's node.
                    var count = node.Entries.Count + node.ChildEntryCount;
                    if (count > 0 && count <= _maxEntriesPerNode)
                    {
                        // We can prune the child nodes. Add all entries from children
                        // to our own list. We can be sure that all child nodes are
                        // leaf nodes, because otherwise there would be too many entries.
                        if (node.Children[0] != null)
                        {
                            for (var j = node.Children[0].Entries.Count; j > 0; --j)
                            {
                                node.Entries.Add(node.Children[0].Entries[j - 1]);
                            }
                            node.Children[0] = null;
                        }
                        if (node.Children[1] != null)
                        {
                            for (var j = node.Children[1].Entries.Count; j > 0; --j)
                            {
                                node.Entries.Add(node.Children[1].Entries[j - 1]);
                            }
                            node.Children[1] = null;
                        }
                        if (node.Children[2] != null)
                        {
                            for (var j = node.Children[2].Entries.Count; j > 0; --j)
                            {
                                node.Entries.Add(node.Children[2].Entries[j - 1]);
                            }
                            node.Children[2] = null;
                        }
                        if (node.Children[3] != null)
                        {
                            for (var j = node.Children[3].Entries.Count; j > 0; --j)
                            {
                                node.Entries.Add(node.Children[3].Entries[j - 1]);
                            }
                            node.Children[3] = null;
                        }

                        // Reset child entry counter.
                        node.ChildEntryCount = 0;
                    }
                    else
                    {
                        // The node needs to stay split. Check if we have empty child nodes.
                        // If so, remove them.
                        if (node.Children[0] != null && (node.Children[0].Entries.Count + node.Children[0].ChildEntryCount) == 0)
                        {
                            node.Children[0] = null;
                        }
                        if (node.Children[1] != null && (node.Children[1].Entries.Count + node.Children[1].ChildEntryCount) == 0)
                        {
                            node.Children[1] = null;
                        }
                        if (node.Children[2] != null && (node.Children[2].Entries.Count + node.Children[2].ChildEntryCount) == 0)
                        {
                            node.Children[2] = null;
                        }
                        if (node.Children[3] != null && (node.Children[3].Entries.Count + node.Children[3].ChildEntryCount) == 0)
                        {
                            node.Children[3] = null;
                        }

                        // If we still have children at this point, we could not merge nor
                        // completely empty this node, meaning there's nothing left for us
                        // to do further up the tree.
                        if (!node.IsLeaf)
                        {
                            return;
                        }
                    }
                }

                // Check parent.
                node = node.Parent;
            }
        }

        /// <summary>
        /// Updates the bounds for the specified entry, moving it to another tree
        /// node if necessary.
        /// </summary>
        /// <param name="newBounds">The new bounds.</param>
        /// <param name="entry">The entry.</param>
        private void UpdateBounds(ref TRectangle newBounds, Entry entry)
        {
            // Node may have changed. Get the node the entry is currently stored in.
            var nodeBounds = _bounds;
            var node = FindNode(ref entry.Bounds, _root, ref nodeBounds);

            // Check if the entry should go to a different node now.
            if (nodeBounds.X >= newBounds.X ||
                nodeBounds.Y >= newBounds.Y ||
                nodeBounds.X + nodeBounds.Width <= newBounds.X + newBounds.Width ||
                nodeBounds.Y + nodeBounds.Height <= newBounds.Y + newBounds.Height ||
                ((node.Entries.Count + node.ChildEntryCount) > _maxEntriesPerNode && ComputeCell(ref nodeBounds, ref newBounds) > -1))
            {
                // Did not fit in node anymore, remove from that node.
                RemoveFromNode(node, entry);

                // Remove the entry from the value lookup.
                _values.Remove(entry.Value);

                // And add again.
                Add(newBounds, entry.Value);
            }
            else
            {
                // Either we push the entry into a child node, or it stays where it is,
                // whichever it is, we'll want to update the entry's bounds.
                entry.Bounds = newBounds;
            }
        }

        #endregion

        #endregion

        #region Utility methods

        #region Tree traversal

        /// <summary>
        /// Computes the cell of a node with the specified position and child
        /// node size the specified bounds falls into. If there is no clear
        /// result, this will return -1, which means the bounds must be stored
        /// in the specified node itself (assuming the node can contain the
        /// bounds).
        /// </summary>
        /// <param name="nodeBounds">The node bounds to check for.</param>
        /// <param name="entryBounds">The entry bounds to check for.</param>
        /// <returns>The cell number the bounds fall into.</returns>
        private static int ComputeCell(ref TRectangle nodeBounds, ref TRectangle entryBounds)
        {
            var halfNodeSize = nodeBounds.Width >> 1;

            // Check if the bounds are on the splits.
            var midX = nodeBounds.X + halfNodeSize;
            if (IsInInterval(entryBounds.Left, entryBounds.X + entryBounds.Width, midX))
            {
                // Y split runs through the bounds.
                return -1;
            }
            var midY = nodeBounds.Y + halfNodeSize;
            if (IsInInterval(entryBounds.Top, entryBounds.Y + entryBounds.Height, midY))
            {
                // X split runs through the bounds.
                return -1;
            }

            // Otherwise check which child node the bounds fall into.
            var cell = 0;
            if (entryBounds.X > midX)
            {
                // Right half.
                cell |= 1;
            }
            if (entryBounds.Y > midY)
            {
                // Lower half.
                cell |= 2;
            }
            return cell;
        }

        /// <summary>
        /// Tests if the specified value lies in the specified interval.
        /// </summary>
        /// <param name="low">The low end of the interval (inclusive).</param>
        /// <param name="high">The high end of the interval (inclusive).</param>
        /// <param name="value">The value to test for.</param>
        /// <returns><code>true</code> if the value lies in the interval.</returns>
        private static bool IsInInterval(int low, int high, int value)
        {
            return value >= low && value <= high;
        }

        /// <summary>
        /// Accumulate all entries in range of a circular range query to the
        /// given list. This recurses the tree down inner nodes that intersect
        /// the query, until it finds a leaf node. Then adds all entries in the
        /// leaf that are in range.
        /// </summary>
        /// <param name="node">The current node.</param>
        /// <param name="nodeBounds">The bounds of the current node.</param>
        /// <param name="point">The query point.</param>
        /// <param name="range">The query range.</param>
        /// <param name="list">The result list.</param>
        private static void Accumulate(Node node, ref TRectangle nodeBounds, ref TPoint point, float range, ref ICollection<T> list)
        {
            // Check how to proceed.
            switch (ComputeIntersection(ref point, range, ref nodeBounds))
            {
                case IntersectionType.Contains:
                {
                    // Box completely contained in query, return all points in it,
                    // no need to recurse further.
                    node.AddOwnEntries(ref list);
                    node.AddChildEntries(ref list);
                    break;
                }
                case IntersectionType.Intersects:
                {
                    // Add all local entries in this node that are in range, regardless of
                    // whether this is an inner or a leaf node.
                    node.AddOwnEntries(ref point, range, ref list);

                    if (!node.IsLeaf)
                    {
                        // Recurse into child nodes.
                        var childBounds = new TRectangle {Width = nodeBounds.Width >> 1, Height = nodeBounds.Height >> 1};

                        // Unrolled loop.
                        if (node.Children[0] != null)
                        {
                            childBounds.X = nodeBounds.X;
                            childBounds.Y = nodeBounds.Y;
                            Accumulate(node.Children[0], ref childBounds, ref point, range, ref list);
                        }
                        if (node.Children[1] != null)
                        {
                            childBounds.X = nodeBounds.X + childBounds.Width;
                            childBounds.Y = nodeBounds.Y;
                            Accumulate(node.Children[1], ref childBounds, ref point, range, ref list);
                        }
                        if (node.Children[2] != null)
                        {
                            childBounds.X = nodeBounds.X;
                            childBounds.Y = nodeBounds.Y + childBounds.Height;
                            Accumulate(node.Children[2], ref childBounds, ref point, range, ref list);
                        }
                        if (node.Children[3] != null)
                        {
                            childBounds.X = nodeBounds.X + childBounds.Width;
                            childBounds.Y = nodeBounds.Y + childBounds.Height;
                            Accumulate(node.Children[3], ref childBounds, ref point, range, ref list);
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Accumulate all entries in range of a rectangular range query to the
        /// given list. This recurses the tree down inner nodes that intersect
        /// the query, until it finds a leaf node. Then adds all entries in the
        /// leaf that are in range.
        /// </summary>
        /// <param name="node">The current node.</param>
        /// <param name="nodeBounds">The bounds of the current node.</param>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="list">The result list.</param>
        private static void Accumulate(Node node, ref TRectangle nodeBounds, ref TRectangle rectangle, ref ICollection<T> list)
        {
            // Check how to proceed.
            switch (ComputeIntersection(ref rectangle, ref nodeBounds))
            {
                case IntersectionType.Contains:
                {
                    // Box completely contained in query, return all points in it,
                    // no need to recurse further.
                    node.AddOwnEntries(ref list);
                    node.AddChildEntries(ref list);
                    break;
                }
                case IntersectionType.Intersects:
                {
                    // Add all local entries in this node that are in range, regardless of
                    // whether this is an inner or a leaf node.
                    node.AddOwnEntries(ref rectangle, ref list);

                    if (!node.IsLeaf)
                    {
                        // Recurse into child nodes.
                        var childBounds = new TRectangle {Width = nodeBounds.Width >> 1, Height = nodeBounds.Height >> 1};

                        // Unrolled loop.
                        if (node.Children[0] != null)
                        {
                            childBounds.X = nodeBounds.X;
                            childBounds.Y = nodeBounds.Y;
                            Accumulate(node.Children[0], ref childBounds, ref rectangle, ref list);
                        }
                        if (node.Children[1] != null)
                        {
                            childBounds.X = nodeBounds.X + childBounds.Width;
                            childBounds.Y = nodeBounds.Y;
                            Accumulate(node.Children[1], ref childBounds, ref rectangle, ref list);
                        }
                        if (node.Children[2] != null)
                        {
                            childBounds.X = nodeBounds.X;
                            childBounds.Y = nodeBounds.Y + childBounds.Height;
                            Accumulate(node.Children[2], ref childBounds, ref rectangle, ref list);
                        }
                        if (node.Children[3] != null)
                        {
                            childBounds.X = nodeBounds.X + childBounds.Width;
                            childBounds.Y = nodeBounds.Y + childBounds.Height;
                            Accumulate(node.Children[3], ref childBounds, ref rectangle, ref list);
                        }
                    }
                    break;
                }
            }
        }

        #endregion

        #region Intersection testing

        /// <summary>
        /// Possible intersection types of geometric shapes.
        /// </summary>
        private enum IntersectionType
        {
            /// <summary>
            /// The shapes are cleanly separated from each other.
            /// </summary>
            Disjoint,
            
            /// <summary>
            /// The two shapes are overlapping each other.
            /// </summary>
            Intersects,

            /// <summary>
            /// One shape is completely contained within the other.
            /// </summary>
            Contains
        }

        /// <summary>
        /// Circle / Box intersection test.
        /// </summary>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="bounds">The box.</param>
        /// <returns>How the two intersect.</returns>
        private static IntersectionType ComputeIntersection(ref TPoint center, float radius, ref TRectangle bounds)
        {
            // Check for axis aligned separation.
            if (bounds.X + bounds.Width < center.X - radius ||
                bounds.Y + bounds.Height < center.Y - radius ||
                bounds.X > center.X + radius ||
                bounds.Y > center.Y + radius)
            {
                return IntersectionType.Disjoint;
            }

            // Check for unaligned separation.
            var right = bounds.X + bounds.Width;
            var bottom = bounds.Y + bounds.Height;
            var closest = center;
            if (center.X < bounds.X)
            {
                closest.X = bounds.X;
            }
            else
            {
                if (center.X > right)
                {
                    closest.X = right;
                }
            }
            if (center.Y < bounds.Y)
            {
                closest.Y = bounds.Y;
            }
            else
            {
                if (center.Y > bottom)
                {
                    closest.Y = bottom;
                }
            }
            var distanceX = closest.X - center.X;
            var distanceY = closest.Y - center.Y;
            var radiusSquared = radius * radius;
            if ((distanceX * distanceX + distanceY * distanceY) > radiusSquared)
            {
                return IntersectionType.Disjoint;
            }

            // At least intersection, check furthest point to check if the
            // box is contained within the circle.
            distanceX = Math.Max(Math.Abs(center.X - bounds.X), Math.Abs(center.X - right));
            distanceY = Math.Max(Math.Abs(center.Y - bounds.Y), Math.Abs(center.Y - bottom));
            var outside = (distanceX * distanceX + distanceY * distanceY) > radiusSquared;
            return outside ? IntersectionType.Intersects : IntersectionType.Contains;
        }

        /// <summary>
        /// Box / Box intersection test.
        /// </summary>
        /// <param name="rectangle">The first box.</param>
        /// <param name="bounds">The second box.</param>
        /// <returns>How the two intersect.</returns>
        private static IntersectionType ComputeIntersection(ref TRectangle rectangle, ref TRectangle bounds)
        {
            var rr = rectangle.X + rectangle.Width;
            var rb = rectangle.Y + rectangle.Height;
            var br = bounds.X + bounds.Width;
            var bb = bounds.Y + bounds.Height;
            if (rectangle.X > br ||
                rectangle.Y > bb ||
                bounds.X > rr ||
                bounds.Y > rb)
            {
                return IntersectionType.Disjoint;
            }

            if (bounds.X >= rectangle.X &&
                bounds.Y >= rectangle.Y &&
                br <= rr &&
                bb <= rb)
            {
                return IntersectionType.Contains;
            }

            return IntersectionType.Intersects;
        }

        #endregion

        #endregion

        #region Types

        /// <summary>
        /// A node in the tree, which can either be a leaf or an inner node.
        /// 
        /// <para>
        /// Leaf nodes only hold a list of entities, whereas inner nodes also
        /// reference to more specific child nodes.
        /// </para>
        /// </summary>
        [DebuggerDisplay("Count = {EntryCount}, Leaf = {IsLeaf}")]
        private sealed class Node
        {
            #region Properties

            /// <summary>
            /// Whether this node is a leaf node.
            /// </summary>
            public bool IsLeaf
            {
                get { return Children[0] == null && Children[1] == null
                     && Children[2] == null && Children[3] == null; }
            }

            #endregion

            #region Fields

            /// <summary>
            /// The parent of this node.
            /// </summary>
            public Node Parent;

            /// <summary>
            /// The children this node points to.
            /// </summary>
            public readonly Node[] Children = new Node[4];

            /// <summary>
            /// All entries stored in this node and its children.
            /// </summary>
            public readonly Bag<Entry> Entries;

            /// <summary>
            /// Number of entries in all descendant nodes of this node.
            /// </summary>
            public int ChildEntryCount;

            #endregion

            #region Constructor

            /// <summary>
            /// Initializes a new instance of the <see cref="QuadTree&lt;T&gt;.Node"/> class
            /// with the specified entity capacity.
            /// </summary>
            /// <param name="capacity">The initial capacity.</param>
            public Node(int capacity)
            {
                Entries = new Bag<Entry>(capacity);
            }

            #endregion

            #region Entry Accumulator

            public void AddOwnEntries(ref ICollection<T> list)
            {
                // Add all entries to the collection.
                foreach (var entry in Entries)
                {
                    list.Add(entry.Value);
                }
            }

            public void AddOwnEntries(ref TPoint point, float range, ref ICollection<T> list)
            {
                // Add all entries to the collection.
                foreach (var entry in Entries)
                {
                    if (ComputeIntersection(ref point, range, ref entry.Bounds) != IntersectionType.Disjoint)
                    {
                        list.Add(entry.Value);
                    }
                }
            }

            public void AddOwnEntries(ref TRectangle rectangle, ref ICollection<T> list)
            {
                // Add all entries to the collection.
                foreach (var entry in Entries)
                {
                    if (ComputeIntersection(ref rectangle, ref entry.Bounds) != IntersectionType.Disjoint)
                    {
                        list.Add(entry.Value);
                    }
                }
            }

            public void AddChildEntries(ref ICollection<T> list)
            {
                // Add all entries to the collection.
                if (Children[0] != null)
                {
                    Children[0].AddOwnEntries(ref list);
                    Children[0].AddChildEntries(ref list);
                }
                if (Children[1] != null)
                {
                    Children[1].AddOwnEntries(ref list);
                    Children[1].AddChildEntries(ref list);
                }
                if (Children[2] != null)
                {
                    Children[2].AddOwnEntries(ref list);
                    Children[2].AddChildEntries(ref list);
                }
                if (Children[3] != null)
                {
                    Children[3].AddOwnEntries(ref list);
                    Children[3].AddChildEntries(ref list);
                }
            }

            #endregion
        }

        /// <summary>
        /// A single entry in the tree, uniquely identified by its value.
        /// </summary>
        [DebuggerDisplay("Bounds = {Bounds}, Value = {Value}")]
        private sealed class Entry
        {
            #region Fields

            /// <summary>
            /// The point at which the entry is stored.
            /// </summary>
            public TRectangle Bounds;

            /// <summary>
            /// The value stored in this entry.
            /// </summary>
            public T Value;

            #endregion
        }

        #endregion

        #region Enumerable

        /// <summary>
        /// Get an enumerator over the values in this tree.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Tuple<TRectangle, T>> GetEnumerator()
        {
            foreach (var entry in _values)
            {
                yield return Tuple.Create(entry.Value.Bounds, entry.Key);
            }
        }

        /// <summary>
        /// Get a non-generic enumerator over the values in this tree.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
