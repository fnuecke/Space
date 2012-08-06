using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Collections;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Util;

namespace Engine.ComponentSystem
{
    /// <summary>
    /// Type managing facilities for component system manager.
    /// </summary>
    public sealed partial class Manager
    {
        #region Type mapping

        /// <summary>
        /// Manages unique IDs for system types.
        /// </summary>
        private static readonly IdManager SystemTypeIds = new IdManager();

        /// <summary>
        /// Maps actual system types to their IDs.
        /// </summary>
        private static readonly Dictionary<Type, int> SystemTypes = new Dictionary<Type, int>();

        /// <summary>
        /// Manages unique IDs for component types.
        /// </summary>
        private static readonly IdManager ComponentTypeIds = new IdManager();

        /// <summary>
        /// Maps actual component types to their IDs.
        /// </summary>
        private static readonly Dictionary<Type, int> ComponentTypes = new Dictionary<Type, int>();

        /// <summary>
        /// Keeps track of type hierarchy among components, i.e. stores for each
        /// component its most direct, known parent.
        /// </summary>
        private static readonly SparseArray<int> ComponentHierarchy = new SparseArray<int>();

        #endregion

        #region Type Resolving

        /// <summary>
        /// Gets the system type id for the specified system type. This will
        /// create a new ID if necessary.
        /// </summary>
        /// <typeparam name="T">The system type to look up the id for.</typeparam>
        /// <returns>The type id for that system.</returns>
        public static int GetSystemTypeId<T>() where T : AbstractSystem
        {
            return GetSystemTypeId(typeof(T));
        }

        /// <summary>
        /// Gets the system type id for the specified system type. This will
        /// create a new ID if necessary.
        /// </summary>
        /// <param name="type">The system type to look up the id for.</param>
        /// <returns>The type id for that system.</returns>
        public static int GetSystemTypeId(Type type)
        {
            Debug.Assert(type.IsSubclassOf(typeof(AbstractSystem)));

            int typeId;
            if (!SystemTypes.TryGetValue(type, out typeId))
            {
                SystemTypes[type] = typeId = SystemTypeIds.GetId();
            }
            return typeId;
        }

        /// <summary>
        /// Gets the component type id for the specified component type. This will
        /// create a new ID if necessary.
        /// </summary>
        /// <typeparam name="T">The component type to look up the id for.</typeparam>
        /// <returns>The type id for that component.</returns>
        public static int GetComponentTypeId<T>() where T : Component
        {
            return GetComponentTypeId(typeof(T));
        }

        /// <summary>
        /// Gets the component type id for the specified component type. This will
        /// create a new ID if necessary.
        /// </summary>
        /// <param name="type">The component type to look up the id for.</param>
        /// <returns>The type id for that component.</returns>
        public static int GetComponentTypeId(Type type)
        {
            Debug.Assert(type.IsSubclassOf(typeof(Component)));

            int typeId;
            if (!ComponentTypes.TryGetValue(type, out typeId))
            {
                typeId = ComponentTypeIds.GetId();

                // New entry, update hierarchy.
                Type closestParentType = null;
                foreach (var otherType in ComponentTypes.Keys)
                {
                    // Check for parents.
                    if (type.IsSubclassOf(otherType))
                    {
                        // Got a potential parent, see if it's better than the one
                        // we already have.
                        if (closestParentType == null || // No other parent.
                            otherType.IsSubclassOf(closestParentType)) // Better than previous parent.
                        {
                            closestParentType = otherType;
                        }
                    }

                    // Check for children.
                    if (otherType.IsSubclassOf(type))
                    {
                        // Got a potential child, see if we're better than the
                        // parent it had before.
                        var otherTypeId = GetComponentTypeId(otherType);
                        var otherParentTypeId = ComponentHierarchy[otherTypeId];
                        if (otherParentTypeId == 0 || // Had no parent.
                            type.IsSubclassOf(GetComponentTypeForTypeId(otherParentTypeId))) // Better than previous parent.
                        {
                            ComponentHierarchy[otherTypeId] = typeId;
                        }
                    }
                }

                // If we found ourselves a parent, set it now.
                if (closestParentType != null)
                {
                    ComponentHierarchy[typeId] = GetComponentTypeId(closestParentType);
                }

                // Add to look-up table.
                ComponentTypes[type] = typeId;
            }
            return typeId;
        }

        /// <summary>
        /// Gets the parent type id for the specified type id, or zero.
        /// </summary>
        /// <param name="typeId">The type id to get the parent for.</param>
        /// <returns>The parent type id.</returns>
        public static int GetParentComponentType(int typeId)
        {
            return ComponentHierarchy[typeId];
        }

        /// <summary>
        /// Gets the component type for type id. This is an inverse dictionary
        /// lookup, which is a linear search and thus slow. But we only use it
        /// when adding component types, which shouldn't happen that often.
        /// </summary>
        /// <param name="typeId">The type id.</param>
        /// <returns>The actual component type.</returns>
        private static Type GetComponentTypeForTypeId(int typeId)
        {
            foreach (var i in ComponentTypes)
            {
                if (i.Value == typeId)
                {
                    return i.Key;
                }
            }
            throw new ArgumentException("Unknown type.");
        }

        #endregion
    }
}
