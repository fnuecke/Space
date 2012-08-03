using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Collections;
using Microsoft.Xna.Framework;

// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
using TPoint = Engine.FarMath.FarPosition;
using TRectangle = Engine.FarMath.FarRectangle;

namespace Tests.Microsoft
{
    /// 
    /// This class efficiently stores and retrieves arbitrarily sized and positioned
    /// objects in a quad-tree data structure.  This can be used to do efficient hit
    /// detection or visiblility checks on objects in a virtualized canvas. 
    /// The object does not need to implement any special interface because the Rect Bounds
    /// of those objects is handled as a separate argument to Insert. 
    ///  
    public class MSQuadTree<T> : IIndex<T, TRectangle, TPoint>
    {
        private TRectangle _bounds; // overall bounds we are indexing.

        private Quadrant _root;

        private Dictionary<T, Quadrant> _table;

        ///  
        /// This determines the overall quad-tree indexing strategy, changing this bounds
        /// is expensive since it has to re-divide the entire thing - like a re-hash operation. 
        /// 
        public TRectangle Bounds
        {
            get { return _bounds; }
            set
            {
                _bounds = value;
                ReIndex();
            }
        }

        #region Implementation of IIndex

        /// <summary>
        /// The number of values stored in this tree.
        /// </summary>
        public int Count
        {
            get { return _table == null ? 0 : _table.Count; }
        }

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
            Insert(item, bounds);
        }

        /// <summary>
        /// Update an entry by changing its bounds. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <param name="newBounds">The new bounds of the item.</param>
        /// <param name="delta"> </param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns><c>true</c> if the update was successful; <c>false</c> otherwise.</returns>
        public bool Update(TRectangle newBounds, Vector2 delta, T item)
        {
            throw new NotSupportedException();
        }

        ///  
        /// Remove the given node from this QuadTree.
        /// 
        /// The node to remove
        /// True if the node was found and removed. 
        public bool Remove(T node)
        {
            if (_table != null)
            {
                Quadrant parent;
                if (_table.TryGetValue(node, out parent))
                {
                    parent.RemoveNode(node);
                    _table.Remove(node);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Test whether this index contains the specified item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns><c>true</c> if the index contains the item; <c>false</c> otherwise.</returns>
        public bool Contains(T item)
        {
            return _table != null && _table.ContainsKey(item);
        }

        /// <summary>
        /// Removes all items from the index.
        /// </summary>
        public void Clear()
        {
            _table = null;
            _root = null;
            ReIndex();
        }

        /// <summary>
        /// Get the bounds at which the specified item is currently stored.
        /// </summary>
        public TRectangle this[T item] { get { return _table[item].Bounds; } }

        /// <summary>
        /// Perform a circular query on this index. This will return all entries
        /// in the index that are in the specified range of the specified point,
        /// using the euclidean distance function (i.e. <c>sqrt(x*x+y*y)</c>).
        /// </summary>
        /// <remarks>
        /// This checks for intersections of the query circle and the bounds of
        /// the entries in the index. Intersections (i.e. bounds not fully contained
        /// in the circle) will be returned, too.
        /// </remarks>
        /// <param name="point">The query point near which to get entries.</param>
        /// <param name="range">The maximum distance an entry may be away
        /// from the query point to be returned.</param>
        /// <param name="list">The list to put the results into. It is guaranteed
        /// that there will be no duplicate entries.</param>
        public void Find(TPoint point, float range, ref ICollection<T> list)
        {
            throw new NotSupportedException();
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
            foreach (var entry in GetNodesInside(rectangle))
            {
                list.Add(entry);
            }
        }

        #endregion

        /// 
        /// Insert a node with given bounds into this QuadTree. 
        /// 
        /// The node to insert
        /// The bounds of this node
        public void Insert(T node, TRectangle bounds)
        {
            if (_bounds.Width == 0 || _bounds.Height == 0)
            {
                throw new ArgumentException();
            }
            if (bounds.Width == 0 || bounds.Height == 0)
            {
                throw new ArgumentException();
            }
            if (_root == null)
            {
                _root = new Quadrant(_bounds);
            }

            var parent = _root.Insert(node, bounds);

            if (_table == null)
            {
                _table = new Dictionary<T, Quadrant>();
            }
            _table[node] = parent;
        }

        /// 
        /// Get a list of the nodes that intersect the given bounds. 
        /// 
        /// The bounds to test 
        /// List of zero or mode nodes found inside the given bounds 
        public IEnumerable<T> GetNodesInside(TRectangle bounds)
        {
            foreach (var n in GetNodes(bounds))
            {
                yield return n.Value;
            }
        }

        ///  
        /// Get a list of the nodes that intersect the given bounds.
        ///  
        /// The bounds to test
        /// List of zero or mode nodes found inside the given bounds
        public bool HasNodesInside(TRectangle bounds)
        {
            return _root != null && _root.HasIntersectingNodes(bounds);
        }

        /// 
        /// Get list of nodes that intersect the given bounds. 
        /// 
        /// The bounds to test 
        /// The list of nodes intersecting the given bounds 
        private IEnumerable<QuadNode> GetNodes(TRectangle bounds)
        {
            var result = new List<QuadNode>();
            if (_root != null)
            {
                _root.GetIntersectingNodes(result, bounds);
            }
            return result;
        }

        /// 
        /// Rebuild all the Quadrants according to the current QuadTree Bounds.
        ///  
        private void ReIndex()
        {
            _root = null;
            foreach (var n in GetNodes(_bounds))
            {
                Insert(n.Value, n.Bounds);
            }
        }

        /// 
        /// Each node stored in the tree has a position, width & height. 
        ///  
        internal sealed class QuadNode
        {
            ///  
            /// The Rect bounds of the node
            ///  
            public readonly TRectangle Bounds;

            ///  
            /// The node
            ///  
            public readonly T Value;

            ///  
            /// QuadNodes form a linked list in the Quadrant. 
            /// 
            public QuadNode Next; // linked in a circular list.

            /// 
            /// Construct new QuadNode to wrap the given node with given bounds 
            ///  
            /// The node
            /// The bounds of that node 
            public QuadNode(T value, TRectangle bounds)
            {
                Value = value;
                Bounds = bounds;
            }
        }


        /// 
        /// The canvas is split up into four Quadrants and objects are stored in the quadrant that contains them 
        /// and each quadrant is split up into four child Quadrants recurrsively.  Objects that overlap more than
        /// one quadrant are stored in the this.nodes list for this Quadrant.
        /// 
        internal sealed class Quadrant
        {
            public TRectangle Bounds; // quadrant bounds. 

            private QuadNode _nodes; // nodes that overlap the sub quadrant boundaries. 

            // The quadrant is subdivided when nodes are inserted that are
            // completely contained within those subdivisions.
            private Quadrant _topLeft;

            private Quadrant _topRight;

            private Quadrant _bottomLeft;

            private Quadrant _bottomRight;

            ///  
            /// Construct new Quadrant with a given bounds all nodes stored inside this quadrant 
            /// will fit inside this bounds.
            ///  
            /// The parent quadrant (if any)
            /// The bounds of this quadrant
            public Quadrant(TRectangle bounds)
            {
                Debug.Assert(bounds.Width != 0 && bounds.Height != 0, "Cannot have empty bound");
                if (bounds.Width == 0 || bounds.Height == 0)
                {
                    throw new ArgumentException();
                }
                Bounds = bounds;
            }

            /// 
            /// Insert the given node 
            /// 
            /// The node  
            /// The bounds of that node 
            /// 
            internal Quadrant Insert(T node, TRectangle bounds)
            {
                Debug.Assert(bounds.Width != 0 && bounds.Height != 0, "Cannot have empty bound");
                if (bounds.Width == 0 || bounds.Height == 0)
                {
                    throw new ArgumentException();
                }

                Quadrant toInsert = this;
                while (true)
                {
                    var w = toInsert.Bounds.Width / 2;
                    if (w < 1)
                    {
                        w = 1;
                    }
                    var h = toInsert.Bounds.Height / 2;
                    if (h < 1)
                    {
                        h = 1;
                    }

                    // assumption that the Rect struct is almost as fast as doing the operations
                    // manually since Rect is a value type. 

                    var topLeft = new TRectangle(toInsert.Bounds.Left, toInsert.Bounds.Top, w, h);
                    var topRight = new TRectangle(toInsert.Bounds.Left + w, toInsert.Bounds.Top, w, h);
                    var bottomLeft = new TRectangle(toInsert.Bounds.Left, toInsert.Bounds.Top + h, w, h);
                    var bottomRight = new TRectangle(toInsert.Bounds.Left + w, toInsert.Bounds.Top + h, w, h);

                    Quadrant child = null;

                    // See if any child quadrants completely contain this node. 
                    if (topLeft.Contains(bounds))
                    {
                        if (toInsert._topLeft == null)
                        {
                            toInsert._topLeft = new Quadrant(topLeft);
                        }
                        child = toInsert._topLeft;
                    }
                    else if (topRight.Contains(bounds))
                    {
                        if (toInsert._topRight == null)
                        {
                            toInsert._topRight = new Quadrant(topRight);
                        }
                        child = toInsert._topRight;
                    }
                    else if (bottomLeft.Contains(bounds))
                    {
                        if (toInsert._bottomLeft == null)
                        {
                            toInsert._bottomLeft = new Quadrant(bottomLeft);
                        }
                        child = toInsert._bottomLeft;
                    }
                    else if (bottomRight.Contains(bounds))
                    {
                        if (toInsert._bottomRight == null)
                        {
                            toInsert._bottomRight = new Quadrant(bottomRight);
                        }
                        child = toInsert._bottomRight;
                    }

                    if (child != null)
                    {
                        toInsert = child;
                    }
                    else
                    {
                        var n = new QuadNode(node, bounds);
                        if (toInsert._nodes == null)
                        {
                            n.Next = n;
                        }
                        else
                        {
                            // link up in circular link list.
                            var x = toInsert._nodes;
                            n.Next = x.Next;
                            x.Next = n;
                        }
                        toInsert._nodes = n;
                        return toInsert;
                    }
                }
            }

            /// 
            /// Returns all nodes in this quadrant that intersect the given bounds.
            /// The nodes are returned in pretty much random order as far as the caller is concerned.
            ///  
            /// List of nodes found in the given bounds
            /// The bounds that contains the nodes you want returned 
            internal void GetIntersectingNodes(ICollection<QuadNode> nodes, TRectangle bounds)
            {
                if (bounds.IsEmpty) return;
                var w = Bounds.Width / 2;
                var h = Bounds.Height / 2;

                // assumption that the Rect struct is almost as fast as doing the operations 
                // manually since Rect is a value type.

                var topLeft = new TRectangle(Bounds.Left, Bounds.Top, w, h);
                var topRight = new TRectangle(Bounds.Left + w, Bounds.Top, w, h);
                var bottomLeft = new TRectangle(Bounds.Left, Bounds.Top + h, w, h);
                var bottomRight = new TRectangle(Bounds.Left + w, Bounds.Top + h, w, h);

                // See if any child quadrants completely contain this node.
                if (topLeft.Intersects(bounds) && _topLeft != null)
                {
                    _topLeft.GetIntersectingNodes(nodes, bounds);
                }

                if (topRight.Intersects(bounds) && _topRight != null)
                {
                    _topRight.GetIntersectingNodes(nodes, bounds);
                }

                if (bottomLeft.Intersects(bounds) && _bottomLeft != null)
                {
                    _bottomLeft.GetIntersectingNodes(nodes, bounds);
                }

                if (bottomRight.Intersects(bounds) && _bottomRight != null)
                {
                    _bottomRight.GetIntersectingNodes(nodes, bounds);
                }

                GetIntersectingNodes(this._nodes, nodes, bounds);
            }

            ///  
            /// Walk the given linked list of QuadNodes and check them against the given bounds.
            /// Add all nodes that intersect the bounds in to the list.
            /// 
            /// The last QuadNode in a circularly linked list 
            /// The resulting nodes are added to this list
            /// The bounds to test against each node 
            private static void GetIntersectingNodes(QuadNode last, ICollection<QuadNode> nodes, TRectangle bounds)
            {
                if (last == null)
                {
                    return;
                }
                var n = last;
                do
                {
                    n = n.Next; // first node.
                    if (bounds.Intersects(n.Bounds))
                    {
                        nodes.Add(n);
                    }
                } while (n != last);
            }

            /// 
            /// Return true if there are any nodes in this Quadrant that intersect the given bounds. 
            ///  
            /// The bounds to test
            /// boolean 
            internal bool HasIntersectingNodes(TRectangle bounds)
            {
                if (bounds.IsEmpty) return false;
                var w = Bounds.Width / 2;
                var h = Bounds.Height / 2;

                // assumption that the Rect struct is almost as fast as doing the operations 
                // manually since Rect is a value type.

                var topLeft = new TRectangle(Bounds.Left, Bounds.Top, w, h);
                var topRight = new TRectangle(Bounds.Left + w, Bounds.Top, w, h);
                var bottomLeft = new TRectangle(Bounds.Left, Bounds.Top + h, w, h);
                var bottomRight = new TRectangle(Bounds.Left + w, Bounds.Top + h, w, h);

                var found = false;

                // See if any child quadrants completely contain this node.
                if (topLeft.Intersects(bounds) && _topLeft != null)
                {
                    found = _topLeft.HasIntersectingNodes(bounds);
                }

                if (!found && topRight.Intersects(bounds) && _topRight != null)
                {
                    found = _topRight.HasIntersectingNodes(bounds);
                }

                if (!found && bottomLeft.Intersects(bounds) && _bottomLeft != null)
                {
                    found = _bottomLeft.HasIntersectingNodes(bounds);
                }

                if (!found && bottomRight.Intersects(bounds) && _bottomRight != null)
                {
                    found = _bottomRight.HasIntersectingNodes(bounds);
                }
                if (!found)
                {
                    found = HasIntersectingNodes(_nodes, bounds);
                }
                return found;
            }

            /// 
            /// Walk the given linked list and test each node against the given bounds/ 
            /// 
            /// The last node in the circularly linked list.
            /// Bounds to test
            /// Return true if a node in the list intersects the bounds 
            private static bool HasIntersectingNodes(QuadNode last, TRectangle bounds)
            {
                if (last != null)
                {
                    var n = last;
                    do
                    {
                        n = n.Next; // first node.
                        if (bounds.Intersects(n.Bounds))
                        {
                            return true;
                        }
                    } while (n != last);
                }
                return false;
            }

            ///  
            /// Remove the given node from this Quadrant.
            ///  
            /// The node to remove 
            /// Returns true if the node was found and removed.
            internal void RemoveNode(T node)
            {
                if (_nodes == null)
                {
                    return;
                }
                var p = _nodes;
                while (p.Next.Value.Equals(node) && p.Next != _nodes)
                {
                    p = p.Next;
                }
                if (!p.Next.Value.Equals(node))
                {
                    return;
                }
                var n = p.Next;
                if (p == n)
                {
                    // list goes to empty 
                    _nodes = null;
                }
                else
                {
                    if (_nodes == n) _nodes = p;
                    p.Next = n.Next;
                }
            }
        }

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<Tuple<TRectangle, T>> GetEnumerator()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
