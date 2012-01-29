using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;

namespace Space.ComponentSystem.Constraints
{
    /// <summary>
    /// Allows lookup of constraints by name.
    /// </summary>
    public static class ConstraintsLibrary
    {
        #region Fields

        /// <summary>
        /// Mapping of types to names to constraints.
        /// </summary>
        private static readonly Dictionary<Type, Dictionary<string, object>> _constraints = new Dictionary<Type, Dictionary<string, object>>();

        private static bool _isInitialized;

        #endregion

        #region Initialization
        
        /// <summary>
        /// Initializes the library with the specified content manager.
        /// </summary>
        /// <param name="content">The content manager to use to load constraints.</param>
        public static void Initialize(ContentManager content)
        {
            if (!_isInitialized)
            {
                Initialize<ArmorConstraints>("Data/Armor", content);
                Initialize<ReactorConstraints>("Data/Reactors", content);
                Initialize<SensorConstraints>("Data/Sensors", content);
                Initialize<ShieldConstraints>("Data/Shields", content);
                Initialize<ShipConstraints>("Data/Ships", content);
                Initialize<ThrusterConstraints>("Data/Thrusters", content);
                Initialize<WeaponConstraints>("Data/Weapons", content);

                _isInitialized = true;
            }
        }

        /// <summary>
        /// Helper for initializing a specific type.
        /// </summary>
        private static void Initialize<T>(string assetName, ContentManager content)
        {
            _constraints[typeof(T)] = new Dictionary<string, object>();
            foreach (var constraint in content.Load<ArmorConstraints[]>("Data/Armor"))
            {
                _constraints[typeof(T)][constraint.Name] = constraint;
            }
        }

        #endregion

        #region Accessors
        
        /// <summary>
        /// Gets the constraints of the specified type with the specified name.
        /// </summary>
        /// <typeparam name="T">The type of constraints to get.</typeparam>
        /// <param name="name">The name of the constraints to get.</param>
        /// <returns>The constraints object.</returns>
        public static T GetConstraints<T>(string name)
        {
            return (T)_constraints[typeof(T)][name];
        }

        #endregion
    }
}
