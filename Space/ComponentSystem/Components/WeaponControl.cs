using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Components.Messages;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Components.Messages;
using Space.ComponentSystem.Entities;
using Space.Data;
using Space.Data.Modules;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Controls whether weapons on an entity should be shooting.
    /// </summary>
    public class WeaponControl : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// Whether ima currently firin mah lazer or not.
        /// </summary>
        public bool IsShooting { get; set; }

        #endregion

        #region Fields

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
#if DEBUG
            base.Update(parameterization);
#endif
            // Reduce cooldowns.
            foreach (var componentUid in new List<int>(_cooldowns.Keys))
            {
                if (_cooldowns[componentUid] > 0)
                {
                    --_cooldowns[componentUid];
                }
            }

            // Check all weapon modules.
            if (IsShooting)
            {
                var modules = Entity.GetComponent<EntityModules<EntityAttributeType>>();
                if (modules != null)
                {
                    foreach (var weapon in modules.GetModules<WeaponModule>())
                    {
                        var energyConsumption = modules.GetValue(EntityAttributeType.WeaponEnergyConsumption, weapon.EnergyConsumption);
                        var energie = Entity.GetComponent<Energy>();
                        if (energie != null && energie.Value >= energyConsumption)
                        {
                            // Test if this weapon is on cooldown.
                            if (_cooldowns[weapon.UID] == 0)
                            {
                                energie.Value -= energyConsumption;
                                // No, fire it.
                                _cooldowns[weapon.UID] = (int)modules.GetValue(EntityAttributeType.WeaponCooldown, weapon.Cooldown);

                                // Generate projectiles.
                                foreach (var projectile in weapon.Projectiles)
                                {
                                    Entity.Manager.AddEntity(EntityFactory.CreateProjectile(Entity, projectile));
                                }

                                // Generate message.
                                Entity.SendMessage(WeaponFired.Create(weapon));
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
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        /// <summary>
        /// Handles messages to check if a weapon was equipped or unequipped.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        public override void HandleMessage(ValueType message)
        {
            if (message is ModuleAdded<EntityAttributeType>)
            {
                var added = (ModuleAdded<EntityAttributeType>)message;
                if (added.Module is WeaponModule)
                {
                    // Weapon was equipped, track a cooldown for it.
                    _cooldowns.Add(added.Module.UID, 0);
                }
            }
            else if (message is ModuleRemoved<EntityAttributeType>)
            {
                var removed = (ModuleRemoved<EntityAttributeType>)message;
                if (removed.Module is WeaponModule)
                {
                    // Weapon was unequipped, stop tracking.
                    _cooldowns.Remove(removed.Module.UID);
                }
            }
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            packet.Write(IsShooting);

            packet.Write(_cooldowns.Count);
            foreach (var kv in _cooldowns)
            {
                packet.Write(kv.Key);
                packet.Write(kv.Value);
            }

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            IsShooting = packet.ReadBoolean();
            _cooldowns.Clear();
            var numCooldowns = packet.ReadInt32();
            for (int i = 0; i < numCooldowns; i++)
            {
                int key = packet.ReadInt32();
                var value = packet.ReadInt32();
                _cooldowns.Add(key, value);
            }
        }

        public override void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(IsShooting));
            foreach (var cooldown in _cooldowns.Values)
            {
                hasher.Put(BitConverter.GetBytes(cooldown));
            }
        }

        public override object Clone()
        {
            var copy = (WeaponControl)base.Clone();

            copy._cooldowns = new Dictionary<int, int>(_cooldowns);

            return copy;
        }

        #endregion
    }
}
