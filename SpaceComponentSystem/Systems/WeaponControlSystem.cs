using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Random;
using Engine.Serialization;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles firing of equipped weapons.
    /// </summary>
    public sealed class WeaponControlSystem : AbstractParallelComponentSystem<WeaponControl>
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

        /// <summary>
        /// List of projectiles we want to create. We iterate in parallel, but
        /// the creation has do be done synchronously because the manager is
        /// not thread safe.
        /// </summary>
        private List<Tuple<ProjectileFactory, int, Weapon, Factions>> _projectilesToCreate = new List<Tuple<ProjectileFactory, int, Weapon, Factions>>(16);

        #endregion

        #region Single allocation

        /// <summary>
        /// Used to iterate the cooldown mapping. We change it (by changing single fields,
        /// which seems to suffice) so we can't directly iterate over the key collection.
        /// </summary>
        private List<int> _reusableEntities = new List<int>();

        #endregion

        #region Logic

        public override void Update(long frame)
        {
            // Reduce cooldowns.
            _reusableEntities.AddRange(_cooldowns.Keys);
            foreach (var slot in _reusableEntities)
            {
                if (_cooldowns[slot] > 0)
                {
                    --_cooldowns[slot];
                }
            }
            _reusableEntities.Clear();

            // Update components (in parallel) to see if someone's shooting.
            base.Update(frame);

            // Create projectiles that were requested.
            foreach (var projectile in _projectilesToCreate)
            {
                projectile.Item1.SampleProjectile(Manager, projectile.Item2, projectile.Item3, projectile.Item4, _random);
            }
            _projectilesToCreate.Clear();
        }

        protected override void UpdateComponent(long frame, WeaponControl component)
        {
            // Nothing to do if we're not shooting.
            if (!component.Shooting)
            {
                return;
            }

            // Get components.
            var character = ((Character<AttributeType>)Manager.GetComponent(component.Entity, Character<AttributeType>.TypeId));
            var equipment = ((Equipment)Manager.GetComponent(component.Entity, Equipment.TypeId));
            var energy = ((Energy)Manager.GetComponent(component.Entity, Energy.TypeId));
            var faction = ((Faction)Manager.GetComponent(component.Entity, Faction.TypeId));

            // Check all equipped weapon.
            for (var i = 0; i < equipment.GetSlotCount<Weapon>(); i++)
            {
                // Get the actual weapon item entity.
                var weaponEntity = equipment.GetItem<Weapon>(i);
                if (!weaponEntity.HasValue)
                {
                    continue;
                }

                // Test if this weapon is on cooldown.
                if (_cooldowns[weaponEntity.Value] > 0)
                {
                    continue;
                }

                // Get the weapon component.
                var weapon = ((Weapon)Manager.GetComponent(weaponEntity.Value, Weapon.TypeId));

                // Get the energy consumption, skip if we don't have enough.
                var energyConsumption = character.GetValue(AttributeType.WeaponEnergyConsumption, weapon.EnergyConsumption);
                if (energy.Value < energyConsumption)
                {
                    continue;
                }

                // Set cooldown.
                _cooldowns[weaponEntity.Value] = (int)(character.GetValue(AttributeType.WeaponCooldown, weapon.Cooldown) * 60f);

                // Consume our energy.
                energy.SetValue(energy.Value - energyConsumption);

                // Generate projectiles.
                foreach (var projectile in weapon.Projectiles)
                {
                    _projectilesToCreate.Add(Tuple.Create(projectile, component.Entity, weapon, faction.Value));
                }

                // Generate message.
                WeaponFired message;
                message.Weapon = weapon;
                message.ShipEntity = component.Entity;
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

            if (message is ItemEquipped)
            {
                var added = (ItemEquipped)(ValueType)message;
                if (Manager.GetComponent(added.Item, Weapon.TypeId) != null)
                {
                    // Weapon was equipped, track a cooldown for it.
                    _cooldowns.Add(added.Item, 0);
                }
            }
            else if (message is ItemUnequipped)
            {
                var removed = (ItemUnequipped)(ValueType)message;
                if (Manager.GetComponent(removed.Item, Weapon.TypeId) != null)
                {
                    // Weapon was unequipped, stop tracking.
                    _cooldowns.Remove(removed.Item);
                }
            }
            else if (message is ComponentRemoved)
            {
                // Item removed from simulation.
                var removed = (ComponentRemoved)(ValueType)message;
                if (removed.Component is Item)
                {
                    _cooldowns.Remove(removed.Component.Entity);
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
            for (var i = 0; i < numCooldowns; i++)
            {
                var key = packet.ReadInt32();
                var value = packet.ReadInt32();
                _cooldowns.Add(key, value);
            }

            packet.ReadPacketizableInto(ref _random);
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
                hasher.Put(cooldown);
            }
            _random.Hash(hasher);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Servers as a copy constructor that returns a new instance of the same
        /// type that is freshly initialized.
        /// 
        /// <para>
        /// This takes care of duplicating reference types to a new copy of that
        /// type (e.g. collections).
        /// </para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (WeaponControlSystem)base.NewInstance();

            copy._cooldowns = new Dictionary<int, int>();
            copy._random = new MersenneTwister(0);
            copy._projectilesToCreate = new List<Tuple<ProjectileFactory, int, Weapon, Factions>>(16);
            copy._reusableEntities = new List<int>();

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// 
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <remarks>The manager for the system to copy into must be set to the
        /// manager into which the system is being copied.</remarks>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (WeaponControlSystem)into;

            copy._cooldowns.Clear();
            foreach (var item in _cooldowns)
            {
                copy._cooldowns.Add(item.Key, item.Value);
            }

            _random.CopyInto(copy._random);
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
            return base.ToString() + ", Cooldowns=[" + string.Join(", ", _cooldowns) + "], Random=" + _random;
        }

        #endregion
    }
}
