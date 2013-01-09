using System.ComponentModel;
using Microsoft.Xna.Framework.Content;

namespace Space.ComponentSystem.Factories
{
    /// <summary>An item pool contains a list of items that may be sampled from it.</summary>
    [DefaultProperty("Name")]
    public sealed class ItemPool
    {
        /// <summary>The logical name of the item pool.</summary>
        [Category("General")]
        [Description("The name of the item pool, by which it may be referenced (e.g. in ships).")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>The maximum amount of simultaneously dropped items.</summary>
        [DefaultValue(0)]
        [Category("Logic")]
        [Description("The maximum number of items to sample from this item pool (actual number is randomly determined per drop, with this as the maximum number of items).")]
        public int MaxDrops
        {
            get { return _maxDrops; }
            set { _maxDrops = value; }
        }

        /// <summary>A list of tuples containing the name of the item and the probability the item will be dropped.</summary>
        [ContentSerializer(FlattenContent = true, CollectionItemName = "Item")]
        [Category("Logic")]
        [Description("The list of items that can be drawn from this item pool.")]
        public DropInfo[] Items
        {
            get { return _items; }
            set { _items = value; }
        }

        private string _name = "";

        private int _maxDrops;

        private DropInfo[] _items;

        /// <summary>The class holding the information about a single drop.</summary>
        [TypeConverter(typeof (ExpandableObjectConverter))]
        public sealed class DropInfo
        {
            /// <summary>The logical name of the item.</summary>
            [Editor("Space.Tools.DataEditor.ItemPoolEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
            [TypeConverter(typeof (ReadonlyItemNameConverter))]
            [Description("The name of the item type to sample.")]
            public string ItemName
            {
                get { return _itemName; }
                set { _itemName = value; }
            }

            /// <summary>The probability with which the item will be dropped.</summary>
            [DefaultValue(0f)]
            [Description("The probability that the item will be sampled when an item from its pool is dropped.")]
            public float Probability
            {
                get { return _probability; }
                set { _probability = value; }
            }

            private string _itemName = "";

            private float _probability;

            /// <summary>
            ///     Returns a <see cref="System.String"/> that represents this instance.
            /// </summary>
            /// <returns>
            ///     A <see cref="System.String"/> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return _itemName + _probability.ToString(" (0.00%)");
            }
        }
    }
}