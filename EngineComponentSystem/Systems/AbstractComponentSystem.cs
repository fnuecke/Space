using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Base class for component systems, pre-implementing adding / removal
    /// of components.
    /// 
    /// <para>
    /// Subclasses should take note that when cloning they must take care of
    /// duplicating reference types, to complete the deep-copy of the object.
    /// Caches, i.e. lists / dictionaries / etc. to quickly look up components
    /// should be reset.
    /// </para>
    /// </summary>
    /// <typeparam name="TUpdateParameterization">the type of parameterization used in this system</typeparam>
    public abstract class AbstractComponentSystem<TUpdateParameterization, TDrawParameterization> : AbstractSystem
    {
        #region Constants

        /// <summary>
        /// Whether the parameterization for the implementing class is the null
        /// parameterization, meaning we will never get any components.
        /// </summary>
        private static readonly bool _isUpdateNullParameterized = (typeof(TUpdateParameterization) == typeof(NullParameterization));

        /// <summary>
        /// Whether the parameterization for the implementing class is the null
        /// parameterization, meaning we will never get any components.
        /// </summary>
        private static readonly bool _isDrawNullParameterized = (typeof(TDrawParameterization) == typeof(NullParameterization));

        #endregion

        #region Properties

        /// <summary>
        /// A list of components registered in this system.
        /// </summary>
        protected ReadOnlyCollection<AbstractComponent> UpdateableComponents { get { return _updateableComponents.AsReadOnly(); } }

        /// <summary>
        /// A list of components registered in this system.
        /// </summary>
        protected ReadOnlyCollection<AbstractComponent> DrawableComponents { get { return _drawableComponents.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        private List<AbstractComponent> _updateableComponents = new List<AbstractComponent>();

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        private List<AbstractComponent> _drawableComponents = new List<AbstractComponent>();

        #endregion

        #region Messaging

        /// <summary>
        /// Inform a system of a message that was sent by another system.
        /// 
        /// <para>
        /// Note that systems will also receive the messages they send themselves.
        /// </para>
        /// </summary>
        /// <param name="message">The sent message.</param>
        public override void HandleMessage<T>(ref T message)
        {
            // Check if it was an entity added / removed message. If so, add or
            // remove all components of that entity.
            if (message is EntityAdded)
            {
                foreach (var component in ((EntityAdded)(ValueType)message).Entity.Components)
                {
                    AddComponent(component);
                }
            }
            else if (message is EntityRemoved)
            {
                foreach (var component in ((EntityRemoved)(ValueType)message).Entity.Components)
                {
                    RemoveComponent(component);
                }
            }
            else if (message is EntitiesCleared)
            {
                Clear();
            }
            else if (message is ComponentAdded)
            {
                AddComponent(((ComponentAdded)(ValueType)message).Component);
            }
            else if (message is ComponentRemoved)
            {
                RemoveComponent(((ComponentAdded)(ValueType)message).Component);
            }
        }

        /// <summary>
        /// Add the component to this system, if it's supported.
        /// </summary>
        /// <param name="component">The component to add.</param>
        /// <returns>This component system, for chaining.</returns>
        private void AddComponent(AbstractComponent component)
        {
            bool wasAdded = false;

            // Does this component support our update parameterization?
            if (SupportsComponentForUpdate(component))
            {
                // Yes, find the index to insert at.
                int index = _updateableComponents.BinarySearch(component, UpdateOrderComparer.Default);
                if (index < 0)
                {
                    // Not in list yet, so the complement is the index to
                    // insert at.
                    index = ~index;

                    // But place it at the end of components with the same
                    // priority, so that elements that were added later will
                    // be updated last.
                    while ((index < _updateableComponents.Count) && (_updateableComponents[index].UpdateOrder == component.UpdateOrder))
                    {
                        index++;
                    }

                    // Got our index, insert.
                    _updateableComponents.Insert(index, component);
                    wasAdded = true;
                }
            }

            // Does this component support our draw parameterization?
            if (SupportsComponentForDraw(component))
            {
                // Yes, find the index to insert at.
                int index = _drawableComponents.BinarySearch(component, DrawOrderComparer.Default);
                if (index < 0)
                {
                    // Not in list yet, so the complement is the index to
                    // insert at.
                    index = ~index;

                    // But place it at the end of components with the same
                    // priority, so that elements that were added later will
                    // be updated last.
                    while ((index < _drawableComponents.Count) && (_drawableComponents[index].DrawOrder == component.DrawOrder))
                    {
                        index++;
                    }

                    // Got our index, insert.
                    _drawableComponents.Insert(index, component);
                    wasAdded = true;
                }
            }

            // If we added the component, let subclasses know.
            if (wasAdded)
            {
                HandleComponentAdded(component);
            }
        }

        /// <summary>
        /// Removes the component from the system, if it's in it.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        private void RemoveComponent(AbstractComponent component)
        {
            bool wasRemoved = false;

            // Remove, if we have it.
            if (_updateableComponents.Remove(component))
            {
                wasRemoved = true;
            }
            if (_drawableComponents.Remove(component))
            {
                wasRemoved = true;
            }

            // If we actually removed it, let subclasses know.
            if (wasRemoved)
            {
                HandleComponentRemoved(component);
            }
        }

        /// <summary>
        /// Removes all components from this system.
        /// </summary>
        protected virtual void Clear()
        {
            _updateableComponents.Clear();
            _drawableComponents.Clear();
        }

        #endregion

        #region Overridable

        /// <summary>
        /// Allows filtering which components should be added as updateable.
        /// </summary>
        /// <remarks>
        /// Per default this is delegated to the component (asking it if it
        /// knows our parameterization), given we have one. If we are null
        /// parameterized (<c>NullParameterization</c>) this will always return
        /// false per default.
        /// </remarks>
        /// <param name="component">The component to check.</param>
        /// <returns>Whether to allow adding it or not.</returns>
        protected virtual bool SupportsComponentForUpdate(AbstractComponent component)
        {
            return !_isUpdateNullParameterized && component.SupportsUpdateParameterization(typeof(TUpdateParameterization));
        }

        /// <summary>
        /// Allows filtering which components should be added as drawable.
        /// </summary>
        /// <remarks>
        /// Per default this is delegated to the component (asking it if it
        /// knows our parameterization), given we have one. If we are null
        /// parameterized (<c>NullParameterization</c>) this will always return
        /// false per default.
        /// </remarks>
        /// <param name="component">The component to check.</param>
        /// <returns>Whether to allow adding it or not.</returns>
        protected virtual bool SupportsComponentForDraw(AbstractComponent component)
        {
            return !_isDrawNullParameterized && component.SupportsDrawParameterization(typeof(TDrawParameterization));
        }

        /// <summary>
        /// Perform actions for newly added components.
        /// </summary>
        /// <param name="component">The component that was added.</param>
        protected virtual void HandleComponentAdded(AbstractComponent component)
        {
        }

        /// <summary>
        /// Perform actions for removed components.
        /// </summary>
        /// <param name="component">The component that was removed.</param>
        protected virtual void HandleComponentRemoved(AbstractComponent component)
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
        public override ISystem DeepCopy(ISystem into)
        {
            // Get something to start with.
            var copy = (AbstractComponentSystem<TUpdateParameterization, TDrawParameterization>)base.DeepCopy(into);
            
            if (copy == into)
            {
                copy._updateableComponents.Clear();
                copy._drawableComponents.Clear();
            }
            else
            {
                copy._updateableComponents = new List<AbstractComponent>();
                copy._drawableComponents = new List<AbstractComponent>();
            }

            return copy;
        }

        #endregion

        #region Comparer

        /// <summary>
        /// Comparer used for inserting / removal.
        /// </summary>
        private sealed class UpdateOrderComparer : IComparer<AbstractComponent>
        {
            public static readonly UpdateOrderComparer Default = new UpdateOrderComparer();

            public int Compare(AbstractComponent x, AbstractComponent y)
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

        /// <summary>
        /// Comparer used for inserting / removal.
        /// </summary>
        private sealed class DrawOrderComparer : IComparer<AbstractComponent>
        {
            public static readonly DrawOrderComparer Default = new DrawOrderComparer();

            public int Compare(AbstractComponent x, AbstractComponent y)
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
                    if (x.DrawOrder < y.DrawOrder)
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
