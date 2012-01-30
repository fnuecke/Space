using System;
using System.Collections.Generic;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Constraints;
using Space.Data;

namespace Space.ComponentSystem.Util
{
    /// <summary>
    /// Utility methods for player classes.
    /// </summary>
    public static class PlayerClassExtensions
    {
        #region Constants

        private static readonly Dictionary<PlayerClassType, Func<ShipConstraints>> _shipLookup = new Dictionary<PlayerClassType, Func<ShipConstraints>>()
        {
            { PlayerClassType.Fighter, () => ConstraintsLibrary.GetConstraints<ShipConstraints>("Player") }
        };

        private static readonly Dictionary<PlayerClassType, Dictionary<Type, Func<ItemConstraints>>> _itemLookup = new Dictionary<PlayerClassType, Dictionary<Type, Func<ItemConstraints>>>()
        {
            { PlayerClassType.Fighter, new Dictionary<Type, Func<ItemConstraints>>()
                {
                    { typeof(Armor), () => ConstraintsLibrary.GetConstraints<ArmorConstraints>("Starter Armor") },
                    { typeof(Reactor), () => ConstraintsLibrary.GetConstraints<ReactorConstraints>("Starter Reactor") },
                    { typeof(Sensor), () => ConstraintsLibrary.GetConstraints<SensorConstraints>("Starter Sensor") },
                    { typeof(Thruster), () => ConstraintsLibrary.GetConstraints<ThrusterConstraints>("Starter Thruster") },
                    { typeof(Weapon), () => ConstraintsLibrary.GetConstraints<WeaponConstraints>("Starter Weapon") }
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
        public static ShipConstraints GetShipConstraints(this PlayerClassType playerClass)
        {
            return _shipLookup[playerClass]();
        }

        /// <summary>
        /// Gets the item constraints for the starter item for the specified
        /// player class of the specified type.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="playerClass">The player class.</param>
        /// <returns>The item constraints.</returns>
        public static ItemConstraints GetStarterItemConstraints<T>(this PlayerClassType playerClass)
        {
            if (_itemLookup[playerClass].ContainsKey(typeof(T)))
            {
                return _itemLookup[playerClass][typeof(T)]();
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}
