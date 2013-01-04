using System;
using System.ComponentModel;
using System.Diagnostics;
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
    [TypeConverter(typeof(ExpandableObjectConverter))]
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

        public AttributeModifier(TAttribute type, float value, AttributeComputationType computationType = AttributeComputationType.Additive)
        {
            Type = type;
            Value = value;
            ComputationType = computationType;
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
        /// <returns>The packet after writing.</returns>
        public Packet Packetize(Packet packet)
        {
            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet. This is called
        /// before automatic depacketization is performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void PreDepacketize(Packet packet)
        {
        }

        /// <summary>
        /// Bring the object to the state in the given packet. This is called
        /// after automatic depacketization has been performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void PostDepacketize(Packet packet)
        {
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
            hasher.Put(Enum.GetName(typeof(TAttribute), Type));
            hasher.Put((byte)ComputationType);
            hasher.Put(Value);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a new copy of the object, that shares no mutable
        /// references with this instance.
        /// </summary>
        /// <returns>The copy.</returns>
        public AttributeModifier<TAttribute> NewInstance()
        {
            return (AttributeModifier<TAttribute>)MemberwiseClone();
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public void CopyInto(AttributeModifier<TAttribute> into)
        {
            Debug.Assert(into != this);

            var copy = (AttributeModifier<TAttribute>)MemberwiseClone();

            copy.Type = Type;
            copy.ComputationType = ComputationType;
            copy.Value = Value;
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
                    return Value * 100 + "% " + Type;
            }
            throw new InvalidOperationException("Unhandled attribute computation type.");
        }

        #endregion
    }
}
