using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>Base class for component systems, pre-implementing adding / removal of components.</summary>
    /// <typeparam name="TComponent">The type of component handled in this system.</typeparam>
    public abstract class AbstractComponentSystem<TComponent> : AbstractSystem
        where TComponent : IComponent
    {
        #region Fields

        /// <summary>List of all currently registered components.</summary>
        [CopyIgnore, PacketizeIgnore]
        protected List<TComponent> Components = new List<TComponent>();

        #endregion

        #region Manager Events

        /// <summary>Called by the manager when a new component was added.</summary>
        /// <param name="message"></param>
        [MessageCallback]
        public virtual void OnComponentAdded(ComponentAdded message)
        {
            Debug.Assert(message.Component.Entity > 0, "component.Entity > 0");
            Debug.Assert(message.Component.Id > 0, "component.Id > 0");

            // Check if the component is of the right type.
            if (!(message.Component is TComponent))
            {
                return;
            }

            // Keep components in order, to stay deterministic.
            var component = (TComponent) message.Component;
            var index = Components.BinarySearch(component);
            Debug.Assert(index < 0);
            Components.Insert(~index, component);
        }

        /// <summary>Called by the manager when a new component was removed.</summary>
        /// <param name="message"></param>
        [MessageCallback]
        public virtual void OnComponentRemoved(ComponentRemoved message)
        {
            Debug.Assert(message.Component.Entity > 0, "component.Entity > 0");
            Debug.Assert(message.Component.Id > 0, "component.Id > 0");

            // Check if the component is of the right type.
            if (!(message.Component is TComponent))
            {
                return;
            }

            // Take advantage of the fact that the list is sorted.
            var component = (TComponent) message.Component;
            var index = Components.BinarySearch(component);
            Debug.Assert(index >= 0);
            Components.RemoveAt(index);
        }

        /// <summary>Called by the manager when the complete environment has been copied or depacketized.</summary>
        [MessageCallback]
        public virtual void OnInitialize(Initialize message)
        {
            Components.Clear();
            foreach (var typedComponent in Manager.Components.OfType<TComponent>())
            {
                // Components are in order (we are iterating in order), so
                // just add it at the end.
                Debug.Assert(Components.BinarySearch(typedComponent) < 0);
                Components.Add(typedComponent);
            }
        }

        #endregion

        #region Copying

        /// <summary>
        ///     Servers as a copy constructor that returns a new instance of the same type that is freshly initialized.
        ///     <para>This takes care of duplicating reference types to a new copy of that type (e.g. collections).</para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (AbstractComponentSystem<TComponent>) base.NewInstance();

            copy.Components = new List<TComponent>();

            return copy;
        }

        /// <summary>
        ///     Creates a deep copy of the system. The passed system must be of the same type.
        ///     <para>
        ///         This clones any contained data types to return an instance that represents a complete copy of the one passed
        ///         in.
        ///     </para>
        /// </summary>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        /// <remarks>
        ///     This will <em>not</em> fill the copy with the same components as this one has; this is done in the
        ///     <see cref="OnInitialize"/> callback, to allow proper copying even for systems that may not be present in the manager
        ///     that was the source (in particular: presentation related systems).
        /// </remarks>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            ((AbstractComponentSystem<TComponent>) into).Components.Clear();
        }

        #endregion
    }
}