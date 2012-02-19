using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Represents a character or unit with a set of base attribute values, and
    /// modified values, based on equipped items and active status effects.
    /// </summary>
    /// <typeparam name="TAttribute">The enum that holds the possible types of
    /// attributes.</typeparam>
    public class Character<TAttribute> : Component
        where TAttribute : struct
    {
        #region Properties

        /// <summary>
        /// The number of base attributes of this character.
        /// </summary>
        public int BaseAttributeCount { get { return _baseAttributes.Count; } }

        /// <summary>
        /// The types of base attributes of this character.
        /// </summary>
        public IEnumerable<TAttribute> BaseAttributeTypes { get { return _baseAttributes.Keys; } }

        #endregion

        #region Fields

        /// <summary>
        /// Base values for attributes.
        /// </summary>
        private Dictionary<TAttribute, float> _baseAttributes = new Dictionary<TAttribute, float>();

        /// <summary>
        /// Modified values, based on equipment and status effects.
        /// </summary>
        private Dictionary<TAttribute, float[]> _modifiedAttributes = new Dictionary<TAttribute, float[]>();

        #endregion

        #region Single allocation

        /// <summary>
        /// Reusable list for modifier computation.
        /// </summary>
        private List<AttributeModifier<TAttribute>> _reusableAdditiveList = new List<AttributeModifier<TAttribute>>();

        /// <summary>
        /// Reusable list for modifier computation.
        /// </summary>
        private List<AttributeModifier<TAttribute>> _reusableMultiplicativeList = new List<AttributeModifier<TAttribute>>();

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
        /// </summary>
        /// <param name="type">The attribute type.</param>
        /// <param name="baseValue">The base value to use.</param>
        /// <returns>The base value for that type.</returns>
        public float GetValue(TAttribute type, float baseValue)
        {
            if (!_modifiedAttributes.ContainsKey(type))
            {
                return baseValue;
            }
            var modifiers = _modifiedAttributes[type];
            return (modifiers[0] + baseValue) * modifiers[1];
        }

        /// <summary>
        /// Gets the modified value for the specified attribute type.
        /// </summary>
        /// <param name="type">The attribute type.</param>
        /// <returns>The base value for that type.</returns>
        public float GetValue(TAttribute type)
        {
            return GetValue(type, 0);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Recomputes the modified values of all attributes.
        /// </summary>
        private void RecomputeAttributes()
        {
            // Find all additive and multiplicative modifiers.

            // Push base values as additive modifiers.
            foreach (var attribute in _baseAttributes)
            {
                _reusableAdditiveList.Add(new AttributeModifier<TAttribute>(attribute.Key, attribute.Value));
            }

            // Parse all items.
            var equipment = Entity.GetComponent<Equipment>();
            if (equipment != null)
            {
                foreach (var item in equipment.AllItems)
                {
                    foreach (var component in item.Components)
                    {
                        if (component is Attribute<TAttribute>)
                        {
                            var attribute = ((Attribute<TAttribute>)component).Modifier;
                            switch (attribute.ComputationType)
                            {
                                case AttributeComputationType.Additive:
                                    _reusableAdditiveList.Add(attribute);
                                    break;

                                case AttributeComputationType.Multiplicative:
                                    _reusableMultiplicativeList.Add(attribute);
                                    break;
                            }
                        }
                    }
                }
            }

            // Parse all status effects.
            foreach (var component in Entity.Components)
            {
                if (component is AttributeStatusEffect<TAttribute>)
                {
                    foreach (var attribute in ((AttributeStatusEffect<TAttribute>)component).Modifiers)
                    {
                        switch (attribute.ComputationType)
                        {
                            case AttributeComputationType.Additive:
                                _reusableAdditiveList.Add(attribute);
                                break;

                            case AttributeComputationType.Multiplicative:
                                _reusableMultiplicativeList.Add(attribute);
                                break;
                        }
                    }
                }
            }

            // Compute.
            _modifiedAttributes.Clear();
            foreach (var modifier in _reusableAdditiveList)
            {
                if (!_modifiedAttributes.ContainsKey(modifier.Type))
                {
                    _modifiedAttributes[modifier.Type] = new float[] { modifier.Value, 1f };
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
            Entity.SendMessage(ref message);
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
        /// <returns>The writting to packet.</returns>
        public Packet PacketizeLocal(Packet packet)
        {
            packet.Write(_baseAttributes.Count);
            foreach (var attribute in _baseAttributes)
            {
                packet.Write(Enum.GetName(typeof(TAttribute), attribute.Key));
                packet.Write(attribute.Value);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            DepacketizeLocal(packet);
        }

        /// <summary>
        /// Pendant to <c>PacketizeLocal</c>, only reads own data and leaves
        /// the base class alone.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void DepacketizeLocal(Packet packet)
        {
            var numBaseAttributes = packet.ReadInt32();
            for (int i = 0; i < numBaseAttributes; i++)
            {
                var key = (TAttribute)Enum.Parse(typeof(TAttribute), packet.ReadString());
                var value = packet.ReadSingle();
                _baseAttributes[key] = value;
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

            foreach (var attribute in _modifiedAttributes)
            {
                hasher.Put(BitConverter.GetBytes(attribute.Value[0]));
                hasher.Put(BitConverter.GetBytes(attribute.Value[1]));
            }
        }

        #endregion

        #region Copying

        public override Component DeepCopy(Component into)
        {
            var copy = (Character<TAttribute>)base.DeepCopy(into);

            if (copy == into)
            {
                copy._baseAttributes.Clear();
                foreach (var attribute in _baseAttributes)
                {
                    copy._baseAttributes[attribute.Key] = attribute.Value;
                }
                copy._modifiedAttributes.Clear();
            }
            else
            {
                copy._baseAttributes = new Dictionary<TAttribute, float>(_baseAttributes);
                copy._modifiedAttributes = new Dictionary<TAttribute, float[]>();

                // For multi-threading.
                copy._reusableAdditiveList = new List<AttributeModifier<TAttribute>>();
                copy._reusableMultiplicativeList = new List<AttributeModifier<TAttribute>>();
            }

            foreach (var attribute in _modifiedAttributes)
            {
                var value = new float[attribute.Value.Length];
                attribute.Value.CopyTo(value, 0);
                copy._modifiedAttributes[attribute.Key] = value;
            }

            return copy;
        }

        #endregion
    }
}
