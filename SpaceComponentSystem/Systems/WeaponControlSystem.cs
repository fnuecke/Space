using System;
using System.Collections.Generic;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles firing of equipped weapons.
    /// </summary>
    public sealed class WeaponControlSystem : AbstractComponentSystem<WeaponControl>
    {
        #region Fields

        /// <summary>
        /// Cooldowns of known weapon components.
        /// </summary>
        private Dictionary<int, int> _cooldowns = new Dictionary<int, int>();

        /// <summary>
        /// Randomizer used for sampling projectiles.
        /// </summary>
        private MersenneTwister _random = new MersenneTwister(0);

        #endregion

        #region Logic

        public override void Update(GameTime gameTime, long frame)
        {
            // Reduce cooldowns.
            foreach (var slot in _cooldowns.Keys)
            {
                if (_cooldowns[slot] > 0)
                {
                    --_cooldowns[slot];
                }
            }

            base.Update(gameTime, frame);
        }

        protected override void UpdateComponent(GameTime gameTime, long frame, WeaponControl component)
        {
            // Nothing to do if we're not shooting.
            if (!component.Shooting)
            {
                return;
            }

            // Get components.
            var character = Manager.GetComponent<Character<AttributeType>>(component.Entity);
            var equipment = Manager.GetComponent<Equipment>(component.Entity);
            var energy = Manager.GetComponent<Energy>(component.Entity);
            var faction = Manager.GetComponent<Faction>(component.Entity);

            // Check all equipped weapon.
            for (int i = 0; i < equipment.GetSlotCount<Weapon>(); i++)
            {
                // Test if this weapon is on cooldown.
                if (_cooldowns[i] > 0)
                {
                    continue;
                }

                // Get the actual weapon item entity.
                var weaponEntity = equipment.GetItem<Weapon>(i);
                if (!weaponEntity.HasValue)
                {
                    continue;
                }
                var weapon = Manager.GetComponent<Weapon>(weaponEntity.Value);

                // Get the energy consumption, skip if we don't have enough.
                var energyConsumption = character.GetValue(AttributeType.WeaponEnergyConsumption, weapon.EnergyConsumption);
                if (energy.Value < energyConsumption)
                {
                    continue;
                }

                // Set cooldown.
                _cooldowns[i] = (int)(character.GetValue(AttributeType.WeaponCooldown, weapon.Cooldown) * 60f);

                // Consume our energy.
                energy.SetValue(energy.Value - energyConsumption);

                // Generate projectiles.
                foreach (var projectile in weapon.Projectiles)
                {
                    projectile.SampleProjectile(Manager, component.Entity, weapon, faction.Value, _random);
                }

                // Generate message.
                WeaponFired message;
                message.Weapon = weapon;
                Manager.SendMessage(ref message);
            }
        }

        /// <summary>
        /// Handles item equipment to track cooldowns.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);

            if (message is ItemAdded)
            {
                var added = (ItemAdded)(ValueType)message;
                if (Manager.GetComponent<Item>(added.Item) is Weapon)
                {
                    // Weapon was equipped, track a cooldown for it.
                    _cooldowns.Add(added.Item, 0);
                }
            }
            else if (message is ItemRemoved)
            {
                var removed = (ItemRemoved)(ValueType)message;
                if (Manager.GetComponent<Item>(removed.Item) is Weapon)
                {
                    // Weapon was unequipped, stop tracking.
                    _cooldowns.Remove(removed.Item);
                }
            }
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

            packet.Write(_cooldowns.Count);
            foreach (var kv in _cooldowns)
            {
                packet.Write(kv.Key);
                packet.Write(kv.Value);
            }

            packet.Write(_random);

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _cooldowns.Clear();
            var numCooldowns = packet.ReadInt32();
            for (int i = 0; i < numCooldowns; i++)
            {
                int key = packet.ReadInt32();
                var value = packet.ReadInt32();
                _cooldowns.Add(key, value);
            }

            _random = packet.ReadPacketizableInto(_random);
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            foreach (var cooldown in _cooldowns.Values)
            {
                hasher.Put(BitConverter.GetBytes(cooldown));
            }
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            var copy = (WeaponControlSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy._cooldowns.Clear();
                foreach (var item in _cooldowns)
                {
                    copy._cooldowns.Add(item.Key, item.Value);
                }
                copy._random = _random.DeepCopy(copy._random);
            }
            else
            {
                copy._cooldowns = new Dictionary<int, int>(_cooldowns);
                copy._random = _random.DeepCopy();
            }

            return copy;
        }

        #endregion
    }
}
