using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    ///     Represents a set of base attribute values, and modified values, for example based on equipped items and active
    ///     status effects.
    /// </summary>
    /// <typeparam name="TAttribute">The enum that holds the possible types of attributes.</typeparam>
    public sealed class Attributes<TAttribute> : Component
        where TAttribute : struct
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>The number of base attributes of this character.</summary>
        public int BaseAttributeCount
        {
            get { return _baseAttributes.Count; }
        }

        /// <summary>The types of base attributes of this character.</summary>
        public IEnumerable<TAttribute> BaseAttributeTypes
        {
            get { return _baseAttributes.Keys; }
        }

        #endregion

        #region Fields

        /// <summary>Base values for attributes.</summary>
        [CopyIgnore, PacketizeIgnore]
        private readonly Dictionary<TAttribute, float> _baseAttributes = new Dictionary<TAttribute, float>();

        /// <summary>
        ///     Modified values, based on equipment and status effects. This stores the absolute value as well as the
        ///     multiplier for the value.
        /// </summary>
        [CopyIgnore, PacketizeIgnore]
        private readonly Dictionary<TAttribute, float[]> _modifiedAttributes = new Dictionary<TAttribute, float[]>();

        #endregion

        #region Single allocation

        /// <summary>Reusable list for modifier computation.</summary>
        [CopyIgnore, PacketizeIgnore]
        private readonly List<AttributeModifier<TAttribute>> _reusableAdditiveList =
            new List<AttributeModifier<TAttribute>>();

        /// <summary>Reusable list for modifier computation.</summary>
        [CopyIgnore, PacketizeIgnore]
        private readonly List<AttributeModifier<TAttribute>> _reusableMultiplicativeList =
            new List<AttributeModifier<TAttribute>>();

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherAttributes = (Attributes<TAttribute>) other;
            foreach (var attribute in otherAttributes._baseAttributes)
            {
                _baseAttributes.Add(attribute.Key, attribute.Value);
            }
            foreach (var attribute in otherAttributes._modifiedAttributes)
            {
                _modifiedAttributes.Add(attribute.Key, new[] {attribute.Value[0], attribute.Value[1]});
            }

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            _baseAttributes.Clear();
            _modifiedAttributes.Clear();
        }

        #endregion

        #region Accessors

        /// <summary>Set the base value of the specified attribute.</summary>
        /// <param name="type">The attribute type.</param>
        /// <param name="value">The base value.</param>
        public void SetBaseValue(TAttribute type, float value)
        {
            _baseAttributes[type] = value;

            RecomputeAttributes();
        }

        /// <summary>Gets the base value for the specified attribute type.</summary>
        /// <param name="type">The attribute type.</param>
        /// <returns>The base value for that type.</returns>
        public float GetBaseValue(TAttribute type)
        {
            if (!_baseAttributes.ContainsKey(type))
            {
                return _baseAttributes[type] = 0;
            }
            return _baseAttributes[type];
        }

        /// <summary>
        ///     Gets the modified value for the specified attribute type. Note that the specified base value will be added to
        ///     the base value stored in this attribute collection before the multiplier is applied.
        /// </summary>
        /// <param name="type">The attribute type.</param>
        /// <param name="baseValue">The base value to use.</param>
        /// <returns>The modified value for that type.</returns>
        public float GetValue(TAttribute type, float baseValue = 0f)
        {
            if (!_modifiedAttributes.ContainsKey(type))
            {
                return baseValue;
            }
            var modifiers = _modifiedAttributes[type];
            return (modifiers[0] + baseValue) * modifiers[1];
        }

        #endregion

        #region Logic

        /// <summary>Recomputes the modified values of all attributes.</summary>
        public void RecomputeAttributes()
        {
            // Ignore while disabled.
            if (!Enabled)
            {
                return;
            }

            // Find all additive and multiplicative modifiers.

            // Use deterministic order.
            var baseAttributeTypes = _baseAttributes.Keys.ToArray();
            Array.Sort(baseAttributeTypes);

            // Push base values as additive modifiers.
            foreach (var attribute in baseAttributeTypes)
            {
                _reusableAdditiveList.Add(new AttributeModifier<TAttribute>(attribute, _baseAttributes[attribute]));
            }

            // Parse all items.
            var equipment = ((ItemSlot) Manager.GetComponent(Entity, ItemSlot.TypeId));
            if (equipment != null)
            {
                foreach (var item in equipment.AllItems)
                {
                    foreach (var component in Manager.GetComponents(item, Attribute<TAttribute>.TypeId))
                    {
                        var modifier = ((Attribute<TAttribute>) component).Value;
                        switch (modifier.ComputationType)
                        {
                            case AttributeComputationType.Additive:
                                _reusableAdditiveList.Add(modifier);
                                break;

                            case AttributeComputationType.Multiplicative:
                                _reusableMultiplicativeList.Add(modifier);
                                break;
                        }
                    }
                }
            }

            // Parse all status effects.
            foreach (var component in Manager.GetComponents(Entity, AttributeStatusEffect<TAttribute>.TypeId))
            {
                foreach (var modifier in ((AttributeStatusEffect<TAttribute>) component).Modifiers)
                {
                    switch (modifier.ComputationType)
                    {
                        case AttributeComputationType.Additive:
                            _reusableAdditiveList.Add(modifier);
                            break;

                        case AttributeComputationType.Multiplicative:
                            _reusableMultiplicativeList.Add(modifier);
                            break;
                    }
                }
            }

            // Compute.
            _modifiedAttributes.Clear();
            foreach (var modifier in _reusableAdditiveList)
            {
                if (!_modifiedAttributes.ContainsKey(modifier.Type))
                {
                    _modifiedAttributes[modifier.Type] = new[] {modifier.Value, 1f};
                }
                else
                {
                    _modifiedAttributes[modifier.Type][0] += modifier.Value;
                }
            }
            foreach (var modifier in _reusableMultiplicativeList)
            {
                if (_modifiedAttributes.ContainsKey(modifier.Type))
                {
                    _modifiedAttributes[modifier.Type][1] *= modifier.Value;
                }
            }

            // Clean up for next time we run this.
            _reusableAdditiveList.Clear();
            _reusableMultiplicativeList.Clear();

            // Send message.
            CharacterStatsInvalidated message;
            message.Entity = Entity;
            Manager.SendMessage(message);
        }

        #endregion

        #region Serialization

        [OnPacketize]
        public IWritablePacket Packetize(IWritablePacket packet)
        {
            return PacketizeLocal(packet);
        }

        [OnPostDepacketize]
        public void Depacketize(IReadablePacket packet)
        {
            DepacketizeLocal(packet);
        }

        /// <summary>Special purpose packetize method, only writing own data, not that of the base class. Used for saving.</summary>
        /// <param name="packet">The packet to write to.</param>
        /// <returns>The written to packet.</returns>
        public IWritablePacket PacketizeLocal(IWritablePacket packet)
        {
            packet.Write(_baseAttributes.Count);
            foreach (var attribute in _baseAttributes)
            {
                packet.Write(Enum.GetName(typeof (TAttribute), attribute.Key));
                packet.Write(attribute.Value);
            }
            packet.Write(_modifiedAttributes.Count);
            foreach (var attribute in _modifiedAttributes)
            {
                packet.Write(Enum.GetName(typeof (TAttribute), attribute.Key));
                packet.Write(attribute.Value[0]);
                packet.Write(attribute.Value[1]);
            }

            return packet;
        }

        /// <summary>
        ///     Corresponds to <c>PacketizeLocal</c>, only reads own data and leaves the base class alone.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void DepacketizeLocal(IReadablePacket packet)
        {
            var baseAttributeCount = packet.ReadInt32();
            for (var i = 0; i < baseAttributeCount; i++)
            {
                var key = (TAttribute) Enum.Parse(typeof (TAttribute), packet.ReadString());
                var value = packet.ReadSingle();
                _baseAttributes[key] = value;
            }
            var modifiedAttributeCount = packet.ReadInt32();
            for (var i = 0; i < modifiedAttributeCount; i++)
            {
                var key = (TAttribute) Enum.Parse(typeof (TAttribute), packet.ReadString());
                var values = new float[2];
                values[0] = packet.ReadSingle();
                values[1] = packet.ReadSingle();
                _modifiedAttributes[key] = values;
            }
        }

        [OnStringify]
        public StreamWriter Dump(StreamWriter w, int indent)
        {
            w.AppendIndent(indent).Write("BaseAttributes = {");
            foreach (var attribute in _baseAttributes)
            {
                w.AppendIndent(indent + 1).Write(Enum.GetName(typeof (TAttribute), attribute.Key));
                w.Write(" = ");
                w.Write(attribute.Value);
            }
            w.AppendIndent(indent).Write("}");

            w.AppendIndent(indent).Write("ModifiedAttributes = {");
            foreach (var attribute in _modifiedAttributes)
            {
                w.AppendIndent(indent + 1).Write(Enum.GetName(typeof (TAttribute), attribute.Key));
                w.Write(" = {Additive:");
                w.Write(attribute.Value[0]);
                w.Write(" Multiplicative:");
                w.Write(attribute.Value[1]);
                w.Write("}");
            }
            w.AppendIndent(indent).Write("}");

            return w;
        }

        #endregion
    }
}