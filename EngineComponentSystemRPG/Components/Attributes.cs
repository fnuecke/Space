using System;
using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Represents a set of base attribute values, and modified values, for example
    /// based on equipped items and active status effects.
    /// </summary>
    /// <typeparam name="TAttribute">The enum that holds the possible types of
    /// attributes.</typeparam>
    public sealed class Attributes<TAttribute> : Component
        where TAttribute : struct
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The number of base attributes of this character.
        /// </summary>
        public int BaseAttributeCount
        {
            get { return _baseAttributes.Count; }
        }

        /// <summary>
        /// The types of base attributes of this character.
        /// </summary>
        public IEnumerable<TAttribute> BaseAttributeTypes
        {
            get { return _baseAttributes.Keys; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Base values for attributes.
        /// </summary>
        [PacketizerIgnore]
        private readonly Dictionary<TAttribute, float> _baseAttributes = new Dictionary<TAttribute, float>();

        /// <summary>
        /// Modified values, based on equipment and status effects. This stores
        /// the absolute value as well as the multiplier for the value.
        /// </summary>
        [PacketizerIgnore]
        private readonly Dictionary<TAttribute, float[]> _modifiedAttributes = new Dictionary<TAttribute, float[]>();

        #endregion

        #region Single allocation

        /// <summary>
        /// Reusable list for modifier computation.
        /// </summary>
        [PacketizerIgnore]
        private readonly List<AttributeModifier<TAttribute>> _reusableAdditiveList =
            new List<AttributeModifier<TAttribute>>();

        /// <summary>
        /// Reusable list for modifier computation.
        /// </summary>
        [PacketizerIgnore]
        private readonly List<AttributeModifier<TAttribute>> _reusableMultiplicativeList =
            new List<AttributeModifier<TAttribute>>();

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var attributes = (Attributes<TAttribute>)other;
            foreach (var attribute in attributes._baseAttributes)
            {
                _baseAttributes.Add(attribute.Key, attribute.Value);
            }
            foreach (var attribute in attributes._modifiedAttributes)
            {
                _modifiedAttributes.Add(attribute.Key, new[] {attribute.Value[0], attribute.Value[1]});
            }

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _baseAttributes.Clear();
            _modifiedAttributes.Clear();
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Set the base value of the specified attribute.
        /// </summary>
        /// <param name="type">The attribute type.</param>
        /// <param name="value">The base value.</param>
        public void SetBaseValue(TAttribute type, float value)
        {
            _baseAttributes[type] = value;

            RecomputeAttributes();
        }

        /// <summary>
        /// Gets the base value for the specified attribute type.
        /// </summary>
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
        /// Gets the modified value for the specified attribute type.
        /// Note that the specified base value will be added to the
        /// base value stored in this attribute collection before the
        /// multiplier is applied.
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

        /// <summary>
        /// Recomputes the modified values of all attributes.
        /// </summary>
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
            for (var i = 0; i < baseAttributeTypes.Length; i++)
            {
                _reusableAdditiveList.Add(new AttributeModifier<TAttribute>(baseAttributeTypes[i], _baseAttributes[baseAttributeTypes[i]]));
            }

            // Parse all items.
            var equipment = ((ItemSlot)Manager.GetComponent(Entity, ItemSlot.TypeId));
            if (equipment != null)
            {
                foreach (var item in equipment.AllItems)
                {
                    foreach (var component in Manager.GetComponents(item, Attribute<TAttribute>.TypeId))
                    {
                        var modifier = ((Attribute<TAttribute>)component).Value;
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
                foreach (var modifier in ((AttributeStatusEffect<TAttribute>)component).Modifiers)
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

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            return PacketizeLocal(packet);
        }

        /// <summary>
        /// Special purpose packetize method, only writing own data, not that
        /// of the base class. Used for saving.
        /// </summary>
        /// <param name="packet">The packet to write to.</param>
        /// <returns>The written to packet.</returns>
        public Packet PacketizeLocal(Packet packet)
        {
            packet.Write(_baseAttributes.Count);
            foreach (var attribute in _baseAttributes)
            {
                packet.Write(Enum.GetName(typeof(TAttribute), attribute.Key));
                packet.Write(attribute.Value);
            }
            packet.Write(_modifiedAttributes.Count);
            foreach (var attribute in _modifiedAttributes)
            {
                packet.Write(Enum.GetName(typeof(TAttribute), attribute.Key));
                packet.Write(attribute.Value[0]);
                packet.Write(attribute.Value[1]);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet. This is called
        /// after automatic depacketization has been performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void PostDepacketize(Packet packet)
        {
            base.PostDepacketize(packet);

            DepacketizeLocal(packet);
        }

        /// <summary>
        /// Corresponds to <c>PacketizeLocal</c>, only reads own data and leaves
        /// the base class alone.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void DepacketizeLocal(Packet packet)
        {
            var numBaseAttributes = packet.ReadInt32();
            for (var i = 0; i < numBaseAttributes; i++)
            {
                var key = (TAttribute)Enum.Parse(typeof(TAttribute), packet.ReadString());
                var value = packet.ReadSingle();
                _baseAttributes[key] = value;
            }
            var numModifiedAttributes = packet.ReadInt32();
            for (var i = 0; i < numModifiedAttributes; i++)
            {
                var key = (TAttribute)Enum.Parse(typeof(TAttribute), packet.ReadString());
                var values = new float[2];
                values[0] = packet.ReadSingle();
                values[1] = packet.ReadSingle();
                _modifiedAttributes[key] = values;
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            foreach (var attribute in _baseAttributes)
            {
                hasher.Put(Enum.GetName(typeof(TAttribute), attribute.Key));
                hasher.Put(attribute.Value);
            }
            foreach (var attribute in _modifiedAttributes)
            {
                hasher.Put(Enum.GetName(typeof(TAttribute), attribute.Key));
                hasher.Put(attribute.Value[0]);
                hasher.Put(attribute.Value[1]);
            }
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
            return base.ToString() + ", BaseAttributes=[" + string.Join(", ", _baseAttributes) + "], ModifiedAttributes=[" + string.Join(", ", _modifiedAttributes) + "]";
        }

        #endregion
    }
}
