using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Graphics;
using Microsoft.Xna.Framework;

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
    /// All nodes can quickly iterate over all entries stored in all of their
    /// child nodes. The actual entries are stored in a linked list, which is
    /// sorted in a way that allows unambiguous mapping of a section of that
    /// linked list to a subtree.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of the values stored in this tree.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class QuadTree<T> : IIndex<T>
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
        /// The number of items in a single cell allowed before splitting the cell.
        /// </summary>
        private readonly int _maxEntriesPerNode;

        /// <summary>
        /// The minimum size of a grid cell, used to stop splitting at a
        /// defined accuracy.
        /// </summary>
        private readonly int _minBucketSize;

        /// <summary>
        /// The current bounds of the tree. This is a dynamic value, adjusted
        /// based on elements added to the tree.
        /// </summary>
        private Microsoft.Xna.Framework.Rectangle _bounds = Microsoft.Xna.Framework.Rectangle.Empty;

        /// <summary>
        /// The root node of the tree.
        /// </summary>
        private Node _root;

        /// <summary>
        /// A list of all entries in the tree. The linked list allows simply
        /// adding an entry to a leaf node, keeping the pointers to the segment
        /// of an inner intact.
        /// </summary>
        private Entry _entries;

        /// <summary>
        /// Mapping back from value to entry, for faster removal.
        /// </summary>
        private readonly Dictionary<T, Entry> _values = new Dictionary<T, Entry>();

        /// <summary>
        /// Reused list when splitting nodes and so on (not re-allocating each
        /// run).
        /// </summary>
        private readonly List<Entry> _reusableEntryList;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new, empty quad tree, with the specified parameters.
        /// </summary>
        /// <param name="maxEntriesPerNode">The maximum number of entries per
        /// node before the node will be split.</param>
        /// <param name="minBucketSize">The minimum size of a bucket, i.e.
        /// nodes of this size or smaller won't be split regardless of the
        /// number of entries in them.</param>
        public QuadTree(int maxEntriesPerNode, int minBucketSize)
        {
            if (maxEntriesPerNode < 1)
            {
                throw new ArgumentException("Split count must be larger than zero.", "maxEntriesPerNode");
            }
            if (minBucketSize < 1)
            {
                throw new ArgumentException("Bucket size must be larger than zero.", "minBucketSize");
            }
            _maxEntriesPerNode = maxEntriesPerNode;
            _minBucketSize = minBucketSize;

            _bounds.X = _bounds.Y = -_minBucketSize;
            _bounds.Width = _bounds.Height = _minBucketSize << 1;

            _root = new Node();

            _reusableEntryList = new List<Entry>(_maxEntriesPerNode + 1);
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Add a new entry to the tree, at the specified position, with the
        /// specified associated value.
        /// </summary>
        /// <param name="point">The point at which to store the entry.</param>
        /// <param name="value">The value associated with the point.</param>
        /// <exception cref="ArgumentException">This pair of point and value
        /// are already stored in the tree.</exception>
        public void Add(ref Vector2 point, T value)
        {
            if (Contains(value))
            {
                throw new ArgumentException("Entry is already in the tree at the specified point.", "value");
            }

            // Create the entry to add.
            var entry = new Entry {Point = point, Value = value};

            // Handle dynamic growth.
            EnsureCapacity(ref point);

            // Get the node to insert in.
            int nodeX, nodeY, nodeSize;
            var insertionNode = FindNode(ref point, out nodeX, out nodeY, out nodeSize);

            // If it's not a leaf node, create the leaf node for the new entry.
            // Also get the node in the linked list to insert after.
            Entry insertAfter;
            if (!insertionNode.IsLeaf)
            {
                var cell = ComputeCell(nodeX, nodeY, nodeSize >> 1, ref point);
                insertionNode.Children[cell] = new Node {Parent = insertionNode};
                insertionNode = insertionNode.Children[cell];
                insertAfter = insertionNode.Parent.HighEntry;
            }
            else
            {
                // Got a leaf, insert in it.
                insertAfter = insertionNode.LowEntry;
            }

            // Add the data, get the newly created list entry.
            if (insertAfter != null)
            {
                entry.InsertAfter(insertAfter);
            }
            else
            {
                _entries = entry;
            }
            _values.Add(value, entry);

            var node = insertionNode;
            while (node != null)
            {
                if (node.LowEntry == node.HighEntry)
                {
                    // Only one node yet, or empty.
                    node.LowEntry = node.LowEntry ?? entry;
                    node.HighEntry = entry;
                }
                else if (node.HighEntry == insertAfter)
                {
                    // Inserted after high node, adjust accordingly.
                    node.HighEntry = entry;
                }

                // Remember we have one more entry.
                ++node.EntryCount;

                // Continue checking in our parent.
                node = node.Parent;
            }

            // We need to split the node.
            SplitNodeIfNecessary(nodeX, nodeY, nodeSize, insertionNode);
        }

        /// <summary>
        /// Add a new entry to the tree, at the specified position, with the
        /// specified associated value.
        /// </summary>
        /// <param name="point">The point at which to store the entry.</param>
        /// <param name="value">The value associated with the point.</param>
        /// <exception cref="ArgumentException">This pair of point and value
        /// are already stored in the tree.</exception>
        public void Add(Vector2 point, T value)
        {
            Add(ref point, value);
        }

        /// <summary>
        /// Update a single entry by changing its position. If the entry is not
        /// already in the tree, it will be added.
        /// </summary>
        /// <param name="newPoint">The new position of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <exception cref="ArgumentException">If there is no such value in
        /// the tree at the specified old position.</exception>
        public void Update(ref Vector2 newPoint, T value)
        {
            // Check if we have that entry, if not add it.
            if (!Contains(value))
            {
                Add(ref newPoint, value);
                return;
            }

            // Get the old position.
            var entry = _values[value];

            // Handle dynamic growth.
            EnsureCapacity(ref newPoint);

            // Out parameters we don't care for.
            int nodeX, nodeY, nodeSize;

            // Get the node the entry would be in.
            var oldNode = FindNode(ref entry.Point, out nodeX, out nodeY, out nodeSize);

            // See if the new point falls into the same node, otherwise re-insert.
            var newNode = FindNode(ref newPoint, out nodeX, out nodeY, out nodeSize);
            if (oldNode == newNode)
            {
                // Same node, just update the entry.
                entry.Point = newPoint;
            }
            else
            {
                // Different node, re-insert.
                RemoveFromNode(oldNode, entry);
                Add(ref newPoint, value);
            }
        }

        /// <summary>
        /// Update a single entry by changing its position. If the entry is not
        /// already in the tree, it will be added.
        /// </summary>
        /// <param name="newPoint">The new position of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <exception cref="ArgumentException">If there is no such value in
        /// the tree at the specified old position.</exception>
        public void Update(Vector2 newPoint, T value)
        {
            Update(ref newPoint, value);
        }

        /// <summary>
        /// Remove the specified value from the tree.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        public bool Remove(T value)
        {
            // See if we have that entry.
            if (Contains(value))
            {
                var entry = _values[value];
                // Get the node the entry would be in.
                int nodeX, nodeY, nodeSize;
                RemoveFromNode(FindNode(ref entry.Point, out nodeX, out nodeY, out nodeSize), entry);
            }
            return false;
        }

        /// <summary>
        /// Test whether this tree contains the specified value at the
        /// specified point.
        /// </summary>
        /// <param name="point">The point at which to look for.</param>
        /// <param name="value">The value to look for.</param>
        /// <returns><c>true</c> if the tree contains the value at the
        /// specified point.</returns>
        public bool Contains(ref Vector2 point, T value)
        {
            return _values.ContainsKey(value) && _values[value].Point.Equals(point);
        }

        /// <summary>
        /// Test whether this tree contains the specified value.
        /// </summary>
        /// <param name="value">The value to look for.</param>
        /// <returns><c>true</c> if the tree contains the value at the
        /// specified point.</returns>
        public bool Contains(T value)
        {
            return _values.ContainsKey(value);
        }

        /// <summary>
        /// Removes all entries from this tree.
        /// </summary>
        public void Clear()
        {
            _root = new Node();
            _entries = null;
            _values.Clear();
        }

        /// <summary>
        /// Perform a range query on this tree. This will return all entries
        /// in the tree that are in the specified range to the specified point,
        /// using a euclidean distance.
        /// </summary>
        /// <param name="point">The query point near which to get entries.</param>
        /// <param name="range">The maximum distance an entry may be away
        /// from the query point to be returned.</param>
        /// <param name="list">The list to put the results into, or null in
        /// which case a new list will be created and returned.</param>
        /// <returns>All objects in the neighborhood of the query point.</returns>
        public ICollection<T> RangeQuery(ref Vector2 point, float range, ICollection<T> list = null)
        {
            var result = list ?? new HashSet<T>();

            // Recurse through the tree, starting at the root node, to find
            // nodes intersecting with the range query.
            Accumulate(_bounds.X, _bounds.Y, _bounds.Width, _root, ref point, range, result);

            return result;
        }

        /// <summary>
        /// Perform a range query on this tree. This will return all entries
        /// in the tree that are in the specified range to the specified point,
        /// using a euclidean distance.
        /// </summary>
        /// <param name="point">The query point near which to get entries.</param>
        /// <param name="range">The maximum distance an entry may be away
        /// from the query point to be returned.</param>
        /// <returns></returns>
        public ICollection<T> RangeQuery(Vector2 point, float range)
        {
            return RangeQuery(ref point, range);
        }

        /// <summary>
        /// Perform a range query on this tree. This will return all entries
        /// in the tree that are in contained the specified rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="list">The list to put the results into, or null in
        /// which case a new list will be created and returned.</param>
        /// <returns>All objects in the query rectangle.</returns>
        public ICollection<T> RangeQuery(ref Microsoft.Xna.Framework.Rectangle rectangle, ICollection<T> list = null)
        {
            var result = list ?? new List<T>();

            // Recurse through the tree, starting at the root node, to find
            // nodes intersecting with the range query.
            Accumulate(_bounds.X, _bounds.Y, _bounds.Width, _root, ref rectangle, result);

            return result;
        }

        /// <summary>
        /// Perform a range query on this tree. This will return all entries
        /// in the tree that are in contained the specified rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <returns>All objects in the query rectangle.</returns>
        public ICollection<T> RangeQuery(Microsoft.Xna.Framework.Rectangle rectangle)
        {
            return RangeQuery(ref rectangle);
        }

        #endregion

        #region Internal functionality

        /// <summary>
        /// Find a node at the given query point. If possible, this will return
        /// a leaf node. If there is no leaf node at the query point, it will
        /// return the inner node that would contain the leaf node that would
        /// hold that point.
        /// </summary>
        /// <param name="point">The point to get the leaf node for.</param>
        /// <param name="nodeX">Will be the x position of the node.</param>
        /// <param name="nodeY">Will be the y position of the node.</param>
        /// <param name="nodeSize">Will be the size of the node.</param>
        /// <returns>The node for the specified query point.</returns>
        private Node FindNode(ref Vector2 point, out int nodeX, out int nodeY, out int nodeSize)
        {
            var node = _root;
            nodeX = _bounds.X;
            nodeY = _bounds.Y;
            nodeSize = _bounds.Width;

            while (!node.IsLeaf)
            {
                // Get current child size.
                var childSize = nodeSize >> 1;

                // Into which child node would we descend?
                var cell = ComputeCell(nodeX, nodeY, childSize, ref point);

                // Do we have to create that child?
                if (node.Children[cell] != null)
                {
                    // Yes, descend into that node.
                    node = node.Children[cell];
                    nodeX += (((cell & 1) == 0) ? 0 : childSize);
                    nodeY += (((cell & 2) == 0) ? 0 : childSize);
                    nodeSize = childSize;
                }
                else
                {
                    // No. Return the current inner node instead.
                    return node;
                }
            }

            return node;
        }

        #region Restructuring

        /// <summary>
        /// Removes an entry from a node.
        /// </summary>
        /// <param name="removalNode">The node to remove from.</param>
        /// <param name="entry">The entry to remove.</param>
        private void RemoveFromNode(Node removalNode, Entry entry)
        {
            // Found it! If it's our low or high state adjust them
            // accordingly.
            var node = removalNode;
            while (node != null)
            {
                if (node.LowEntry == node.HighEntry)
                {
                    // Only one left, clear the node.
                    node.LowEntry = null;
                    node.HighEntry = null;
                }
                else if (node.LowEntry == entry)
                {
                    // It's the low node, adjust accordingly.
                    node.LowEntry = node.LowEntry.Next;
                }
                else if (node.HighEntry == entry)
                {
                    // It's the high node, adjust accordingly.
                    node.HighEntry = node.HighEntry.Previous;
                }

                // Adjust entry count.
                --node.EntryCount;

                // Continue checking in our parent.
                node = node.Parent;
            }

            // Remove the entry from the list of entries.
            entry.Remove();
            _values.Remove(entry.Value);
            if (entry == _entries)
            {
                _entries = null;
            }

            // See if we can compact the node's parent. This has to
            // be done in a post-processing step because the entry
            // has to be removed first (to update entry counts).
            CleanNode(removalNode);
        }

        /// <summary>
        /// Try to clean up a node and its parents. This walks the tree towards
        /// the root, removing child nodes where possible.
        /// </summary>
        /// <param name="node">The node to start cleaning at.</param>
        private void CleanNode(Node node)
        {
            // Do nothing for leaf nodes or when passing the root node.
            if (node == null)
            {
                return;
            }

            // Check if child nodes are unnecessary for this node.
            if (node.EntryCount <= _maxEntriesPerNode)
            {
                // We can prune the child nodes.
                node.Children[0] = null;
                node.Children[1] = null;
                node.Children[2] = null;
                node.Children[3] = null;
            }
            else
            {
                // Check if we have empty child nodes.
                for (var i = 0; i < 4; i++)
                {
                    // If so, remove them.
                    if (node.Children[i] != null && node.Children[i].EntryCount == 0)
                    {
                        node.Children[i] = null;
                    }
                }
            }

            // Check parent.
            CleanNode(node.Parent);
        }

        /// <summary>
        /// Ensures the tree can contain the given point.
        /// </summary>
        /// <param name="point">The point to ensure tree size for.</param>
        private void EnsureCapacity(ref Vector2 point)
        {
            if (!_bounds.Contains((int)point.X, (int)point.Y))
            {
                // Point is outside our current tree bounds. Expand it to allow
                // fitting in the new point.
                var neededSizeX = GetNextHighestPowerOfTwo((uint)Math.Abs(point.X));
                var neededSizeY = GetNextHighestPowerOfTwo((uint)Math.Abs(point.Y));
                var neededSize = (int)Math.Max(neededSizeX, neededSizeY);

                // Avoid possible issues when adding the first point at (0, 0).
                if (neededSize == 0)
                {
                    neededSize = _minBucketSize;
                }

                // Already got a root node. Push as many levels above it as
                // we need for the new entry. This ensures there will be a
                // node at the point we're trying to insert.
                while (_bounds.X > -neededSize)
                {
                    InsertLevel();
                }
            }
        }

        /// <summary>
        /// Inserts a new level on top of the root node, making it the new root
        /// node. Will reattach all of the root node's child nodes to the
        /// appropriate child nodes of the new root node.
        /// </summary>
        private void InsertLevel()
        {
            // Create the new root node.
            var node = new Node {EntryCount = _root.EntryCount, LowEntry = _root.LowEntry, HighEntry = _root.HighEntry};

            // Copy list start and end (which will just be the first and last
            // elements in the list of all entries).

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
            for (var childNumber = 0; childNumber < 4; childNumber++)
            {
                // Get the old child node that will be attached to the new node.
                var child = _root.Children[childNumber];

                // Skip unset ones.
                if (child == null)
                {
                    continue;
                }

                // Allocate new node.
                var wrapper = node.Children[childNumber] = new Node {Parent = node};

                // Set opposing corner inside that node to old node in that corner.
                // The (3 - x) will always yield the diagonally opposite cell to x.
                wrapper.Children[3 - childNumber] = child;
                child.Parent = wrapper;

                // Copy values from child node (it's the only one, so they are the same).
                wrapper.EntryCount = child.EntryCount;
                wrapper.LowEntry = child.LowEntry;
                wrapper.HighEntry = child.HighEntry;
            }

            // Set the new root node, adjust the overall tree bounds.
            _root = node;
            _bounds.X = _bounds.X << 1;
            _bounds.Y = _bounds.Y << 1;
            _bounds.Width = _bounds.Width << 1;
            _bounds.Height = _bounds.Height << 1;
        }

        /// <summary>
        /// Check if a node needs to be split, and split it if allowed to.
        /// </summary>
        /// <param name="x">The x position of the node.</param>
        /// <param name="y">The y position of the node.</param>
        /// <param name="size">The size of the node.</param>
        /// <param name="node">The actual node to split.</param>
        private void SplitNodeIfNecessary(int x, int y, int size, Node node)
        {
            // Should we split?
            if (!node.IsLeaf || node.EntryCount <= _maxEntriesPerNode || size <= _minBucketSize)
            {
                // No.
                return;
            }

            // Precompute child size, used several times.
            var childSize = size >> 1;

            // Used to keep track of the new high entry due to possible
            // resorting.
            Entry highEntry = null;

            // Check each entry to which new cell it'll belong.
            for (var entry = node.LowEntry; entry != null && entry != node.HighEntry.Next; entry = entry.Next)
            {
                _reusableEntryList.Add(entry);
            }
            foreach (var entry in _reusableEntryList)
            {
                // In which child node would we insert?
                int cell = ComputeCell(x, y, childSize, ref entry.Point);

                // Do we have to create that child?
                if (node.Children[cell] == null)
                {
                    // Yes.
                    node.Children[cell] = new Node {Parent = node, LowEntry = entry};

                    // No shuffling, mark this as the last entry.
                    highEntry = entry;
                }
                else if (node.Children[cell].HighEntry.Next != entry)
                {
                    // Out of order. Sort the sublist to represent in up to
                    // four intervals the entries of the child nodes.
                    entry.Remove();
                    entry.InsertAfter(node.Children[cell].HighEntry);
                }
                else
                {
                    // No shuffling, mark this as the last entry.
                    highEntry = entry;
                }

                // Either way, one more node.
                ++node.Children[cell].EntryCount;

                // List is now in order, so we set the highest to this entry.
                node.Children[cell].HighEntry = entry;
            }
            _reusableEntryList.Clear();

            // Adjust parent high node if it changed due to sorting.
            if (node.HighEntry != highEntry)
            {
                // Need to adjust parents who had our high entry (including
                // the node that was split).
                var oldHighEntry = node.HighEntry;
                var parent = node;
                while (parent != null)
                {
                    if (parent.HighEntry == oldHighEntry)
                    {
                        parent.HighEntry = highEntry;
                        parent = parent.Parent;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Do this recursively if the split resulted in another bucket that
            // is too large.
            for (var i = 0; i < 4; ++i)
            {
                if (node.Children[i] != null)
                {
                    SplitNodeIfNecessary(
                        x + (((i & 1) == 0) ? 0 : childSize),
                        y + (((i & 2) == 0) ? 0 : childSize),
                        childSize, node.Children[i]);
                }
            }
        }

        #endregion

        #endregion

        #region Utility methods

        /// <summary>
        /// Gets the next higher power of two for a given number. Used when
        /// inserting new nodes into the tree, to check if our bounds suffice.
        /// </summary>
        /// <remarks>
        /// If a power of two is given, the next higher one will be returned,
        /// not the given one. When zero is given, zero is returned.
        /// </remarks>
        /// <see cref="http://jeffreystedfast.blogspot.com/2008/06/calculating-nearest-power-of-2.html"/>
        /// <param name="i">The number to get the next higher power of two
        /// for.</param>
        /// <returns>The next higher power of two.</returns>
        private static uint GetNextHighestPowerOfTwo(uint i)
        {
            uint j, k;
            if ((j = i & 0xFFFF0000) == 0) j = i;
            if ((k = j & 0xFF00FF00) == 0) k = j;
            if ((j = k & 0xF0F0F0F0) == 0) j = k;
            if ((k = j & 0xCCCCCCCC) == 0) k = j;
            if ((j = k & 0xAAAAAAAA) == 0) j = k;
            return j << 1;
        }

        /// <summary>
        /// Computes the cell of a node with the specified position and child
        /// node size the specified point falls into.
        /// </summary>
        /// <param name="x">The x coordinate of the node.</param>
        /// <param name="y">The y coordinate of the node.</param>
        /// <param name="childSize">The size of the nodes child nodes.</param>
        /// <param name="point">The point to check for.</param>
        /// <returns>The cell number the point falls into.</returns>
        private static int ComputeCell(int x, int y, int childSize, ref Vector2 point)
        {
            var cell = 0;
            if ((int)point.X >= x + childSize)
            {
                // Right half.
                cell |= 1;
            }
            if ((int)point.Y >= y + childSize)
            {
                // Lower half.
                cell |= 2;
            }
            return cell;
        }

        /// <summary>
        /// Accumulate all entries in range of a circular range query to the
        /// given list. This recurses the tree down inner nodes that intersect
        /// the query, until it finds a leaf node. Then adds all entries in the
        /// leaf that are in range.
        /// </summary>
        /// <param name="x">The x position of the current node.</param>
        /// <param name="y">The y position of the current node.</param>
        /// <param name="size">The size of the current node.</param>
        /// <param name="node">The current node.</param>
        /// <param name="point">The query point.</param>
        /// <param name="range">The query range.</param>
        /// <param name="list">The result list.</param>
        private static void Accumulate(int x, int y, int size, Node node, ref Vector2 point, float range, ICollection<T> list)
        {
            var intersectionType = ComputeIntersection(ref point, range, x, y, size);
            if (intersectionType == IntersectionType.Contained)
            {
                // Box completely contained in query, return all points in it,
                // no need to recurse further.
                for (var entry = node.LowEntry; entry != null && entry != node.HighEntry.Next; entry = entry.Next)
                {
                    // No need for a range check at this point.
                    list.Add(entry.Value);
                }
            }
            else if (intersectionType == IntersectionType.Overlapping)
            {
                // Node intersects with the query.
                if (node.IsLeaf)
                {
                    // Add all entries in this node that are in range.
                    var rangeSquared = range * range;
                    for (var entry = node.LowEntry;
                         entry != null && entry != node.HighEntry.Next;
                         entry = entry.Next)
                    {
                        var distanceX = point.X - entry.Point.X;
                        var distanceY = point.Y - entry.Point.Y;
                        if ((distanceX * distanceX + distanceY * distanceY) < rangeSquared)
                        {
                            list.Add(entry.Value);
                        }
                    }
                }
                else
                {
                    // Recurse into child nodes.
                    var childSize = size >> 1;

                    // Unrolled loop.
                    if (node.Children[0] != null)
                    {
                        Accumulate(x, y, childSize, node.Children[0], ref point, range, list);
                    }
                    if (node.Children[1] != null)
                    {
                        Accumulate(x + childSize, y, childSize, node.Children[1], ref point, range, list);
                    }
                    if (node.Children[2] != null)
                    {
                        Accumulate(x, y + childSize, childSize, node.Children[2], ref point, range, list);
                    }
                    if (node.Children[3] != null)
                    {
                        Accumulate(x + childSize, y + childSize, childSize, node.Children[3], ref point, range, list);
                    }
                }
            }
            // else: No intersection.
        }

        /// <summary>
        /// Accumulate all entries in range of a rectangular range query to the
        /// given list. This recurses the tree down inner nodes that intersect
        /// the query, until it finds a leaf node. Then adds all entries in the
        /// leaf that are in range.
        /// </summary>
        /// <param name="x">The x position of the current node.</param>
        /// <param name="y">The y position of the current node.</param>
        /// <param name="size">The size of the current node.</param>
        /// <param name="node">The current node.</param>
        /// <param name="query">The query rectangle.</param>
        /// <param name="list">The result list.</param>
        private static void Accumulate(int x, int y, int size, Node node, ref Microsoft.Xna.Framework.Rectangle query, ICollection<T> list)
        {
            var intersectionType = ComputeIntersection(ref query, x, y, size);
            if (intersectionType == IntersectionType.Contained)
            {
                // Box completely contained in query, return all points in it,
                // no need to recurse further.
                for (var entry = node.LowEntry; entry != null && entry != node.HighEntry.Next; entry = entry.Next)
                {
                    // No need for a range check at this point.
                    list.Add(entry.Value);
                }
            }
            else if (intersectionType == IntersectionType.Overlapping)
            {
                // Node intersects with the query.
                if (node.IsLeaf)
                {
                    // Add all entries in this node that are in range.
                    for (var entry = node.LowEntry; entry != null && entry != node.HighEntry.Next; entry = entry.Next)
                    {
                        if (query.Contains((int)entry.Point.X, (int)entry.Point.Y))
                        {
                            list.Add(entry.Value);
                        }
                    }
                }
                else
                {
                    // Recurse into child nodes.
                    var childSize = size >> 1;
                    for (var i = 0; i < 4; i++)
                    {
                        if (node.Children[i] == null)
                        {
                            continue;
                        }

                        var childX = x + (((i & 1) == 0) ? 0 : childSize);
                        var childY = y + (((i & 2) == 0) ? 0 : childSize);
                        Accumulate(childX, childY, childSize, node.Children[i], ref query, list);
                    }
                }
            }
            // else: No intersection.
        }

        /// <summary>
        /// Possible intersection types of geometric shapes.
        /// </summary>
        private enum IntersectionType
        {
            /// <summary>
            /// The shapes are cleanly separated from each other.
            /// </summary>
            Separated,

            /// <summary>
            /// The two shapes are overlapping each other.
            /// </summary>
            Overlapping,

            /// <summary>
            /// One shape is completely contained within the other.
            /// </summary>
            Contained
        }

        /// <summary>
        /// Circle / Box intersection test.
        /// </summary>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="x">The x position of the box.</param>
        /// <param name="y">The y position of the box.</param>
        /// <param name="size">The size of the box.</param>
        /// <returns>How the two intersect.</returns>
        private static IntersectionType ComputeIntersection(ref Vector2 center, float radius, int x, int y, int size)
        {
            // Check for axis aligned separation.
            if (x + size < center.X - radius ||
                y + size < center.Y - radius ||
                x > center.X + radius ||
                y > center.Y + radius)
            {
                return IntersectionType.Separated;
            }

            // Check for unaligned separation.
            var closest = center;
            if (center.X < x)
            {
                closest.X = x;
            }
            else if (center.X > x + size)
            {
                closest.X = x + size;
            }
            if (center.Y < y)
            {
                closest.Y = y;
            }
            else if (center.Y > y + size)
            {
                closest.Y = y + size;
            }
            var distanceX = closest.X - center.X;
            var distanceY = closest.Y - center.Y;
            if ((distanceX * distanceX + distanceY * distanceY) > radius * radius)
            {
                return IntersectionType.Separated;
            }

            // At least intersection, check furthest point to check if the
            // box is contained within the circle.
            distanceX = Math.Max(center.X - x, x + size - center.X);
            distanceY = Math.Max(center.Y - y, y + size - center.Y);
            var outside = (distanceX * distanceX + distanceY * distanceY) > radius * radius;
            return outside ? IntersectionType.Overlapping : IntersectionType.Contained;
        }

        /// <summary>
        /// Box / Box intersection test.
        /// </summary>
        /// <param name="rectangle">The first box.</param>
        /// <param name="x">The x position of the second box.</param>
        /// <param name="y">The y position of the second box.</param>
        /// <param name="size">The size of the second box.</param>
        /// <returns>How the two intersect.</returns>
        private static IntersectionType ComputeIntersection(ref Microsoft.Xna.Framework.Rectangle rectangle, int x,
                                                            int y, int size)
        {
            Microsoft.Xna.Framework.Rectangle other;
            other.X = x;
            other.Y = y;
            other.Width = size;
            other.Height = size;

            bool result;
            rectangle.Intersects(ref other, out result);
            if (!result)
            {
                return IntersectionType.Separated;
            }
            rectangle.Contains(ref other, out result);
            return result ? IntersectionType.Contained : IntersectionType.Overlapping;
        }

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
                get
                {
                    return (Children[0] == null) && (Children[1] == null) && (Children[2] == null) &&
                           (Children[3] == null);
                }
            }

            #endregion

            #region Fields

            /// <summary>
            /// The parent of this node.
            /// </summary>
            public Node Parent;

            /// <summary>
            /// The low entry in the entity list (low end of the interval).
            /// </summary>
            public Entry LowEntry;

            /// <summary>
            /// The high entry in the entity list (high end of the interval).
            /// </summary>
            public Entry HighEntry;

            /// <summary>
            /// Number of entries in this node.
            /// </summary>
            public int EntryCount;

            /// <summary>
            /// The children this node points to.
            /// </summary>
            public readonly Node[] Children = new Node[4];

            #endregion
        }

        /// <summary>
        /// A single entry in the tree, uniquely identified by its position
        /// and value.
        /// </summary>
        [DebuggerDisplay("Point = {Point}, Value = {Value}")]
        private sealed class Entry
        {
            #region Fields

            /// <summary>
            /// Next entry in the linked list.
            /// </summary>
            public Entry Next;

            /// <summary>
            /// Previous entry in the linked list.
            /// </summary>
            public Entry Previous;

            /// <summary>
            /// The point at which the entry is stored.
            /// </summary>
            public Vector2 Point;

            /// <summary>
            /// The value stored in this entry.
            /// </summary>
            public T Value;

            #endregion

            #region Methods

            /// <summary>
            /// Remove this entry from the linked list.
            /// </summary>
            public void Remove()
            {
                if (Previous != null)
                {
                    Previous.Next = Next;
                }
                if (Next != null)
                {
                    Next.Previous = Previous;
                }

                Next = null;
                Previous = null;
            }

            /// <summary>
            /// Insert this node into the linked list, after the specified entry.
            /// </summary>
            /// <param name="entry">The entry to insert after.</param>
            public void InsertAfter(Entry entry)
            {
                var insertAfter = entry;
                var insertBefore = insertAfter.Next;

                insertAfter.Next = this;
                if (insertBefore != null)
                {
                    insertBefore.Previous = this;
                }

                Previous = insertAfter;
                Next = insertBefore;
            }

            #endregion
        }

        #endregion

        #region Enumerable

        /// <summary>
        /// Get an enumerator over the values in this tree.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _values.Keys.GetEnumerator();
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

        #region Rendering

        /// <summary>
        /// Renders a graphical representation of this tree's cells using the
        /// specified shape renderer.
        /// </summary>
        /// <param name="shape">The shape renderer to paint with.</param>
        /// <param name="translation">The translation to apply to all draw
        ///   operation.</param>
        public void Draw(AbstractShape shape, Vector2 translation)
        {
            DrawNode(_root, translation.X, translation.Y, _bounds.Width, shape);
        }

        /// <summary>
        /// Renders a single note into a sprite batch, and recursively render
        /// its children.
        /// </summary>
        private void DrawNode(Node node, float centerX, float centerY, int size, AbstractShape shape)
        {
            // Abort if there is no node here.
            if (node == null)
            {
                return;
            }

            shape.SetCenter(centerX, centerY);
            shape.SetSize(size - 1);
            shape.Draw();

            // Check for child nodes.
            for (var i = 0; i < 4; ++i)
            {
                var childX = centerX + (((i & 1) == 0) ? -(size >> 2) : (size >> 2));
                var childY = centerY + (((i & 2) == 0) ? -(size >> 2) : (size >> 2));
                DrawNode(node.Children[i], childX, childY, size >> 1, shape);
            }
        }

        /// <summary>
        /// Tests if a value is in the specified interval.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="low">The lower bound of the interval.</param>
        /// <param name="high">The upper bound of the intervale.</param>
        /// <returns></returns>
        private static bool IsInInterval(float value, float low, float high)
        {
            return value >= low && value <= high;
        }

        #endregion
    }
}
