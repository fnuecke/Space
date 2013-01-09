using System;
using System.Collections;
using System.Collections.Generic;

namespace Engine.Collections
{
    /// <summary>Utility class providing a fixed size array as a struct.</summary>
    /// <typeparam name="T">The type stored in the array.</typeparam>
    public struct FixedArray2<T> : IList<T>
    {
        /// <summary>The items of the array.</summary>
        public T Item1, Item2;

        /// <summary>
        ///     Gets or sets the <see cref="T"/> at the specified index.
        /// </summary>
        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Item1;
                    case 1:
                        return Item2;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Item1 = value;
                        break;
                    case 1:
                        Item2 = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #region IList interface

        /// <summary>
        ///     Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>. Can be set
        ///     to indicate how many entries are used.
        /// </summary>
        public int Count { get; set; }

        /// <summary>Always false.</summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        ///     Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </param>
        /// <returns>
        ///     true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>;
        ///     otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            if (Count == 0)
            {
                return false;
            }
            if (Equals(Item1, item))
            {
                return true;
            }
            if (Count == 1)
            {
                return false;
            }
            if (Equals(Item2, item))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </param>
        /// <returns>
        ///     The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(T item)
        {
            if (Count == 0)
            {
                return -1;
            }
            if (Equals(Item1, item))
            {
                return 0;
            }
            if (Count == 1)
            {
                return -1;
            }
            if (Equals(Item2, item))
            {
                return 1;
            }
            return -1;
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (Count == 0)
            {
                yield break;
            }
            yield return Item1;
            if (Count == 1)
            {
                yield break;
            }
            yield return Item2;
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/>
        ///     to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/>
        ///     index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from
        ///     <see cref="T:System.Collections.Generic.ICollection`1"/>. The
        ///     <see cref="T:System.Array"/> must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        ///     The zero-based index in <paramref name="array"/> at which copying begins.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="array"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="arrayIndex"/> is less than 0.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        ///     <paramref name="array"/> is multidimensional. -or- The number of elements in the source
        ///     <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from
        ///     <paramref name="arrayIndex"/> to the end of the destination
        ///     <paramref name="array"/>. -or- Type <paramref name="{T}"/> cannot be cast automatically to the type of the
        ///     destination
        ///     <paramref name="array"/>.
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (Count == 0)
            {
                return;
            }
            array[arrayIndex + 0] = Item1;
            if (Count == 1)
            {
                return;
            }
            array[arrayIndex + 1] = Item2;
        }

        /// <summary>Not supported, always throws.</summary>
        /// <exception cref="T:System.NotSupportedException">Always.</exception>
        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>Not supported, always throws.</summary>
        /// <exception cref="T:System.NotSupportedException">Always.</exception>
        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>Not supported, always throws.</summary>
        /// <exception cref="T:System.NotSupportedException">Always.</exception>
        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>Not supported, always throws.</summary>
        /// <exception cref="T:System.NotSupportedException">Always.</exception>
        public void Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>Not supported, always throws.</summary>
        /// <exception cref="T:System.NotSupportedException">Always.</exception>
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    /// <summary>Utility class providing a fixed size array as a struct.</summary>
    /// <typeparam name="T">The type stored in the array.</typeparam>
    public struct FixedArray3<T> : IList<T>
    {
        /// <summary>The items of the array.</summary>
        public T Item1, Item2, Item3;

        /// <summary>
        ///     Gets or sets the <see cref="T"/> at the specified index.
        /// </summary>
        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Item1;
                    case 1:
                        return Item2;
                    case 2:
                        return Item3;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Item1 = value;
                        break;
                    case 1:
                        Item2 = value;
                        break;
                    case 2:
                        Item3 = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #region IList interface

        /// <summary>
        ///     Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>. Can be set
        ///     to indicate how many entries are used.
        /// </summary>
        public int Count { get; set; }

        /// <summary>Always false.</summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        ///     Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </param>
        /// <returns>
        ///     true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>;
        ///     otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            if (Count == 0)
            {
                return false;
            }
            if (Equals(Item1, item))
            {
                return true;
            }
            if (Count == 1)
            {
                return false;
            }
            if (Equals(Item2, item))
            {
                return true;
            }
            if (Count == 2)
            {
                return false;
            }
            if (Equals(Item3, item))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </param>
        /// <returns>
        ///     The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(T item)
        {
            if (Count == 0)
            {
                return -1;
            }
            if (Equals(Item1, item))
            {
                return 0;
            }
            if (Count == 1)
            {
                return - 1;
            }
            if (Equals(Item2, item))
            {
                return 1;
            }
            if (Count == 2)
            {
                return -1;
            }
            if (Equals(Item3, item))
            {
                return 2;
            }
            return -1;
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (Count == 0)
            {
                yield break;
            }
            yield return Item1;
            if (Count == 1)
            {
                yield break;
            }
            yield return Item2;
            if (Count == 2)
            {
                yield break;
            }
            yield return Item3;
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/>
        ///     to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/>
        ///     index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from
        ///     <see cref="T:System.Collections.Generic.ICollection`1"/>. The
        ///     <see cref="T:System.Array"/> must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        ///     The zero-based index in <paramref name="array"/> at which copying begins.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="array"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="arrayIndex"/> is less than 0.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        ///     <paramref name="array"/> is multidimensional. -or- The number of elements in the source
        ///     <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from
        ///     <paramref name="arrayIndex"/> to the end of the destination
        ///     <paramref name="array"/>. -or- Type <paramref name="{T}"/> cannot be cast automatically to the type of the
        ///     destination
        ///     <paramref name="array"/>.
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (Count == 0)
            {
                return;
            }
            array[arrayIndex + 0] = Item1;
            if (Count == 1)
            {
                return;
            }
            array[arrayIndex + 1] = Item2;
            if (Count == 2)
            {
                return;
            }
            array[arrayIndex + 2] = Item3;
        }

        /// <summary>Not supported, always throws.</summary>
        /// <exception cref="T:System.NotSupportedException">Always.</exception>
        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>Not supported, always throws.</summary>
        /// <exception cref="T:System.NotSupportedException">Always.</exception>
        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>Not supported, always throws.</summary>
        /// <exception cref="T:System.NotSupportedException">Always.</exception>
        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>Not supported, always throws.</summary>
        /// <exception cref="T:System.NotSupportedException">Always.</exception>
        public void Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>Not supported, always throws.</summary>
        /// <exception cref="T:System.NotSupportedException">Always.</exception>
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}