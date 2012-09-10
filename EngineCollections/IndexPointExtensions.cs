using System.Collections.Generic;
using Microsoft.Xna.Framework;

// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
#if FARMATH
using Engine.Collections;
using TPoint = Engine.FarMath.FarPosition;
using TSingle = Engine.FarMath.FarValue;
using TRectangle = Engine.FarMath.FarRectangle;
#else
using TPoint = Microsoft.Xna.Framework.Vector2;
using TSingle = System.Single;
using TRectangle = Engine.Math.RectangleF;
#endif

#if FARMATH
namespace Engine.FarCollections
#else
namespace Engine.Collections
#endif
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
        public static void Add<T>(this IIndex<T, TRectangle, TPoint> index, TPoint point, T item)
        {
            // Convert to rectangle, then add that.
            TRectangle bounds;
            bounds.X = point.X;
            bounds.Y = point.Y;
            bounds.Width = bounds.Height = 0;
            index.Add(bounds, item);
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
        public static bool Update<T>(this IIndex<T, TRectangle, TPoint> index, TPoint newPoint, T item)
        {
            // Convert to rectangle, then update with that.
            TRectangle bounds;
            bounds.X = newPoint.X;
            bounds.Y = newPoint.Y;
            bounds.Width = bounds.Height = 0;
            return index.Update(bounds, Vector2.Zero, item);
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
        public static void Find<T>(this IIndex<T, TRectangle, TPoint> index, Vector2 point, float range, ref ISet<T> list)
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
