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
            get { return _iconName ?? (_iconName = Manager.GetComponent<Item>(Entity).IconName); }
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
            var item = Manager.GetComponent<SpaceItem>(Entity);
            var displayName = ItemNames.ResourceManager.GetString(item.Name) ?? ("!!" + item.Name + "!!");

            // Unique items don't need prefix.
            if (item.Quality != ItemQuality.Unique)
            {
                var maxValue = float.MinValue;
                var type = AttributeType.None;

                // Go through all attributes and calculate highest ranking.
                foreach (var component in Manager.GetComponents(Entity))
                {
                    if (component is Attribute<AttributeType>)
                    {
                        var attribute = ((Attribute<AttributeType>)component).Modifier;
                        // Check if we have a new max value.
                        if (attribute.Type.GetValue(attribute.Value) > maxValue)
                        {
                            maxValue = attribute.Type.GetValue(attribute.Value);
                            type = attribute.Type;
                        }
                    }
                }
                if (type != AttributeType.None)
                {
                    return ItemNames.ResourceManager.GetString(item.Quality.ToString()) + " " + type.ToLocalizedPrefixString() + " " + displayName;
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
            foreach (var component in Manager.GetComponents(Entity))
            {
                if (component is Attribute<AttributeType>)
                {
                    descripton.Attributes.Add(((Attribute<AttributeType>)component).Modifier);
                }
            }

            // If it's a weapon, flag that and add info.
            var weapon = Manager.GetComponent<Weapon>(Entity);
            if (weapon != null)
            {
                descripton.IsWeapon = true;
                descripton.WeaponDamage = weapon.Damage;
                descripton.WeaponCooldown = weapon.Cooldown;
                descripton.WeaponEnergyConsumption = weapon.EnergyConsumption;
                descripton.WeaponProjectileCount = weapon.Projectiles.Length;
            }
            else
            {
                descripton.IsWeapon = false;
            }
        }

        #endregion
    }
}
