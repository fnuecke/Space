using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Space.Data;

namespace Space.ComponentSystem.Components.Items
{
    class ItemInfo:AbstractComponent
    {
        private string _name;

        private string _texture;

        private List<Attribute<AttributeType>> attributes;
        public string Name()
        {
            if (_name == null)
            {
                //var item = Entity.GetComponent<Item>();
                _name = "Test Name";
            }
            return _name;
        }

        public string Texture()
        {
            if (_texture == null)
                _texture = "SampleTexture";
            return _texture;
        }

        public List<Attribute<AttributeType>> Attributes()
        {
            if (attributes == null)
            {
                attributes = new List<Attribute<AttributeType>>();
                foreach (var component in Entity.Components)
                {
                    if (component is Attribute<AttributeType>)
                    {
                        attributes.Add((Attribute<AttributeType>)component);
                    }
                }
            }
            return attributes;
        }
    }
}
