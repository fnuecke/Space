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
    /// A <see href="http://en.wikipedia.org/wiki/Quadtree">QuadTree</see> that
    /// can dynamically grow as needed.
    /// 
    /// <para>
    /// A special restriction is that all nodes will be sized at some power of
    /// two, where every level that power increases:
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
    /// <typeparam name="T">The type of the values stored in the index.</typeparam>
    /// <remarks>
    /// When querying the tree, a cache is generated per node, for the entries
    /// fetched from that node. The cache gets invalidated when the subtree
    /// the node is root of changes. This allows for faster iteration when
    /// repeatedly querying the same area of the tree (as it'll just be an
    /// iteration over an array, instead of a walk through the linked list,
    /// dereferencing the pointer to the next entry for each entry).
    /// 
    /// <para>
    /// The minimum node size can be specified as an arbitrary value larger
    /// than zero, but as node bounds will always be a power of two, the
    /// actual stop level will be the closest smaller power of two (including
    /// the value).
    /// </para>
    /// </remarks>
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
        /// based on elements added and removed to the tree (it shrinks, too).
        /// </summary>
        private TRectangle _bounds;

        /// <summary>
        /// The root node of the tree.
        /// </summary>
        private Node _root;

        /// <summary>
        /// Mapping back from value to entry, for faster value to entry lookup
        /// when removing or updating items.
        /// </summary>
        private readonly Dictionary<T, Entry> _values = new Dictionary<T, Entry>();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new, empty quad tree, with the specified split and
        /// stop criteria.
        /// </summary>
        /// <param name="maxEntriesPerNode">The maximum number of entries per
        /// node before the node will be split.</param>
        /// <param name="minNodeBounds">The minimum bounds size of a node, i.e.
        /// nodes of this size or smaller won't be split regardless of the
        /// number of entries in them. See class remarks.</param>
        /// <exception cref="T:System.ArgumentException">
        /// One or both of the specified parameters are invalid (must be larger
        /// than zero).
        /// </exception>
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

            Clear();
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Add a new item to the index, with the specified bounds.
        /// </summary>
        /// <param name="bounds">The bounds of the item.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="T:System.ArgumentException">
        /// The item is already stored in the index.
        /// </exception>
        public void Add(TRectangle bounds, T item)
        {
            if (Contains(item))
            {
                throw new ArgumentException("Item is already in the index.", "item");
            }

            // Create the entry to add.
            var entry = new Entry {Bounds = bounds, Value = item};

            // Handle dynamic growth.
            EnsureCapacity(ref bounds);

            // Get the node to insert in.
            var nodeBounds = _bounds;
            var node = FindNode(ref bounds, _root, ref nodeBounds);

            // Add the entry to that node.
            AddToNode(node, ref nodeBounds, entry);

            // Store the entry in the value lookup.
            _values.Add(entry.Value, entry);
        }

        /// <summary>
        /// Update an entry by changing its bounds. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <param name="newBounds">The new bounds of the item.</param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns><c>true</c> if the update was successful; <c>false</c> otherwise.</returns>
        public bool Update(TRectangle newBounds, T item)
        {
            // Check if we have that item.
            if (!Contains(item))
            {
                // No, nothing to do, then.
                return false;
            }

            // Get the old bounds.
            var entry = _values[item];

            // Update tree.
            UpdateBounds(ref newBounds, entry);

            // We had the entry, so return true.
            return true;
        }

        /// <summary>
        /// Remove the specified item from the index. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns><c>true</c> if the item was removed; <c>false</c> otherwise.</returns>
        public bool Remove(T item)
        {
            // See if we have that entry.
            if (!Contains(item))
            {
                // No, nothing to do, then.
                return false;
            }

            // Get the existing entry.
            var entry = _values[item];

            // Get the node the entry should be in (if it isn't something
            // went terribly, terribly wrong).
            var nodeBounds = _bounds;
            var node = FindNode(ref entry.Bounds, _root, ref nodeBounds);

            // Remove the entry from that node.
            RemoveFromNode(node, entry);

            // Remove the entry from the value lookup.
            _values.Remove(entry.Value);

            // If the tree is empty, restore the bounds to their defaults.
            if (Count == 0)
            {
                _bounds.X = _bounds.Y = -_minNodeBounds;
                _bounds.Width = _bounds.Height = _minNodeBounds << 1;
            }

            // We had the entry, so return true.
            return true;
        }

        /// <summary>
        /// Test whether this index contains the specified item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>
        ///   <c>true</c> if the index contains the item; <c>false</c> otherwise.
        /// </returns>
        public bool Contains(T item)
        {
            // Use the reverse look up for faster checking.
            return _values.ContainsKey(item);
        }

        /// <summary>
        /// Removes all items from the index.
        /// </summary>
        public void Clear()
        {
            // Reset the root node.
            _root = new Node();

            // And the bounds.
            _bounds.X = _bounds.Y = -_minNodeBounds;
            _bounds.Width = _bounds.Height = _minNodeBounds << 1;

            // And clear the reverse look up.
            _values.Clear();
        }

        /// <summary>
        /// Get the bounds at which the specified item is currently stored.
        /// </summary>
        public TRectangle this[T item] { get { return _values[item].Bounds; } }

        /// <summary>
        /// Perform a circular query on this index. This will return all entries
        /// in the index that are in the specified range of the specified point,
        /// using the euclidean distance function (i.e. <c>sqrt(x*x+y*y)</c>).
        /// </summary>
        /// <param name="point">The query point near which to get entries.</param>
        /// <param name="range">The maximum distance an entry may be away
        /// from the query point to be returned.</param>
        /// <param name="list">The list to put the results into. It is guaranteed
        /// that there will be no duplicate entries.</param>
        /// <remarks>
        /// This checks for intersections of the query circle and the bounds of
        /// the entries in the index. Intersections (i.e. bounds not fully contained
        /// in the circle) will be returned, too.
        /// </remarks>
        public void Find(TPoint point, float range, ref ICollection<T> list)
        {
            // Skip if the tree is empty.
            if (Count > 0)
            {
                // Build the minimal bounding rectangle around the circle query
                // so that we can use it to check against nodes, which is way
                // faster than doing circle/box checks all the time.
                TRectangle bounds;
                bounds.X = (int)(point.X - range);
                bounds.Y = (int)(point.Y - range);
                bounds.Width = bounds.Height = (int)Math.Ceiling(range + range);

                // Recurse through the tree, starting at the root node, to find
                // nodes intersecting with the range query.
                Accumulate(_root, ref _bounds, ref bounds, ref point, range, ref list);
            }
        }

        /// <summary>
        /// Perform an area query on this index. This will return all entries
        /// in the tree that are contained in or intersecting with the specified
        /// query rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="list">The list to put the results into. It is guaranteed
        /// that there will be no duplicate entries.</param>
        public void Find(ref TRectangle rectangle, ref ICollection<T> list)
        {
            // Skip if the tree is empty.
            if (Count > 0)
            {
                // Recurse through the tree, starting at the root node, to find
                // nodes intersecting with the area query.
                Accumulate(_root, ref _bounds, ref rectangle, ref list);
            }
        }

        #endregion

        #region Enumerable

        /// <summary>
        /// Get an enumerator over the items in this tree, together with the
        /// bounds they ares stored at.
        /// </summary>
        /// <returns>
        /// An enumerator of all items in this index, with their bounds.
        /// </returns>
        public IEnumerator<Tuple<TRectangle, T>> GetEnumerator()
        {
            foreach (var entry in _values)
            {
                yield return Tuple.Create(entry.Value.Bounds, entry.Key);
            }
        }

        /// <summary>
        /// Get a non-generic enumerator over the entries in this tree.
        /// </summary>
        /// <returns>
        /// An enumerator of all items in this index, with their bounds.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// A utility enumerator allowing the iteration over all nodes in the
        /// tree. This yields the bounds for each node and an enumerator over
        /// all entries in it.
        /// </summary>
        /// <returns>
        /// An enumerator over all nodes in the tree.
        /// </returns>
        /// <remarks>
        /// This is mainly intended for debugging purposes, to allow rendering
        /// the node bounds.
        /// </remarks>
        public IEnumerable<Tuple<TRectangle, IEnumerable<T>>> GetNodeEnumerable()
        {
            // Keep local stack of nodes so we don't create a load of enumerators.
            var nodes = new Stack<Tuple<Node, TRectangle>>(32);

            // Push root node, if it exists.
            if (_root != null)
            {
                nodes.Push(Tuple.Create(_root, _bounds));
            }

            // Keep going while there are nodes.
            while (nodes.Count > 0)
            {
                // Get node to process.
                var entry = nodes.Pop();
                var node = entry.Item1;
                var bounds = entry.Item2;

                // Push child nodes for next iteration.
                var childBounds = new TRectangle {Width = bounds.Width >> 1, Height = bounds.Height >> 1};
                if (node.Children[0] != null)
                {
                    childBounds.X = bounds.X;
                    childBounds.Y = bounds.Y;
                    nodes.Push(Tuple.Create(node.Children[0], childBounds));
                }
                if (node.Children[1] != null)
                {
                    childBounds.X = bounds.X + childBounds.Width;
                    childBounds.Y = bounds.Y;
                    nodes.Push(Tuple.Create(node.Children[1], childBounds));
                }
                if (node.Children[2] != null)
                {
                    childBounds.X = bounds.X;
                    childBounds.Y = bounds.Y + childBounds.Height;
                    nodes.Push(Tuple.Create(node.Children[2], childBounds));
                }
                if (node.Children[3] != null)
                {
                    childBounds.X = bounds.X + childBounds.Width;
                    childBounds.Y = bounds.Y + childBounds.Height;
                    nodes.Push(Tuple.Create(node.Children[3], childBounds));
                }

                // Return data for this node.
                yield return Tuple.Create(bounds, node.GetEntryEnumerable());
            }
        }

        #endregion

        #region Restructuring

        /// <summary>
        /// Ensures the tree can contain the specified bounds.
        /// </summary>
        /// <param name="bounds">The bounds to ensure the tree size for.</param>
        private void EnsureCapacity(ref TRectangle bounds)
        {
            // Inserts a new level below the root node, making it the new children
            // of root the node (which in turn have the old root's children as
            // their own children).
            // This is repeated until the tree bounds completely contain the
            // specified entry bounds.
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
                                      // Its parent is the root node.
                                      Parent = _root,
                                      // Its child entries are the child entries of the
                                      // previous child at this position, including that
                                      // child own entries.
                                      FirstChildEntry = child.FirstChildEntry ?? child.FirstEntry,
                                      LastChildEntry = child.LastEntry ?? child.LastChildEntry,
                                      EntryCount = child.EntryCount
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
        /// <param name="entry">The entry to insert.</param>
        private void AddToNode(Node node, ref TRectangle nodeBounds, Entry entry)
        {
            // In the following we'll get the node in the linked list to insert after.
            Entry insertAfter;

            // Check what type of tree node we have.
            if (node.IsLeaf)
            {
                // Got a leaf, insert in it. We're guaranteed to already have
                // at least one entry in this leaf if we come here, because
                // the first entry comes directly with the creation of the node.
                // And if it were to become empty due to removal, it would be
                // trimmed from the tree. Meaning FirstEntry cannot be null.
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
                    // Yes, we can push it to a non-existent child node. Allocate it.
                    node.Children[cell] = new Node {Parent = node};

                    // Insert at the end of the child entry segment of the parent node,
                    // to begin a new segment for this node (as to not interfere with
                    // segments of other child nodes, if they exist). This must be not
                    // null unless the entire tree is empty, which will be handled
                    // below, exactly by checking whether insertAfter is null or not.
                    insertAfter = node.LastChildEntry;

                    // Mark the new node as the current one (the one we're adding to).
                    // This is necessary to allow proper propagation of possible
                    // linked list segment end changes up to the root node. This will
                    // make the following split do nothing, too, as we'll have too few
                    // entries to split (one). That is also why we don't have to update
                    // the node bounds -- they won't be used.
                    node = node.Children[cell];
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

            // Add the entry to the existing list if possible. If the insertion point
            // is null it means the tree is yet empty, so it will simply be the the
            // complete linked list for now.
            if (insertAfter != null)
            {
                // Insert into the existing list.
                entry.InsertAfter(insertAfter);
            }

            // Remember we have one more entry in this node.
            ++node.EntryCount;

            // Invalidate cache.
            node.LocalCache = null;

            // Update all parent nodes. We might have changed the ends of some segments
            // so we need to adjust those in the parent nodes.
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent.FirstChildEntry == parent.LastChildEntry)
                {
                    // Same logic as for the local segment, just for the reference
                    // to child nodes. Can be null if we created the first child
                    // node for that parent, and can be one for obvious reasons.
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

                // Remember we have one more entry in this subtree.
                ++parent.EntryCount;

                // Invalidate cache.
                node.ChildCache = null;

                // Continue checking in our parent.
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

            // Check each entry to which new cell it'll belong. While doing this, we also
            // separate the entries into two main segments, that of entries moved into the
            // child nodes, and that of entries that had to remain in this node. The
            // remaining entries will be in the back, because the other will have been
            // moved to the front (or rather: to behind another child node entry).

            // We must keep track of the next node manually because the position of the
            // current entry might change due to shuffling (moving child entries to the
            // segment they belong to). But we only move entries "to the left" (i.e.
            // before other entries), so as long as we remember the next entry that's
            // not a problem.
            for (Entry entry = node.FirstEntry, next = entry.Next, end = node.LastEntry.Next;
                entry != end;
                entry = next, next = (next != end ? next.Next : end)) // This is essentially a test for null.
            {
                // In which child node would we insert?
                var cell = ComputeCell(ref nodeBounds, ref entry.Bounds);
                if (cell < 0)
                {
                    // We must keep that entry in the current node. See if it's the
                    // first one, because then we use it as a reference as to where
                    // to put entries that can be pushed to children that follow.
                    if (node.FirstEntry == null)
                    {
                        // This is the first remaining entry we found, track it.
                        node.FirstEntry = entry;
                    }
                }
                else
                {
                    // If this was the first entry we null the first local entry, to set
                    // it to the first remaining node as soon as we find it.
                    if (entry == node.FirstEntry)
                    {
                        node.FirstEntry = null;
                    }

                    // Do we have to create that child? (It might already exist because
                    // we created it in a previous iteration for another entry)
                    if (node.Children[cell] == null)
                    {
                        // Yes. If we already have nodes we need to keep, move this in
                        // front of that segment. Otherwise this position is fine.
                        if (node.FirstEntry != null)
                        {
                            // If this was the last local node, update that reference.
                            if (entry == node.LastEntry)
                            {
                                // Just point to the one before us. This won't go out
                                // of bounds (our local segment) because we already
                                // have at least one remaining local node.
                                node.LastEntry = entry.Previous;
                            }

                            // Move the entry to before the remaining local segment.
                            entry.Remove();
                            entry.InsertBefore(node.FirstEntry);
                        }

                        // Create the node and set the entry as the first child, and
                        // mark it as the last as well.
                        node.Children[cell] = new Node {Parent = node, FirstEntry = entry, LastEntry = entry};

                        // If it's the first entry moved to a child node (first new segment),
                        // mark it as the first child node.
                        node.FirstChildEntry = node.FirstChildEntry ?? entry;

                        // Mark this as the last child entry either way.
                        node.LastChildEntry = entry;
                    }
                    else
                    {
                        // The node exists, check if the next linked node is this one,
                        // because if it is we don't need to shuffle.
                        if (entry != node.Children[cell].LastEntry.Next)
                        {
                            // This means we have to sort the sublist by moving this
                            // entry to the correct position.

                            // In case this is the last node we must update the reference
                            // to the last local node.
                            if (entry == node.LastEntry)
                            {
                                // Just point to the one before us. This won't go out of
                                // bounds (our local segment) because we already have a
                                // child node, meaning we're not the first.
                                node.LastEntry = entry.Previous;
                            }

                            // ThenMove the entry to the end of the segment of the node
                            // it goes into.
                            entry.Remove();
                            entry.InsertAfter(node.Children[cell].LastEntry);
                        }

                        // If the last entry in the node we inserted into was the last one
                        // in the child entry segment of our parent, update that pointer.
                        // (This means the node we insert into has the last segment).
                        if (node.Children[cell].LastEntry == node.LastChildEntry)
                        {
                            node.LastChildEntry = entry;
                        }

                        // We replaced the last entry, so set that in the node.
                        node.Children[cell].LastEntry = entry;
                    }

                    // Either way, one more entry in the child node.
                    ++node.Children[cell].EntryCount;
                }
            }

            // Clear this list for future reuse.
            //_reusableEntryList.Clear();

            // If the node is a leaf, still, this means not a single entry could be
            // moved to a child node, which means nothing changed, so we can stop.
            if (node.IsLeaf)
            {
                return;
            }

            // Invalidate cache, but only if something changed.
            node.LocalCache = node.ChildCache = null;

            // At this point the entries in in the segment that is delimited by the
            // node's first and last references is sorted into child node entries and
            // local entries. This allows us to test if any local entries remained:
            // there are none if the last child entry equals the last local entry.
            if (node.LastChildEntry == node.LastEntry)
            {
                // No more local entries.
                node.FirstEntry = null;
                node.LastEntry = null;
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
                    // Head reference changed. We're guaranteed to have some child
                    // entries at this point (else we'd have returned earlier).
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
                    // Stop if there were no more updates (all inner segments).
                    break;
                }
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
            // Adjust parent nodes if necessary. If we remove from somewhere in the
            // middle we don't really care, as the parents won't reference that entry,
            // but we update the entry counts in this run, and invalidate caches, too.
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

                // Invalidate cache.
                parent.ChildCache = null;

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

            // Invalidate cache.
            node.LocalCache = null;

            // Remove the entry from the linked list of entries.
            entry.Remove();

            // See if we can collapse the branch.
            CollapseBranch(node);
        }

        /// <summary>
        /// Try to collapse a branch starting with the specified child node.
        /// This walks the tree towards the root, removing child nodes while
        /// possible.
        /// </summary>
        /// <param name="node">The node to start cleaning at.</param>
        private void CollapseBranch(Node node)
        {
            // Skip leaf nodes.
            if (!node.IsLeaf)
            {
                // Check if child nodes are unnecessary for this node. This is the
                // case if there is a smaller number than the split count, of course.
                if (node.EntryCount <= _maxEntriesPerNode)
                {
                    // If we're empty we could use the else branch (null children), but
                    // that's kind of superfluous, because we'll get nulled ourselves
                    // in our parent, in that case, so just skip that.
                    if (node.EntryCount > 0)
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

                        // Invalidate caches.
                        node.LocalCache = node.ChildCache = null;
                    }
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
                (node.EntryCount > _maxEntriesPerNode && ComputeCell(ref nodeBounds, ref newBounds) > -1))
            {
                // Did not fit in node anymore, or we can push the entry into
                // a child node, remove from that node.
                RemoveFromNode(node, entry);

                // Remove the entry from the value lookup.
                _values.Remove(entry.Value);

                // And add again.
                Add(newBounds, entry.Value);
            }
            else
            {
                // It stays where it is, just update the entry's bounds.
                entry.Bounds = newBounds;
            }
        }

        #endregion

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
        /// <returns>
        /// The cell number the bounds fall into.
        /// </returns>
        private static int ComputeCell(ref TRectangle nodeBounds, ref TRectangle entryBounds)
        {
            var halfNodeSize = nodeBounds.Width >> 1;

            // Check if the bounds are on the splits.
            var midX = nodeBounds.X + halfNodeSize;
            if (midX >= entryBounds.X && midX <= entryBounds.X + entryBounds.Width)
            {
                // Y split runs through the bounds.
                return -1;
            }
            var midY = nodeBounds.Y + halfNodeSize;
            if (midY >= entryBounds.Y && midY <= entryBounds.Y + entryBounds.Height)
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
        /// Find the minimal node that can contain the specified bounds. If
        /// possible, this will return a leaf node. If there is no leaf node
        /// that can contain the rectangle, it will return the smallest inner
        /// node that can contain the bounds.
        /// </summary>
        /// <param name="bounds">The bounds to get the node for.</param>
        /// <param name="node">The node to start searching in.</param>
        /// <param name="nodeBounds">The bounds of the node we start in. Will
        /// hold the bounds of the resulting node.</param>
        /// <returns>
        /// The node containing the specified bounds.
        /// </returns>
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

            // Return the best match for the bounds.
            return node;
        }

        /// <summary>
        /// Accumulate all entries in the specified circle into the specified list.
        /// This recurses the tree down inner nodes that intersect the query, until
        /// it finds a leaf node. Then adds all entries in the leaf that are in range.
        /// On the way, it adds all entries kept in inner nodes that intersect the
        /// query.
        /// </summary>
        /// <param name="node">The current node.</param>
        /// <param name="nodeBounds">The bounds of the current node.</param>
        /// <param name="rectangle">The minimal bounding box around the circle query.</param>
        /// <param name="point">The query point.</param>
        /// <param name="range">The query range.</param>
        /// <param name="list">The result list.</param>
        private static void Accumulate(Node node, ref TRectangle nodeBounds, ref TRectangle rectangle, ref TPoint point, float range, ref ICollection<T> list)
        {
            // Check how to proceed.
            switch (ComputeIntersection(ref rectangle, ref nodeBounds))
            {
                case IntersectionType.Contains:
                {
                    // Node completely contained in query, return all entries in it,
                    // no need to recurse further.
                    node.AddOwnEntries(ref list);
                    node.AddChildEntries(ref list);
                    break;
                }
                case IntersectionType.Intersects:
                {
                    // Add all local entries in this node that are in range, regardless
                    // of whether this is an inner or a leaf node.
                    node.AddOwnEntries(ref point, range, ref list);

                    // If it's not a leaf recurse into child nodes.
                    if (!node.IsLeaf)
                    {
                        // Build the bounds for each child in the following.
                        var childBounds = new TRectangle
                                          {
                                              Width = nodeBounds.Width >> 1,
                                              Height = nodeBounds.Height >> 1
                                          };

                        // Unrolled loop.
                        if (node.Children[0] != null)
                        {
                            childBounds.X = nodeBounds.X;
                            childBounds.Y = nodeBounds.Y;
                            Accumulate(node.Children[0], ref childBounds, ref rectangle, ref point, range, ref list);
                        }
                        if (node.Children[1] != null)
                        {
                            childBounds.X = nodeBounds.X + childBounds.Width;
                            childBounds.Y = nodeBounds.Y;
                            Accumulate(node.Children[1], ref childBounds, ref rectangle, ref point, range, ref list);
                        }
                        if (node.Children[2] != null)
                        {
                            childBounds.X = nodeBounds.X;
                            childBounds.Y = nodeBounds.Y + childBounds.Height;
                            Accumulate(node.Children[2], ref childBounds, ref rectangle, ref point, range, ref list);
                        }
                        if (node.Children[3] != null)
                        {
                            childBounds.X = nodeBounds.X + childBounds.Width;
                            childBounds.Y = nodeBounds.Y + childBounds.Height;
                            Accumulate(node.Children[3], ref childBounds, ref rectangle, ref point, range, ref list);
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
                    // Node completely contained in query, return all entries in it,
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
                        var childBounds = new TRectangle
                                          {
                                              Width = nodeBounds.Width >> 1,
                                              Height = nodeBounds.Height >> 1
                                          };

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
        /// <returns>
        /// How the two intersect.
        /// </returns>
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
        /// <returns>
        /// How the two intersect.
        /// </returns>
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

        #region Types

        /// <summary>
        /// A node in the tree, which can either be a leaf or an inner node.
        /// <para>
        /// Leaf nodes only hold a list of entries, whereas inner nodes also
        /// reference to more specific child nodes (in addition to local entries
        /// in case they cannot be put in a child node because they lie on a
        /// split).
        /// </para>
        /// </summary>
        [DebuggerDisplay("Count = {EntryCount}, Leaf = {IsLeaf}")]
        private sealed class Node
        {
            #region Properties

            /// <summary>
            /// Whether this node is a leaf node or not.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is leaf node; otherwise, <c>false</c>.
            /// </value>
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
            /// The children this node points to.
            /// </summary>
            public readonly Node[] Children = new Node[4];

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
            /// Cache of entries contained in this node by itself (leaf node
            /// or internal node with entries on split).
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public Entry[] LocalCache;

            /// <summary>
            /// Cache of entries in child nodes.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public Entry[] ChildCache;

            #endregion

            #region Entry Accumulator

            /// <summary>
            /// Rebuilds the local cache.
            /// </summary>
            private void RebuildLocalCache()
            {
                if (LocalCache == null)
                {
                    var cache = new List<Entry>();
                    if (FirstEntry != null)
                    {
                        for (Entry entry = FirstEntry, end = LastEntry.Next; entry != end; entry = entry.Next)
                        {
                            cache.Add(entry);
                        }
                    }
                    LocalCache = cache.ToArray();
                }
            }

            /// <summary>
            /// Rebuilds the child cache.
            /// </summary>
            private void RebuildChildCache()
            {
                if (ChildCache == null)
                {
                    var cache = new List<Entry>();
                    if (FirstChildEntry != null)
                    {
                        for (Entry entry = FirstChildEntry, end = LastChildEntry.Next; entry != end; entry = entry.Next)
                        {
                            cache.Add(entry);
                        }
                    }
                    ChildCache = cache.ToArray();
                }
            }

            /// <summary>
            /// Adds all entries stored in this node to the specified list.
            /// </summary>
            /// <param name="list">The list.</param>
            public void AddOwnEntries(ref ICollection<T> list)
            {
                // Rebuild entry cache if necessary.
                RebuildLocalCache();

                // Add all entries to the collection.
                for (int i = 0, j = LocalCache.Length; i < j; i++)
                {
                    list.Add(LocalCache[i].Value);
                }
            }

            /// <summary>
            /// Adds all entries stored in this node to the specified list if
            /// they intersect with the specified circle.
            /// </summary>
            /// <param name="point">The center of the circle.</param>
            /// <param name="range">The radius of the circle.</param>
            /// <param name="list">The list.</param>
            public void AddOwnEntries(ref TPoint point, float range, ref ICollection<T> list)
            {
                // Rebuild entry cache if necessary.
                RebuildLocalCache();

                // Add all entries to the collection.
                for (int i = 0, j = LocalCache.Length; i < j; i++)
                {
                    if (ComputeIntersection(ref point, range, ref LocalCache[i].Bounds) != IntersectionType.Disjoint)
                    {
                        list.Add(LocalCache[i].Value);
                    }
                }
            }

            /// <summary>
            /// Adds all entries stored in this node to the specified list if
            /// they intersect with the specified rectangle.
            /// </summary>
            /// <param name="rectangle">The rectangle.</param>
            /// <param name="list">The list.</param>
            public void AddOwnEntries(ref TRectangle rectangle, ref ICollection<T> list)
            {
                // Rebuild entry cache if necessary.
                RebuildLocalCache();

                // Add all entries to the collection.
                for (int i = 0, j = LocalCache.Length; i < j; i++)
                {
                    if (ComputeIntersection(ref rectangle, ref LocalCache[i].Bounds) != IntersectionType.Disjoint)
                    {
                        list.Add(LocalCache[i].Value);
                    }
                }
            }

            /// <summary>
            /// Adds all child entries to the specified list.
            /// </summary>
            /// <param name="list">The list.</param>
            public void AddChildEntries(ref ICollection<T> list)
            {
                // Rebuild entry cache if necessary.
                RebuildChildCache();

                // Add all entries to the collection.
                for (int i = 0, j = ChildCache.Length; i < j; i++)
                {
                    list.Add(ChildCache[i].Value);
                }
            }

            #endregion

            #region Enumerator

            /// <summary>
            /// Enumerates all entries stored directly in this node. It is used
            /// by the node iterator.
            /// </summary>
            /// <returns>
            /// An enumerator for all entries in this node.
            /// </returns>
            public IEnumerable<T> GetEntryEnumerable()
            {
                // Rebuild entry cache if necessary.
                RebuildLocalCache();

                // Yield all entries to the collection.
                for (int i = 0, j = LocalCache.Length; i < j; i++)
                {
                    yield return LocalCache[i].Value;
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
            public TRectangle Bounds;

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
                // Adjust other nodes' values.
                var insertBefore = entry.Next;
                entry.Next = this;
                if (insertBefore != null)
                {
                    insertBefore.Previous = this;
                }

                // Adjust own values.
                Previous = entry;
                Next = insertBefore;
            }

            /// <summary>
            /// Insert this node into the linked list, before the specified entry.
            /// </summary>
            /// <param name="entry">The entry to insert before.</param>
            public void InsertBefore(Entry entry)
            {
                // Adjust other nodes' values.
                var insertAfter = entry.Previous;
                entry.Previous = this;
                if (insertAfter != null)
                {
                    insertAfter.Next = this;
                }

                // Adjust own values.
                Previous = insertAfter;
                Next = entry;
            }

            #endregion
        }

        #endregion
    }
}
