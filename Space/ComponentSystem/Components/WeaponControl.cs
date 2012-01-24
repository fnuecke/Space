using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Entities;
using Space.ComponentSystem.Messages;
using Space.ComponentSystem.Modules;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Controls whether weapons on an entity should be shooting.
    /// </summary>
    public class WeaponControl : AbstractComponent
    {
        #region Fields

        /// <summary>
        /// Whether ima currently firin mah lazer or not.
        /// </summary>
        public bool Shooting;

        /// <summary>
        /// Cooldowns of known weapon components.
        /// </summary>
        private Dictionary<int, int> _cooldowns = new Dictionary<int, int>();

        #endregion

        #region Logic

        /// <summary>
        /// Takes care of firing weapons that are not on cooldown, and reducing
        /// cooldown for weapons that are.
        /// </summary>
        /// <param name="parameterization">The parameters to use.</param>
        public override void Update(object parameterization)
        {
            // Reduce cooldowns.
            foreach (var componentUid in new List<int>(_cooldowns.Keys))
            {
                if (_cooldowns[componentUid] > 0)
                {
                    --_cooldowns[componentUid];
                }
            }

            // Check all weapon modules.
            if (Shooting)
            {
                var modules = Entity.GetComponent<ModuleManager<SpaceModifier>>();
                var energy = Entity.GetComponent<Energy>();
                var faction = Entity.GetComponent<Faction>();

                if (modules != null && energy != null && faction != null)
                {
                    foreach (var weapon in modules.GetModules<Weapon>())
                    {
                        var energyConsumption = modules.GetValue(SpaceModifier.WeaponEnergyConsumption, weapon.EnergyConsumption);
                        if (energy != null && energy.Value >= energyConsumption)
                        {
                            // Test if this weapon is on cooldown.
                            if (_cooldowns[weapon.UID] == 0)
                            {
                                energy.Value -= energyConsumption;
                                // No, fire it.
                                _cooldowns[weapon.UID] = (int)modules.GetValue(SpaceModifier.WeaponCooldown, weapon.Cooldown);

                                // Generate projectiles.
                                foreach (var projectileData in weapon.Projectiles)
                                {
                                    Entity.Manager.AddEntity(EntityFactory.CreateProjectile(
                                        projectileData, Entity, faction.Value));
                                }

                                // Generate message.
                                WeaponFired message;
                                message.Weapon = weapon;
                                Entity.SendMessage(ref message);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Accepts parameterizations of type <c>DefaultLogicParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The type to check.</param>
        /// <returns>Whether the type is supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        /// <summary>
        /// Handles messages to check if a weapon was equipped or unequipped.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is ModuleAdded<SpaceModifier>)
            {
                var added = (ModuleAdded<SpaceModifier>)(ValueType)message;
                if (added.Module is Weapon)
                {
                    // Weapon was equipped, track a cooldown for it.
                    _cooldowns.Add(added.Module.UID, 0);
                }
            }
            else if (message is ModuleRemoved<SpaceModifier>)
            {
                var removed = (ModuleRemoved<SpaceModifier>)(ValueType)message;
                if (removed.Module is Weapon)
                {
                    // Weapon was unequipped, stop tracking.
                    _cooldowns.Remove(removed.Module.UID);
                }
            }
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
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(Shooting);

            packet.Write(_cooldowns.Count);
            foreach (var kv in _cooldowns)
            {
                packet.Write(kv.Key);
                packet.Write(kv.Value);
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

            Shooting = packet.ReadBoolean();
            _cooldowns.Clear();
            var numCooldowns = packet.ReadInt32();
            for (int i = 0; i < numCooldowns; i++)
            {
                int key = packet.ReadInt32();
                var value = packet.ReadInt32();
                _cooldowns.Add(key, value);
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
            
            hasher.Put(BitConverter.GetBytes(Shooting));
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
        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (WeaponControl)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Shooting = Shooting;
                copy._cooldowns.Clear();
                foreach (var item in _cooldowns)
                {
                    copy._cooldowns.Add(item.Key, item.Value);
                }
            }
            else
            {
                copy._cooldowns = new Dictionary<int, int>(_cooldowns);
            }

            return copy;
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
            return base.ToString() + ", " + Shooting.ToString();
        }

        #endregion
    }
}
