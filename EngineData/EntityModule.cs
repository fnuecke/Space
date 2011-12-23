using System;
using System.Collections.Generic;
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
    public abstract class AbstractEntityModule<TAttribute> : ICloneable, IPacketizable, IHashable
        where TAttribute : struct
    {
        #region Properties

        /// <summary>
        /// Unique id of this component relative to its current entity module.
        /// </summary>
        [ContentSerializerIgnore]
        public int UID { get; set; }

        /// <summary>
        /// A list of all known attributes this module brings with it.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public List<ModuleAttribute<TAttribute>> Attributes { get; set; }

        #endregion

        #region Constructor

        protected AbstractEntityModule()
        {
            this.UID = -1;
            this.Attributes = new List<ModuleAttribute<TAttribute>>();
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

        public void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(UID));
            foreach (var attribute in Attributes)
            {
                attribute.Hash(hasher);
            }
        }

        public virtual object Clone()
        {
            var copy = (AbstractEntityModule<TAttribute>)MemberwiseClone();

            // Create copies of the attributes.
            copy.Attributes = new List<ModuleAttribute<TAttribute>>();
            foreach (var attribute in Attributes)
            {
                copy.Attributes.Add((ModuleAttribute<TAttribute>)attribute.Clone());
            }

            return copy;
        }

        #endregion
    }
}
