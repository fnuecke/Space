using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Engine.Collections
{
    /// <summary>
    /// Represents a sparse array, i.e. an allows indexing arbitrary positions in an
    /// array and resizes as necessary to insert.
    /// 
    /// <para>
    /// The enumerator returned by this class will only return non-default values.
    /// For class types this means it will return all non-null entries, for value
    /// types it will return all entries that are not the default value for that
    /// value type (e.g. it will skip all zeros for <c>T=int</c>).
    /// </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class SparseArray<T> : IEnumerable<T>
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

        #region Enumeration

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// It is important to note that this enumerator will skip entries
        /// with default values for stored struct types (e.g. if T=int it
        /// will skip all entries that are '0', even if set to be so).
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can
        /// be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _data.Length; i++)
            {
                if (!Equals(_data[i], default(T)))
                {
                    yield return _data[i];
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// It is important to note that this enumerator will skip entries
        /// with default values for stored struct types (e.g. if T=int it
        /// will skip all entries that are '0', even if set to be so).
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be
        /// used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
