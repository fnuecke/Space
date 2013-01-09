using System.Collections.Generic;
using System.IO;
using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// A status effect that modifies attributes.
    /// </summary>
    public class AttributeStatusEffect<TAttribute> : StatusEffect
        where TAttribute : struct
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The actual attribute modifiers which are applied.
        /// </summary>
        [PacketizerIgnore]
        public readonly List<AttributeModifier<TAttribute>> Modifiers = new List<AttributeModifier<TAttribute>>();

        #endregion
        
        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Modifiers.AddRange(((AttributeStatusEffect<TAttribute>)other).Modifiers);

            return this;
        }

        /// <summary>
        /// Initialize with the specified modifiers.
        /// </summary>
        /// <param name="value">The value.</param>
        public AttributeStatusEffect<TAttribute> Initialize(IEnumerable<AttributeModifier<TAttribute>> value)
        {
            Modifiers.AddRange(value);

            return this;
        }

        /// <summary>
        /// Initialize with the specified modifier.
        /// </summary>
        /// <param name="value">The value.</param>
        public AttributeStatusEffect<TAttribute> Initialize(AttributeModifier<TAttribute> value)
        {
            return Initialize(new[] { value });
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Modifiers.Clear();
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
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);
            packet.Write((ICollection<AttributeModifier<TAttribute>>)Modifiers);
            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void PostDepacketize(IReadablePacket packet)
        {
            base.PostDepacketize(packet);

            Modifiers.AddRange(packet.ReadPacketizables<AttributeModifier<TAttribute>>());
        }

        /// <summary>Writes a string representation of the object to a string builder.</summary>
        /// <param name="w"> </param>
        /// <param name="indent">The indentation level.</param>
        /// <returns>The string builder, for call chaining.</returns>
        public override StreamWriter Dump(StreamWriter w, int indent)
        {
            base.Dump(w, indent);

            w.AppendIndent(indent).Write("Modifiers = {");
            foreach (var modifier in Modifiers)
            {
                w.AppendIndent(indent + 1).Dump(modifier, indent + 1);
            }
            w.AppendIndent(indent).Write("}");

            return w;
        }

        #endregion
    }
}
