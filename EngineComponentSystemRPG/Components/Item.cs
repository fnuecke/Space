using Engine.ComponentSystem.Components;
using System.Collections.Generic;

using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;
namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Marks an entity as being an item. This should be extended to add item
    /// specific properties, as necessary.
    /// </summary>
    public class Item<TAttribute> : AbstractComponent

        where TAttribute : struct
    {
        protected string _itemTexture = "Textures/Icons/Buffs/default";
        protected string _name = "Test Name";
        protected List<Attribute<TAttribute>> attributes;
       
        public string Name()
        {
            if (_name == null)
            {
                //var item = Entity.GetComponent<Item>();
                _name = "Test Name";
            }
            return _name;
        }

        public virtual string Texture()
        {
            if (_itemTexture == null)
                _itemTexture = "Textures/Icons/Buffs/default";
            return _itemTexture;
        }

        public virtual  List<Attribute<TAttribute>> Attributes()
        {
            if (attributes == null)
            {
                attributes = new List<Attribute<TAttribute>>();
                foreach (var component in Entity.Components)
                {
                    if (component is Attribute<TAttribute>)
                    {
                        attributes.Add((Attribute<TAttribute>)component);
                    }
                }
            }
            return attributes;
        }
        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(_itemTexture)
                .Write(_name);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _itemTexture = packet.ReadString();
            _name = packet.ReadString();
        }
    }
}
