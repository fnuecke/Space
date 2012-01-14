using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework.Content;

namespace Engine.Data
{
    /// <summary>
    /// Some sort of module applying to an entity, containing a list of
    /// attributes that should apply to the entity.
    /// </summary>
    /// <typeparam name="TAttribute">The enum that holds the possible types of
    /// attributes.</typeparam>
    public abstract class AbstractEntityModule<TAttribute> : ICopyable<AbstractEntityModule<TAttribute>>, IPacketizable, IHashable
        where TAttribute : struct
    {
        #region Properties

        /// <summary>
        /// Unique id of this component relative to its current entity module.
        /// </summary>
        [ContentSerializerIgnore]
        public int UID;

        /// <summary>
        /// A list of all known attributes this module brings with it.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public List<ModuleAttribute<TAttribute>> Attributes;

        /// <summary>
        /// A list of all attributes whose cache should be invalidated when
        /// this module is added or removed.
        /// </summary>
        [ContentSerializerIgnore]
        public ReadOnlyCollection<TAttribute> AttributesToInvalidate { get { return _attributesToInvalidate.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// Actual value for property.
        /// </summary>
        private readonly List<TAttribute> _attributesToInvalidate = new List<TAttribute>();

        #endregion

        #region Constructor

        protected AbstractEntityModule()
        {
            this.UID = -1;
            this.Attributes = new List<ModuleAttribute<TAttribute>>();
        }

        /// <summary>
        /// Call in subclasses with the attribute types that this instance
        /// should invalidate. This is used to clear caches for attributes
        /// that this instance may not have its own attribute for, but to
        /// which some of its properties correspond.
        /// </summary>
        /// <param name="attributeType"></param>
        protected void AddAttributeTypeToInvalidate(TAttribute attributeType)
        {
            _attributesToInvalidate.Add(attributeType);
        }

        #endregion

        #region Serialization / Cloning

        public virtual Packet Packetize(Packet packet)
        {
            return packet
                .Write(UID)
                .Write(Attributes);
        }

        public virtual void Depacketize(Packet packet)
        {
            UID = packet.ReadInt32();
            Attributes.Clear();
            Attributes.AddRange(packet.ReadPacketizables<ModuleAttribute<TAttribute>>());
        }

        public virtual void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(UID));
            foreach (var attribute in Attributes)
            {
                attribute.Hash(hasher);
            }
        }

        public AbstractEntityModule<TAttribute> DeepCopy()
        {
            return DeepCopy(null);
        }

        public virtual AbstractEntityModule<TAttribute> DeepCopy(AbstractEntityModule<TAttribute> into)
        {
            var copy = into ?? (AbstractEntityModule<TAttribute>)MemberwiseClone();

            if (copy == into)
            {
                // Other instance, copy fields.
                copy.UID = UID;

                // Adjust list length.
                if (copy.Attributes.Count > Attributes.Count)
                {
                    copy.Attributes.RemoveRange(Attributes.Count, copy.Attributes.Count - Attributes.Count);
                }
                // Copy as many as we can re-using existing entires.
                int i = 0;
                for (; i < copy.Attributes.Count; ++i)
                {
                    copy.Attributes[i] = Attributes[i].DeepCopy(copy.Attributes[i]);
                }
                // Create the rest creating new instances.
                for (; i < Attributes.Count; ++i)
                {
                    copy.Attributes.Add((ModuleAttribute<TAttribute>)Attributes[i].DeepCopy());
                }
            }
            else
            {
                // Shallow copy, new instances for reference types.

                // Create copies of the attributes.
                copy.Attributes = new List<ModuleAttribute<TAttribute>>();
                foreach (var attribute in Attributes)
                {
                    copy.Attributes.Add((ModuleAttribute<TAttribute>)attribute.DeepCopy());
                }

                // Attributes to invalidate can be reused because it's read-only.
            }

            return copy;
        }

        #endregion
    }
}
