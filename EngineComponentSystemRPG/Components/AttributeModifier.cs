using System;
using System.Text;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Computation types of module attributes. This is how they should be
    /// computed when evaluating a specific attribute type (determined by its
    /// actual class).
    /// </summary>
    public enum AttributeComputationType
    {
        /// <summary>
        /// Additive operation. For reducing influences use a
        /// negative value.
        /// </summary>
        Additive,

        /// <summary>
        /// Multiplicative operation. For reducing influences use
        /// a value smaller than one.
        /// </summary>
        Multiplicative
    }

    /// <summary>
    /// Base class for describing attribute values in the way this value should
    /// computed in the overall attribute value.
    /// </summary>
    /// <typeparam name="TAttribute">The enum of possible attributes.</typeparam>
    public sealed class AttributeModifier<TAttribute> : IPacketizable, IHashable, ICopyable<AttributeModifier<TAttribute>>
    {
        #region Fields

        /// <summary>
        /// The actual type of this attribute, which tells the game how to
        /// handle it.
        /// </summary>
        public TAttribute Type;

        /// <summary>
        /// The actual value for this specific attribute.
        /// </summary>
        public float Value;

        /// <summary>
        /// The computation type of this attribute, i.e. how it should be used
        /// in computation.
        /// </summary>
        public AttributeComputationType ComputationType;

        #endregion

        #region Constructor

        public AttributeModifier(TAttribute type, float value, AttributeComputationType computationType)
        {
            this.Type = type;
            this.Value = value;
            this.ComputationType = computationType;
        }

        public AttributeModifier(TAttribute type, float value)
            : this(type, value, AttributeComputationType.Additive)
        {
        }

        public AttributeModifier()
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
        public Packet Packetize(Packet packet)
        {
            return packet
                .Write(Enum.GetName(typeof(TAttribute), Type))
                .Write((byte)ComputationType)
                .Write(Value);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            Type = (TAttribute)Enum.Parse(typeof(TAttribute), packet.ReadString());
            ComputationType = (AttributeComputationType)packet.ReadByte();
            Value = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
            hasher.Put(Encoding.UTF8.GetBytes(Enum.GetName(typeof(TAttribute), Type)));
            hasher.Put(BitConverter.GetBytes((byte)ComputationType));
            hasher.Put(BitConverter.GetBytes(Value));
        }

        #endregion

        #region Copying

        public AttributeModifier<TAttribute> DeepCopy()
        {
            return DeepCopy(null);
        }

        public AttributeModifier<TAttribute> DeepCopy(AttributeModifier<TAttribute> into)
        {
            var copy = into ?? (AttributeModifier<TAttribute>)MemberwiseClone();

            if (copy == into)
            {
                copy.Type = Type;
                copy.ComputationType = ComputationType;
                copy.Value = Value;
            }

            return copy;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            switch (ComputationType)
            {
                case AttributeComputationType.Additive:
                    return Value + " " + Type;
                case AttributeComputationType.Multiplicative:
                    return (1 - Value) + "% " + Type;
            }
            throw new InvalidOperationException("Unhandled attribute computation type.");
        }

        #endregion
    }
}
