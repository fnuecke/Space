using System;

namespace Engine.Physics.Detail
{
    /// <summary>
    /// Utility class providing a fixed size array as a struct.
    /// </summary>
    /// <typeparam name="T">The type stored in the array.</typeparam>
    internal struct FixedArray2<T>
    {
        /// <summary>
        /// The items of the array.
        /// </summary>
        public T Item0, Item1;

        /// <summary>
        /// Gets or sets the <see cref="T"/> at the specified index.
        /// </summary>
        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Item0;
                    case 1:
                        return Item1;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Item0 = value;
                        break;
                    case 1:
                        Item1 = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// Utility class providing a fixed size array as a struct.
    /// </summary>
    /// <typeparam name="T">The type stored in the array.</typeparam>
    internal struct FixedArray3<T>
    {
        /// <summary>
        /// The items of the array.
        /// </summary>
        public T Item0, Item1, Item2;

        /// <summary>
        /// Gets or sets the <see cref="T"/> at the specified index.
        /// </summary>
        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Item0;
                    case 1:
                        return Item1;
                    case 2:
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
                        Item0 = value;
                        break;
                    case 1:
                        Item1 = value;
                        break;
                    case 2:
                        Item2 = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
