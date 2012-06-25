using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        protected IEnumerable<TComponent> Components { get { return _components; } }

        #endregion

        #region Fields

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        private HashSet<TComponent> _components = new HashSet<TComponent>();

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
        public override void Update(GameTime gameTime, long frame)
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
        public override void Draw(GameTime gameTime, long frame)
        {
            _updatingComponents.AddRange(_components);
            foreach (var component in _updatingComponents)
            {
                if (component.Enabled)
                {
                    //Todo hier schon begin /end? performanz?
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
                var component = ((ComponentAdded)(ValueType)message).Component;

                Debug.Assert(component.Entity > 0, "component.Entity > 0");
                Debug.Assert(component.Id > 0, "component.Id > 0");

                // Check if the component is of the right type.
                if (component is TComponent)
                {
                    var typedComponent = (TComponent)component;
                    if (!_components.Contains(typedComponent))
                    {
                        _components.Add(typedComponent);

                        // Tell subclasses.
                        OnComponentAdded(typedComponent);
                    }
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

                    if (_components.Remove(typedComponent))
                    {
                        OnComponentRemoved(typedComponent);
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
                copy._components = new HashSet<TComponent>();
                copy._updatingComponents = new List<TComponent>();
            }

            return copy;
        }

        #endregion
    }
}
