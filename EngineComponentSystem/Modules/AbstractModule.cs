using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework.Content;

namespace Engine.ComponentSystem.Modules
{
    /// <summary>
    /// Some sort of module applying to an entity, containing a list of
    /// attributes that should apply to the entity.
    /// </summary>
    /// <remarks>
    /// When changing a module directly, either by setting a base value or
    /// by adding or removing modifiers, make sure to call <c>Invalidate()</c>
    /// after the change, to propagate the new value through the simulation.
    /// </remarks>
    /// <typeparam name="TModifier">The enum that holds the possible types of
    /// attributes.</typeparam>
    public abstract class AbstractModule<TModifier> : ICopyable<AbstractModule<TModifier>>, IPacketizable, IHashable
        where TModifier : struct
    {
        #region Properties

        /// <summary>
        /// A list of all attributes whose cache should be invalidated when
        /// this module is added or removed.
        /// </summary>
        [ContentSerializerIgnore]
        public ReadOnlyCollection<TModifier> ModifiersToInvalidate { get { return _modifiersToInvalidate.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// Unique id of this module relative to its current component.
        /// </summary>
        [ContentSerializerIgnore]
        public int UID;

        /// <summary>
        /// A list of all known attributes this module brings with it.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public List<Modifier<TModifier>> Modifiers;
        
        /// <summary>
        /// The component this module is managed by.
        /// </summary>
        [ContentSerializerIgnore]
        public ModuleManager<TModifier> Component;

        /// <summary>
        /// Actual value for property.
        /// </summary>
        private readonly List<TModifier> _modifiersToInvalidate = new List<TModifier>();

        #endregion

        #region Constructor

        protected AbstractModule()
        {
            this.UID = -1;
            this.Modifiers = new List<Modifier<TModifier>>();
        }

        /// <summary>
        /// Call in subclasses with the attribute types that this instance
        /// should invalidate. This is used to clear caches for attributes
        /// that this instance may not have its own attribute for, but to
        /// which some of its properties correspond.
        /// </summary>
        /// <param name="attributeType"></param>
        protected void AddAttributeTypeToInvalidate(TModifier attributeType)
        {
            _modifiersToInvalidate.Add(attributeType);
        }

        #endregion

        #region Invalidation

        /// <summary>
        /// Invalidates the possibly cached value for the modifiers this module
        /// contributes in the module manager, triggering re-computation of
        /// values throughout the simulation, where necessary.
        /// </summary>
        public void Invalidate()
        {
            if (Component != null)
            {
                foreach (var attribute in Modifiers)
                {
                    Component.Invalidate(attribute.Type);
                }
                foreach (var attributeType in ModifiersToInvalidate)
                {
                    Component.Invalidate(attributeType);
                }
            }
        }

        #endregion

        #region Serialization / Cloning

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public virtual Packet Packetize(Packet packet)
        {
            return packet
                .Write(UID)
                .Write(Modifiers);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public virtual void Depacketize(Packet packet)
        {
            UID = packet.ReadInt32();
            Modifiers.Clear();
            Modifiers.AddRange(packet.ReadPacketizables<Modifier<TModifier>>());
            Component = null;
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public virtual void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(UID));
            foreach (var attribute in Modifiers)
            {
                attribute.Hash(hasher);
            }
        }

        public AbstractModule<TModifier> DeepCopy()
        {
            return DeepCopy(null);
        }

        public virtual AbstractModule<TModifier> DeepCopy(AbstractModule<TModifier> into)
        {
            var copy = (into != null && into.GetType() == this.GetType())
                ? into
                : (AbstractModule<TModifier>)MemberwiseClone();

            // Null the component, must be re-set from the outside.
            copy.Component = null;

            if (copy == into)
            {
                // Other instance, copy fields.
                copy.UID = UID;

                // Adjust list length.
                if (copy.Modifiers.Count > Modifiers.Count)
                {
                    copy.Modifiers.RemoveRange(Modifiers.Count, copy.Modifiers.Count - Modifiers.Count);
                }
                // Copy as many as we can re-using existing entires.
                int i = 0;
                for (; i < copy.Modifiers.Count; ++i)
                {
                    copy.Modifiers[i] = Modifiers[i].DeepCopy(copy.Modifiers[i]);
                }
                // Create the rest creating new instances.
                for (; i < Modifiers.Count; ++i)
                {
                    copy.Modifiers.Add((Modifier<TModifier>)Modifiers[i].DeepCopy());
                }
            }
            else
            {
                // Shallow copy, new instances for reference types.

                // Create copies of the attributes.
                copy.Modifiers = new List<Modifier<TModifier>>();
                foreach (var attribute in Modifiers)
                {
                    copy.Modifiers.Add((Modifier<TModifier>)attribute.DeepCopy());
                }

                // Attributes to invalidate can be reused because it's read-only.
            }

            return copy;
        }

        #endregion
    }
}
