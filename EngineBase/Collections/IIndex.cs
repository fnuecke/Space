using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Engine.Collections
{
    /// <summary>
    /// Interface for index structures.
    /// </summary>
    /// <typeparam name="T">The type of the values stored in this index.</typeparam>
    public interface IIndex<T> : IEnumerable<T>
    {
        #region Properties

        /// <summary>
        /// The number of values stored in this tree.
        /// </summary>
        int Count { get; }

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
        void Add(ref Vector2 point, T value);

        /// <summary>
        /// Add a new entry to the tree, at the specified position, with the
        /// specified associated value.
        /// </summary>
        /// <param name="point">The point at which to store the entry.</param>
        /// <param name="value">The value associated with the point.</param>
        /// <exception cref="ArgumentException">This pair of point and value
        /// are already stored in the tree.</exception>
        void Add(Vector2 point, T value);

        /// <summary>
        /// Update a single entry by changing its position. If the entry is not
        /// already in the tree, it will be added.
        /// </summary>
        /// <param name="newPoint">The new position of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <exception cref="ArgumentException">If there is no such value in
        /// the tree at the specified old position.</exception>
        void Update(ref Vector2 newPoint, T value);

        /// <summary>
        /// Update a single entry by changing its position. If the entry is not
        /// already in the tree, it will be added.
        /// </summary>
        /// <param name="newPoint">The new position of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <exception cref="ArgumentException">If there is no such value in
        /// the tree at the specified old position.</exception>
        void Update(Vector2 newPoint, T value);

        /// <summary>
        /// Remove the specified value from the tree.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        bool Remove(T value);

        /// <summary>
        /// Test whether this tree contains the specified value at the
        /// specified point.
        /// </summary>
        /// <param name="point">The point at which to look for.</param>
        /// <param name="value">The value to look for.</param>
        /// <returns><c>true</c> if the tree contains the value at the
        /// specified point.</returns>
        bool Contains(ref Vector2 point, T value);

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
        ICollection<T> RangeQuery(ref Vector2 point, float range, ICollection<T> list = null);

        /// <summary>
        /// Perform a range query on this tree. This will return all entries
        /// in the tree that are in the specified range to the specified point,
        /// using a euclidean distance.
        /// </summary>
        /// <param name="point">The query point near which to get entries.</param>
        /// <param name="range">The maximum distance an entry may be away
        /// from the query point to be returned.</param>
        /// <returns></returns>
        ICollection<T> RangeQuery(Vector2 point, float range);

        /// <summary>
        /// Perform a range query on this tree. This will return all entries
        /// in the tree that are in contained the specified rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="list">The list to put the results into, or null in
        /// which case a new list will be created and returned.</param>
        /// <returns>All objects in the query rectangle.</returns>
        ICollection<T> RangeQuery(ref Rectangle rectangle, ICollection<T> list = null);

        /// <summary>
        /// Perform a range query on this tree. This will return all entries
        /// in the tree that are in contained the specified rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <returns>All objects in the query rectangle.</returns>
        ICollection<T> RangeQuery(Rectangle rectangle);

        #endregion
    }
}
