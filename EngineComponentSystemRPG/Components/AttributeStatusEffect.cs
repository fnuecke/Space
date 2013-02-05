using System.Collections.Generic;
using System.IO;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>A status effect that modifies attributes.</summary>
    public class AttributeStatusEffect<TAttribute> : StatusEffect
        where TAttribute : struct
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>The actual attribute modifiers which are applied.</summary>
        [CopyIgnore, PacketizeIgnore]
        public readonly List<AttributeModifier<TAttribute>> Modifiers = new List<AttributeModifier<TAttribute>>();

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Modifiers.AddRange(((AttributeStatusEffect<TAttribute>) other).Modifiers);

            return this;
        }

        /// <summary>Initialize with the specified modifiers.</summary>
        /// <param name="value">The value.</param>
        public AttributeStatusEffect<TAttribute> Initialize(IEnumerable<AttributeModifier<TAttribute>> value)
        {
            Modifiers.AddRange(value);

            return this;
        }

        /// <summary>Initialize with the specified modifier.</summary>
        /// <param name="value">The value.</param>
        public AttributeStatusEffect<TAttribute> Initialize(AttributeModifier<TAttribute> value)
        {
            return Initialize(new[] {value});
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Modifiers.Clear();
        }

        #endregion

        #region Serialization / Hashing

        [OnPacketize]
        public IWritablePacket Packetize(IWritablePacket packet)
        {
            return packet.Write((ICollection<AttributeModifier<TAttribute>>) Modifiers);
        }

        [OnPostDepacketize]
        public void Depacketize(IReadablePacket packet)
        {
            Modifiers.AddRange(packet.ReadPacketizables<AttributeModifier<TAttribute>>());
        }

        [OnStringify]
        public StreamWriter Dump(StreamWriter w, int indent)
        {
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