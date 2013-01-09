using System.ComponentModel;
using Engine.ComponentSystem.RPG.Constraints;
using Microsoft.Xna.Framework.Content;
using Space.Data;

namespace Space.ComponentSystem.Factories
{
    /// <summary>An attribute pool contains a list of attributes that may be sampled from it (e.g. for item creation).</summary>
    [DefaultProperty("Name")]
    public sealed class AttributePool
    {
        #region Properties

        /// <summary>The logical name of the item pool.</summary>
        [Category("General")]
        [Description("The name of the item pool, by which it may be referenced (e.g. in ships).")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>The list of attributes provided by this pool.</summary>
        [Category("Logic")]
        [Description("The list of attributes provided by this pool.")]
        public AttributeInfo[] Attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        private string _name = "";

        private AttributeInfo[] _attributes;

        /// <summary>Intermediate class with sampling information for a single attribute.</summary>
        [TypeConverter(typeof (ExpandableObjectConverter))]
        public sealed class AttributeInfo
        {
            /// <summary>An attribute modifier that can be applied to the generated item, just with random values.</summary>
            [Description("Description of the actual attribute that may be sampled from the pool.")]
            public AttributeModifierConstraint<AttributeType> Attribute
            {
                get { return _attribute; }
                set { _attribute = value; }
            }

            /// <summary>
            ///     When sampling, the weight of an attribute is normalized using the sum of the weights of all available
            ///     attributes, resulting in the probability of the attribute being sampled.
            /// </summary>
            [DefaultValue(0)]
            [Description("The weight of the attribute when sampling, which correlates to its probability being sampled."
                )]
            public int Weight
            {
                get { return _weight; }
                set { _weight = value; }
            }

            /// <summary>
            ///     Determines whether the attribute may only be sampled once, for any single sampling process (false), or
            ///     multiple times (true).
            /// </summary>
            [ContentSerializer(Optional = true)]
            [DefaultValue(false)]
            [Description("Whether this attribute may be sampled multiple times for a single item.")]
            public bool AllowRedraw
            {
                get { return _allowRedraw; }
                set { _allowRedraw = value; }
            }

            /// <summary>The scale of the attribute values to be used after the owning item has gained a new level.</summary>
            [DefaultValue(0f)]
            [Description("The scale for this attribute when gaining a new level ")]
            public float LevelScale { get; set; }

            private AttributeModifierConstraint<AttributeType> _attribute =
                new AttributeModifierConstraint<AttributeType>();

            private int _weight;

            private bool _allowRedraw;

            /// <summary>
            ///     Returns a <see cref="System.String"/> that represents this instance.
            /// </summary>
            /// <returns>
            ///     A <see cref="System.String"/> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return _attribute + " (" + _weight + ")";
            }
        }

        #endregion
    }
}