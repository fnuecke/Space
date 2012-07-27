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
        private Rectangle _bounds;

        /// <summary>
        /// The root node of the tree.
        /// </summary>
        private Node _root;

        /// <summary>
        /// Mapping back from value to entry, for faster value to entry lookup,
        /// when removing items or updating items.
        /// </summary>
        private readonly Dictionary<T, Entry> _values = new Dictionary<T, Entry>();

        /// <summary>
        /// Reused list when splitting nodes.
        /// </summary>
        private readonly List<Entry> _reusableEntryList;

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
        public QuadTree(int maxEntriesPerNode, int minNodeBounds)
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
            _reusableEntryList = new List<Entry>(_maxEntriesPerNode + 1);

            Clear();
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Add a new entry to the tree, with the specified bounds, with the
        /// specified associated value.
        /// </summary>
        /// <param name="bounds">The bounds of the entry.</param>
        /// <param name="value">The value associated with the point.</param>
        /// <exception cref="ArgumentException">This value is already stored
        /// in the tree.</exception>
        public void Add(ref Rectangle bounds, T value)
        {
            if (Contains(value))
            {
                throw new ArgumentException("Entry is already in the tree.", "value");
            }

            // Create the entry to add.
            var entry = new Entry {Bounds = bounds, Value = value};

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
        /// Add a new entry to the tree, at the specified position, with the
        /// specified associated value.
        /// </summary>
        /// <remarks>
        /// This will generate a bounding rectangle with zero extent to use
        /// as the bounds for the value.
        /// </remarks>
        /// <param name="point">The point at which to store the entry.</param>
        /// <param name="value">The value associated with the point.</param>
        /// <exception cref="ArgumentException">This value is already stored
        /// in the tree.</exception>
        public void Add(Vector2 point, T value)
        {
            var bounds = new Rectangle {X = (int)point.X, Y = (int)point.Y};
            Add(ref bounds, value);
        }

        /// <summary>
        /// Update a single entry by changing its bounds. If the entry is not
        /// already in the tree, this will return <code>false</code>.
        /// </summary>
        /// <param name="newBounds">The new bounds of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <returns><code>true</code> if the update was successful.</returns>
        public bool Update(ref Rectangle newBounds, T value)
        {
            // Check if we have that entry, if not add it.
            if (!Contains(value))
            {
                return false;
            }

            // Get the old position.
            var entry = _values[value];

            // Update tree.
            UpdateBounds(ref newBounds, entry);

            return true;
        }

        /// <summary>
        /// Update a single entry by changing its position. If the entry is not
        /// already in the tree, this will return <code>false</code>.
        /// </summary>
        /// <remarks>
        /// This will generate a bounding rectangle with zero extent to use
        /// as the bounds for the value.
        /// </remarks>
        /// <param name="newPoint">The new position of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <returns><code>true</code> if the update was successful.</returns>
        public bool Update(Vector2 newPoint, T value)
        {
            var bounds = new Rectangle {X = (int)newPoint.X, Y = (int)newPoint.Y};
            return Update(ref bounds, value);
        }

        /// <summary>
        /// Similar to <code>Update</code> this changes an entry's bounds. Unlike
        /// <code>Update</code>, however, this just moves the bounds to the
        /// specified location. The specified position is used as the new center
        /// for the bounds.
        /// </summary>
        /// <param name="position">The new position of the bounds.</param>
        /// <param name="value">The entry for which to update the bounds.</param>
        /// <returns></returns>
        public bool Move(Vector2 position, T value)
        {
            // Check if we have that entry, if not add it.
            if (!Contains(value))
            {
                return false;
            }

            // Get the old position.
            var entry = _values[value];

            // Update bounds.
            var newBounds = entry.Bounds;
            newBounds.X = (int)position.X - newBounds.Width / 2;
            newBounds.Y = (int)position.Y - newBounds.Height / 2;

            // Update tree.
            UpdateBounds(ref newBounds, entry);

            return true;
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

            // Yes, get the instance.
            var entry = _values[value];

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
            _bounds.X = _bounds.Y = -_minNodeBounds;
            _bounds.Width = _bounds.Height = _minNodeBounds << 1;
            _values.Clear();
        }

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
        public void Find(Vector2 point, float range, ref ICollection<T> list)
        {
            // Skip if the tree is empty.
            if (Count > 0)
            {
                // Recurse through the tree, starting at the root node, to find
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
        public void Find(ref Rectangle rectangle, ref ICollection<T> list)
        {
            // Skip if the tree is empty.
            if (Count > 0)
            {
                // Recurse through the tree, starting at the root node, to find
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
        private static Node FindNode(ref Rectangle bounds, Node node, ref Rectangle nodeBounds)
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
        private void EnsureCapacity(ref Rectangle bounds)
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
                    var wrapper = new Node
                    {
                        Parent = _root,
                        EntryCount = child.EntryCount,
                        FirstChildEntry = child.FirstChildEntry ?? child.FirstEntry,
                        LastChildEntry = child.LastEntry ?? child.LastChildEntry
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
        private void AddToNode(Node node, ref Rectangle nodeBounds, Entry entry)
        {
            // In the following we'll get the node in the linked list to insert after.
            Entry insertAfter;

            // Check what type of node we have.
            if (node.IsLeaf)
            {
                // Got a leaf, insert in it. We're guaranteed to already have
                // at least one entry in this leaf if we come here, because
                // the first entry comes directly with the creation of the node.
                // And if it were to become empty due to removal, it would be
                // trimmed from the tree.
                insertAfter = node.FirstEntry;
            }
            else
            {
                // Inner node, see if we can create a child node for that entry (there
                // is none yet, because otherwise we would have received that as the
                // parameter).
                var cell = ComputeCell(ref nodeBounds, ref entry.Bounds);
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
                    // Yes we can. Allocate the new child node.
                    node.Children[cell] = new Node {Parent = node};

                    // Insert at the end of the child entry segment of the parent node,
                    // to begin a new segment for this node (as to not interfere with
                    // segments of other child nodes, if they exist). This must be not
                    // null unless the entire tree is empty.
                    insertAfter = node.LastChildEntry;

                    // Mark the new node as the current one (the one we're adding to).
                    node = node.Children[cell];

                    // Also adjust the current bounds, for the following split operation.
                    nodeBounds.Width >>= 1;
                    nodeBounds.Height >>= 1;
                    nodeBounds.X += (((cell & 1) == 0) ? 0 : nodeBounds.Width);
                    nodeBounds.Y += (((cell & 2) == 0) ? 0 : nodeBounds.Height);
                }
            }

            // Update the references in the node we inserted into.
            if (node.FirstEntry == node.LastEntry)
            {
                // The node either had no local entries yet (null == null, empty tree or
                // empty inner node), or it had only one. In the first case we can set
                // both to the same, in the second we only need to set the last entry.
                // We do this by just updating the first node if it's null. The last one
                // has to be updated either way.
                node.FirstEntry = node.FirstEntry ?? entry;
                node.LastEntry = entry;
            }

            // Remember we have one more entry.
            ++node.EntryCount;

            // Add the entry to the existing list if possible. If the insertion point
            // is null it means the tree is yet empty, so it will simply the the
            // complete linked list for now.
            if (insertAfter != null)
            {
                // Insert into the existing list.
                entry.InsertAfter(insertAfter);
            }

            // Update all parent nodes.
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent.FirstChildEntry == parent.LastChildEntry)
                {
                    // Same logic as for the local segment, just for the reference
                    // to child nodes.
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
            SplitNode(node, ref nodeBounds);
        }

        /// <summary>
        /// Check if a node needs to be split, and split it if allowed to.
        /// </summary>
        /// <param name="node">The actual node to split.</param>
        /// <param name="nodeBounds">The bounds of the node.</param>
        private void SplitNode(Node node, ref Rectangle nodeBounds)
        {
            // Should we split?
            if (!node.IsLeaf || // Already is split.
                node.EntryCount <= _maxEntriesPerNode || // No reason to.
                nodeBounds.Width <= _minNodeBounds) // We can't (too small already).
            {
                return;
            }

            // Remember the previous start and end of the interval in this node, so that
            // we may afterwards check if we need to update the references in parent nodes
            // due to reshuffling some of the entries.
            var prevFirstEntry = node.FirstEntry;
            var prevLastEntry = node.LastEntry;

            // Create a copy of the list of entries in this node. Because we
            // will reattach them at different locations in the linked list
            // we cannot directly iterate them.
            var end = node.LastEntry.Next;
            for (var entry = node.FirstEntry; entry != end; entry = entry.Next)
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
                var cell = ComputeCell(ref nodeBounds, ref entry.Bounds);
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
                        node.Children[cell] = new Node { Parent = node, FirstEntry = entry };

                        // No shuffling at this point, as this marks the creation of a
                        // new segment, too. Mark this as the last entry.
                        node.LastChildEntry = entry;

                        // Also, if it's the first entry moved to a child node, mark it as
                        // the first child node.
                        node.FirstChildEntry = node.FirstChildEntry ?? entry;
                    }
                    else
                    {
                        // The node exists, check if the next linked node is this one,
                        // because if it is we don't need to shuffle.
                        if (entry != node.Children[cell].LastEntry.Next)
                        {
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

                        // If the last entry in the node we inserted into was the last one in the
                        // child entry segment of our parent, update that pointer.
                        if (node.Children[cell].LastEntry == node.LastChildEntry)
                        {
                            node.LastChildEntry = entry;
                        }
                    }

                    // List is now in order, so we set the end of the segment to this entry.
                    node.Children[cell].LastEntry = entry;

                    // Either way, one more entry in the child node.
                    ++node.Children[cell].EntryCount;
                }
            }

            // Clear this list for future reuse.
            _reusableEntryList.Clear();

            // If the node is a leaf, still, this means not a single entry could be
            // moved to a child node, which means nothing changed, so we can stop.
            if (node.IsLeaf)
            {
                return;
            }

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
            }

            // Adjust parent nodes if references to the ends of the segment for this
            // node changed.
            var parent = node.Parent;
            while (parent != null)
            {
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
            var childBounds = new Rectangle {Width = nodeBounds.Width >> 1, Height = nodeBounds.Height >> 1};

            if (node.Children[0] != null)
            {
                childBounds.X = nodeBounds.X;
                childBounds.Y = nodeBounds.Y;
                SplitNode(node.Children[0], ref childBounds);
            }
            if (node.Children[1] != null)
            {
                childBounds.X = nodeBounds.X + childBounds.Width;
                childBounds.Y = nodeBounds.Y;
                SplitNode(node.Children[1], ref childBounds);
            }
            if (node.Children[2] != null)
            {
                childBounds.X = nodeBounds.X;
                childBounds.Y = nodeBounds.Y + childBounds.Height;
                SplitNode(node.Children[2], ref childBounds);
                childBounds.Y -= childBounds.Height;
            }
            if (node.Children[3] != null)
            {
                childBounds.X = nodeBounds.X + childBounds.Width;
                childBounds.Y = nodeBounds.Y + childBounds.Height;
                SplitNode(node.Children[3], ref childBounds);
            }
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

                // Adjust entry count.
                --parent.EntryCount;

                // Continue checking in our parent.
                parent = parent.Parent;
            }

            // Adjust the node itself.
            if (node.FirstEntry == node.LastEntry)
            {
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

            // Adjust entry count.
            --node.EntryCount;

            // Remove the entry from the list of entries.
            entry.Remove();

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
            // Skip leaf nodes.
            if (!node.IsLeaf)
            {
                // Check if child nodes are unnecessary for this node.
                if (node.EntryCount > 0 && node.EntryCount <= _maxEntriesPerNode)
                {
                    // We can prune the child nodes.

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

        private void UpdateBounds(ref Rectangle newBounds, Entry entry)
        {
            // Node may have changed. Get the node the entry is currently stored in.
            var nodeBounds = _bounds;
            var node = FindNode(ref entry.Bounds, _root, ref nodeBounds);

            // Check if the entry should go to a different node now.
            if (nodeBounds.X >= newBounds.X ||
                nodeBounds.Y >= newBounds.Y ||
                nodeBounds.X + nodeBounds.Width <= newBounds.X + newBounds.Width ||
                nodeBounds.Y + nodeBounds.Height <= newBounds.Y + newBounds.Height ||
                (node.EntryCount > _maxEntriesPerNode && ComputeCell(ref nodeBounds, ref newBounds) > -1))
            {
                // Did not fit in node anymore or can be inserted into child node,
                // remove from that node.
                RemoveFromNode(node, entry);

                // Remove the entry from the value lookup.
                _values.Remove(entry.Value);

                // And add again.
                Add(ref newBounds, entry.Value);
            }
            else
            {
                // Stays in the same node. Update the position in the entry.
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
        private static int ComputeCell(ref Rectangle nodeBounds, ref Rectangle entryBounds)
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
        private static void Accumulate(Node node, ref Rectangle nodeBounds, ref Vector2 point, float range, ref ICollection<T> list)
        {
            // Check how to proceed.
            switch (ComputeIntersection(ref point, range, ref nodeBounds))
            {
                case IntersectionType.Contains:
                {
                    // Box completely contained in query, return all points in it,
                    // no need to recurse further.
                    var begin = node.FirstChildEntry ?? node.FirstEntry;
                    var end = (node.LastEntry ?? node.LastChildEntry).Next;
                    for (var entry = begin; entry != end; entry = entry.Next)
                    {
                        // No need for a range check at this point.
                        list.Add(entry.Value);
                    }
                    break;
                }
                case IntersectionType.Intersects:
                {
                    // Add all local entries in this node that are in range, regardless of
                    // whether this is an inner or a leaf node.
                    if (node.FirstEntry != null)
                    {
                        var begin = node.FirstEntry;
                        var end = node.LastEntry.Next;
                        for (var entry = begin; entry != end; entry = entry.Next)
                        {
                            if (ComputeIntersection(ref point, range, ref entry.Bounds) != IntersectionType.Disjoint)
                            {
                                list.Add(entry.Value);
                            }
                        }
                    }

                    if (!node.IsLeaf)
                    {
                        // Recurse into child nodes.
                        var childBounds = new Rectangle {Width = nodeBounds.Width >> 1, Height = nodeBounds.Height >> 1};

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
        private static void Accumulate(Node node, ref Rectangle nodeBounds, ref Rectangle rectangle, ref ICollection<T> list)
        {
            // Check how to proceed.
            switch (ComputeIntersection(ref rectangle, ref nodeBounds))
            {
                case IntersectionType.Contains:
                {
                    // Box completely contained in query, return all points in it,
                    // no need to recurse further.
                    var begin = node.FirstChildEntry ?? node.FirstEntry;
                    var end = (node.LastEntry ?? node.LastChildEntry).Next;
                    for (var entry = begin; entry != end; entry = entry.Next)
                    {
                        // No need for a range check at this point.
                        list.Add(entry.Value);
                    }
                    break;
                }
                case IntersectionType.Intersects:
                {
                    // Add all local entries in this node that are in range, regardless of
                    // whether this is an inner or a leaf node.
                    if (node.FirstEntry != null)
                    {
                        var begin = node.FirstEntry;
                        var end = node.LastEntry.Next;
                        for (var entry = begin; entry != end; entry = entry.Next)
                        {
                            if (ComputeIntersection(ref rectangle, ref entry.Bounds) != IntersectionType.Disjoint)
                            {
                                list.Add(entry.Value);
                            }
                        }
                    }
                    if (!node.IsLeaf)
                    {
                        // Recurse into child nodes.
                        var childBounds = new Rectangle {Width = nodeBounds.Width >> 1, Height = nodeBounds.Height >> 1};

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
        private static IntersectionType ComputeIntersection(ref Vector2 center, float radius, ref Rectangle bounds)
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
        private static IntersectionType ComputeIntersection(ref Rectangle rectangle, ref Rectangle bounds)
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
                get
                {
                    return (Children[0] == null) &&
                           (Children[1] == null) &&
                           (Children[2] == null) &&
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
        /// A single entry in the tree, uniquely identified by its value.
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
        public IEnumerator<Tuple<Rectangle, T>> GetEnumerator()
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
        private static void DrawNode(Node node, float centerX, float centerY, int nodeSize, Vector2 translation,
                                     Graphics.AbstractShape shape)
        {
            // Abort if there is no node here.
            if (node == null)
            {
                return;
            }

            shape.SetCenter(translation.X + centerX, translation.Y + centerY);
            shape.SetSize(nodeSize - 1);
            shape.Draw();

            if (node.FirstEntry != null)
            {
                var end = node.LastEntry.Next;
                for (var entry = node.FirstEntry; entry != end; entry = entry.Next)
                {
                    var bounds = entry.Bounds;
                    shape.SetCenter(translation.X + bounds.X + bounds.Width / 2f, translation.Y + bounds.Y + bounds.Height / 2f);
                    shape.SetSize(bounds.Width, bounds.Height);
                    shape.Draw();
                }
            }

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
