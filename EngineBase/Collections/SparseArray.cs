namespace Engine.Collections
{
    /// <summary>
    /// Represents a sparse array, i.e. an allows indexing arbitrary positions in an
    /// array and resizes as necessary to insert.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class SparseArray<T>
    {
        #region Fields

        /// <summary>
        /// The actual array holding our data.
        /// </summary>
        private T[] _data;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SparseArray&lt;T&gt;"/> class
        /// with a default initial capacity of 16.
        /// </summary>
        public SparseArray()
            : this(16)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SparseArray&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="capacity">The initial capacity of the sparse array.</param>
        public SparseArray(uint capacity)
        {
            _data = new T[capacity];
        } 

        #endregion

        #region Logic

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <returns>
        /// The element at the specified index.
        /// </returns>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        public T this[int index]
        {
            get { return (index >= 0 && index < _data.Length) ? _data[index] : default(T); }
            set
            {
                if (_data.Length <= index)
                {
                    var data = new T[_data.Length * 3 / 2 + 1];
                    _data.CopyTo(data, 0);
                    _data = data;
                }
                _data[index] = value;
            }
        }

        /// <summary>
        /// Removes all items from the array.
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < _data.Length; i++)
            {
                _data[i] = default(T);
            }
        }

        /// <summary>
        /// Copies the elements of the array to another <see cref="T:System.Array"/>, starting at a particular index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type <paramref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
        }

        #endregion
    }
}
