using System;
using System.Collections.Generic;

// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
using TRectangle = Microsoft.Xna.Framework.Rectangle;
using TVector = Microsoft.Xna.Framework.Vector2;

namespace Engine.Collections
{
    /// <summary>
    /// Interface for index structures providing options for faster neighbor
    /// search.
    /// </summary>
    /// <typeparam name="T">The type of the values stored in the index.</typeparam>
    public interface IIndex<T> : IEnumerable<Tuple<TRectangle, T>>
    {
        #region Properties

        /// <summary>
        /// The number of values stored in this tree.
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
        void Add(ref TRectangle bounds, T item);

        /// <summary>
        /// Add a new item to the index, with the specified position.
        /// </summary>
        /// <remarks>
        /// In most implementations this will lead to the point being
        /// converted to an empty rectangle at the point's position,
        /// which will then be inserted, instead.
        /// </remarks>
        /// <param name="point">The position of the item.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="T:System.ArgumentException">
        /// The item is already stored in the index.
        /// </exception>
        void Add(TVector point, T item);

        /// <summary>
        /// Update an entry by changing its bounds. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <param name="newBounds">The new bounds of the item.</param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns><c>true</c> if the update was successful; <c>false</c> otherwise.</returns>
        bool Update(ref TRectangle newBounds, T item);

        /// <summary>
        /// Update an entry by changing its position. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <remarks>
        /// In most implementations this will lead to the point being
        /// converted to an empty rectangle at the point's position,
        /// which will then be used, instead.
        /// </remarks>
        /// <param name="newPoint">The new position of the item.</param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns><c>true</c> if the update was successful; <c>false</c> otherwise.</returns>
        bool Update(TVector newPoint, T item);

        /// <summary>
        /// Similar to <see cref="Update(TVector, T)"/> this changes an
        /// entry's bounds. Unlike <see cref="Update(TVector, T)"/>, however,
        /// this just moves the bounds to the specified location without the
        /// option to change their size. The specified position is used as the
        /// new center for the bounds.
        /// </summary>
        /// <remarks>
        /// This is purely a helper method, it will compute the new bounds and
        /// then call <see cref="Update(TVector, T)"/> internally, in most
        /// implementations.
        /// </remarks>
        /// <param name="position">The new position of the item.</param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns><c>true</c> if the update was successful; <c>false</c> otherwise.</returns>
        bool Move(TVector position, T item);

        /// <summary>
        /// Remove the specified item from the index. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns><c>true</c> if the item was removed; <c>false</c> otherwise.</returns>
        bool Remove(T item);

        /// <summary>
        /// Test whether this index contains the specified item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns><c>true</c> if the index contains the item; <c>false</c> otherwise.</returns>
        bool Contains(T item);

        /// <summary>
        /// Removes all items from the index.
        /// </summary>
        void Clear();

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
        void Find(TVector point, float range, ref ICollection<T> list);

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
