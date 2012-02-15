using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Space.ComponentSystem.Factories
{
    public class ItemPool
    {
        /// <summary>
        /// THe Class holding the Infos about the Drop
        /// </summary>
        public class DropInfo
        {
            /// <summary>
            /// The Name of the Item
            /// </summary>
            public string ItemName;
            /// <summary>
            /// The Dropchance of the Item
            /// </summary>
            public float Dropchance;

            
        }
        /// <summary>
        /// The Id of the Item Pool
        /// </summary>
        public string Name;
        /// <summary>
        /// The Maximum amount of drops
        /// </summary>
        public int MaxDrops;
        /// <summary>
        /// A List of Tuples containing the Name of the Item and the Drop Chance
        /// </summary>
        public List<DropInfo> Items;
    }
}
