using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Engine.ComponentSystem;
using Engine.ComponentSystem.RPG.Constraints;
using Engine.FarMath;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Factories.SunSystemFactoryTypes;
using Space.Data;

namespace Space.ComponentSystem.Factories
{
     [DefaultProperty("Name")]
    public class AttributePool
    {
        #region Properties

        /// <summary>
        /// The logical name of the item pool.
        /// </summary>
        [Category("General")]
        [Description("The name of the item pool, by which it may be referenced (e.g. in ships).")]
        public string Name { get; set; }

         [Category("Attributes")]
        public ItemAttributes[] Attributes
        {
             get { return _attributes; }
            set { _attributes = value; }
        }

        private ItemAttributes[] _attributes;
        ///<summary>
        ///Describes an orbit system with a dominant axis.
        ///</summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public sealed class ItemAttributes
        {

            ///<summary>
            ///A attribute modifier that can be applied to
            ///the generated item, just with random values.
            ///</summary>
            
            [Category("Attribute")]
            [Description("Attribute bonuses that can be used to sample the item.")]
            public AttributeModifierConstraint<AttributeType> Attribute {
                get { return _attributes; }
                set { _attributes = value; }
            }


            ///<summary>
            ///The chance that this attribute will be choosen
            ///</summary>
            [DefaultValue(0f), Category("Stats"), Description("The Chance to choose this attribute")]
            public float Chance { get; set; }

            /// <summary>
            /// The scale of the Attribute to be used after the Item gained a new level
            /// </summary>
            [DefaultValue(0f), Category("Stats"), Description("The scale for this attribute when gaining a new level ")]
            public float LevelScale { get; set; }
            private AttributeModifierConstraint<AttributeType> _attributes = new AttributeModifierConstraint<AttributeType>();

            public override string ToString()
            {
                return _attributes.ToString();
            }
        }

        #endregion
        #region Attributes


        #endregion

    }
}
