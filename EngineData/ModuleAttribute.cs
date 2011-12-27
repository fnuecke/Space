using System;
using System.Collections.Generic;
using System.Text;
using Engine.Serialization;
using Engine.Util;

namespace Engine.Data
{
    /// <summary>
    /// Computation types of module attributes. This is how they should be
    /// computed when evaluating a specific attribute type (determined by its
    /// actual class).
    /// </summary>
    public enum ModuleAttributeComputationType
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
    /// <typeparam name="TAttribute">The enum that holds the possible types of
    /// attributes.</typeparam>
    public sealed class ModuleAttribute<TAttribute> : ICloneable, IPacketizable, IHashable
        where TAttribute : struct
    {
        #region Properties

        /// <summary>
        /// The actual type of this attribute, which tells the game how to
        /// handle it.
        /// </summary>
        public TAttribute Type { get; set; }

        /// <summary>
        /// The computation type of this attribute, i.e. how it should be used
        /// in computation.
        /// </summary>
        public ModuleAttributeComputationType ComputationType { get; set; }

        /// <summary>
        /// The actual value for this specific attribute.
        /// </summary>
        public float Value { get; set; }

        #endregion

        #region Serialization / Cloning

        public Packet Packetize(Packet packet)
        {
            return packet
                .Write(Enum.GetName(typeof(TAttribute), Type))
                .Write((byte)ComputationType)
                .Write(Value);
        }

        public void Depacketize(Packet packet)
        {
            Type = (TAttribute)Enum.Parse(typeof(TAttribute), packet.ReadString());
            ComputationType = (ModuleAttributeComputationType)packet.ReadByte();
            Value = packet.ReadSingle();
        }

        public void Hash(Hasher hasher)
        {
            hasher.Put(Encoding.UTF8.GetBytes(Enum.GetName(typeof(TAttribute), Type)));
            hasher.Put(BitConverter.GetBytes((byte)ComputationType));
            hasher.Put(BitConverter.GetBytes(Value));
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        #region Overrides
        
        public override string ToString()
        {
            switch (ComputationType)
            {
                case ModuleAttributeComputationType.Additive:
                    return Value + " " + Type.ToString();
                case ModuleAttributeComputationType.Multiplicative:
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
        public static float Accumulate<TAttribute>(this ModuleAttribute<TAttribute>[] attributes, TAttribute attributeType)
            where TAttribute : struct
        {
            return new List<ModuleAttribute<TAttribute>>(attributes).Accumulate(attributeType);
        }

        /// <summary>
        /// Compute the accumulative value of a certain attribute type for a
        /// collection of attributes.
        /// </summary>
        /// <param name="attributeType">The type for which to compute the
        /// overall value.</param>
        /// <param name="attributes">The list of attributes to use.</param>
        /// <returns>The accumulative value of the specified attribute type
        /// over all attributes in the specified list.</returns>
        public static float Accumulate<TAttribute>(this ICollection<ModuleAttribute<TAttribute>> attributes, TAttribute attributeType)
            where TAttribute : struct
        {
            float result = 0;
            foreach (var attribute in attributes)
            {
                if (attribute.Type.Equals(attributeType) &&
                    attribute.ComputationType == ModuleAttributeComputationType.Additive)
                {
                    result += attribute.Value;
                }
            }
            foreach (var attribute in attributes)
            {
                if (attribute.Type.Equals(attributeType) &&
                    attribute.ComputationType == ModuleAttributeComputationType.Multiplicative)
                {
                    result *= attribute.Value;
                }
            }
            return result;
        }
    }

    #endregion
}
