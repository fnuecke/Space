using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Engine.Collections
{
    /// <summary>
    /// Interface for index structures.
    /// </summary>
    /// <typeparam name="T">The type of the values stored in this index.</typeparam>
    public interface IIndex<T> : IEnumerable<Tuple<Rectangle, T>>
    {
        #region Properties

        /// <summary>
        /// The number of values stored in this tree.
        /// </summary>
        int Count { get; }

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
        void Add(ref Rectangle bounds, T value);

        /// <summary>
        /// Add a new entry to the tree, at the specified position, with the
        /// specified associated value.
        /// </summary>
        /// <param name="point">The point at which to store the entry.</param>
        /// <param name="value">The value associated with the point.</param>
        /// <exception cref="ArgumentException">This value is already stored
        /// in the tree.</exception>
        void Add(Vector2 point, T value);

        /// <summary>
        /// Update a single entry by changing its bounds. If the entry is not
        /// already in the tree, this will return <code>false</code>.
        /// </summary>
        /// <param name="newBounds">The new bounds of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <returns><code>true</code> if the update was successful.</returns>
        bool Update(ref Rectangle newBounds, T value);

        /// <summary>
        /// Update a single entry by changing its position. If the entry is not
        /// already in the tree, this will return <code>false</code>.
        /// </summary>
        /// <param name="newPoint">The new position of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <returns><code>true</code> if the update was successful.</returns>
        bool Update(Vector2 newPoint, T value);

        /// <summary>
        /// Similar to <code>Update</code> this changes an entry's bounds. Unlike
        /// <code>Update</code>, however, this just moves the bounds to the
        /// specified location. The specified position is used as the new center
        /// for the bounds.
        /// </summary>
        /// <param name="position">The new position of the bounds.</param>
        /// <param name="value">The entry for which to update the bounds.</param>
        /// <returns></returns>
        bool Move(Vector2 position, T value);

        /// <summary>
        /// Remove the specified value from the tree.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        bool Remove(T value);

        /// <summary>
        /// Test whether this tree contains the specified value.
        /// </summary>
        /// <param name="value">The value to look for.</param>
        /// <returns><c>true</c> if the tree contains the value at the
        /// specified point.</returns>
        bool Contains(T value);

        /// <summary>
        /// Removes all entries from this tree.
        /// </summary>
        void Clear();

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
        void Find(Vector2 point, float range, ref ICollection<T> list);

        /// <summary>
        /// Perform an area query on this tree. This will return all entries
        /// in the tree that are in contained the specified rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="list">The list to put the results into, or null in
        /// which case a new list will be created and returned.</param>
        void Find(ref Rectangle rectangle, ref ICollection<T> list);

        #endregion
    }
}
