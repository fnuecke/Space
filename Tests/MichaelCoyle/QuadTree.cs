using System;
using System.Collections;
using System.Collections.Generic;
using Engine.Collections;
// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
using TPoint = Microsoft.Xna.Framework.Point;
using TRectangle = Microsoft.Xna.Framework.Rectangle;

namespace Tests.MichaelCoyle
{
    /// <summary>
    /// A Quadtree is a structure designed to partition space so
    /// that it's faster to find out what is inside or outside a given 
    /// area. See http://en.wikipedia.org/wiki/Quadtree
    /// This QuadTree contains items that have an area (Rectangle)
    /// it will store a reference to the item in the quad 
    /// that is just big enough to hold it. Each quad has a bucket that 
    /// contain multiple items.
    /// </summary>
    public class MCQuadTree<T> : IIndex<T>
    {
        public sealed class Wrapper : IHasRect
        {
            public TRectangle Rectangle { get; set; }
            public T Value;
        }

        /// <summary>
        /// The root QuadTreeNode
        /// </summary>
        private MCQuadTreeNode<T> _root;

        /// <summary>
        /// The bounds of this QuadTree
        /// </summary>
        private readonly TRectangle _rectangle;

        /// <summary>
        /// An delegate that performs an action on a QuadTreeNode
        /// </summary>
        /// <param name="obj"></param>
        public delegate void QTAction(MCQuadTreeNode<T> obj);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rectangle"></param>
        public MCQuadTree(TRectangle rectangle)
        {
            _rectangle = rectangle;
            _root = new MCQuadTreeNode<T>(_rectangle);
        }

        #region Implementation of IIndex

        /// <summary>
        /// Get the count of items in the QuadTree
        /// </summary>
        public int Count
        {
            get { return _root.Count; }
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
            _root.Insert(new Wrapper {Rectangle = bounds, Value = item});
        }

        /// <summary>
        /// Update an entry by changing its bounds. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <param name="newBounds">The new bounds of the item.</param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns><c>true</c> if the update was successful; <c>false</c> otherwise.</returns>
        public bool Update(TRectangle newBounds, T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Remove the specified item from the index. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns><c>true</c> if the item was removed; <c>false</c> otherwise.</returns>
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Test whether this index contains the specified item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns><c>true</c> if the index contains the item; <c>false</c> otherwise.</returns>
        public bool Contains(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes all items from the index.
        /// </summary>
        public void Clear()
        {
            _root = new MCQuadTreeNode<T>(_rectangle);
        }

        /// <summary>
        /// Get the bounds at which the specified item is currently stored.
        /// </summary>
        public TRectangle this[T item] { get { return TRectangle.Empty; } }

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
            _root.Query(ref rectangle, ref list);
        }

        #endregion

        /// <summary>
        /// Do the specified action for each item in the quadtree
        /// </summary>
        /// <param name="action"></param>
        public void ForEach(QTAction action)
        {
            _root.ForEach(action);
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
