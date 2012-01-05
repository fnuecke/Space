using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;

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
        public IComponentSystemManager Manager { get; set; }

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
        private readonly bool _isUpdateNullParameterized = (typeof(TUpdateParameterization) == typeof(NullParameterization));

        /// <summary>
        /// Whether the parameterization for the implementing class is the null
        /// parameterization, meaning we will never get any components.
        /// </summary>
        private readonly bool _isDrawNullParameterized = (typeof(TDrawParameterization) == typeof(NullParameterization));

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        private List<AbstractComponent> _updateableComponents = new List<AbstractComponent>();

        /// <summary>
        /// List of all currently registered components.
        /// </summary>
        private List<AbstractComponent> _drawableComponents = new List<AbstractComponent>();

        #endregion

        #region Interface

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
        /// <param name="frame">The frame in which the update is applied.</param>
        public virtual void Draw(long frame)
        {
        }

        /// <summary>
        /// Add the component to this system, if it's supported.
        /// </summary>
        /// <param name="component">The component to add.</param>
        /// <returns>This component system, for chaining.</returns>
        public virtual IComponentSystem AddComponent(AbstractComponent component)
        {
            bool wasAdded = false;
            if (!_isUpdateNullParameterized)
            {
                int index = _updateableComponents.BinarySearch(component, UpdateOrderComparer.Default);
                if (index < 0)
                {
                    if (component.SupportsParameterization(typeof(TUpdateParameterization)))
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
            }
            if (!_isDrawNullParameterized)
            {
                int index = _drawableComponents.BinarySearch(component, DrawOrderComparer.Default);
                if (index < 0)
                {
                    if (component.SupportsParameterization(typeof(TDrawParameterization)))
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
            if (!_isUpdateNullParameterized && _updateableComponents.Remove(component))
            {
                wasRemoved = true;
            }
            if (!_isDrawNullParameterized && _drawableComponents.Remove(component))
            {
                wasRemoved = true;
            }
            if (wasRemoved)
            {
                HandleComponentRemoved(component);
            }
        }

        /// <summary>
        /// Inform a system of a message that was sent by another system.
        /// 
        /// <para>
        /// Note that systems will also receive the messages they send themselves.
        /// </para>
        /// </summary>
        /// <param name="message">The sent message.</param>
        public virtual void HandleMessage(ValueType message)
        {
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public virtual Packet Packetize(Packet packet)
        {
            throw new NotSupportedException();
        }

        public virtual void Depacketize(Packet packet)
        {
            throw new NotSupportedException();
        }

        public virtual void Hash(Hasher hasher)
        {
        }

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
        public virtual object Clone()
        {
            // Get something to start with.
            var copy = (AbstractComponentSystem<TUpdateParameterization, TDrawParameterization>)MemberwiseClone();

            // If we're not null parameterized, use a different list. Copy over
            // non-entity components.
            var toClone = new HashSet<AbstractComponent>();
            if (!_isUpdateNullParameterized)
            {
                copy._updateableComponents = new List<AbstractComponent>();
                foreach (var component in _updateableComponents)
                {
                    if (component.Entity == null)
                    {
                        toClone.Add(component);
                    }
                }
            }
            if (!_isDrawNullParameterized)
            {
                copy._drawableComponents = new List<AbstractComponent>();
                foreach (var component in _drawableComponents)
                {
                    if (component.Entity == null)
                    {
                        toClone.Add(component);
                    }
                }
            }
            foreach (var component in toClone)
            {
                copy.AddComponent((AbstractComponent)component.Clone());
            }
            
            // No manager at first. Must be re-set in (e.g. in cloned manager).
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
