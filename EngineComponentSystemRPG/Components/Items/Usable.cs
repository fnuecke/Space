namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// A usable item can be 'activated', triggering some response. An example
    /// would be buff scrolls or healing potions.
    /// </summary>
    public abstract class Usable : Item
    {
        #region Constructor

        /// <summary>
        /// Creates a new usable item with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        public Usable(string name, string iconName)
            : base(name, iconName)
        {
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public Usable()
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Use the item, have it trigger its logic.
        /// </summary>
        public abstract void Use();

        #endregion
    }
}
