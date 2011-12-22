using System;
using System.Collections.Generic;
using Engine.Serialization;
using Engine.Util;

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
        /// A list of all known attributes this module brings with it.
        /// </summary>
        public List<EntityAttribute<TAttribute>> Attributes { get; set; }

        #endregion

        #region Constructor

        protected AbstractEntityModule()
        {
            this.Attributes = new List<EntityAttribute<TAttribute>>();
        }

        #endregion

        #region Serialization / Cloning

        public virtual Packet Packetize(Packet packet)
        {
            packet.Write(Attributes.Count);
            foreach (var attribute in Attributes)
            {
                packet.Write(attribute);
            }
            return packet;
        }

        public virtual void Depacketize(Packet packet)
        {
            Attributes.Clear();
            int numAttributes = packet.ReadInt32();
            for (int i = 0; i < numAttributes; i++)
            {
                Attributes.Add(packet.ReadPacketizable(new EntityAttribute<TAttribute>()));
            }
        }

        public void Hash(Hasher hasher)
        {
            foreach (var attribute in Attributes)
            {
                attribute.Hash(hasher);
            }
        }

        public virtual object Clone()
        {
            var copy = (AbstractEntityModule<TAttribute>)MemberwiseClone();

            // Create copies of the attributes.
            copy.Attributes = new List<EntityAttribute<TAttribute>>();
            foreach (var attribute in Attributes)
            {
                copy.Attributes.Add((EntityAttribute<TAttribute>)attribute.Clone());
            }

            return copy;
        }

        #endregion
    }
}
