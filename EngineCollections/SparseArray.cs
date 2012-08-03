using System.Diagnostics;

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
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
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
                    var newCapacity = _data.Length * 3 / 2 + 1;
                    if (newCapacity <= index)
                    {
                        newCapacity = index + 1;
                    }
                    var data = new T[newCapacity];
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

        #endregion
    }
}
