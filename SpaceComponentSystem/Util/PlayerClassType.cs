using System.Collections.Generic;
using Space.Data;

namespace Space.ComponentSystem.Util
{
    /// <summary>
    /// Utility methods for player classes.
    /// </summary>
    public static class PlayerClassExtensions
    {
        #region Constants

        private static readonly Dictionary<PlayerClassType, string> ShipLookup =
            new Dictionary<PlayerClassType, string>
            {
                {PlayerClassType.Fighter, "Player"}
            };

        #endregion

        #region Methods

        /// <summary>
        /// Gets the ship constraints that define the player class' ship
        /// information.
        /// </summary>
        /// <param name="playerClass">The player class.</param>
        /// <returns>The ship constraints.</returns>
        public static string GetShipFactoryName(this PlayerClassType playerClass)
        {
            return ShipLookup[playerClass];
        }

        #endregion
    }
}
