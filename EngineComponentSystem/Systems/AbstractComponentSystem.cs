using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Microsoft.Xna.Framework;

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
        #region Properties

        /// <summary>
        /// A list of components registered in this system.
        /// </summary>
        protected ReadOnlyCollection<TComponent> Components { get { return _components.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        private List<TComponent> _components = new List<TComponent>();

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components when updating, to avoid
        /// modifications to the list of components breaking the update.
        /// </summary>
        private List<TComponent> _updatingComponents = new List<TComponent>();

        #endregion

        #region Logic

        /// <summary>
        /// Loops over all components and calls <c>UpdateComponent()</c>.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public sealed override void Update(GameTime gameTime, long frame)
        {
            _updatingComponents.AddRange(_components);
            foreach (var component in _updatingComponents)
            {
                if (component.Enabled)
                {
                    UpdateComponent(gameTime, frame, component);
                }
            }
            _updatingComponents.Clear();
        }

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public sealed override void Draw(GameTime gameTime, long frame)
        {
            _updatingComponents.AddRange(_components);
            foreach (var component in _updatingComponents)
            {
                if (component.Enabled)
                {
                    DrawComponent(gameTime, frame, component);
                }
            }
            _updatingComponents.Clear();
        }

        /// <summary>
        /// Applies the system's logic to the specified component.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="component">The component to update.</param>
        protected virtual void UpdateComponent(GameTime gameTime, long frame, TComponent component)
        {
        }

        /// <summary>
        /// Applies the system's rendering to the specified component.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="component">The component to draw.</param>
        protected virtual void DrawComponent(GameTime gameTime, long frame, TComponent component)
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
                TryAdd(((ComponentAdded)(ValueType)message).Component);
            }
            else if (message is ComponentRemoved)
            {
                TryRemove(((ComponentRemoved)(ValueType)message).Component);
            }
            else if (message is EntityRemoved)
            {
                foreach (var component in Manager.GetComponents(((EntityRemoved)(ValueType)message).Entity))
                {
                    TryRemove(component);
                }
            }
        }

        /// <summary>
        /// Adds the specified component only if its of the correct type.
        /// </summary>
        /// <param name="component">The component to add.</param>
        private void TryAdd(Component component)
        {
            // Check if the component is of the right type.
            if (component is TComponent)
            {
                var typedComponent = (TComponent)component;

                // Yes, find the index to insert at.
                int index = _components.BinarySearch(typedComponent, UpdateOrderComparer.Default);
                if (index < 0)
                {
                    // Not in list yet, so the complement is the index to
                    // insert at.
                    index = ~index;

                    // But place it at the end of components with the same
                    // priority, so that elements that were added later will
                    // be updated last.
                    while ((index < _components.Count) && (_components[index].UpdateOrder == component.UpdateOrder))
                    {
                        index++;
                    }

                    // Got our index, insert.
                    _components.Insert(index, typedComponent);

                    // Tell subclasses.
                    OnComponentAdded(typedComponent);
                }
            }
        }

        /// <summary>
        /// Removes the specified component if its of the correct type.
        /// </summary>
        /// <param name="component">The component.</param>
        private void TryRemove(Component component)
        {
            // Check if the component is of the right type.
            if (component is TComponent)
            {
                var typedComponent = (TComponent)component;

                if (_components.Remove(typedComponent))
                {
                    OnComponentRemoved(typedComponent);
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

        #region Copying

        /// <summary>
        /// Creates a deep copy, with a component list only containing
        /// clones of components not bound to an entity. If possible, the
        /// specified instance will be reused.
        /// 
        /// <para>
        /// Subclasses must take care of duplicating reference types, to complete
        /// the deep-copy of the object. Caches, i.e. lists / dictionaries / etc.
        /// to quickly look up components must be reset / rebuilt.
        /// </para>
        /// </summary>
        /// <returns>A deep, with a semi-cleared copy of this system.</returns>
        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            // Get something to start with.
            var copy = (AbstractComponentSystem<TComponent>)base.DeepCopy(into);
            
            if (copy == into)
            {
                copy._components.Clear();
            }
            else
            { 
                copy._components = new List<TComponent>();
                copy._updatingComponents = new List<TComponent>();
            }

            return copy;
        }

        #endregion

        #region Comparer

        /// <summary>
        /// Comparer used for inserting / removal.
        /// </summary>
        private sealed class UpdateOrderComparer : IComparer<Component>
        {
            public static readonly UpdateOrderComparer Default = new UpdateOrderComparer();

            public int Compare(Component x, Component y)
            {
                if ((x == null) && (y == null))
                {
                    return 0;
                }
                if (x != null)
                {
                    if (y == null)
                    {
                        return -1;
                    }
                    if (x.Equals(y))
                    {
                        return 0;
                    }
                    if (x.UpdateOrder < y.UpdateOrder)
                    {
                        return -1;
                    }
                }
                return 1;
            }
        }

        #endregion
    }
}
