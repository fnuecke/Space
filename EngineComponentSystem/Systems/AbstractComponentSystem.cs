using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.Util;

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
        protected List<TComponent> Components = new List<TComponent>();

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components when updating, to avoid
        /// modifications to the list of components breaking the update.
        /// </summary>
        protected List<TComponent> UpdatingComponents = new List<TComponent>();

        #endregion

        #region Logic

        /// <summary>
        /// Loops over all components and calls <c>UpdateComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Update(long frame)
        {
            UpdatingComponents.AddRange(Components);
            foreach (var component in UpdatingComponents)
            {
                if (component.Enabled)
                {
                    UpdateComponent(frame, component);
                }
            }
            UpdatingComponents.Clear();
        }

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Draw(long frame)
        {
            foreach (var component in Components)
            {
                if (component.Enabled)
                {
                    DrawComponent(frame, component);
                }
            }
        }

        /// <summary>
        /// Applies the system's logic to the specified component.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="component">The component to update.</param>
        protected virtual void UpdateComponent(long frame, TComponent component)
        {
        }

        /// <summary>
        /// Applies the system's rendering to the specified component.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="component">The component to draw.</param>
        protected virtual void DrawComponent(long frame, TComponent component)
        {
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Checks for added and removed components, and stores / forgets them
        /// if they are of the type handled in this system.
        /// </summary>
        /// <param name="message">The sent message.</param>
        public override void Receive<T>(ref T message)
        {
            if (message is ComponentAdded)
            {
                var component = ((ComponentAdded)(ValueType)message).Component;

                Debug.Assert(component.Entity > 0, "component.Entity > 0");
                Debug.Assert(component.Id > 0, "component.Id > 0");

                // Check if the component is of the right type.
                if (component is TComponent)
                {
                    var typedComponent = (TComponent)component;
                    Debug.Assert(!Components.Contains(typedComponent));

                    // Keep components in order, to stay deterministic.
                    Components.Insert(~Components.BinarySearch(typedComponent, Component.Comparer), typedComponent);

                    // Tell subclasses.
                    OnComponentAdded(typedComponent);
                }
            }
            else if (message is ComponentRemoved)
            {
                var component = ((ComponentRemoved)(ValueType)message).Component;

                Debug.Assert(component.Entity > 0, "component.Entity > 0");
                Debug.Assert(component.Id > 0, "component.Id > 0");

                // Check if the component is of the right type.
                if (component is TComponent)
                {
                    var typedComponent = (TComponent)component;

                    var index = Components.BinarySearch(typedComponent, Component.Comparer);
                    if (index >= 0)
                    {
                        Components.RemoveAt(index);
                        OnComponentRemoved(typedComponent);
                    }
                }
            }
            else if (message is Depacketized)
            {
                // Done depacketizing. Instead of sending all components in
                // the game state, we just rebuild the list afterwards, which
                // saves us a lot of bandwidth.
                Components.Clear();
                foreach (var component in Manager.Components)
                {
                    if (component is TComponent)
                    {
                        var typedComponent = (TComponent)component;
                        Debug.Assert(!Components.Contains(typedComponent));

                        // Components are in order (we are iterating in order).
                        Components.Add(typedComponent);

                        // Do *NOT* tell our subclasses, as this is semantically
                        // different to a new component being added.
                    }
                }
            }
        }

        #endregion

        #region Overridable

        /// <summary>
        /// Perform actions for newly added components.
        /// </summary>
        /// <param name="component">The component that was added.</param>
        protected virtual void OnComponentAdded(TComponent component)
        {
        }

        /// <summary>
        /// Perform actions for removed components.
        /// </summary>
        /// <param name="component">The component that was removed.</param>
        protected virtual void OnComponentRemoved(TComponent component)
        {
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
            copy.UpdatingComponents = new List<TComponent>();

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
        /// <remarks>The manager for the system to copy into must be set to the
        /// manager into which the system is being copied.</remarks>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (AbstractComponentSystem<TComponent>)into;

            copy.Components.Clear();
            foreach (var component in Components)
            {
                var componentCopy = copy.Manager.GetComponentById(component.Id);
                Debug.Assert(componentCopy is TComponent);
                copy.Components.Add((TComponent)componentCopy);
            }
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
