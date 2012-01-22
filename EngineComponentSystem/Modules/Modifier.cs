using System;
using System.Collections.Generic;
using System.Text;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Modules
{
    /// <summary>
    /// Computation types of module attributes. This is how they should be
    /// computed when evaluating a specific attribute type (determined by its
    /// actual class).
    /// </summary>
    public enum ModifierComputationType
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
    /// Base class for attributes.
    /// </summary>
    /// <typeparam name="TModifier">The enum that holds the possible types of
    /// attributes.</typeparam>
    public sealed class Modifier<TModifier> : ICopyable<Modifier<TModifier>>, IPacketizable, IHashable
        where TModifier : struct
    {
        #region Fields

        /// <summary>
        /// The actual type of this attribute, which tells the game how to
        /// handle it.
        /// </summary>
        public TModifier Type;

        /// <summary>
        /// The computation type of this attribute, i.e. how it should be used
        /// in computation.
        /// </summary>
        public ModifierComputationType ComputationType;

        /// <summary>
        /// The actual value for this specific attribute.
        /// </summary>
        public float Value;

        #endregion

        #region Serialization / Cloning

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
                .Write(Enum.GetName(typeof(TModifier), Type))
                .Write((byte)ComputationType)
                .Write(Value);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            Type = (TModifier)Enum.Parse(typeof(TModifier), packet.ReadString());
            ComputationType = (ModifierComputationType)packet.ReadByte();
            Value = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
            hasher.Put(Encoding.UTF8.GetBytes(Enum.GetName(typeof(TModifier), Type)));
            hasher.Put(BitConverter.GetBytes((byte)ComputationType));
            hasher.Put(BitConverter.GetBytes(Value));
        }

        public Modifier<TModifier> DeepCopy()
        {
            return DeepCopy(null);
        }

        public Modifier<TModifier> DeepCopy(Modifier<TModifier> into)
        {
            if (into == null)
            {
                return (Modifier<TModifier>)MemberwiseClone();
            }
            else
            {
                into.Type = Type;
                into.ComputationType = ComputationType;
                into.Value = Value;
                return into;
            }
        }

        #endregion

        #region Overrides

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
                case ModifierComputationType.Additive:
                    return Value + " " + Type.ToString();
                case ModifierComputationType.Multiplicative:
                default:
                    return (1 - Value) + "% " + Type.ToString();
            }
        }

        #endregion
    }

    #region Utility methods

    public static class ModuleAttributeExtension
    {

        /// <summary>
        /// Compute the accumulative value of a certain attribute type for a
        /// collection of attributes.
        /// </summary>
        /// <param name="attributeType">The type for which to compute the
        /// overall value.</param>
        /// <param name="attributes">The list of attributes to use.</param>
        /// <returns>The accumulative value of the specified attribute type
        /// over all attributes in the specified list.</returns>
        public static float Accumulate<TAttribute>(this Modifier<TAttribute>[] attributes, TAttribute attributeType)
            where TAttribute : struct
        {
            return new List<Modifier<TAttribute>>(attributes).Accumulate(attributeType);
        }

        /// <summary>
        /// Compute the accumulative value of a certain attribute type for a
        /// collection of attributes.
        /// </summary>
        /// <param name="attributeType">The type for which to compute the
        /// overall value.</param>
        /// <param name="attributes">The list of attributes to use.</param>
        /// <param name="baseValue">The base value to start from.</param>
        /// <returns>The accumulative value of the specified attribute type
        /// over all attributes in the specified list.</returns>
        public static float Accumulate<TAttribute>(this ICollection<Modifier<TAttribute>> attributes, TAttribute attributeType, float baseValue = 0)
            where TAttribute : struct
        {
            float result = baseValue;
            foreach (var attribute in attributes)
            {
                if (attribute.Type.Equals(attributeType) &&
                    attribute.ComputationType == ModifierComputationType.Additive)
                {
                    result += attribute.Value;
                }
            }
            foreach (var attribute in attributes)
            {
                if (attribute.Type.Equals(attributeType) &&
                    attribute.ComputationType == ModifierComputationType.Multiplicative)
                {
                    result *= attribute.Value;
                }
            }
            return result;
        }
    }

    #endregion
}
