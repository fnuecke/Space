using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// A facade and caching class used to make it easier to query information
    /// about an item.
    /// </summary>
    public sealed class ItemInfo : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The full, localized name of the item as it may be displayed in the
        /// user interface.
        /// </summary>
        public string Name
        {
            get { return _name ?? (_name = ComputeName()); }
        }

        /// <summary>
        /// The asset name of the texture to use to display the item in menus
        /// and the inventory, e.g.
        /// </summary>
        public string IconName
        {
            get { return _iconName ?? (_iconName = ((Item)Manager.GetComponent(Entity, Item.TypeId)).IconName); }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The actual, localized, full name of the item to use in the GUI.
        /// </summary>
        private string _name;

        /// <summary>
        /// The name of the texture to use for rendering the item in menus and
        /// the inventory.
        /// </summary>
        private string _iconName;

        /// <summary>
        /// A cached string describing the item.
        /// </summary>
        //protected List<string> _description;

        #endregion

        #region Initialization

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _name = null;
            _iconName = null;
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Determines the localized item name based on its id and effects.
        /// </summary>
        /// <returns>The localized complete item name.</returns>
        private string ComputeName()
        {
            var item = ((Item)Manager.GetComponent(Entity, Item.TypeId));
            var displayName = ItemNames.ResourceManager.GetString(item.Name) ?? ("!!ItemNames:" + item.Name + "!!");

            // Non-space and unique items don't need prefix.
            if ((item is SpaceItem) && ((SpaceItem)item).Quality != ItemQuality.Unique)
            {
                var maxValue = float.MinValue;
                var type = AttributeType.None;

                // Go through all attributes and calculate highest ranking.
                foreach (var c in Manager.GetComponents(Entity, Attribute<AttributeType>.TypeId))
                {
                    var component = (Attribute<AttributeType>)c;
                    // Check if we have a new max value.
                    var modifier = component.Value;
                    if (modifier.Type.GetValue(modifier.Value) > maxValue)
                    {
                        maxValue = modifier.Type.GetValue(modifier.Value);
                        type = modifier.Type;
                    }
                }
                if (type != AttributeType.None)
                {
                    return type.ToLocalizedPrefixString() + " " + displayName;
                }
            }

            return displayName;
        }
        
        /// <summary>
        /// Puts item specific information into the given descripton object.
        /// </summary>
        /// <param name="descripton">The object to write the object information
        /// into.</param>
        public void GetDescription(ref ItemDescription descripton)
        {
            // Add attributes.
            descripton.Attributes = descripton.Attributes ?? new List<AttributeModifier<AttributeType>>();
            descripton.Attributes.Clear();
            foreach (var component in Manager.GetComponents(Entity, Attribute<AttributeType>.TypeId))
            {
                descripton.Attributes.Add(((Attribute<AttributeType>)component).Value);
            }

            // If it's a weapon, flag that and add info.
            var weapon = ((Weapon)Manager.GetComponent(Entity, Weapon.TypeId));
            if (weapon != null)
            {
                descripton.IsWeapon = true;
                descripton.WeaponProjectileCount = weapon.Projectiles.Length;
            }
            else
            {
                descripton.IsWeapon = false;
            }
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Name=" + _name + ", IconName=" + _iconName;
        }

        #endregion
    }
}
