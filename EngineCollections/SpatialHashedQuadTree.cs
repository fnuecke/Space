using System;
using System.Collections;
using System.Collections.Generic;
using Engine.FarMath;
using Engine.Util;
using Microsoft.Xna.Framework;

using TPoint = Engine.FarMath.FarPosition;
using TRectangle = Engine.FarMath.FarRectangle;

namespace Engine.Collections
{
    /// <summary>
    /// This is a two level index structure, using a spatial hash as the primary
    /// structure and working with <seealso cref="FarValue"/>s. It splits areas
    /// into those defined by the segment size of far values and indexes these
    /// areas using quad trees. On that level the index works with normal float
    /// values for better performance.
    /// </summary>
    /// <typeparam name="T">The type to store in the index.</typeparam>
    public sealed class SpatialHashedQuadTree<T> : IIndex<T, TRectangle, TPoint>
    {
        #region Constants

        /// <summary>
        /// Amount by which to oversize entry bounds to allow for small movement
        /// the item without having to update the tree. Idea taken from Box2D.
        /// </summary>
        private const int BoundExtension = 10;

        /// <summary>
        /// Amount by which to oversize entry bounds in the direction they moved
        /// during an update, to predict future movement. Idea taken from Box2D.
        /// </summary>
        private const int MovingBoundMultiplier = 2;

        #endregion

        #region Properties

        /// <summary>
        /// The number of values stored in this index.
        /// </summary>
        public int Count
        {
            get { return _entryBounds.Count; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The max entries per node in quad trees.
        /// </summary>
        private readonly int _maxEntriesPerNode;

        /// <summary>
        /// The min node bounds for nodes in quad trees.
        /// </summary>
        private readonly int _minNodeBounds;

        /// <summary>
        /// The buckets with the quad trees storing the actual entries.
        /// </summary>
        private readonly Dictionary<ulong, QuadTree<T>> _entries = new Dictionary<ulong, QuadTree<T>>();

        /// <summary>
        /// Maps entries back to their bounds, for removal.
        /// </summary>
        private readonly Dictionary<T, TRectangle> _entryBounds = new Dictionary<T, TRectangle>();
        
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialHash&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="maxEntriesPerNode">The max entries per node in quad trees.</param>
        /// <param name="minNodeBounds">The min node bounds for nodes in quad trees.</param>
        public SpatialHashedQuadTree(int maxEntriesPerNode, int minNodeBounds)
        {
            _maxEntriesPerNode = maxEntriesPerNode;
            _minNodeBounds = minNodeBounds;
        }

        #endregion

        #region Implementation of IIndex<T,FarRectangle,FarPosition>

        /// <summary>
        /// Add a new item to the index, with the specified bounds.
        /// </summary>
        /// <param name="bounds">The bounds of the item.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="T:System.ArgumentException">
        /// The item is already stored in the index.
        /// </exception>
        public void Add(FarRectangle bounds, T item)
        {
            if (Contains(item))
            {
                throw new ArgumentException("Entry is already in the index.", "item");
            }

            // Extend bounds.
            bounds.Inflate(BoundExtension, BoundExtension);

            // Add to each cell the element's bounds intersect with.
            foreach (var cell in ComputeCells(bounds))
            {
                // Create the quad tree for that cell if it doesn't yet exist.
                if (!_entries.ContainsKey(cell.Item1))
                {
                    _entries.Add(cell.Item1, new QuadTree<T>(_maxEntriesPerNode, _minNodeBounds, false));
                }

                // Convert the item bounds to the tree's local coordinate space.
                var relativeBounds = bounds;
                relativeBounds.Offset(cell.Item2);

                // And add the item to the tree.
                _entries[cell.Item1].Add((Rectangle)relativeBounds, item);
            }

            // Store element itself for future retrieval (removals, item lookup).
            _entryBounds.Add(item, bounds);
        }

        /// <summary>
        /// Update an entry by changing its bounds. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <param name="newBounds">The new bounds of the item.</param>
        /// <param name="delta">The amount by which the object moved.</param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns>
        ///   <c>true</c> if the update was successful; <c>false</c> otherwise.
        /// </returns>
        public bool Update(FarRectangle newBounds, Vector2 delta, T item)
        {
            // Check if we have that entry, if not add it.
            if (!Contains(item))
            {
                return false;
            }

            // Get the old bounds.
            var oldBounds = _entryBounds[item];

            // Nothing to do if our approximation in the tree still contains the item.
            if (oldBounds.Contains(newBounds))
            {
                return true;
            }

            // Estimate movement by bounds delta to predict position and
            // extend the bounds accordingly, to avoid tree updates.
            delta.X *= MovingBoundMultiplier;
            delta.Y *= MovingBoundMultiplier;
            var absDeltaX = delta.X < 0 ? -delta.X : delta.X;
            var absDeltaY = delta.Y < 0 ? -delta.Y : delta.Y;
            newBounds.Width += (int)absDeltaX;
            if (delta.X < 0)
            {
                newBounds.X += (int)delta.X;
            }
            newBounds.Height += (int)absDeltaY;
            if (delta.Y < 0)
            {
                newBounds.Y += (int)delta.Y;
            }

            // Extend bounds.
            newBounds.Inflate(BoundExtension, BoundExtension);

            // Figure out what changed (the delta in cells).
            var oldCells = new HashSet<Tuple<ulong, TPoint>>(ComputeCells(oldBounds));
            var newCells = new HashSet<Tuple<ulong, TPoint>>(ComputeCells(newBounds));

            // Get all cells that the entry no longer is in.
            var removedCells = new HashSet<Tuple<ulong, TPoint>>(oldCells);
            removedCells.ExceptWith(newCells);
            foreach (var cell in removedCells)
            {
                // Remove from the tree.
                _entries[cell.Item1].Remove(item);

                // Clean up: remove the tree if it's empty.
                if (_entries[cell.Item1].Count == 0)
                {
                    _entries.Remove(cell.Item1);
                }
            }

            // Get all the cells the entry now is in.
            var addedCells = new HashSet<Tuple<ulong, TPoint>>(newCells);
            addedCells.ExceptWith(oldCells);
            foreach (var cell in addedCells)
            {
                // Create the quad tree for that cell if it doesn't yet exist.
                if (!_entries.ContainsKey(cell.Item1))
                {
                    _entries.Add(cell.Item1, new QuadTree<T>(_maxEntriesPerNode, _minNodeBounds, false));
                }

                // Convert the item bounds to the tree's local coordinate space.
                var relativeBounds = newBounds;
                relativeBounds.Offset(cell.Item2);

                // And add the item to the tree.
                _entries[cell.Item1].Add((Rectangle)relativeBounds, item);
            }

            // Get all cells the entry still is in.
            oldCells.ExceptWith(addedCells);
            oldCells.ExceptWith(removedCells);
            foreach (var cell in oldCells)
            {
                // Convert the item bounds to the tree's local coordinate space.
                var relativeBounds = newBounds;
                relativeBounds.Offset(cell.Item2);

                // And update the item to the tree.
                _entries[cell.Item1].Update((Rectangle)relativeBounds, Vector2.Zero, item);
            }

            // Store the new item bounds.
            _entryBounds[item] = newBounds;

            return true;
        }

        /// <summary>
        /// Remove the specified item from the index. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>
        ///   <c>true</c> if the item was removed; <c>false</c> otherwise.
        /// </returns>
        public bool Remove(T item)
        {
            // See if we have that entry.
            if (!Contains(item))
            {
                return false;
            }

            // Remove the entry from each tree it is part of, according to
            // its current bounds.
            foreach (var cell in ComputeCells(_entryBounds[item]))
            {
                // Remove from the tree.
                _entries[cell.Item1].Remove(item);

                // Clean up: remove the tree if it's empty.
                if (_entries[cell.Item1].Count == 0)
                {
                    _entries.Remove(cell.Item1);
                }
            }

            // Forget bounds for this item, thus removing it from the index.
            _entryBounds.Remove(item);

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
            return _entryBounds.ContainsKey(item);
        }

        /// <summary>
        /// Removes all items from the index.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _entryBounds.Clear();
        }

        /// <summary>
        /// Get the bounds at which the specified item is currently stored.
        /// </summary>
        public FarRectangle this[T item]
        {
            get { return _entryBounds[item]; }
        }

        /// <summary>
        /// Perform a circular query on this index. This will return all entries
        /// in the index that are in the specified range of the specified point,
        /// using the euclidean distance function (i.e. <c>sqrt(x*x+y*y)</c>).
        /// </summary>
        /// <param name="point">The query point near which to get entries.</param>
        /// <param name="range">The maximum distance an entry may be away
        ///   from the query point to be returned.</param>
        /// <param name="results"> </param>
        /// <remarks>
        /// This checks for intersections of the query circle and the bounds of
        /// the entries in the index. Intersections (i.e. bounds not fully contained
        /// in the circle) will be returned, too.
        /// </remarks>
        public void Find(FarPosition point, float range, ref ISet<T> results)
        {
            // Compute the area bounds for that query to get the involved trees.
            var bounds = new TRectangle
            {
                X = point.X - range,
                Y = point.Y - range,
                Width = range + range,
                Height = range + range
            };

            foreach (var cell in ComputeCells(bounds))
            {
                // Only if the cell exists.
                if (_entries.ContainsKey(cell.Item1))
                {
                    // Convert the query to the tree's local coordinate space.
                    var relativePoint = (Vector2)(point + cell.Item2);

                    // And do the query.
                    _entries[cell.Item1].Find(relativePoint, range, ref results);
                }
            }
        }

        /// <summary>
        /// Perform an area query on this index. This will return all entries
        /// in the tree that are contained in or intersecting with the specified
        /// query rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="results"> </param>
        public void Find(ref FarRectangle rectangle, ref ISet<T> results)
        {
            foreach (var cell in ComputeCells(rectangle))
            {
                if (_entries.ContainsKey(cell.Item1))
                {
                    // Convert the query bounds to the tree's local coordinate space.
                    var relativeFarBounds = rectangle;
                    relativeFarBounds.Offset(cell.Item2);

                    // And do the query.
                    var relativeBounds = (Rectangle)relativeFarBounds;
                    _entries[cell.Item1].Find(ref relativeBounds, ref results);
                }
            }
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Computes the cells the specified rectangle falls into.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns>The cells the rectangle intersects with.</returns>
        private static IEnumerable<Tuple<ulong, TPoint>> ComputeCells(TRectangle rectangle)
        {
            var left = rectangle.X.Segment;
            var right = rectangle.Right.Segment;
            var top = rectangle.Y.Segment;
            var bottom = rectangle.Bottom.Segment;

            TPoint center;
            for (var x = left; x <= right; x++)
            {
                center.X = -x * FarValue.SegmentSize;
                for (var y = top; y <= bottom; y++)
                {
                    center.Y = -y * FarValue.SegmentSize;
                    yield return Tuple.Create(BitwiseMagic.Pack(x, y), center);
                }
            }
        }

        #endregion

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<Tuple<FarRectangle, T>> GetEnumerator()
        {
            foreach (var entry in _entryBounds)
            {
                yield return Tuple.Create(entry.Value, entry.Key);
            }
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
        
        /// <summary>
        /// A utility enumerator allowing the iteration over all trees in the
        /// index. This also yields the position for each tree.
        /// </summary>
        /// <returns>
        /// An enumerator over all trees in the index.
        /// </returns>
        /// <remarks>
        /// This is mainly intended for debugging purposes, to allow rendering
        /// the tree bounds.
        /// </remarks>
        public IEnumerable<Tuple<TPoint, QuadTree<T>>> GetTreeEnumerable()
        {
            foreach (var entry in _entries)
            {
                int segmentX, segmentY;
                BitwiseMagic.Unpack(entry.Key, out segmentX, out segmentY);

                FarPosition center;
                center.X = segmentX * FarValue.SegmentSize;
                center.Y = segmentY * FarValue.SegmentSize;
                yield return Tuple.Create(center, entry.Value);
            }
        }

        #endregion
    }
}
