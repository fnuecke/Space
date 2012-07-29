using System.Collections.Generic;

// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
using TPoint = Microsoft.Xna.Framework.Point;
using TRectangle = Microsoft.Xna.Framework.Rectangle;

namespace Engine.Collections
{
    /// <summary>
    /// Seamless integration for point data.
    /// </summary>
    public static class IndexPointExtensions
    {
        /// <summary>
        /// Add a new item to the index, with the specified position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index">The index to add to.</param>
        /// <param name="point">The position of the item.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="T:System.ArgumentException">
        /// The item is already stored in the index.
        ///   </exception>
        /// <remarks>
        /// This will lead to the point being converted to an empty
        /// rectangle at the point's position, which will then be
        /// inserted, instead.
        /// </remarks>
        public static void Add<T>(this IIndex<T> index, TPoint point, T item)
        {
            // Convert to rectangle, then add that.
            TRectangle bounds;
            bounds.X = point.X;
            bounds.Y = point.Y;
            bounds.Width = bounds.Height = 0;
            index.Add(bounds, item);
        }

        /// <summary>
        /// Add a new item to the index, with the specified position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index">The index to add to.</param>
        /// <param name="point">The position of the item.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="T:System.ArgumentException">
        /// The item is already stored in the index.
        ///   </exception>
        /// <remarks>
        /// In most implementations this will lead to the point being
        /// converted to an empty rectangle at the point's position,
        /// which will then be inserted, instead.
        /// </remarks>
        public static void Add<T>(this IIndex<T> index, Microsoft.Xna.Framework.Vector2 point, T item)
        {
            // Convert to integer point type, then add that.
            TPoint p;
            p.X = (int)point.X;
            p.Y = (int)point.Y;
            index.Add(p, item);
        }

        /// <summary>
        /// Update an entry by changing its position. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index">The index to update.</param>
        /// <param name="newPoint">The new position of the item.</param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns>
        ///   <c>true</c> if the update was successful; <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        /// This will lead to the point being converted to an empty rectangle
        /// at the point's position, which will then be used, instead.
        /// </remarks>
        public static bool Update<T>(this IIndex<T> index, TPoint newPoint, T item)
        {
            // Convert to rectangle, then update with that.
            TRectangle bounds;
            bounds.X = newPoint.X;
            bounds.Y = newPoint.Y;
            bounds.Width = bounds.Height = 0;
            return index.Update(bounds, item);
        }

        /// <summary>
        /// Update an entry by changing its position. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index">The index to update.</param>
        /// <param name="newPoint">The new position of the item.</param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns>
        ///   <c>true</c> if the update was successful; <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        /// This will lead to the point being converted to an empty rectangle
        /// at the point's position, which will then be used, instead.
        /// </remarks>
        public static bool Update<T>(this IIndex<T> index, Microsoft.Xna.Framework.Vector2 newPoint, T item)
        {
            // Convert to integer point type.
            TPoint p;
            p.X = (int)newPoint.X;
            p.Y = (int)newPoint.Y;

            // Perform actual update.
            return index.Update(p, item);
        }

        /// <summary>
        /// Similar to <see cref="Update{T}(IIndex{T}, TPoint, T)"/> this changes an
        /// entry's bounds. Unlike <see cref="Update{T}(IIndex{T}, TPoint, T)"/>, however,
        /// this just moves the bounds to the specified location without the
        /// option to change their size. The specified position is used as the
        /// new center for the bounds.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index">The index to update.</param>
        /// <param name="position">The new position of the item.</param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns>
        ///   <c>true</c> if the update was successful; <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        /// This is purely a helper method, it will compute the new bounds and
        /// then call <see cref="Update{T}(IIndex{T}, TPoint, T)"/> internally.
        /// </remarks>
        public static bool Move<T>(this IIndex<T> index, TPoint position, T item)
        {
            // Check if we have that item.
            if (!index.Contains(item))
            {
                // No, nothing to do, then.
                return false;
            }

            // Get the old bounds.
            var bounds = index[item];

            // Compute the new bounds.
            bounds.X = position.X - bounds.Width / 2;
            bounds.Y = position.Y - bounds.Height / 2;

            // Update tree.
            index.Update(bounds, item);

            // We had the entry, so return true.
            return true;
        }

        /// <summary>
        /// Similar to <see cref="Update{T}(IIndex{T}, TPoint, T)"/> this changes an
        /// entry's bounds. Unlike <see cref="Update{T}(IIndex{T}, TPoint, T)"/>, however,
        /// this just moves the bounds to the specified location without the
        /// option to change their size. The specified position is used as the
        /// new center for the bounds.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index">The index to update.</param>
        /// <param name="position">The new position of the item.</param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns>
        ///   <c>true</c> if the update was successful; <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        /// This is purely a helper method, it will compute the new bounds and
        /// then call <see cref="Update{T}(IIndex{T}, TPoint, T)"/> internally.
        /// </remarks>
        public static bool Move<T>(this IIndex<T> index, Microsoft.Xna.Framework.Vector2 position, T item)
        {
            // Convert to integer point type.
            TPoint p;
            p.X = (int)position.X;
            p.Y = (int)position.Y;

            // Perform actual move.
            return index.Move(p, item);
        }

        /// <summary>
        /// Perform a circular query on this index. This will return all entries
        /// in the index that are in the specified range of the specified point,
        /// using the euclidean distance function (i.e. <c>sqrt(x*x+y*y)</c>).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index">The index to search in.</param>
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
        public static void Find<T>(this IIndex<T> index, Microsoft.Xna.Framework.Vector2 point, float range, ref ICollection<T> list)
        {
            // Convert to integer point type.
            TPoint p;
            p.X = (int)point.X;
            p.Y = (int)point.Y;

            // Perform actual search.
            index.Find(p, range, ref list);
        }
    }
}
