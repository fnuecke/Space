using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Engine.Collections
{
    /// <summary>
    /// Interface for index structures providing options for faster neighbor
    /// search.
    /// </summary>
    /// <typeparam name="T">The type of the values stored in the index.</typeparam>
    /// <typeparam name="TRectangle">The type of the rectangles used.</typeparam>
    /// <typeparam name="TPoint">The type of the points used.</typeparam>
    public interface IIndex<T, TRectangle, TPoint> : IEnumerable<Tuple<TRectangle, T>>
    {
        #region Properties

        /// <summary>
        /// The number of values stored in this index.
        /// </summary>
        int Count { get; }

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
        void Add(TRectangle bounds, T item);

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
        bool Update(TRectangle newBounds, Vector2 delta, T item);

        /// <summary>
        /// Remove the specified item from the index. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>
        ///   <c>true</c> if the item was removed; <c>false</c> otherwise.
        /// </returns>
        bool Remove(T item);

        /// <summary>
        /// Test whether this index contains the specified item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>
        ///   <c>true</c> if the index contains the item; <c>false</c> otherwise.
        /// </returns>
        bool Contains(T item);

        /// <summary>
        /// Removes all items from the index.
        /// </summary>
        void Clear();

        /// <summary>
        /// Get the bounds at which the specified item is currently stored.
        /// </summary>
        TRectangle this[T item] { get; }

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
        void Find(TPoint point, float range, ref ICollection<T> list);

        /// <summary>
        /// Perform an area query on this index. This will return all entries
        /// in the tree that are contained in or intersecting with the specified
        /// query rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="list">The list to put the results into. It is guaranteed
        /// that there will be no duplicate entries.</param>
        void Find(ref TRectangle rectangle, ref ICollection<T> list);

        #endregion
    }
}
