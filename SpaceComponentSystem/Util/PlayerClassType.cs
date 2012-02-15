using System;
using System.Collections.Generic;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Util
{
    /// <summary>
    /// Utility methods for player classes.
    /// </summary>
    public static class PlayerClassExtensions
    {
        #region Constants

        private static readonly Dictionary<PlayerClassType, string> _shipLookup = new Dictionary<PlayerClassType, string>()
        {
            { PlayerClassType.Fighter, "Player" }
        };

        private static readonly Dictionary<PlayerClassType, Dictionary<Type, string>> _itemLookup = new Dictionary<PlayerClassType, Dictionary<Type, string>>()
        {
            { PlayerClassType.Fighter, new Dictionary<Type, string>()
                {
                    { typeof(Armor), "StarterArmor" },
                    { typeof(Reactor), "StarterReactor" },
                    { typeof(Sensor), "StarterSensor" },
                    { typeof(Thruster), "StarterThruster" },
                    { typeof(Weapon), "StarterWeapon" }
                }
            }
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
            return _shipLookup[playerClass];
        }

        /// <summary>
        /// Gets the item constraints for the starter item for the specified
        /// player class of the specified type.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="playerClass">The player class.</param>
        /// <returns>The item constraints.</returns>
        public static string GetStarterItemFactoryName<T>(this PlayerClassType playerClass)
        {
            if (_itemLookup[playerClass].ContainsKey(typeof(T)))
            {
                return _itemLookup[playerClass][typeof(T)];
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}
