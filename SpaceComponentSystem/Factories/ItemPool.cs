using System.Collections.Generic;

namespace Space.ComponentSystem.Factories
{
    public sealed class ItemPool
    {
        /// <summary>
        /// The class holding the information about a single drop.
        /// </summary>
        public sealed class DropInfo
        {
            /// <summary>
            /// The logical name of the item.
            /// </summary>
            public string ItemName;

            /// <summary>
            /// The probability with which the item will be dropped.
            /// </summary>
            public float Probability;
        }

        /// <summary>
        /// The logical name of the item pool.
        /// </summary>
        public string Name;

        /// <summary>
        /// The maximum amount of simultaneously dropped items.
        /// </summary>
        public int MaxDrops;

        /// <summary>
        /// A list of tuples containing the name of the item and the
        /// probability the item will be dropped.
        /// </summary>
        public List<DropInfo> Items;
    }
}
