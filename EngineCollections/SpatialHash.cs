using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Util;
using Microsoft.Xna.Framework;

// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
using TPoint = Engine.FarMath.FarPosition;
using TRectangle = Engine.FarMath.FarRectangle;

namespace Engine.Collections
{
    /// <summary>
    /// Implements a spatial hash, storing entries in buckets determined by
    /// a hash based on the coordinates of the entry. This only works well
    /// when all entries are not vastly different in size, and an appropriate
    /// cell size is chosen.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class SpatialHash<T> : IIndex<T, TRectangle, TPoint>
    {
        #region Properties

        /// <summary>
        /// The number of values stored in this tree.
        /// </summary>
        public int Count
        {
            get { return _entryBounds.Count; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Size of a single cell.
        /// </summary>
        private readonly int _cellSize;

        /// <summary>
        /// Actual hash map mapping cells to lists of entries inside.
        /// </summary>
        private readonly Dictionary<ulong, List<T>> _entries = new Dictionary<ulong, List<T>>();

        /// <summary>
        /// Maps entries back to their bounds, for removal.
        /// </summary>
        private readonly Dictionary<T, TRectangle> _entryBounds = new Dictionary<T, TRectangle>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialHash&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="cellSize">Size of a single cell.</param>
        public SpatialHash(int cellSize)
        {
            Debug.Assert(cellSize > 0);
            _cellSize = cellSize;
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
                throw new ArgumentException("Entry is already in the index.", "item");
            }

            foreach (var cell in ComputeCells(bounds))
            {
                if (!_entries.ContainsKey(cell))
                {
                    _entries.Add(cell, new List<T>());
                }
                _entries[cell].Add(item);
            }
            _entryBounds.Add(item, bounds);
        }

        /// <summary>
        /// Update a single entry by changing its bounds. If the entry is not
        /// already in the tree, this will return <code>false</code>.
        /// </summary>
        /// <param name="newBounds">The new bounds of the entry.</param>
        /// <param name="delta"> </param>
        /// <param name="item">The value of the entry.</param>
        /// <returns><code>true</code> if the update was successful.</returns>
        public bool Update(TRectangle newBounds, Vector2 delta, T item)
        {
            // Check if we have that entry, if not add it.
            if (!Contains(item))
            {
                return false;
            }

            // Figure out what changed (the delta).
            var oldCells = new HashSet<ulong>(ComputeCells(_entryBounds[item]));
            var newCells = new HashSet<ulong>(ComputeCells(newBounds));

            // Get all cells that the entry no longer is in.
            var removedCells = new HashSet<ulong>(oldCells);
            removedCells.ExceptWith(newCells);
            foreach (var cell in removedCells)
            {
                _entries[cell].Remove(item);
                if (_entries[cell].Count == 0)
                {
                    _entries.Remove(cell);
                }
            }

            // Get all the cells the entry now is in.
            newCells.ExceptWith(oldCells);
            foreach (var cell in newCells)
            {
                if (!_entries.ContainsKey(cell))
                {
                    _entries.Add(cell, new List<T>());
                }
                _entries[cell].Add(item);
            }

            _entryBounds[item] = newBounds;

            return true;
        }

        /// <summary>
        /// Remove the specified value from the tree.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        public bool Remove(T item)
        {
            // See if we have that entry.
            if (!Contains(item))
            {
                return false;
            }

            foreach (var cell in ComputeCells(_entryBounds[item]))
            {
                _entries[cell].Remove(item);
                if (_entries[cell].Count == 0)
                {
                    _entries.Remove(cell);
                }
            }
            _entryBounds.Remove(item);

            return true;
        }

        /// <summary>
        /// Test whether this tree contains the specified value.
        /// </summary>
        /// <param name="item">The value to look for.</param>
        /// <returns><c>true</c> if the tree contains the value at the
        /// specified point.</returns>
        public bool Contains(T item)
        {
            return _entryBounds.ContainsKey(item);
        }

        /// <summary>
        /// Removes all entries from this tree.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _entryBounds.Clear();
        }

        /// <summary>
        /// Get the bounds at which the specified item is currently stored.
        /// </summary>
        public TRectangle this[T item]
        {
            get { return _entryBounds[item]; }
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
        public void Find(TPoint point, float range, ref ICollection<T> list)
        {
            var bounds = new TRectangle
                         {
                             X = (int)(point.X - range),
                             Y = (int)(point.Y - range),
                             Width = (int)(range + range),
                             Height = (int)(range + range)
                         };
            Find(ref bounds, ref list);
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
            var added = new HashSet<T>();
            foreach (var cell in ComputeCells(rectangle))
            {
                if (_entries.ContainsKey(cell))
                {
                    foreach (var entry in _entries[cell])
                    {
                        if (!added.Contains(entry))
                        {
                            added.Add(entry);
                            list.Add(entry);
                        }
                    }
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
        private IEnumerable<ulong> ComputeCells(TRectangle rectangle)
        {
            var left = rectangle.X / _cellSize;
            var right = rectangle.Right / _cellSize;
            var top = rectangle.Y / _cellSize;
            var bottom = rectangle.Bottom / _cellSize;

            for (var x = (int)left; x <= right; x++)
            {
                for (var y = (int)top; y <= bottom; y++)
                {
                    yield return BitwiseMagic.Pack(x.GetHashCode(), y.GetHashCode());
                }
            }
        }

        #endregion

        #region Enumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<Tuple<TRectangle, T>> GetEnumerator()
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

        #endregion
    }
}
