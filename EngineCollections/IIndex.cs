using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Engine.Collections
{
    /// <summary>
    ///     A callback method for circle and area index queries. Callbacks will be executed for each found hit, and search
    ///     will continue based on the returned value.
    /// </summary>
    /// <typeparam name="T">The type of value stored in the index.</typeparam>
    /// <param name="value">The value that the query found a hit for.</param>
    /// <returns>
    ///     Whether the search should continue (<c>true</c>) or not (<c>false</c>).
    /// </returns>
    public delegate bool SimpleQueryCallback<in T>(T value);

    /// <summary>
    ///     A callback method for line index queries. Callbacks will be executed  for each found hit, and search will
    ///     continue based on the returned value. The line callback method may change the fraction value, to adjust the search
    ///     space. - If a negative value is returned, the result is ignored and the maximum fraction will not change. - If zero
    ///     is returned the search is stopped. - Otherwise the returned value is taken as the new maximum fraction, i.e. hits
    ///     further away will be ignored.
    /// </summary>
    /// <typeparam name="T">The type of value stored in the index.</typeparam>
    /// <param name="value">The value that the query found a hit for.</param>
    /// <param name="fraction">The fraction to allow future hits in.</param>
    /// <returns>The new maximum fraction up to which to search. If set to less or equal to zero the search will be stopped.</returns>
    public delegate float LineQueryCallback<in T>(T value, float fraction);

    /// <summary>Interface for index structures providing options for faster neighbor search.</summary>
    /// <typeparam name="T">The type of the values stored in the index.</typeparam>
    /// <typeparam name="TRectangle">The type of the rectangles used.</typeparam>
    /// <typeparam name="TPoint">The type of the points used.</typeparam>
    public interface IIndex<T, TRectangle, in TPoint> : IEnumerable<Tuple<TRectangle, T>>
    {
        #region Properties

        /// <summary>The number of values stored in this index.</summary>
        int Count { get; }

        #endregion

        #region Accessors

        /// <summary>Add a new item to the index, with the specified bounds.</summary>
        /// <param name="bounds">The bounds of the item.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="T:System.ArgumentException">The item is already stored in the index.</exception>
        void Add(TRectangle bounds, T item);

        /// <summary>
        ///     Update an entry by changing its bounds. If the item is not stored in the index, this will return <code>false</code>
        ///     .
        /// </summary>
        /// <param name="newBounds">The new bounds of the item.</param>
        /// <param name="delta">The amount by which the object moved.</param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns>
        ///     <c>true</c> if the update was successful; <c>false</c> otherwise.
        /// </returns>
        bool Update(TRectangle newBounds, Vector2 delta, T item);

        /// <summary>
        ///     Remove the specified item from the index. If the item is not stored in the index, this will return
        ///     <code>false</code>.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>
        ///     <c>true</c> if the item was removed; <c>false</c> otherwise.
        /// </returns>
        bool Remove(T item);

        /// <summary>Test whether this index contains the specified item.</summary>
        /// <param name="item">The item to check.</param>
        /// <returns>
        ///     <c>true</c> if the index contains the item; <c>false</c> otherwise.
        /// </returns>
        bool Contains(T item);

        /// <summary>Removes all items from the index.</summary>
        void Clear();

        /// <summary>Get the bounds at which the specified item is currently stored.</summary>
        TRectangle this[T item] { get; }

        /// <summary>
        ///     Perform a circular query on this index. This will return all entries in the index that are in the specified range
        ///     of the specified point, using the euclidean distance function (i.e. <c>sqrt(x*x+y*y)</c>).
        /// </summary>
        /// <param name="center">The query point near which to get entries.</param>
        /// <param name="radius">The maximum distance an entry may be away from the query point to be returned.</param>
        /// <param name="results">The list to put the results into.</param>
        /// <remarks>
        ///     This checks for intersections of the query circle and the bounds of the entries in the index. Intersections
        ///     (i.e. bounds not fully contained in the circle) will be returned, too.
        /// </remarks>
        void Find(TPoint center, float radius, ISet<T> results);

        /// <summary>
        ///     Perform a circular query on this index. This will return all entries in the index that are in the specified range
        ///     of the specified point, using the euclidean distance function (i.e. <c>sqrt(x*x+y*y)</c>).
        /// </summary>
        /// <param name="center">The query point near which to get entries.</param>
        /// <param name="radius">The maximum distance an entry may be away from the query point to be returned.</param>
        /// <param name="callback">The method to call for each found hit.</param>
        /// <returns></returns>
        /// <remarks>
        ///     This checks for intersections of the query circle and the bounds of the entries in the index. Intersections
        ///     (i.e. bounds not fully contained in the circle) will be returned, too.
        /// </remarks>
        bool Find(TPoint center, float radius, SimpleQueryCallback<T> callback);

        /// <summary>
        ///     Perform an area query on this index. This will return all entries in the index that are contained in or
        ///     intersecting with the specified query rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="results">The list to put the results into.</param>
        void Find(TRectangle rectangle, ISet<T> results);

        /// <summary>
        ///     Perform an area query on this index. This will return all entries in the index that are contained in or
        ///     intersecting with the specified query rectangle.
        /// </summary>
        /// <param name="rectangle">The query rectangle.</param>
        /// <param name="callback">The method to call for each found hit.</param>
        /// <returns></returns>
        bool Find(TRectangle rectangle, SimpleQueryCallback<T> callback);

        /// <summary>
        ///     Perform a line query on this index. This will return all entries in the index that are intersecting with the
        ///     specified query line.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        /// <param name="t">The fraction along the line to consider.</param>
        /// <param name="results">The list to put the results into.</param>
        /// <returns></returns>
        void Find(TPoint start, TPoint end, float t, ISet<T> results);

        /// <summary>
        ///     Perform a line query on this index. This will return all entries in the index that are intersecting with the
        ///     specified query line.
        ///     <para>
        ///         Note that the callback will be passed the fraction along the line that the hit occurred at, and may return
        ///         the new maximum fraction up to which the search will run. If the returned fraction is exactly zero the search
        ///         will be stopped. If the returned fraction is negative the hit will be ignored, that is the max fraction will
        ///         not change.
        ///     </para>
        /// </summary>
        /// <param name="start">The start of the line.</param>
        /// <param name="end">The end of the line.</param>
        /// <param name="t">The fraction along the line to consider.</param>
        /// <param name="callback">The method to call for each found hit.</param>
        /// <returns></returns>
        bool Find(TPoint start, TPoint end, float t, LineQueryCallback<T> callback);

        #endregion
    }
}