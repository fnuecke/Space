using System;
using System.Collections;
using System.Collections.Generic;
using Engine.Collections;

// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
using TPoint = Engine.FarMath.FarPosition;
using TRectangle = Engine.FarMath.FarRectangle;

namespace Tests.JohnMcDonald
{
    /// <summary>
    /// A QuadTree Object that provides fast and efficient storage of objects in a world space.
    /// </summary>
    /// <typeparam name="T">Any object implementing IQuadStorable.</typeparam>
    public class JMDQuadTree<T> : ICollection<T>, IIndex<T>
    {
        internal sealed class Wrapper
        {
            public TRectangle Rect { get; set; }
            public T Value;
        }

        #region Private Members

        private readonly Dictionary<T, JMDQuadTreeObject> _wrappedDictionary = new Dictionary<T, JMDQuadTreeObject>();

        private readonly Dictionary<T, Wrapper> _wrappers = new Dictionary<T, Wrapper>();

        // Alternate method, use Parallel arrays

        // The root of this quad tree
        private readonly JMDQuadTreeNode _quadTreeRoot;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a QuadTree for the specified area.
        /// </summary>
        /// <param name="rect">The area this QuadTree object will encompass.</param>
        public JMDQuadTree(TRectangle rect)
        {
            _quadTreeRoot = new JMDQuadTreeNode(rect);
        }


        /// <summary>
        /// Creates a QuadTree for the specified area.
        /// </summary>
        /// <param name="x">The top-left position of the area rectangle.</param>
        /// <param name="y">The top-right position of the area rectangle.</param>
        /// <param name="width">The width of the area rectangle.</param>
        /// <param name="height">The height of the area rectangle.</param>
        public JMDQuadTree(int x, int y, int width, int height)
        {
            _quadTreeRoot = new JMDQuadTreeNode(new TRectangle(x, y, width, height));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the rectangle that bounds this QuadTree
        /// </summary>
        public TRectangle QuadRect
        {
            get { return _quadTreeRoot.QuadRect; }
        }

        /// <summary>
        /// Get the objects in this tree that intersect with the specified rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to find objects in.</param>
        public ICollection<T> GetObjects(TRectangle rect)
        {
            return _quadTreeRoot.GetObjects(rect);
        }

        /// <summary>
        /// Get the objects in this tree that intersect with the specified rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to find objects in.</param>
        /// <param name="results">A reference to a list that will be populated with the results.</param>
        public void GetObjects(TRectangle rect, ref ICollection<T> results)
        {
            _quadTreeRoot.GetObjects(rect, ref results);
        }


        /// <summary>
        /// Get all objects in this Quad, and it's children.
        /// </summary>
        public List<T> GetAllObjects()
        {
            return new List<T>(_wrappedDictionary.Keys);
        }


        /// <summary>
        /// Moves the object in the tree
        /// </summary>
        /// <param name="item">The item that has moved</param>
        public bool Move(T item)
        {
            if (Contains(item))
            {
                _quadTreeRoot.Move(_wrappedDictionary[item]);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region ICollection<T> Members

        ///<summary>
        ///Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        ///</summary>
        ///
        ///<param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        ///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
        public void Add(T item)
        {
            throw new NotSupportedException();
        }


        ///<summary>
        ///Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        ///</summary>
        ///
        ///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only. </exception>
        public void Clear()
        {
            _wrappedDictionary.Clear();
            _quadTreeRoot.Clear();
        }

        ///<summary>
        ///Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        ///</summary>
        ///
        ///<returns>
        ///true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.
        ///</returns>
        ///
        ///<param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        public bool Contains(T item)
        {
            return _wrappedDictionary.ContainsKey(item);
        }


        ///<summary>
        ///Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        ///</summary>
        ///
        ///<param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        ///<param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        ///<exception cref="T:System.ArgumentNullException"><paramref name="array" /> is null.</exception>
        ///<exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is less than 0.</exception>
        ///<exception cref="T:System.ArgumentException"><paramref name="array" /> is multidimensional.-or-<paramref name="arrayIndex" /> is equal to or greater than the length of <paramref name="array" />.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.-or-Type <paramref name="T" /> cannot be cast automatically to the type of the destination <paramref name="array" />.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _wrappedDictionary.Keys.CopyTo(array, arrayIndex);
        }

        ///<summary>
        ///Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        ///</summary>
        ///<returns>
        ///The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        ///</returns>
        public int Count
        {
            get { return _wrappedDictionary.Count; }
        }

        ///<summary>
        ///Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        ///</summary>
        ///
        ///<returns>
        ///true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.
        ///</returns>
        ///
        public bool IsReadOnly
        {
            get { return false; }
        }

        ///<summary>
        ///Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        ///</summary>
        ///
        ///<returns>
        ///true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
        ///</returns>
        ///
        ///<param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        ///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
        public bool Remove(T item)
        {
            if (Contains(item))
            {
                _quadTreeRoot.Delete(_wrappedDictionary[item], true);
                _wrappedDictionary.Remove(item);
                _wrappers.Remove(item);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Implementation of IIndex

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
            var wrapper = new Wrapper {Rect = bounds, Value = item};
            _wrappers.Add(item, wrapper);
            var wrappedObject = new JMDQuadTreeObject(wrapper);
            _wrappedDictionary.Add(item, wrappedObject);
            _quadTreeRoot.Insert(wrappedObject);
        }

        /// <summary>
        /// Update an entry by changing its bounds. If the item is not
        /// stored in the index, this will return <code>false</code>.
        /// </summary>
        /// <param name="newBounds">The new bounds of the item.</param>
        /// <param name="delta"> </param>
        /// <param name="item">The item for which to update the bounds.</param>
        /// <returns><c>true</c> if the update was successful; <c>false</c> otherwise.</returns>
        public bool Update(TRectangle newBounds, TPoint delta, T item)
        {
            // Check if we have that item.
            if (!Contains(item))
            {
                // No, nothing to do, then.
                return false;
            }

            _wrappers[item].Rect = newBounds;
            Move(item);

            return true;
        }

        /// <summary>
        /// Get the bounds at which the specified item is currently stored.
        /// </summary>
        public TRectangle this[T item] { get { return _wrappers[item].Rect; } }

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
            GetObjects(rectangle, ref list);
        }

        #endregion

        #region IEnumerable<T> and IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        IEnumerator<Tuple<TRectangle, T>> IEnumerable<Tuple<TRectangle, T>>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        ///<summary>
        ///Returns an enumerator that iterates through the collection.
        ///</summary>
        ///
        ///<returns>
        ///A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator()
        {
            return _wrappedDictionary.Keys.GetEnumerator();
        }


        ///<summary>
        ///Returns an enumerator that iterates through a collection.
        ///</summary>
        ///
        ///<returns>
        ///An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion


        /// <summary>
        /// The top left child for this QuadTree, only usable in debug mode
        /// </summary>
        public JMDQuadTreeNode RootQuad
        {
            get { return _quadTreeRoot; }
        }

        /// <summary>
        /// Used internally to attach an Owner to each object stored in the QuadTree
        /// </summary>
        internal class JMDQuadTreeObject
        {
            /// <summary>
            /// The wrapped data value
            /// </summary>
            public Wrapper Data
            {
                get;
                private set;
            }

            /// <summary>
            /// The QuadTreeNode that owns this object
            /// </summary>
            internal JMDQuadTreeNode Owner
            {
                get;
                set;
            }

            /// <summary>
            /// Wraps the data value
            /// </summary>
            /// <param name="data">The data value to wrap</param>
            public JMDQuadTreeObject(Wrapper data)
            {
                Data = data;
            }
        }

        /// <summary>
        /// A QuadTree Object that provides fast and efficient storage of objects in a world space.
        /// </summary>
        public class JMDQuadTreeNode
        {
            #region Constants

            // How many objects can exist in a QuadTree before it sub divides itself
            private const int MaxObjectsPerNode = 2;

            #endregion

            #region Private Members

            //private List<T> m_objects = null;       // The objects in this QuadTree
            private List<JMDQuadTreeObject> _objects;
            private TRectangle _rect; // The area this QuadTree represents

            private readonly JMDQuadTreeNode _parent; // The parent of this quad

            private JMDQuadTreeNode _childTl; // Top Left Child
            private JMDQuadTreeNode _childTr; // Top Right Child
            private JMDQuadTreeNode _childBl; // Bottom Left Child
            private JMDQuadTreeNode _childBr; // Bottom Right Child

            #endregion

            #region Public Properties

            /// <summary>
            /// The area this QuadTree represents.
            /// </summary>
            public TRectangle QuadRect
            {
                get { return _rect; }
            }

            /// <summary>
            /// The top left child for this QuadTree
            /// </summary>
            public JMDQuadTreeNode TopLeftChild
            {
                get { return _childTl; }
            }

            /// <summary>
            /// The top right child for this QuadTree
            /// </summary>
            public JMDQuadTreeNode TopRightChild
            {
                get { return _childTr; }
            }

            /// <summary>
            /// The bottom left child for this QuadTree
            /// </summary>
            public JMDQuadTreeNode BottomLeftChild
            {
                get { return _childBl; }
            }

            /// <summary>
            /// The bottom right child for this QuadTree
            /// </summary>
            public JMDQuadTreeNode BottomRightChild
            {
                get { return _childBr; }
            }

            /// <summary>
            /// This QuadTree's parent
            /// </summary>
            public JMDQuadTreeNode Parent
            {
                get { return _parent; }
            }

            /// <summary>
            /// The objects contained in this QuadTree at it's level (ie, excludes children)
            /// </summary>
            //public List<T> Objects { get { return m_objects; } }
            internal List<JMDQuadTreeObject> Objects
            {
                get { return _objects; }
            }

            /// <summary>
            /// How many total objects are contained within this QuadTree (ie, includes children)
            /// </summary>
            public int Count
            {
                get { return ObjectCount(); }
            }

            /// <summary>
            /// Returns true if this is a empty leaf node
            /// </summary>
            public bool IsEmptyLeaf
            {
                get { return Count == 0 && _childTl == null; }
            }

            #endregion

            #region Constructor

            /// <summary>
            /// Creates a QuadTree for the specified area.
            /// </summary>
            /// <param name="rect">The area this QuadTree object will encompass.</param>
            public JMDQuadTreeNode(TRectangle rect)
            {
                this._rect = rect;
            }


            /// <summary>
            /// Creates a QuadTree for the specified area.
            /// </summary>
            /// <param name="x">The top-left position of the area rectangle.</param>
            /// <param name="y">The top-right position of the area rectangle.</param>
            /// <param name="width">The width of the area rectangle.</param>
            /// <param name="height">The height of the area rectangle.</param>
            public JMDQuadTreeNode(int x, int y, int width, int height)
            {
                _rect = new TRectangle(x, y, width, height);
            }


            private JMDQuadTreeNode(JMDQuadTreeNode parent, TRectangle rect)
                : this(rect)
            {
                this._parent = parent;
            }

            #endregion

            #region Private Members

            /// <summary>
            /// Add an item to the object list.
            /// </summary>
            /// <param name="item">The item to add.</param>
            private void Add(JMDQuadTreeObject item)
            {
                if (_objects == null)
                {
                    //m_objects = new List<T>();
                    _objects = new List<JMDQuadTreeObject>();
                }

                item.Owner = this;
                _objects.Add(item);
            }


            /// <summary>
            /// Remove an item from the object list.
            /// </summary>
            /// <param name="item">The object to remove.</param>
            private void Remove(JMDQuadTreeObject item)
            {
                if (_objects != null)
                {
                    int removeIndex = _objects.IndexOf(item);
                    if (removeIndex >= 0)
                    {
                        _objects[removeIndex] = _objects[_objects.Count - 1];
                        _objects.RemoveAt(_objects.Count - 1);
                    }
                }
            }


            /// <summary>
            /// Get the total for all objects in this QuadTree, including children.
            /// </summary>
            /// <returns>The number of objects contained within this QuadTree and its children.</returns>
            private int ObjectCount()
            {
                int count = 0;

                // Add the objects at this level
                if (_objects != null)
                {
                    count += _objects.Count;
                }

                // Add the objects that are contained in the children
                if (_childTl != null)
                {
                    count += _childTl.ObjectCount();
                    count += _childTr.ObjectCount();
                    count += _childBl.ObjectCount();
                    count += _childBr.ObjectCount();
                }

                return count;
            }


            /// <summary>
            /// Subdivide this QuadTree and move it's children into the appropriate Quads where applicable.
            /// </summary>
            private void Subdivide()
            {
                // We've reached capacity, subdivide...
                var size = new TPoint(_rect.Width / 2, _rect.Height / 2);
                var mid = new TPoint(_rect.X + size.X, _rect.Y + size.Y);

                _childTl = new JMDQuadTreeNode(this, new TRectangle(_rect.Left, _rect.Top, size.X, size.Y));
                _childTr = new JMDQuadTreeNode(this, new TRectangle(mid.X, _rect.Top, size.X, size.Y));
                _childBl = new JMDQuadTreeNode(this, new TRectangle(_rect.Left, mid.Y, size.X, size.Y));
                _childBr = new JMDQuadTreeNode(this, new TRectangle(mid.X, mid.Y, size.X, size.Y));

                // If they're completely contained by the quad, bump objects down
                for (int i = 0; i < _objects.Count; i++)
                {
                    JMDQuadTreeNode destTree = GetDestinationTree(_objects[i]);

                    if (destTree != this)
                    {
                        // Insert to the appropriate tree, remove the object, and back up one in the loop
                        destTree.Insert(_objects[i]);
                        Remove(_objects[i]);
                        i--;
                    }
                }
            }


            /// <summary>
            /// Get the child Quad that would contain an object.
            /// </summary>
            /// <param name="item">The object to get a child for.</param>
            /// <returns></returns>
            private JMDQuadTreeNode GetDestinationTree(JMDQuadTreeObject item)
            {
                // If a child can't contain an object, it will live in this Quad
                JMDQuadTreeNode destTree = this;

                if (_childTl.QuadRect.Contains(item.Data.Rect))
                {
                    destTree = _childTl;
                }
                else if (_childTr.QuadRect.Contains(item.Data.Rect))
                {
                    destTree = _childTr;
                }
                else if (_childBl.QuadRect.Contains(item.Data.Rect))
                {
                    destTree = _childBl;
                }
                else if (_childBr.QuadRect.Contains(item.Data.Rect))
                {
                    destTree = _childBr;
                }

                return destTree;
            }


            private void Relocate(JMDQuadTreeObject item)
            {
                // Are we still inside our parent?
                if (QuadRect.Contains(item.Data.Rect))
                {
                    // Good, have we moved inside any of our children?
                    if (_childTl != null)
                    {
                        JMDQuadTreeNode dest = GetDestinationTree(item);
                        if (item.Owner != dest)
                        {
                            // Delete the item from this quad and add it to our child
                            // Note: Do NOT clean during this call, it can potentially delete our destination quad
                            JMDQuadTreeNode formerOwner = item.Owner;
                            Delete(item, false);
                            dest.Insert(item);

                            // Clean up ourselves
                            formerOwner.CleanUpwards();
                        }
                    }
                }
                else
                {
                    // We don't fit here anymore, move up, if we can
                    if (_parent != null)
                    {
                        _parent.Relocate(item);
                    }
                }
            }


            private void CleanUpwards()
            {
                if (_childTl != null)
                {
                    // If all the children are empty leaves, delete all the children
                    if (_childTl.IsEmptyLeaf &&
                            _childTr.IsEmptyLeaf &&
                            _childBl.IsEmptyLeaf &&
                            _childBr.IsEmptyLeaf)
                    {
                        _childTl = null;
                        _childTr = null;
                        _childBl = null;
                        _childBr = null;

                        if (_parent != null && Count == 0)
                        {
                            _parent.CleanUpwards();
                        }
                    }
                }
                else
                {
                    // I could be one of 4 empty leaves, tell my parent to clean up
                    if (_parent != null && Count == 0)
                    {
                        _parent.CleanUpwards();
                    }
                }
            }

            #endregion

            #region Internal Methods

            /// <summary>
            /// Clears the QuadTree of all objects, including any objects living in its children.
            /// </summary>
            internal void Clear()
            {
                // Clear out the children, if we have any
                if (_childTl != null)
                {
                    _childTl.Clear();
                    _childTr.Clear();
                    _childBl.Clear();
                    _childBr.Clear();
                }

                // Clear any objects at this level
                if (_objects != null)
                {
                    _objects.Clear();
                    _objects = null;
                }

                // Set the children to null
                _childTl = null;
                _childTr = null;
                _childBl = null;
                _childBr = null;
            }


            /// <summary>
            /// Deletes an item from this QuadTree. If the object is removed causes this Quad to have no objects in its children, it's children will be removed as well.
            /// </summary>
            /// <param name="item">The item to remove.</param>
            /// <param name="clean">Whether or not to clean the tree</param>
            internal void Delete(JMDQuadTreeObject item, bool clean)
            {
                if (item.Owner != null)
                {
                    if (item.Owner == this)
                    {
                        Remove(item);
                        if (clean)
                        {
                            CleanUpwards();
                        }
                    }
                    else
                    {
                        item.Owner.Delete(item, clean);
                    }
                }
            }



            /// <summary>
            /// Insert an item into this QuadTree object.
            /// </summary>
            /// <param name="item">The item to insert.</param>
            internal void Insert(JMDQuadTreeObject item)
            {
                // If this quad doesn't contain the items rectangle, do nothing, unless we are the root
                if (!_rect.Contains(item.Data.Rect))
                {
                    System.Diagnostics.Debug.Assert(_parent == null, "We are not the root, and this object doesn't fit here. How did we get here?");
                    if (_parent == null)
                    {
                        // This object is outside of the QuadTree bounds, we should add it at the root level
                        Add(item);
                    }
                    else
                    {
                        return;
                    }
                }

                if (_objects == null ||
                        (_childTl == null && _objects.Count + 1 <= MaxObjectsPerNode))
                {
                    // If there's room to add the object, just add it
                    Add(item);
                }
                else
                {
                    // No quads, create them and bump objects down where appropriate
                    if (_childTl == null)
                    {
                        Subdivide();
                    }

                    // Find out which tree this object should go in and add it there
                    JMDQuadTreeNode destTree = GetDestinationTree(item);
                    if (destTree == this)
                    {
                        Add(item);
                    }
                    else
                    {
                        destTree.Insert(item);
                    }
                }
            }


            /// <summary>
            /// Get the objects in this tree that intersect with the specified rectangle.
            /// </summary>
            /// <param name="searchRect">The rectangle to find objects in.</param>
            internal ICollection<T> GetObjects(TRectangle searchRect)
            {
                ICollection<T> results = new List<T>();
                GetObjects(searchRect, ref results);
                return results;
            }

            /// <summary>
            /// Get the objects in this tree that intersect with the specified rectangle.
            /// </summary>
            /// <param name="searchRect">The rectangle to find objects in.</param>
            /// <param name="results">A reference to a list that will be populated with the results.</param>
            internal void GetObjects(TRectangle searchRect, ref ICollection<T> results)
            {
                // We can't do anything if the results list doesn't exist
                if (results != null)
                {
                    if (searchRect.Contains(this._rect))
                    {
                        // If the search area completely contains this quad, just get every object this quad and all it's children have
                        GetAllObjects(ref results);
                    }
                    else if (searchRect.Intersects(this._rect))
                    {
                        // Otherwise, if the quad isn't fully contained, only add objects that intersect with the search rectangle
                        if (_objects != null)
                        {
                            for (int i = 0; i < _objects.Count; i++)
                            {
                                if (searchRect.Intersects(_objects[i].Data.Rect))
                                {
                                    results.Add(_objects[i].Data.Value);
                                }
                            }
                        }

                        // Get the objects for the search rectangle from the children
                        if (_childTl != null)
                        {
                            _childTl.GetObjects(searchRect, ref results);
                            _childTr.GetObjects(searchRect, ref results);
                            _childBl.GetObjects(searchRect, ref results);
                            _childBr.GetObjects(searchRect, ref results);
                        }
                    }
                }
            }

            /// <summary>
            /// Get all objects in this Quad, and it's children.
            /// </summary>
            /// <param name="results">A reference to a list in which to store the objects.</param>
            private void GetAllObjects(ref ICollection<T> results)
            {
                // If this Quad has objects, add them
                if (_objects != null)
                {
                    foreach (JMDQuadTreeObject qto in _objects)
                    {
                        results.Add(qto.Data.Value);
                    }
                }

                // If we have children, get their objects too
                if (_childTl != null)
                {
                    _childTl.GetAllObjects(ref results);
                    _childTr.GetAllObjects(ref results);
                    _childBl.GetAllObjects(ref results);
                    _childBr.GetAllObjects(ref results);
                }
            }


            /// <summary>
            /// Moves the QuadTree object in the tree
            /// </summary>
            /// <param name="item">The item that has moved</param>
            internal void Move(JMDQuadTreeObject item)
            {
                if (item.Owner != null)
                {
                    item.Owner.Relocate(item);
                }
                else
                {
                    Relocate(item);
                }
            }

            #endregion
        }
    }
}
