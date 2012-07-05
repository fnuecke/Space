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
        #region Fields

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        protected HashSet<TComponent> Components = new HashSet<TComponent>();

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
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Update(GameTime gameTime, long frame)
        {
            UpdatingComponents.AddRange(Components);
            foreach (var component in UpdatingComponents)
            {
                if (component.Enabled)
                {
                    UpdateComponent(gameTime, frame, component);
                }
            }
            UpdatingComponents.Clear();
        }

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Draw(GameTime gameTime, long frame)
        {
            foreach (var component in Components)
            {
                if (component.Enabled)
                {
                    DrawComponent(gameTime, frame, component);
                }
            }
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
                    if (!Components.Contains(typedComponent))
                    {
                        Components.Add(typedComponent);

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

                    if (Components.Remove(typedComponent))
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
                copy.Components.Clear();
            }
            else
            {
                copy.Components = new HashSet<TComponent>();
                copy.UpdatingComponents = new List<TComponent>();
            }

            return copy;
        }

        #endregion
    }
}
