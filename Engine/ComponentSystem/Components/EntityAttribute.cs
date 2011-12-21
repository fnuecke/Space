using System;
using System.Collections.Generic;
using Engine.Math;
using Engine.Serialization;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Computation types of entity attributes. This is how they should be
    /// computed when evaluating a specific attribute type (determined by its
    /// actual class).
    /// </summary>
    public enum EntityAttributeComputationType
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
    public class EntityAttribute<TAttribute> : IPacketizable, ICloneable
        where TAttribute : struct
    {
        #region Properties

        /// <summary>
        /// Unique ID of the attribute relative to the component it is
        /// currently handled by.
        /// </summary>
        public int UID { get; set; }

        /// <summary>
        /// The actual type of this attribute, which tells the game how to
        /// handle it.
        /// </summary>
        public TAttribute Type { get; set; }

        /// <summary>
        /// The computation type of this attribute, i.e. how it should be used
        /// in computation.
        /// </summary>
        public EntityAttributeComputationType ComputationType { get; set; }

        /// <summary>
        /// The actual value for this specific attribute.
        /// </summary>
        public Fixed Value { get; set; }

        #endregion

        #region Constructor

        public EntityAttribute()
        {
            // Avoid being indexed by 0.
            UID = -1;
        }

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
            ComputationType = (EntityAttributeComputationType)packet.ReadByte();
            Value = packet.ReadFixed();
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
                case EntityAttributeComputationType.Additive:
                    return Value.DoubleValue + " " + Type.ToString();
                case EntityAttributeComputationType.Multiplicative:
                default:
                    return (1 - Value.DoubleValue) + "% " + Type.ToString();
            }
        }

        #endregion
    }

    #region Utility methods

    public static class EntityAttributeExtension
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
        public static Fixed Accumulate<TAttribute>(this EntityAttribute<TAttribute>[] attributes, TAttribute attributeType)
            where TAttribute : struct
        {
            return new List<EntityAttribute<TAttribute>>(attributes).Accumulate(attributeType);
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
        public static Fixed Accumulate<TAttribute>(this ICollection<EntityAttribute<TAttribute>> attributes, TAttribute attributeType)
            where TAttribute : struct
        {
            Fixed result = Fixed.Zero;
            foreach (var attribute in attributes)
            {
                if (attribute.Type.Equals(attributeType) &&
                    attribute.ComputationType == EntityAttributeComputationType.Additive)
                {
                    result += attribute.Value;
                }
            }
            foreach (var attribute in attributes)
            {
                if (attribute.Type.Equals(attributeType) &&
                    attribute.ComputationType == EntityAttributeComputationType.Multiplicative)
                {
                    result *= attribute.Value;
                }
            }
            return result;
        }
    }

    #endregion
}
