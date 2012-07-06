using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.Serialization;
using Engine.Util;
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

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <remarks>
        /// Must be overridden in subclasses setting <c>ShouldSynchronize</c>
        /// to true.
        /// </remarks>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(Components.Count);
            foreach (var component in Components)
            {
                packet.Write(component.Id);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <remarks>
        /// Must be overridden in subclasses setting <c>ShouldSynchronize</c>
        /// to true.
        /// </remarks>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Components.Clear();
            var numComponents = packet.ReadInt32();
            for (var i = 0; i < numComponents; ++i)
            {
                var componentId = packet.ReadInt32();
                var component = Manager.GetComponentById(componentId);

                Debug.Assert(component is TComponent);

                Components.Add((TComponent)component);
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Components.Count));
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

            copy.Components = new HashSet<TComponent>();
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
        public override AbstractSystem CopyInto(AbstractSystem into)
        {
            // Get something to start with.
            var copy = (AbstractComponentSystem<TComponent>)base.CopyInto(into);

            copy.Components.Clear();
            foreach (var component in Components)
            {
                var componentCopy = copy.Manager.GetComponentById(component.Id);
                Debug.Assert(componentCopy is TComponent);
                copy.Components.Add((TComponent)componentCopy);
            }

            return copy;
        }

        #endregion
    }
}
