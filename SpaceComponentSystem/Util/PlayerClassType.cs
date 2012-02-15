using System;
using System.Collections.Generic;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.Data;

namespace Space.ComponentSystem.Util
{
    /// <summary>
    /// Utility methods for player classes.
    /// </summary>
    public static class PlayerClassExtensions
    {
        #region Constants

        private static readonly Dictionary<PlayerClassType, Func<ShipFactory>> _shipLookup = new Dictionary<PlayerClassType, Func<ShipFactory>>()
        {
            { PlayerClassType.Fighter, () => FactoryLibrary.GetConstraints<ShipFactory>("Player") }
        };

        private static readonly Dictionary<PlayerClassType, Dictionary<Type, Func<ItemFactory>>> _itemLookup = new Dictionary<PlayerClassType, Dictionary<Type, Func<ItemFactory>>>()
        {
            { PlayerClassType.Fighter, new Dictionary<Type, Func<ItemFactory>>()
                {
                    { typeof(Armor), () => FactoryLibrary.GetConstraints<ArmorFactory>("Starter Armor") },
                    { typeof(Reactor), () => FactoryLibrary.GetConstraints<ReactorFactory>("Starter Reactor") },
                    { typeof(Sensor), () => FactoryLibrary.GetConstraints<SensorFactory>("Starter Sensor") },
                    { typeof(Thruster), () => FactoryLibrary.GetConstraints<ThrusterFactory>("Starter Thruster") },
                    { typeof(Weapon), () => FactoryLibrary.GetConstraints<WeaponFactory>("Starter Weapon") }
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
        public static ShipFactory GetShipConstraints(this PlayerClassType playerClass)
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
        public static ItemFactory GetStarterItemConstraints<T>(this PlayerClassType playerClass)
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
