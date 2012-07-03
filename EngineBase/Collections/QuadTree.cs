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

            Clear();

            _reusableEntryList = new List<Entry>(_maxEntriesPerNode + 1);
        }

        #endregion

        #region Accessors

        public void Add(ref Rectangle bounds, T value)
        {
            if (Contains(value))
            {
                throw new ArgumentException("Entry is already in the tree at the specified point.", "value");
            }

            // Create the entry to add.
            var entry = new Entry { Bounds = bounds, Value = value };

            // Handle dynamic growth.
            EnsureCapacity(ref bounds);

            // Get the node to insert in.
            var nodeX = _bounds.X;
            var nodeY = _bounds.Y;
            var nodeSize = _bounds.Width;
            var node = FindNode(ref bounds, _root, ref nodeX, ref nodeY, ref nodeSize);
            AddToNode(node, nodeX, nodeY, nodeSize, entry);

            // Store the entry in the value lookup.
            _values.Add(entry.Value, entry);
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
            var bounds = new Rectangle {X = (int)point.X, Y = (int)point.Y};
            Add(ref bounds, value);
        }

        public bool Update(ref Rectangle newBounds, T value)
        {
            // Check if we have that entry, if not add it.
            if (!Contains(value))
            {
                return false;
            }

            Remove(value);
            Add(ref newBounds, value);
            return true;

            // Handle dynamic growth.
            EnsureCapacity(ref newBounds);

            // Get the old position.
            var entry = _values[value];

            // Get the node the entry would be in. Start searching at root level.
            var bounds = _bounds;
            var node = FindNode(ref entry.Bounds, _root, ref bounds.X, ref bounds.Y, ref bounds.Width);
            bounds.Height = bounds.Width;

            // Update the position in the entry.
            entry.Bounds = newBounds;
            if (!bounds.Contains(newBounds))
            {
                // Did not fit in node, search leaf node starting in current node.
                // Remove before looking for the node to insert in to avoid invalidating
                // the node we'd want to insert in (cascaded remove).
                RemoveFromNode(node, entry);

                // Find actual node to insert into, then add.
                bounds = _bounds;
                var newNode = FindNode(ref newBounds, _root, ref bounds.X, ref bounds.Y, ref bounds.Width);
                AddToNode(newNode, bounds.X, bounds.Y, bounds.Width, entry);
            }
            return true;

            // Find smallest parent cell we can re-insert it into.
            var insertionNode = node;
            while (!bounds.Contains(newBounds))
            {
                // Check how to shift the cell coordinates.
                if (insertionNode.Parent.Children[1] == insertionNode)
                {
                    // Was top right.
                    bounds.X -= bounds.Width;
                }
                else if (insertionNode.Parent.Children[2] == insertionNode)
                {
                    // Was bottom left.
                    bounds.Y -= bounds.Width;
                }
                else if (insertionNode.Parent.Children[3] == insertionNode)
                {
                    // Was bottom right.
                    bounds.X -= bounds.Width;
                    bounds.Y -= bounds.Height;
                }
                else
                {
                    // Was top left, nothing to do.
                    Debug.Assert(insertionNode.Parent.Children[0] == insertionNode);
                }

                // Adjust cell size.
                bounds.Width = bounds.Width << 1;
                bounds.Height = bounds.Height << 1;

                // Move on to parent node.
                insertionNode = insertionNode.Parent;
            }

            // Update the position in the entry.
            entry.Bounds = newBounds;

            // Check if we need to re-insert.
            if (node != insertionNode)
            {
                // Did not fit in node, search leaf node starting in current node.
                // Remove before looking for the node to insert in to avoid invalidating
                // the node we'd want to insert in (cascaded remove).
                RemoveFromNode(node, entry);

                // Find actual node to insert into, then add.
                var newNode = FindNode(ref newBounds, insertionNode, ref bounds.X, ref bounds.Y, ref bounds.Width);
                AddToNode(newNode, bounds.X, bounds.Y, bounds.Width, entry);
            }

            return true;
        }

        /// <summary>
        /// Update a single entry by changing its position. If the entry is not
        /// already in the tree, this will return <code>false</code>.
        /// </summary>
        /// <param name="newPoint">The new position of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <returns><code>true</code> if the update was successful.</returns>
        public bool Update(Vector2 newPoint, T value)
        {
            var bounds = new Rectangle { X = (int)newPoint.X, Y = (int)newPoint.Y };
            return Update(ref bounds, value);
        }

        /// <summary>
        /// Remove the specified value from the tree.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        public bool Remove(T value)
        {
            // See if we have that entry.
            if (!Contains(value))
            {
                return false;
            }

            var entry = _values[value];

            // Get the node the entry would be in.
            var nodeX = _bounds.X;
            var nodeY = _bounds.Y;
            var nodeSize = _bounds.Width;
            var node = FindNode(ref entry.Bounds, _root, ref nodeX, ref nodeY, ref nodeSize);
            RemoveFromNode(node, entry);

            // Remove the entry from the value lookup.
            _values.Remove(entry.Value);

            return false;
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
            _bounds.X = _bounds.Y = -_minBucketSize;
            _bounds.Width = _bounds.Height = _minBucketSize << 1;
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
        public void RangeQuery(Vector2 point, float range, ref ICollection<T> list)
        {
            // Recurse through the tree, starting at the root node, to find
            // nodes intersecting with the range query.
            Accumulate(_root, ref _bounds, ref point, range, ref list);
        }

        /// <summary>
        /// Perform a range query on this tree. This will return all entries
        /// in the tree that are in contained the specified rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="list">The list to put the results into, or null in
        /// which case a new list will be created and returned.</param>
        /// <returns>All objects in the query rectangle.</returns>
        public void RangeQuery(ref Rectangle rectangle, ref ICollection<T> list)
        {
            // Recurse through the tree, starting at the root node, to find
            // nodes intersecting with the range query.
            Accumulate(_root, ref _bounds, ref rectangle, ref list);
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
        /// <param name="nodeX">Will be the x position of the node.</param>
        /// <param name="nodeY">Will be the y position of the node.</param>
        /// <param name="nodeSize">Will be the size of the node.</param>
        /// <returns>The node for the specified query point.</returns>
        private static Node FindNode(ref Rectangle bounds, Node node, ref int nodeX, ref int nodeY, ref int nodeSize)
        {
            while (!node.IsLeaf)
            {
                // Get current child size.
                var childSize = nodeSize >> 1;

                // Into which child node would we descend?
                var cell = ComputeCell(nodeX, nodeY, childSize, ref bounds);

                // Can we descend and do we have to create that child?
                if (cell < 0 || node.Children[cell] == null)
                {
                    // No, return the current node.
                    return node;
                }

                // Yes, descend into that node.
                node = node.Children[cell];
                nodeX += (((cell & 1) == 0) ? 0 : childSize);
                nodeY += (((cell & 2) == 0) ? 0 : childSize);
                nodeSize = childSize;
            }

            return node;
        }

        #region Restructuring

        /// <summary>
        /// Adds an entry to a node and handles overflow as necessary.
        /// </summary>
        /// <param name="node">The node to insert in.</param>
        /// <param name="nodeX">The x coordinate of the node.</param>
        /// <param name="nodeY">The y coordinate of the node.</param>
        /// <param name="nodeSize">The size of the node.</param>
        /// <param name="entry">The node to insert.</param>
        private void AddToNode(Node node, int nodeX, int nodeY, int nodeSize, Entry entry)
        {
            // If it's not a leaf node, create the leaf node for the new entry.
            // Also get the node in the linked list to insert after.
            Entry insertAfter;
            if (node.IsLeaf)
            {
                Debug.Assert((node.FirstEntry != null && node.LastEntry != null) || _entries == null); // invalid leaf otherwise

                // Got a leaf, insert in it. We're guaranteed to already have
                // at least one entry in this leaf if we come here, because
                // the first entry comes directly with the creation of the node.
                // And if it were to become empty due to removal, it would be
                // trimmed from the tree.
                insertAfter = node.FirstEntry;
            }
            else
            {
                Debug.Assert(((node.FirstEntry ?? node.FirstChildEntry) != null && (node.LastEntry ?? node.LastChildEntry) != null) || _entries == null); // else invalid inner node
                Debug.Assert(node.EntryCount > _maxEntriesPerNode);

                // No, see if we can create a child node for that entry.
                var cell = ComputeCell(nodeX, nodeY, nodeSize >> 1, ref entry.Bounds);
                if (cell < 0)
                {
                    // No, we must insert into this node. We're in an internal node,
                    // so we're guaranteed to have a list of child entries. So we add
                    // this entry either after the first entry of local nodes, if we
                    // have any, or after the last child entry. This guarantees us to
                    // get a non-null reference.
                    insertAfter = node.FirstEntry ?? node.LastChildEntry;
                }
                else
                {
                    Debug.Assert(node.Children[cell] == null); // else find node would have given that node

                    // Yes we can. Allocate the new child node.
                    node.Children[cell] = new Node {Parent = node};

                    // Insert at the end of the child entry segment of the parent node,
                    // to begin a new segment for this node (as to not interfere with
                    // segments of other child nodes, if they exist). This must be not
                    // null unless the entire tree is empty, in which case it will be
                    // entered as the primary entry below.
                    insertAfter = node.LastChildEntry;

                    // Mark the new node as the current one (the one we're adding to).
                    node = node.Children[cell];
                }
            }

            // Add the entry to the existing list if possible. If the insertion point
            // is null it means the tree is yet empty, so we'll add it as the primary
            // entry.
            if (insertAfter != null)
            {
                // Insert into the existing list.
                entry.InsertAfter(insertAfter);
            }
            else
            {
                // Mark as primary entry.
                Debug.Assert(_entries == null);
                _entries = entry;
            }

            // Update the references in the node we inserted into.
            if (node.FirstEntry == node.LastEntry)
            {
                // The node either had no local entries yet (null == null), or it had
                // only one. In the first case we can set both to the same, in the
                // second we only need to set the last entry. We do this by just
                // updating the first node if it's null.
                node.FirstEntry = node.FirstEntry ?? entry;
                node.LastEntry = entry;
            }

            // Remember we have one more entry.
            ++node.EntryCount;

            // Update all parent nodes if we changed the last entry, which only
            // happens when the first and second entries are added. But we also
            // update the entry counts in that iteration, so do it either way.
            var parent = node.Parent;
            while (parent != null)
            {
                Debug.Assert(insertAfter != null);

                if (parent.FirstChildEntry == parent.LastChildEntry)
                {
                    // Same logic as for the local segment, just for the reference
                    // to child nodes. This only applies if the node had no other
                    // child nodes, yet.
                    parent.FirstChildEntry = parent.FirstChildEntry ?? entry;
                    parent.LastChildEntry = entry;
                }
                else if (parent.LastChildEntry == insertAfter)
                {
                    // In case it already had other entries, and we have altered the
                    // last entry (to create a new segment), adjust that reference
                    // accordingly.
                    parent.LastChildEntry = entry;
                }

                // Remember we have one more entry.
                ++parent.EntryCount;

                // Continue checking in our parent.
                parent = parent.Parent;
            }

            // Check whether we need to split the node, and do it if necessary.
            SplitNodeIfNecessary(nodeX, nodeY, nodeSize, node);

            ValidateBranch(_root, _bounds.Width);
        }

        /// <summary>
        /// Removes an entry from a node.
        /// </summary>
        /// <param name="node">The node to remove from.</param>
        /// <param name="entry">The entry to remove.</param>
        private void RemoveFromNode(Node node, Entry entry)
        {
            // Adjust parent nodes if necessary. If we remove from somewhere in the
            // middle we don't really care, as the parents won't reference that entry,
            // but we update the entry counts in this run, too.
            var parent = node.Parent;
            while (parent != null)
            {
                // Adjust the node itself. Based on where we
                if (parent.FirstChildEntry == parent.LastChildEntry)
                {
                    Debug.Assert(parent.FirstChildEntry == entry);

                    // Only one entry in this node, clear it out.
                    parent.FirstChildEntry = null;
                    parent.LastChildEntry = null;
                }
                else if (parent.FirstChildEntry == entry)
                {
                    // It's the low node, and we have more than one entry
                    // (otherwise we would be in the first case), so adjust
                    // the head reference accordingly.
                    parent.FirstChildEntry = parent.FirstChildEntry.Next;
                }
                else if (parent.LastChildEntry == entry)
                {
                    // It's the high node, and we have more than one entry
                    // (otherwise we would be in the first case), so adjust
                    // the tail reference accordingly.
                    parent.LastChildEntry = parent.LastChildEntry.Previous;
                }
                else
                {
                    Debug.Assert(parent.EntryCount > 2);
                }

                // Adjust entry count.
                --parent.EntryCount;

                // Continue checking in our parent.
                parent = parent.Parent;
            }

            // Adjust the node itself.
            if (node.FirstEntry == node.LastEntry)
            {
                Debug.Assert(node.FirstEntry == entry);

                // Only one entry in this node, clear it out.
                node.FirstEntry = null;
                node.LastEntry = null;
            }
            else if (node.FirstEntry == entry)
            {
                // It's the low node, and we have more than one entry
                // (otherwise we would be in the first case), so adjust
                // the head reference accordingly.
                node.FirstEntry = node.FirstEntry.Next;
            }
            else if (node.LastEntry == entry)
            {
                // It's the high node, and we have more than one entry
                // (otherwise we would be in the first case), so adjust
                // the tail reference accordingly.
                node.LastEntry = node.LastEntry.Previous;
            }
            else
            {
                Debug.Assert(node.EntryCount > 2);
            }

            // Adjust entry count.
            --node.EntryCount;

            // Remove the entry from the list of entries.
            entry.Remove();

            // If this was the last entry in the tree, this was the primary
            // entry. If so, unset it.
            if (entry == _entries)
            {
                _entries = null;
            }

            // See if we can collapse the branch. This has to be done in a
            // post-processing step, so that the entry counts are correct
            // in the parent nodes.
            CollapseBranch(node);

            ValidateBranch(_root, _bounds.Width);
        }

        /// <summary>
        /// Try to collapse up a branch starting with the specified child node.
        /// This walks the tree towards the root, removing child nodes while possible.
        /// </summary>
        /// <param name="node">The node to start cleaning at.</param>
        private void CollapseBranch(Node node)
        {
            // Skip leaf nodes.
            if (!node.IsLeaf)
            {
                // Check if child nodes are unnecessary for this node.
                if (node.EntryCount > 0 && node.EntryCount <= _maxEntriesPerNode)
                {
                    // We can prune the child nodes.

                    Debug.Assert((node.FirstChildEntry ?? node.FirstEntry) != null && (node.LastChildEntry ?? node.LastEntry) != null);

                    // Make the first child node our first local node, thus adding the
                    // segments of child nodes to our local nodes. If no high node was
                    // set this means we had no local entries, so we want to set that
                    // if it was null. In case the last entry from our only child node
                    // was removed, the child entry pointer may be null even though
                    // we have local nodes, so make sure to keep that if we have no
                    // child entry referenced.
                    node.FirstEntry = node.FirstChildEntry ?? node.FirstEntry;
                    node.LastEntry = node.LastEntry ?? node.LastChildEntry;

                    // Remove references to child nodes.
                    node.Children[0] = null;
                    node.Children[1] = null;
                    node.Children[2] = null;
                    node.Children[3] = null;
                    node.FirstChildEntry = null;
                    node.LastChildEntry = null;

                    Debug.Assert(node.FirstEntry != null && node.LastEntry != null);
                }
                else
                {
                    // The node needs to stay split. Check if we have empty child nodes.
                    // If so, remove them.
                    if (node.Children[0] != null && node.Children[0].EntryCount == 0)
                    {
                        node.Children[0] = null;
                    }
                    if (node.Children[1] != null && node.Children[1].EntryCount == 0)
                    {
                        node.Children[1] = null;
                    }
                    if (node.Children[2] != null && node.Children[2].EntryCount == 0)
                    {
                        node.Children[2] = null;
                    }
                    if (node.Children[3] != null && node.Children[3].EntryCount == 0)
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
            if (node.Parent != null)
            {
                CollapseBranch(node.Parent);
            }
        }

        /// <summary>
        /// Ensures the tree can contain the given point.
        /// </summary>
        /// <param name="bounds">The bounds to ensure tree size for.</param>
        private void EnsureCapacity(ref Rectangle bounds)
        {
            while (!_bounds.Contains(bounds))
            {
                InsertLevel();
            }
        }

        /// <summary>
        /// Inserts a new level on top of the root node, making it the new root
        /// node. Will reattach all of the root node's child nodes to the
        /// appropriate child nodes of the new root node.
        /// </summary>
        private void InsertLevel()
        {
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
                var wrapper = _root.Children[childNumber] = new Node {Parent = _root};

                // Set opposing corner inside that node to old node in that corner.
                // The (3 - x) will always yield the diagonally opposite cell to x.
                wrapper.Children[3 - childNumber] = child;
                child.Parent = wrapper;

                // Copy values from child node. These are much the same, as the newly
                // created node won't have any children of its own. Only the reference
                // to the last child entry depends on whether the old node hat local
                // entries or not.
                wrapper.EntryCount = child.EntryCount;
                wrapper.FirstChildEntry = child.FirstChildEntry ?? child.FirstEntry;
                wrapper.LastChildEntry = child.LastEntry ?? child.LastChildEntry;

                CollapseBranch(child);
            }

            // Adjust the overall tree bounds.
            _bounds.X = _bounds.X << 1;
            _bounds.Y = _bounds.Y << 1;
            _bounds.Width = _bounds.Width << 1;
            _bounds.Height = _bounds.Height << 1;

            ValidateBranch(_root, _bounds.Width);
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
            if (!node.IsLeaf || // Already is split.
                node.EntryCount <= _maxEntriesPerNode || // No reason to.
                size <= _minBucketSize) // We can't (too small already).
            {
                return;
            }

            // Precompute child size, used several times.
            var childSize = size >> 1;

            // Remember the previous start and end of the interval in this node, so that
            // we may afterwards check if we need to update the references in parent nodes
            // due to reshuffling some of the entries.
            var prevFirstEntry = node.FirstEntry;
            var prevLastEntry = node.LastEntry;

            // Create a copy of the list of entries in this node. Because we
            // will reattach them at different locations in the linked list
            // we cannot directly iterate them.
            var end = node.LastEntry.Next;
            for (var entry = node.FirstEntry; entry != null && entry != end; entry = entry.Next)
            {
                _reusableEntryList.Add(entry);
            }

            // Check each entry to which new cell it'll belong. While doing this, we also
            // separate the entries into two main segments, that of entries moved into the
            // child nodes, and that of entries that had to remain in this node. The
            // remaining entries will be in the back, because the other will have been
            // moved to the front (or rather: to behind another child node entry).
            // We don't want to use a foreach loop here, because in that case the order
            // would not be guaranteed.
            for (var i = 0; i < _reusableEntryList.Count; ++i)
            {
                var entry = _reusableEntryList[i];

                // In which child node would we insert?
                var cell = ComputeCell(x, y, childSize, ref entry.Bounds);
                if (cell < 0)
                {
                    // We must keep that entry in the current node. Move it to the back
                    // of the local segment, if it's not the last one already.
                    if (entry != node.LastEntry)
                    {
                        entry.Remove();
                        entry.InsertAfter(node.LastEntry);
                        node.LastEntry = entry;
                    }
                }
                else
                {

                    // Do we have to create that child? (It might already exist because
                    // we created it in a previous iteration for another entry)
                    if (node.Children[cell] == null)
                    {
                        // Yes. This makes things simple, just create the node and set the
                        // entry as the first child, and mark it as the last as well.
                        node.Children[cell] = new Node {Parent = node, FirstEntry = entry};

                        // No shuffling at this point, as this marks the creation of a
                        // new segment, too. Mark this as the last entry.
                        node.LastChildEntry = entry;

                        // Also, if it's the first entry moved to a child node, mark it as
                        // the first child node.
                        node.FirstChildEntry = node.FirstChildEntry ?? entry;
                    }
                    else if (entry != node.Children[cell].LastEntry.Next)
                    {
                        // The node exists and the next linked node is not this one.
                        // This means we have to sort the sublist by moving this entry
                        // to the correct position.

                        // In case this is the last node we must update the reference
                        // to the last local node.
                        if (entry == node.LastEntry)
                        {
                            // Just point to the one before us.
                            node.LastEntry = entry.Previous;
                        }

                        // Then move the entry.
                        entry.Remove();
                        entry.InsertAfter(node.Children[cell].LastEntry);

                    }

                    // If the entry we inserted after was previously marked as the
                    // last entry, move that reference to this entry instead.
                    if (node.Children[cell].LastEntry == node.LastChildEntry)
                    {
                        node.LastChildEntry = entry;
                    }

                    // Either way, one more entry in the child node.
                    ++node.Children[cell].EntryCount;

                    // List is now in order, so we set the end of the segment to this entry.
                    node.Children[cell].LastEntry = entry;
                }
            }

            // Clear this list for future reuse.
            _reusableEntryList.Clear();

            foreach (var child in node.Children)
            {
                if (child != null)
                {
                    Debug.Assert(child.FirstEntry != null && child.LastEntry != null);
                }
            }

            // If the node is a leaf, still, this means not a single entry could be
            // moved to a child node, which means nothing changed, so we can stop.
            if (node.IsLeaf)
            {
                Debug.Assert(node.FirstChildEntry == null && node.LastChildEntry == null);
                Debug.Assert(node.FirstEntry != null && node.LastEntry != null);

                return;
            }

            Debug.Assert(node.FirstChildEntry != null && node.LastChildEntry != null);
            Debug.Assert(node.FirstEntry == null ? node.LastEntry == null : node.LastEntry != null);

            // At this point the entries in in the segment that is delimited by the
            // node's first and last references is sorted into child node entries and
            // local entries. The reference to the last local entry cannot have
            // changed, as it was the last entry being checked, whereas we'll have to
            // adjust the first reference now -- which is just the entry after the last
            // child entry. This allows us to test if any local entries remained: there
            // are none if the last child entry equals the last local entry.
            if (node.LastChildEntry == node.LastEntry)
            {
                // No more local entries.
                node.FirstEntry = null;
                node.LastEntry = null;
            }
            else
            {
                // Some local entries remain, adjust reference to first one.
                node.FirstEntry = node.LastChildEntry.Next;

                Debug.Assert(node.FirstEntry != null && node.LastEntry != null);
            }

            // Adjust parent nodes if references to the ends of the segment for this
            // node changed.
            var parent = node.Parent;
            while (parent != null)
            {
                Debug.Assert(node.EntryCount > _maxEntriesPerNode);

                // See if we need to update a reference to one of the segment bounds.
                var changed = false;
                if (parent.FirstChildEntry == prevFirstEntry)
                {
                    // Head reference changed.
                    parent.FirstChildEntry = node.FirstChildEntry;
                    changed = true;
                }
                if (parent.LastChildEntry == prevLastEntry)
                {
                    // Tail reference changed.
                    parent.LastChildEntry = node.LastEntry ?? node.LastChildEntry;
                    changed = true;
                }

                Debug.Assert(parent.FirstChildEntry != null && parent.LastChildEntry != null);

                if (changed)
                {
                    // Continue with the next parent node.
                    parent = parent.Parent;
                }
                else
                {
                    // Stop if there were no more updates.
                    break;
                }
            }

            // Do this recursively if the split resulted in another node that
            // has too many entries.
            if (node.Children[0] != null)
            {
                SplitNodeIfNecessary(x, y, childSize, node.Children[0]);
            }
            if (node.Children[1] != null)
            {
                SplitNodeIfNecessary(x + childSize, y, childSize, node.Children[1]);
            }
            if (node.Children[2] != null)
            {
                SplitNodeIfNecessary(x, y + childSize, childSize, node.Children[2]);
            }
            if (node.Children[3] != null)
            {
                SplitNodeIfNecessary(x + childSize, y + childSize, childSize, node.Children[3]);
            }
        }

        #endregion

        #endregion

        #region Utility methods

        /// <summary>
        /// Computes the cell of a node with the specified position and child
        /// node size the specified bounds falls into. If there is no clear
        /// result, this will return -1, which means the bounds must be stored
        /// in the specified node itself (assuming the node can contain the
        /// bounds).
        /// </summary>
        /// <param name="x">The x coordinate of the node.</param>
        /// <param name="y">The y coordinate of the node.</param>
        /// <param name="childSize">The size of the nodes child nodes.</param>
        /// <param name="bounds">The bounds to check for.</param>
        /// <returns>The cell number the bounds fall into.</returns>
        private static int ComputeCell(int x, int y, int childSize, ref Rectangle bounds)
        {
            // Check if the bounds are on the splits.
            var midX = x + (childSize >> 1);
            if (midX >= bounds.X && midX <= bounds.Right)
            {
                // Y split runs through the bounds.
                return -1;
            }
            var midY = y + (childSize >> 1);
            if (midY >= bounds.Y && midY <= bounds.Bottom)
            {
                // X split runs through the bounds.
                return -1;
            }

            // Otherwise check which child node the bounds fall into.
            var cell = 0;
            if (bounds.X >= x + childSize)
            {
                // Right half.
                cell |= 1;
            }
            if (bounds.Y >= y + childSize)
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
        /// <param name="node">The current node.</param>
        /// <param name="nodeBounds">The bounds of the current node.</param>
        /// <param name="point">The query point.</param>
        /// <param name="range">The query range.</param>
        /// <param name="list">The result list.</param>
        private static void Accumulate(Node node, ref Rectangle nodeBounds, ref Vector2 point, float range, ref ICollection<T> list)
        {
            // Skip if the node is empty.
            if (node.EntryCount == 0)
            {
                return;
            }

            // Check how to proceed.
            switch (ComputeIntersection(ref point, range, ref nodeBounds))
            {
                case IntersectionType.Contained:
                {
                    // Box completely contained in query, return all points in it,
                    // no need to recurse further.
                    var begin = node.FirstChildEntry ?? node.FirstEntry;
                    var end = node.LastEntry ?? node.LastChildEntry;
                    end = end.Next;
                    for (var entry = begin; entry != null && entry != end; entry = entry.Next)
                    {
                        // No need for a range check at this point.
                        list.Add(entry.Value);
                    }
                }
                    break;
                case IntersectionType.Overlapping:
                    if (node.IsLeaf)
                    {
                        // Add all entries in this node that are in range.
                        var begin = node.FirstChildEntry ?? node.FirstEntry;
                        var end = node.LastEntry ?? node.LastChildEntry;
                        end = end.Next;
                        for (var entry = begin; entry != null && entry != end; entry = entry.Next)
                        {
                            if (ComputeIntersection(ref point, range, ref entry.Bounds) != IntersectionType.Separated)
                            {
                                list.Add(entry.Value);
                            }
                        }
                    }
                    else
                    {
                        // Recurse into child nodes.
                        var childBounds = new Rectangle { Width = nodeBounds.Width >> 1, Height = nodeBounds.Height >> 1 };

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
            // else: No intersection.
        }

        /// <summary>
        /// Accumulate all entries in range of a rectangular range query to the
        /// given list. This recurses the tree down inner nodes that intersect
        /// the query, until it finds a leaf node. Then adds all entries in the
        /// leaf that are in range.
        /// </summary>
        /// <param name="node">The current node.</param>
        /// <param name="nodeBounds">The bounds of the current node.</param>
        /// <param name="query">The query rectangle.</param>
        /// <param name="list">The result list.</param>
        private static void Accumulate(Node node, ref Rectangle nodeBounds, ref Rectangle query, ref ICollection<T> list)
        {
            // Skip if the node is empty.
            if (node.EntryCount == 0)
            {
                return;
            }

            // Check how to proceed.
            switch (ComputeIntersection(ref query, ref nodeBounds))
            {
                case IntersectionType.Contained:
                {
                    // Box completely contained in query, return all points in it,
                    // no need to recurse further.
                    var begin = node.FirstChildEntry ?? node.FirstEntry;
                    var end = node.LastEntry ?? node.LastChildEntry;
                    end = end.Next;
                    for (var entry = begin; entry != null && entry != end; entry = entry.Next)
                    {
                        // No need for a range check at this point.
                        list.Add(entry.Value);
                    }
                }
                    break;
                case IntersectionType.Overlapping:
                    if (node.IsLeaf)
                    {
                        // Add all entries in this node that are in range.
                        var begin = node.FirstChildEntry ?? node.FirstEntry;
                        var end = node.LastEntry ?? node.LastChildEntry;
                        end = end.Next;
                        for (var entry = begin; entry != null && entry != end; entry = entry.Next)
                        {
                            if (query.Intersects(entry.Bounds))
                            {
                                list.Add(entry.Value);
                            }
                        }
                    }
                    else
                    {
                        // Recurse into child nodes.
                        var childBounds = new Rectangle {Width = nodeBounds.Width >> 1, Height = nodeBounds.Height >> 1};

                        // Unrolled loop.
                        if (node.Children[0] != null)
                        {
                            childBounds.X = nodeBounds.X;
                            childBounds.Y = nodeBounds.Y;
                            Accumulate(node.Children[0], ref childBounds, ref query, ref list);
                        }
                        if (node.Children[1] != null)
                        {
                            childBounds.X = nodeBounds.X + childBounds.Width;
                            childBounds.Y = nodeBounds.Y;
                            Accumulate(node.Children[1], ref childBounds, ref query, ref list);
                        }
                        if (node.Children[2] != null)
                        {
                            childBounds.X = nodeBounds.X;
                            childBounds.Y = nodeBounds.Y + childBounds.Height;
                            Accumulate(node.Children[2], ref childBounds, ref query, ref list);
                        }
                        if (node.Children[3] != null)
                        {
                            childBounds.X = nodeBounds.X + childBounds.Width;
                            childBounds.Y = nodeBounds.Y + childBounds.Height;
                            Accumulate(node.Children[3], ref childBounds, ref query, ref list);
                        }
                    }
                    break;
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
        /// <param name="bounds">The box.</param>
        /// <returns>How the two intersect.</returns>
        private static IntersectionType ComputeIntersection(ref Vector2 center, float radius, ref Rectangle bounds)
        {
            // Check for axis aligned separation.
            if (bounds.X + bounds.Width < center.X - radius ||
                bounds.Y + bounds.Height < center.Y - radius ||
                bounds.X > center.X + radius ||
                bounds.Y > center.Y + radius)
            {
                return IntersectionType.Separated;
            }

            // Check for unaligned separation.
            var closest = center;
            if (center.X < bounds.X)
            {
                closest.X = bounds.X;
            }
            else if (center.X > bounds.X + bounds.Width)
            {
                closest.X = bounds.X + bounds.Width;
            }
            if (center.Y < bounds.Y)
            {
                closest.Y = bounds.Y;
            }
            else if (center.Y > bounds.Y + bounds.Height)
            {
                closest.Y = bounds.Y + bounds.Height;
            }
            var distanceX = closest.X - center.X;
            var distanceY = closest.Y - center.Y;
            if ((distanceX * distanceX + distanceY * distanceY) > radius * radius)
            {
                return IntersectionType.Separated;
            }

            // At least intersection, check furthest point to check if the
            // box is contained within the circle.
            distanceX = Math.Max(center.X - bounds.X, bounds.X + bounds.Width - center.X);
            distanceY = Math.Max(center.Y - bounds.Y, bounds.Y + bounds.Height - center.Y);
            var outside = (distanceX * distanceX + distanceY * distanceY) > radius * radius;
            return outside ? IntersectionType.Overlapping : IntersectionType.Contained;
        }

        /// <summary>
        /// Box / Box intersection test.
        /// </summary>
        /// <param name="rectangle">The first box.</param>
        /// <param name="other">The second box.</param>
        /// <returns>How the two intersect.</returns>
        private static IntersectionType ComputeIntersection(ref Rectangle rectangle, ref Rectangle other)
        {
            bool result;
            rectangle.Intersects(ref other, out result);
            if (!result)
            {
                return IntersectionType.Separated;
            }
            rectangle.Contains(ref other, out result);
            return result ? IntersectionType.Contained : IntersectionType.Overlapping;
        }

        [Conditional("DEBUG")]
        private void ValidateBranch(Node node, int nodeSize)
        {
            if (_entries == null)
            {
                return;
            }

            if (node.IsLeaf)
            {
                Debug.Assert(node.EntryCount <= _maxEntriesPerNode || nodeSize == _minBucketSize);
                Debug.Assert(node.FirstEntry != null);
                Debug.Assert(node.LastEntry != null);
            }
            else
            {
                Debug.Assert(node.EntryCount > _maxEntriesPerNode);
                Debug.Assert(node.FirstChildEntry != null);
                Debug.Assert(node.LastChildEntry != null);
            }

            foreach (var child in node.Children)
            {
                if (child != null)
                {
                    ValidateBranch(child, nodeSize >> 1);
                }
            }
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
            /// The first entry in the child entity list.
            /// </summary>
            public Entry FirstChildEntry;

            /// <summary>
            /// The last entry in the child entity list.
            /// </summary>
            public Entry LastChildEntry;

            /// <summary>
            /// The first entry in the local entity list.
            /// </summary>
            public Entry FirstEntry;

            /// <summary>
            /// The last entry in the local entity list.
            /// </summary>
            public Entry LastEntry;

            /// <summary>
            /// Number of entries in this node and all its children combined.
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
        [DebuggerDisplay("Bounds = {Bounds}, Value = {Value}")]
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
            public Rectangle Bounds;

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
        public void Draw(Graphics.AbstractShape shape, Vector2 translation)
        {
            DrawNode(_root, 0, 0, _bounds.Width, translation, shape);
        }

        /// <summary>
        /// Renders a single note into a sprite batch, and recursively render
        /// its children.
        /// </summary>
        private static void DrawNode(Node node, float centerX, float centerY, int nodeSize, Vector2 translation, Graphics.AbstractShape shape)
        {
            // Abort if there is no node here.
            if (node == null)
            {
                return;
            }

            shape.SetCenter(translation.X + centerX, translation.Y + centerY);
            shape.SetSize(nodeSize - 1);
            shape.Draw();

            // Check for child nodes.
            for (var i = 0; i < 4; ++i)
            {
                var childX = centerX + (((i & 1) == 0) ? -(nodeSize >> 2) : (nodeSize >> 2));
                var childY = centerY + (((i & 2) == 0) ? -(nodeSize >> 2) : (nodeSize >> 2));
                DrawNode(node.Children[i], childX, childY, nodeSize >> 1, translation, shape);
            }
        }

        #endregion
    }
}
