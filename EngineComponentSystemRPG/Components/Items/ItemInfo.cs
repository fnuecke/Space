using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.RPG.Components
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
            get
            {
                if (_name == null)
                {
                    _name = ComputeName();
                }
                return _name;
            }
        }

        /// <summary>
        /// The asset name of the texture to use to display the item in menus
        /// and the inventory, e.g.
        /// </summary>
        public string IconName
        {
            get
            {
                if (_iconName == null)
                {
                    _iconName = Entity.GetComponent<Item>().IconName;
                }
                return _iconName;
            }
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

        #region Utility methods

        /// <summary>
        /// Determines the localized item name based on its id and effects.
        /// </summary>
        /// <returns>The localized complete item name.</returns>
        private string ComputeName()
        {
            // TODO: implement actual logic
            return "Name";
        }

        #endregion
    }
}
