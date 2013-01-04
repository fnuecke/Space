using System.Collections.Generic;
using System.Diagnostics;
using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Base class for component systems, pre-implementing adding / removal
    /// of components.
    /// </summary>
    /// <typeparam name="TComponent">The type of component handled in this system.</typeparam>
    public abstract class AbstractComponentSystem<TComponent> : AbstractSystem
        where TComponent : Component
    {
        #region Fields

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        protected List<TComponent> Components = new List<TComponent>();

        #endregion

        #region Manager Events

        /// <summary>
        /// Called by the manager when a new component was added.
        /// </summary>
        /// <param name="component">The component that was added.</param>
        public override void OnComponentAdded(Component component)
        {
            base.OnComponentAdded(component);

            Debug.Assert(component.Entity > 0, "component.Entity > 0");
            Debug.Assert(component.Id > 0, "component.Id > 0");

            // Check if the component is of the right type.
            if (component is TComponent)
            {
                var typedComponent = (TComponent)component;

                // Keep components in order, to stay deterministic.
                var index = Components.BinarySearch(typedComponent, Component.Comparer);
                Debug.Assert(index < 0);
                Components.Insert(~index, typedComponent);
            }
        }

        /// <summary>
        /// Called by the manager when a new component was removed.
        /// </summary>
        /// <param name="component">The component that was removed.</param>
        public override void OnComponentRemoved(Component component)
        {
            base.OnComponentRemoved(component);

            Debug.Assert(component.Entity > 0, "component.Entity > 0");
            Debug.Assert(component.Id > 0, "component.Id > 0");

            // Check if the component is of the right type.
            if (component is TComponent)
            {
                var typedComponent = (TComponent)component;

                // Take advantage of the fact that the list is sorted.
                var index = Components.BinarySearch(typedComponent, Component.Comparer);
                Debug.Assert(index >= 0);
                Components.RemoveAt(index);
            }
        }

        /// <summary>
        /// Called by the manager when the complete environment has been
        /// depacketized.
        /// </summary>
        public override void OnDepacketized()
        {
            base.OnDepacketized();

            RebuildComponentList();
        }

        /// <summary>
        /// Called by the manager when the complete environment has been
        /// copied from another manager.
        /// </summary>
        public override void OnCopied()
        {
            base.OnCopied();

            RebuildComponentList();
        }

        /// <summary>
        /// Rebuilds the component list by fetching all components handled by us.
        /// </summary>
        private void RebuildComponentList()
        {
            Components.Clear();
            foreach (var component in Manager.Components)
            {
                if (component is TComponent)
                {
                    var typedComponent = (TComponent)component;

                    // Components are in order (we are iterating in order), so
                    // just add it at the end.
                    Debug.Assert(Components.BinarySearch(typedComponent, Component.Comparer) < 0);
                    Components.Add(typedComponent);
                }
            }
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Components.Count);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Servers as a copy constructor that returns a new instance of the same
        /// type that is freshly initialized.
        /// 
        /// <para>
        /// This takes care of duplicating reference types to a new copy of that
        /// type (e.g. collections).
        /// </para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (AbstractComponentSystem<TComponent>)base.NewInstance();

            copy.Components = new List<TComponent>();

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// 
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        /// <remarks>
        /// This will <em>not</em> fill the copy with the same components as this
        /// one has; this is done in the <see cref="OnCopied"/> callback, to allow
        /// proper copying even for systems that may not be present in the manager
        /// that was the source (in particular: presentation related systems).
        /// </remarks>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            ((AbstractComponentSystem<TComponent>)into).Components.Clear();
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", ComponentCount=" + Components.Count;
        }

        #endregion
    }
}
