﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Engine.Collections;
using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem
{
    /// <summary>Entity utility type, for keeping track of components attached to an entity.</summary>
    public sealed partial class Manager
    {
        /// <summary>
        ///     Represents an entity, for easier internal access. We do not expose this class, to keep the whole component
        ///     system's representation to the outside world 'flatter', which also means it's easier to copy and serialize, and to
        ///     guarantee that everything in the system is in a valid state.
        /// </summary>
        [DebuggerDisplay("Components = {Components.Count}")]
        private sealed class Entity
        {
            #region Fields

            /// <summary>List of all components attached to this entity.</summary>
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public readonly List<Component> Components = new List<Component>();

            /// <summary>Cache used for faster look-up of components of a specific type.</summary>
            public readonly SparseArray<List<Component>> TypeCache = new SparseArray<List<Component>>();

            #endregion

            #region Accessors

            /// <summary>Adds the specified component.</summary>
            /// <param name="component">The component.</param>
            public void Add(Component component)
            {
                // Add to overall list. This list does not have to be sorted,
                // because it's order is deterministic anyway, as it'll be
                // serialized / deserialized in deterministic order.
                Components.Add(component);

                // Add to all relevant caches.
                foreach (var typeId in ComponentHierarchy[component.GetTypeId()])
                {
                    // Register for type, create list if necessary.
                    if (TypeCache[typeId] == null)
                    {
                        TypeCache[typeId] = new List<Component>();
                    }
                    
                    // Keep components in type caches sorted, to keep looping
                    // over them deterministic (otherwise recently deserialized
                    // instances may behave differently).
                    TypeCache[typeId].Insert(~TypeCache[typeId].BinarySearch(component), component);
                }
            }

            /// <summary>Removes the specified component.</summary>
            /// <param name="component">The component.</param>
            public void Remove(Component component)
            {
                // Remove from overall list.
                Components.Remove(component);

                // Remove from all relevant caches.
                foreach (var typeId in ComponentHierarchy[component.GetTypeId()])
                {
                    if (TypeCache[typeId] != null)
                    {
                        TypeCache[typeId].RemoveAt(TypeCache[typeId].BinarySearch(component));
                    }
                }
            }

            /// <summary>Gets the first component of the specified type.</summary>
            /// <param name="typeId">The type id of the component.</param>
            /// <returns>The first component of that type.</returns>
            public Component GetComponent(int typeId)
            {
                if (TypeCache[typeId] == null)
                {
                    BuildTypeCache(typeId);
                }
                Contract.Assume(TypeCache[typeId] != null);
                return TypeCache[typeId].Count > 0 ? TypeCache[typeId][0] : null;
            }

            /// <summary>Gets the components of the specified type.</summary>
            /// <param name="typeId">The type of the components to get.</param>
            /// <returns>The components of that type.</returns>
            public IEnumerable<Component> GetComponents(int typeId)
            {
                if (TypeCache[typeId] == null)
                {
                    BuildTypeCache(typeId);
                }
                return TypeCache[typeId];
            }

            /// <summary>
            ///     Builds the type cache for the specified type. We may need to do this for types that were registered after the
            ///     creation of this entity.
            /// </summary>
            /// <param name="typeId">The type id.</param>
            private void BuildTypeCache(int typeId)
            {
                lock (this)
                {
                    // Test again after locking, because the cache might have
                    // actually already been built by another thread between the
                    // outer check and getting here.
                    if (TypeCache[typeId] != null)
                    {
                        return;
                    }

                    // No cache for this type yet, create it.
                    TypeCache[typeId] = new List<Component>();

                    // Iterate over all known components.
                    for (int i = 0, count = Components.Count; i < count; ++i)
                    {
                        // Check for direct match or if it's a subclass.
                        if (ComponentHierarchy[Components[i].GetTypeId()].Contains(typeId))
                        {
                            TypeCache[typeId].Insert(~TypeCache[typeId].BinarySearch(Components[i]), Components[i]);
                        }
                    }
                }
            }

            #endregion
        }
    }
}