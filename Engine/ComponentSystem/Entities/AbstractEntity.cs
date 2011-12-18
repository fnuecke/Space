using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Entities
{
    /// <summary>
    /// Base class for entities, implementing logic for distributing unique ids.
    /// </summary>
    public abstract class AbstractEntity : IEntity
    {
        #region Properties

        /// <summary>
        /// A globally unique id for this object.
        /// </summary>
        public long UID { get; set; }

        /// <summary>
        /// A list of all of this entities components.
        /// </summary>
        public ReadOnlyCollection<IComponent> Components { get { return components.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// List of all this entities components.
        /// </summary>
        protected List<IComponent> components = new List<IComponent>();

        #endregion

        protected AbstractEntity()
        {
            // Init to -1 as a default, so these aren't found due to
            // badly initialized 'pointers'.
            this.UID = -1;
        }

        /// <summary>
        /// Get a component of the specified type from this entity, if it
        /// has one.
        /// </summary>
        /// <typeparam name="T">the type of the component to get.</typeparam>
        /// <returns>the component, or <c>null</c> if the entity has none of this type.</returns>
        public T GetComponent<T>()
        {
            foreach (var component in components)
            {
                if (component.GetType().Equals(typeof(T)))
                {
                    return (T)component;
                }
            }
            return default(T);
        }

        #region Interfaces

        /// <summary>
        /// Create a deep copy of the object.
        /// </summary>
        /// <returns></returns>
        public virtual object Clone()
        {
            var copy = (AbstractEntity)MemberwiseClone();
            copy.components = new List<IComponent>();
            foreach (var component in components)
            {
                var componentCopy = (IComponent)component.Clone();
                componentCopy.Entity = copy;
                copy.components.Add(componentCopy);
            }
            return copy;
        }

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">the packet to write the data to.</param>
        public virtual void Packetize(Serialization.Packet packet)
        {
            packet.Write(UID);
            foreach (var component in components)
            {
                component.Packetize(packet);
            }
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">the packet to read from.</param>
        public virtual void Depacketize(Serialization.Packet packet)
        {
            UID = packet.ReadInt64();
            foreach (var component in components)
            {
                component.Depacketize(packet);
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public virtual void Hash(Util.Hasher hasher)
        {
            foreach (var component in components)
            {
                component.Hash(hasher);
            }
        }

        #endregion

    }
}
