using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    public sealed class QuadTree<T> : IEnumerable<T>
    {
        #region Properties

        /// <summary>
        /// The number of values stored in this tree.
        /// </summary>
        public int Count { get { return _entries.Count; } }

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
        private Rectangle _bounds = Rectangle.Empty;

        /// <summary>
        /// The root node of the tree.
        /// </summary>
        private Node _root;

        /// <summary>
        /// A list of all entries in the tree. The linked list allows simply
        /// adding an entry to a leaf node, keeping the pointers to the segment
        /// of an inner intact.
        /// </summary>
        private LinkedList<Entry> _entries = new LinkedList<Entry>();

        /// <summary>
        /// Reused list when splitting nodes and so on (not re-allocating each
        /// run).
        /// </summary>
        private List<LinkedListNode<Entry>> _reusableEntryList;

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

            _root = GetNode(null);

            _reusableEntryList = new List<LinkedListNode<Entry>>(_maxEntriesPerNode + 1);
        }

        #endregion

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
            // Create the entry to add.
            var entry = GetListNode(point, value);

            // Handle dynamic growth.
            EnsureCapacity(ref point);

            // Get the node to insert in.
            int nodeX, nodeY, nodeSize;
            var insertionNode = FindNode(ref point, out nodeX, out nodeY, out nodeSize);

            // If it's not a leaf node, create the leaf node for the new entry.
            // Also get the node in the linked list to insert after.
            LinkedListNode<Entry> insertAfter;
            if (!insertionNode.IsLeaf)
            {
                var cell = ComputeCell(nodeX, nodeY, nodeSize >> 1, ref point);
                insertionNode.Children[cell] = GetNode(insertionNode);
                insertionNode = insertionNode.Children[cell];
                insertAfter = insertionNode.Parent.HighEntry;
            }
            else
            {
                // Got a leaf, check if we already have that point.
                for (var existingEntry = insertionNode.LowEntry; existingEntry != null && existingEntry != insertionNode.HighEntry.Next; existingEntry = existingEntry.Next)
                {
                    if (entry.Equals(existingEntry))
                    {
                        throw new ArgumentException("Entry is already in the tree at the specified point.", "value");
                    }
                }
                // Not yet in the tree.
                insertAfter = insertionNode.LowEntry;
            }

            // Add the data, get the newly created list entry.
            if (insertAfter != null)
            {
                _entries.AddAfter(insertAfter, entry);
            }
            else
            {
                _entries.AddFirst(entry);
            }

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
        /// Update a single entry by changing its position.
        /// </summary>
        /// <param name="oldPoint">The old position of the entry.</param>
        /// <param name="newPoint">The new position of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <exception cref="ArgumentException">If there is no such value in
        /// the tree at the specified old position.</exception>
        public void Update(ref Vector2 oldPoint, ref Vector2 newPoint, T value)
        {
            // Handle dynamic growth.
            EnsureCapacity(ref newPoint);

            // Get the node the entry would be in.
            int nodeX, nodeY, nodeSize;
            var oldNode = FindNode(ref oldPoint, out nodeX, out nodeY, out nodeSize);
            
            // Is the node a leaf node? If not we don't have that entry.
            if (oldNode.IsLeaf)
            {
                // Check if we have that entry.
                for (var entry = oldNode.LowEntry; entry != null && entry != oldNode.HighEntry.Next; entry = entry.Next)
                {
                    if (entry.Value.Point.Equals(oldPoint) &&
                        entry.Value.Value.Equals(value))
                    {
                        // Found it! See if the new point falls into the same
                        // node, otherwise re-insert.
                        var newNode = FindNode(ref newPoint, out nodeX, out nodeY, out nodeSize);
                        if (oldNode == newNode)
                        {
                            // Same node, just update the entry.
                            entry.Value.Point = newPoint;
                        }
                        else
                        {
                            // Different node, re-insert.
                            Remove(ref oldPoint, value);
                            Add(ref newPoint, value);
                        }

                        // Success, don't throw.
                        return;
                    }
                }
            }
            throw new ArgumentException("Entry not in the tree at the specified point.", "value");
        }
        
        /// <summary>
        /// Update a single entry by changing its position.
        /// </summary>
        /// <param name="oldPoint">The old position of the entry.</param>
        /// <param name="newPoint">The new position of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <exception cref="ArgumentException">If there is no such value in
        /// the tree at the specified old position.</exception>
        public void Update(Vector2 oldPoint, Vector2 newPoint, T value)
        {
            Update(ref oldPoint, ref newPoint, value);
        }

        /// <summary>
        /// Remove the specified value at the specified point from the tree.
        /// </summary>
        /// <param name="point">The position to remove the value at.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns><c>true</c> if the specified pair of point and value was
        /// in the tree, <c>false</c> otherwise.</returns>
        public bool Remove(ref Vector2 point, T value)
        {
            // Get the node the entry would be in.
            int nodeX, nodeY, nodeSize;
            var removalNode = FindNode(ref point, out nodeX, out nodeY, out nodeSize);

            // Is the node a leaf node? If not we don't have that entry.
            if (removalNode.IsLeaf)
            {
                // Check if we have that entry.
                for (var entry = removalNode.LowEntry; entry != null && entry != removalNode.HighEntry.Next; entry = entry.Next)
                {
                    if (entry.Value.Point.Equals(point) &&
                        entry.Value.Value.Equals(value))
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
                        _entries.Remove(entry);
                        FreeListNode(entry);

                        // See if we can compact the node's parent. This has to
                        // be done in a post-processing step because the entry
                        // has to be removed first (to update entry counts).
                        CleanNode(removalNode);

                        return true;
                    }
                }
            }
            return false;
        }
        
        /// <summary>
        /// Remove the specified value at the specified point from the tree.
        /// </summary>
        /// <param name="point">The position to remove the value at.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns><c>true</c> if the specified pair of point and value was
        /// in the tree, <c>false</c> otherwise.</returns>
        public bool Remove(Vector2 point, T value)
        {
            return Remove(ref point, value);
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
            // Get the node the entry would be in.
            int nodeX, nodeY, nodeSize;
            var node = FindNode(ref point, out nodeX, out nodeY, out nodeSize);
            
            // Is the node a leaf node? If not we don't have that entry.
            if (node.IsLeaf)
            {
                // Check if we have that entry.
                for (var entry = node.LowEntry; entry != null && entry != node.HighEntry.Next; entry = entry.Next)
                {
                    if (entry.Value.Point.Equals(point) &&
                        entry.Value.Value.Equals(value))
                    {
                        // Got it :)
                        return true;
                    }
                }
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
        public bool Contains(Vector2 point, T value)
        {
            return Contains(ref point, value);
        }

        /// <summary>
        /// Removes all entries from this tree.
        /// </summary>
        public void Clear()
        {
            // Free all tree nodes.
            FreeBranch(_root);
            _root = GetNode(null);

            // Free all list nodes.
            for (var node = _entries.First; node != null; node = node.Next)
            {
                FreeListNode(node);
            }
            _entries.Clear();
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
        public List<T> RangeQuery(ref Vector2 point, float range, List<T> list = null)
        {
            var result = list ?? new List<T>();

            // Recurse through the tree, starting at the root node, to find
            // nodes intersecting with the range query.
            Accumulate(_bounds.X, _bounds.Y, _bounds.Width, _root,
                ref point, range, result);

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
        public List<T> RangeQuery(Vector2 point, float range)
        {
            return RangeQuery(ref point, range);
        }

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
                FreeNode(node.Children[0]);
                FreeNode(node.Children[1]);
                FreeNode(node.Children[2]);
                FreeNode(node.Children[3]);
                node.Children[0] = null;
                node.Children[1] = null;
                node.Children[2] = null;
                node.Children[3] = null;
            }
            else
            {
                // Check if we have empty child nodes.
                for (int i = 0; i < 4; i++)
                {
                    // If so, remove them.
                    if (node.Children[i] != null && node.Children[i].EntryCount == 0)
                    {
                        FreeNode(node.Children[i]);
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
                uint neededSizeX = GetNextHighestPowerOfTwo(
                    (uint)System.Math.Abs(point.X));
                uint neededSizeY = GetNextHighestPowerOfTwo(
                    (uint)System.Math.Abs(point.Y));
                int neededSize = (int)System.Math.Max(neededSizeX, neededSizeY);

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
            var node = GetNode(null);

            // Copy list start and end (which will just be the first and last
            // elements in the list of all entries).
            node.EntryCount = _root.EntryCount;
            node.LowEntry = _root.LowEntry;
            node.HighEntry = _root.HighEntry;

            // Check top left sector, add it as top left sectors lower right
            // node, if it is set.
            if (_root.Children[0] != null)
            {
                node.Children[0] = GetNode(node);
                node.Children[0].Children[3] = _root.Children[0];
                _root.Children[0].Parent = node.Children[0];

                node.Children[0].EntryCount = _root.Children[0].EntryCount;
                node.Children[0].LowEntry = _root.Children[0].LowEntry;
                node.Children[0].HighEntry = _root.Children[0].HighEntry;
            }

            // Check top right sector, add it as top right sectors lower left
            // node, if it is set.
            if (_root.Children[1] != null)
            {
                node.Children[1] = GetNode(node);
                node.Children[1].Children[2] = _root.Children[1];
                _root.Children[1].Parent = node.Children[1];

                node.Children[1].EntryCount = _root.Children[1].EntryCount;
                node.Children[1].LowEntry = _root.Children[1].LowEntry;
                node.Children[1].HighEntry = _root.Children[1].HighEntry;
            }

            // Check bottom left sector, add it as bottom left sectors top
            // right node, if it is set.
            if (_root.Children[2] != null)
            {
                node.Children[2] = GetNode(node);
                node.Children[2].Children[1] = _root.Children[2];
                _root.Children[2].Parent = node.Children[2];

                node.Children[2].EntryCount = _root.Children[2].EntryCount;
                node.Children[2].LowEntry = _root.Children[2].LowEntry;
                node.Children[2].HighEntry = _root.Children[2].HighEntry;
            }

            // Check bottom right sector, add it as bottom right sectors top
            // left node, if it is set.
            if (_root.Children[3] != null)
            {
                node.Children[3] = GetNode(node);
                node.Children[3].Children[0] = _root.Children[3];
                _root.Children[3].Parent = node.Children[3];

                node.Children[3].EntryCount = _root.Children[3].EntryCount;
                node.Children[3].LowEntry = _root.Children[3].LowEntry;
                node.Children[3].HighEntry = _root.Children[3].HighEntry;
            }

            // Kill of the old root.
            FreeNode(_root);

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
            LinkedListNode<Entry> highEntry = null;

            // Check each entry to which new cell it'll belong.
            for (var entry = node.LowEntry; entry != null && entry != node.HighEntry.Next; entry = entry.Next)
            {
                _reusableEntryList.Add(entry);
            }
            foreach (var entry in _reusableEntryList)
            {
                // In which child node would we insert?
                int cell = ComputeCell(x, y, childSize, ref entry.Value.Point);

                // Do we have to create that child?
                if (node.Children[cell] == null)
                {
                    // Yes.
                    node.Children[cell] = GetNode(node);
                    node.Children[cell].LowEntry = entry;

                    // No shuffling, mark this as the last entry.
                    highEntry = entry;
                }
                else if (node.Children[cell].HighEntry.Next != entry)
                {
                    // Out of order. Sort the sublist to represent in up to
                    // four intervals the entries of the child nodes.
                    _entries.Remove(entry);
                    _entries.AddAfter(node.Children[cell].HighEntry, entry);
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
            for (int i = 0; i < 4; ++i)
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
        private static void Accumulate(int x, int y, int size, Node node, ref Vector2 point, float range, List<T> list)
        {
            var intersectionType = ComputeIntersection(ref point, range, x, y, size);
            if (intersectionType == IntersectionType.Contained)
            {
                // Box completely contained in query, return all points in it,
                // no need to recurse further.
                for (var entry = node.LowEntry; entry != null && entry != node.HighEntry.Next; entry = entry.Next)
                {
                    // No need for a range check at this point.
                    list.Add(entry.Value.Value);
                }
            }
            else if (intersectionType == IntersectionType.Overlapping)
            {
                // Node intersects with the query.
                if (node.IsLeaf)
                {
                    // Add all entries in this node that are in range.
                    float rangeSquared = range * range;
                    for (var entry = node.LowEntry; entry != null && entry != node.HighEntry.Next; entry = entry.Next)
                    {
                        float distanceX = point.X - entry.Value.Point.X;
                        float distanceY = point.Y - entry.Value.Point.Y;
                        if ((distanceX * distanceX + distanceY * distanceY) < rangeSquared)
                        {
                            list.Add(entry.Value.Value);
                        }
                    }
                }
                else
                {
                    // Recurse into child nodes.
                    var childSize = size >> 1;
                    for (int i = 0; i < 4; i++)
                    {
                        if (node.Children[i] != null)
                        {
                            Accumulate(
                                x + (((i & 1) == 0) ? 0 : childSize),
                                y + (((i & 2) == 0) ? 0 : childSize),
                                childSize, node.Children[i],
                                ref point, range, list);
                        }
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
        /// <param name="radiusSquared">The squared radius of the circle.</param>
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
            Vector2 closest = center;
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
            float distanceX = closest.X - center.X;
            float distanceY = closest.Y - center.Y;
            if ((distanceX * distanceX + distanceY * distanceY) > radius * radius)
            {
                return IntersectionType.Separated;
            }

            // At least intersection, check furthest point to check if the
            // box is contained within the circle.
            distanceX = System.Math.Max(center.X - x, x + size - center.X);
            distanceY = System.Math.Max(center.Y - y, y + size - center.Y);
            if ((distanceX * distanceX + distanceY * distanceY) > radius * radius)
            {
                return IntersectionType.Overlapping;
            }
            else
            {
                return IntersectionType.Contained;
            }
        }

        ///// <summary>
        ///// Circle / Box intersection test.
        ///// </summary>
        ///// <param name="center">The center of the circle.</param>
        ///// <param name="radiusSquared">The squared radius of the circle.</param>
        ///// <param name="x">The x position of the box.</param>
        ///// <param name="y">The y position of the box.</param>
        ///// <param name="size">The size of the box.</param>
        ///// <returns>Whether the two intersect or not.</returns>
        //private static bool Intersect(ref Vector2 center, float radiusSquared, int x, int y, int size)
        //{
        //    Vector2 closest = center;
        //    if (center.X < x)
        //    {
        //        closest.X = x;
        //    }
        //    else if (center.X > x + size)
        //    {
        //        closest.X = x + size;
        //    }
        //    if (center.Y < y)
        //    {
        //        closest.Y = y;
        //    }
        //    else if (center.Y > y + size)
        //    {
        //        closest.Y = y + size;
        //    }
        //    float distanceX = closest.X - center.X;
        //    float distanceY = closest.Y - center.Y;
        //    return (distanceX * distanceX + distanceY * distanceY) <= radiusSquared;
        //}

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
        [DebuggerDisplay("Count = {EntryCount}, Children = {GetChildrenCount()}")]
        private class Node
        {
            #region Properties
            
            /// <summary>
            /// Whether this node is a leaf node.
            /// </summary>
            public bool IsLeaf { get { return GetChildrenCount() == 0; } }

            #endregion

            #region Fields
            
            /// <summary>
            /// The parent of this node.
            /// </summary>
            public Node Parent;

            /// <summary>
            /// The low entry in the entity list (low end of the interval).
            /// </summary>
            public LinkedListNode<Entry> LowEntry;

            /// <summary>
            /// The high entry in the entity list (high end of the interval).
            /// </summary>
            public LinkedListNode<Entry> HighEntry;

            /// <summary>
            /// Number of entries in this node.
            /// </summary>
            public int EntryCount;

            /// <summary>
            /// The children this node points to.
            /// </summary>
            public readonly Node[] Children = new Node[4];

            #endregion

            #region Accessors
            
            /// <summary>
            /// Get the number of child nodes this node references.
            /// </summary>
            /// <returns>The number of child nodes of this node.</returns>
            public int GetChildrenCount()
            {
                return ((Children[0] == null) ? 0 : 1) +
                       ((Children[1] == null) ? 0 : 1) +
                       ((Children[2] == null) ? 0 : 1) +
                       ((Children[3] == null) ? 0 : 1);
            }

            #endregion
        }

        /// <summary>
        /// A single entry in the tree, uniquely identified by its position
        /// and value.
        /// </summary>
        [DebuggerDisplay("Point = {Point}, Value = {Value}")]
        private class Entry
        {
            #region Fields
            
            /// <summary>
            /// The point at which the entry is stored.
            /// </summary>
            public Vector2 Point;

            /// <summary>
            /// The value stored in this entry.
            /// </summary>
            public T Value;
            
            #endregion

            #region Overrides

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }

                Entry e = obj as Entry;
                if (e == null)
                {
                    return false;
                }

                return (Point.Equals(e.Point)) && (Value.Equals(e.Value));
            }

            public bool Equals(Entry e)
            {
                if (e == null)
                {
                    return false;
                }

                return (Point.Equals(e.Point)) && (Value.Equals(e.Value));
            }

            public override int GetHashCode()
            {
                return Point.GetHashCode() ^ Value.GetHashCode();
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
            foreach (var entry in _entries)
            {
                yield return entry.Value;
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

        #region Object pooling

        /// <summary>
        /// List of available nodes.
        /// </summary>
        private readonly List<Node> _nodePool = new List<Node>(512);

        /// <summary>
        /// List of available linked list nodes (entries).
        /// </summary>
        private readonly List<LinkedListNode<Entry>> _listNodePool = new List<LinkedListNode<Entry>>(1024);

        /// <summary>
        /// Allocate more nodes, if we ran out of them.
        /// </summary>
        private void AllocNodes()
        {
            for (int i = _nodePool.Count; i < _nodePool.Capacity; i++)
            {
                _nodePool.Add(new Node());
            }
        }

        /// <summary>
        /// Allocate more linked list nodes, if we ran out of them.
        /// </summary>
        private void AllocListNodes()
        {
            for (int i = _listNodePool.Count; i < _listNodePool.Capacity; i++)
            {
                _listNodePool.Add(new LinkedListNode<Entry>(new Entry()));
            }
        }

        /// <summary>
        /// Get a fresh node.
        /// </summary>
        /// <param name="parent">For constructor.</param>
        /// <returns>Initialized node.</returns>
        private Node GetNode(Node parent)
        {
            if (_nodePool.Count == 0)
            {
                AllocNodes();
            }
            var result = _nodePool[_nodePool.Count - 1];
            _nodePool.RemoveAt(_nodePool.Count - 1);
            result.Children[0] = null;
            result.Children[1] = null;
            result.Children[2] = null;
            result.Children[3] = null;
            result.EntryCount = 0;
            result.HighEntry = null;
            result.LowEntry = null;
            result.Parent = parent;
            return result;
        }

        /// <summary>
        /// Releases a node to be reused.
        /// </summary>
        /// <param name="node">The node to free.</param>
        private void FreeNode(Node node)
        {
            if (node != null)
            {
                _nodePool.Add(node);
            }
        }

        /// <summary>
        /// Releases a node and all its child nodes.
        /// </summary>
        /// <param name="node"></param>
        private void FreeBranch(Node node)
        {
            if (node != null)
            {
                // If its an inner node, free all children first.
                if (!node.IsLeaf)
                {
                    foreach (var child in node.Children)
                    {
                        FreeBranch(child);
                    }
                }

                // Then the node itself.
                FreeNode(node);
            }
        }

        /// <summary>
        /// Gets a fresh linked list node.
        /// </summary>
        /// <param name="position">For constructor of entry.</param>
        /// <param name="value">For constructor of entry.</param>
        /// <returns>A linked list node.</returns>
        private LinkedListNode<Entry> GetListNode(Vector2 position, T value)
        {
            if (_listNodePool.Count == 0)
            {
                AllocListNodes();
            }
            var result = _listNodePool[_listNodePool.Count - 1];
            _listNodePool.RemoveAt(_listNodePool.Count - 1);
            result.Value.Point = position;
            result.Value.Value = value;
            return result;
        }

        /// <summary>
        /// Releases a linked list node to be reused.
        /// </summary>
        /// <param name="node">The node to free.</param>
        private void FreeListNode(LinkedListNode<Entry> node)
        {
            if (node != null)
            {
                _listNodePool.Add(node);
            }
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Renders a graphical representation of this tree's cells using the
        /// specified shape renderer.
        /// </summary>
        /// <param name="rectangle">The shape renderer to paint with.</param>
        /// <param name="translation">The translation to apply to all draw
        /// operation.</param>
        public void Draw(Engine.Graphics.AbstractShape shape, Vector2 translation)
        {
            DrawNode(_root, translation.X, translation.Y, _bounds.Width, shape);
        }

        /// <summary>
        /// Renders a single note into a sprite batch, and recursively render
        /// its children.
        /// </summary>
        private void DrawNode(Node node, float centerX, float centerY, int size, Engine.Graphics.AbstractShape shape)
        {
            // Abort if there is no node here.
            if (node == null)
            {
                return;
            }

            // Render the bounds for this node, if they are visible.
            float left = centerX - (size >> 1);
            float top = centerY - (size >> 1);
            float right = left + size;
            float bottom = top + size;
            float width = shape.GraphicsDevice.Viewport.Width;
            float height = shape.GraphicsDevice.Viewport.Height;

            if (IsInInterval(left, 0, width) ||
                IsInInterval(right, 0, width) ||
                IsInInterval(top, 0, height) ||
                IsInInterval(bottom, 0, height))
            {
                shape.SetCenter(centerX, centerY);
                shape.SetSize(size - 1);
                shape.Draw();
            }

            // Check for child nodes.
            for (int i = 0; i < 4; ++i)
            {
                DrawNode(node.Children[i],
                            centerX + (((i & 1) == 0) ? -(size >> 2) : (size >> 2)),
                            centerY + (((i & 2) == 0) ? -(size >> 2) : (size >> 2)),
                            size >> 1,
                            shape);
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
