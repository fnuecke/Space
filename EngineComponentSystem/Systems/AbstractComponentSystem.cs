using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Utility base class for component systems, pre-implementing adding / removal
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
    public abstract class AbstractComponentSystem<TUpdateParameterization, TDrawParameterization> : IComponentSystem
    {
        #region Properties

        /// <summary>
        /// The component system manager this system is part of.
        /// </summary>
        public virtual IComponentSystemManager Manager { get; set; }

        /// <summary>
        /// A list of components registered in this system.
        /// </summary>
        public ReadOnlyCollection<AbstractComponent> UpdateableComponents { get { return _updateableComponents.AsReadOnly(); } }

        /// <summary>
        /// A list of components registered in this system.
        /// </summary>
        public ReadOnlyCollection<AbstractComponent> DrawableComponents { get { return _drawableComponents.AsReadOnly(); } }

        /// <summary>
        /// Tells if this component system should be packetized and sent via
        /// the network (server to client). This should only be true for logic
        /// related systems, that affect functionality that has to work exactly
        /// the same on both server and client.
        /// 
        /// <para>
        /// If the game has no network functionality, this flag is irrelevant.
        /// </para>
        /// </summary>
        public bool ShouldSynchronize { get; protected set; }

        #endregion

        #region Fields

        /// <summary>
        /// Whether the parameterization for the implementing class is the null
        /// parameterization, meaning we will never get any components.
        /// </summary>
        private bool _isUpdateNullParameterized = (typeof(TUpdateParameterization) == typeof(NullParameterization));

        /// <summary>
        /// Whether the parameterization for the implementing class is the null
        /// parameterization, meaning we will never get any components.
        /// </summary>
        private bool _isDrawNullParameterized = (typeof(TDrawParameterization) == typeof(NullParameterization));

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        private List<AbstractComponent> _updateableComponents = new List<AbstractComponent>();

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        private List<AbstractComponent> _drawableComponents = new List<AbstractComponent>();

        #endregion

        #region Logic

        /// <summary>
        /// Default implementation does nothing.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public virtual void Update(long frame)
        {
        }

        /// <summary>
        /// Default implementation does nothing.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public virtual void Draw(GameTime gameTime, long frame)
        {
        }

        #endregion

        #region Components

        /// <summary>
        /// Add the component to this system, if it's supported.
        /// </summary>
        /// <param name="component">The component to add.</param>
        /// <returns>This component system, for chaining.</returns>
        public IComponentSystem AddComponent(AbstractComponent component)
        {
            bool wasAdded = false;
            if (SupportsComponentUpdate(component))
            {
                int index = _updateableComponents.BinarySearch(component, UpdateOrderComparer.Default);
                if (index < 0)
                {
                    index = ~index;
                    while ((index < _updateableComponents.Count) && (_updateableComponents[index].UpdateOrder == component.UpdateOrder))
                    {
                        index++;
                    }
                    _updateableComponents.Insert(index, component);
                    wasAdded = true;
                }
            }
            if (SupportsComponentDraw(component))
            {
                int index = _drawableComponents.BinarySearch(component, DrawOrderComparer.Default);
                if (index < 0)
                {
                    index = ~index;
                    while ((index < _drawableComponents.Count) && (_drawableComponents[index].DrawOrder == component.DrawOrder))
                    {
                        index++;
                    }
                    _drawableComponents.Insert(index, component);
                    wasAdded = true;
                }
            }
            if (wasAdded)
            {
                HandleComponentAdded(component);
            }
            return this;
        }

        /// <summary>
        /// Removes the component from the system, if it's in it.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveComponent(AbstractComponent component)
        {
            bool wasRemoved = false;
            if (_updateableComponents.Remove(component))
            {
                wasRemoved = true;
            }
            if (_drawableComponents.Remove(component))
            {
                wasRemoved = true;
            }
            if (wasRemoved)
            {
                HandleComponentRemoved(component);
            }
        }

        /// <summary>
        /// Removes all components from this system.
        /// </summary>
        public virtual void Clear()
        {
            _updateableComponents.Clear();
            _drawableComponents.Clear();
        }

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
        protected virtual bool SupportsComponentUpdate(AbstractComponent component)
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
        protected virtual bool SupportsComponentDraw(AbstractComponent component)
        {
            return !_isDrawNullParameterized && component.SupportsDrawParameterization(typeof(TDrawParameterization));
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Inform all components in this system of a message.
        /// </summary>
        /// <param name="message">The sent message.</param>
        public void SendMessageToComponents<T>(ref T message) where T : struct
        {
        }

        /// <summary>
        /// Inform a system of a message that was sent by another system.
        /// 
        /// <para>
        /// Note that systems will also receive the messages they send themselves.
        /// </para>
        /// </summary>
        /// <param name="message">The sent message.</param>
        public virtual void HandleMessage<T>(ref T message) where T : struct
        {
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public virtual Packet Packetize(Packet packet)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public virtual void Depacketize(Packet packet)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public virtual void Hash(Hasher hasher)
        {
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy, with a component list only containing
        /// clones of components not bound to an entity.
        /// 
        /// <para>
        /// Subclasses must take care of duplicating reference types, to complete
        /// the deep-copy of the object. Caches, i.e. lists / dictionaries / etc.
        /// to quickly look up components must be reset / rebuilt.
        /// </para>
        /// </summary>
        /// <returns>A deep, with a semi-cleared copy of this system.</returns>
        public IComponentSystem DeepCopy()
        {
            return DeepCopy(null);
        }

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
        public virtual IComponentSystem DeepCopy(IComponentSystem into)
        {
            // Get something to start with.
            var copy = (AbstractComponentSystem<TUpdateParameterization, TDrawParameterization>)
                ((into != null && into.GetType() == this.GetType())
                ? into
                : MemberwiseClone());

            if (copy == into)
            {
                copy.ShouldSynchronize = ShouldSynchronize;
                copy._isUpdateNullParameterized = _isUpdateNullParameterized;
                copy._isDrawNullParameterized = _isDrawNullParameterized;
                copy._updateableComponents.Clear();
                copy._drawableComponents.Clear();
            }
            else
            {
                copy._updateableComponents = new List<AbstractComponent>();
                copy._drawableComponents = new List<AbstractComponent>();
            }

            // No manager at first. Must be re-set (e.g. in cloned manager).
            copy.Manager = null;

            return copy;
        }

        #endregion

        #region Internal components tracking

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
