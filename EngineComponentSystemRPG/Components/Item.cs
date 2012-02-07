using Engine.ComponentSystem.Components;
using System.Collections.Generic;

using Engine.ComponentSystem.RPG.Components;
namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Marks an entity as being an item. This should be extended to add item
    /// specific properties, as necessary.
    /// </summary>
    public class Item : AbstractComponent
    {
        protected string _itemTexture;
        protected string _name;

       
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

        //public List<Attribute<TAttribute>> Attributes<TAttribute>()
        //where TAttribute : struct
        //{
        //    if (attributes == null)
        //    {
        //        attributes = new List<Attribute<TAttribute>>();
        //        foreach (var component in Entity.Components)
        //        {
        //            if (component is Attribute<TAttribute>)
        //            {
        //                attributes.Add((Attribute<TAttribute>)component);
        //            }
        //        }
        //    }
        //    return attributes;
        //}
    }
}
