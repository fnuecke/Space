using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// A usable item can be 'activated', triggering some response. An example
    /// would be buff scrolls or healing potions.
    /// </summary>
    public abstract class Usable : AbstractComponent
    {
        #region Logic

        /// <summary>
        /// Use the item, have it trigger its logic.
        /// </summary>
        public abstract void Use();

        #endregion
    }
}
