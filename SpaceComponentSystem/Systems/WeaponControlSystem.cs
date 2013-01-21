using System.Collections.Generic;
using System.IO;
using System.Linq;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Random;
using Engine.Serialization;
using Engine.Util;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Handles firing of equipped weapons.</summary>
    public sealed class WeaponControlSystem : AbstractParallelComponentSystem<WeaponControl>, IMessagingSystem
    {
        #region Fields

        /// <summary>Cooldowns of known weapon components.</summary>
        [CopyIgnore, PacketizerIgnore]
        private Dictionary<int, int> _cooldowns = new Dictionary<int, int>();

        /// <summary>Randomizer used for sampling projectiles.</summary>
        private MersenneTwister _random = new MersenneTwister(0);

        /// <summary>
        ///     List of projectiles we want to create. We iterate in parallel, but the creation has do be done synchronously
        ///     because the manager is not thread safe.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        private List<PendingProjectile> _projectilesToCreate = new List<PendingProjectile>(16);

        #endregion

        #region Single allocation

        /// <summary>
        ///     Used to iterate the cooldown mapping. We change it (by changing single fields, which seems to suffice) so we
        ///     can't directly iterate over the key collection.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
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

            // Create projectiles that were requested. Sort projectiles by
            // emitter id to get deterministic behavior even though we're
            // multithreading. We use LINQ because that's stable. The normal
            // Sort for arrays/collections isn't.
            foreach (var projectile in _projectilesToCreate.OrderBy(x => x.Entity))
            {
                projectile.Factory.SampleProjectile(
                    Manager,
                    projectile.Entity,
                    projectile.Offset,
                    projectile.Rotation,
                    projectile.Weapon,
                    projectile.Faction,
                    _random);
            }
            _projectilesToCreate.Clear();
        }

        /// <summary>Updates the component.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, WeaponControl component)
        {
            // Nothing to do if we're not shooting.
            if (!component.Shooting)
            {
                return;
            }

            // Get components.
            var attributes = (Attributes<AttributeType>) Manager.GetComponent(component.Entity, Attributes<AttributeType>.TypeId);
            var equipment = (ItemSlot) Manager.GetComponent(component.Entity, ItemSlot.TypeId);
            var energy = (Energy) Manager.GetComponent(component.Entity, Energy.TypeId);
            var faction = (Faction) Manager.GetComponent(component.Entity, Faction.TypeId);

            // Check all equipped weapon.
            foreach (var slot in equipment.AllSlots)
            {
                // Skip empty slots.
                if (slot.Item <= 0)
                {
                    continue;
                }

                // Skip if it's not a weapon.
                var weapon = (Weapon) Manager.GetComponent(slot.Item, Weapon.TypeId);
                if (weapon == null)
                {
                    continue;
                }

                // Test if this weapon is on cooldown.
                if (_cooldowns[weapon.Entity] > 0)
                {
                    continue;
                }

                // Get the energy consumption, skip if we don't have enough.
                var energyConsumption = 0f;
                if (weapon.Attributes.ContainsKey(AttributeType.WeaponEnergyConsumption))
                {
                    energyConsumption = attributes.GetValue(
                        AttributeType.WeaponEnergyConsumption,
                        weapon.Attributes[AttributeType.WeaponEnergyConsumption]);
                }
                if (energy.Value < energyConsumption)
                {
                    continue;
                }

                // Set cooldown.
                var cooldown = 0f;
                if (weapon.Attributes.ContainsKey(AttributeType.WeaponCooldown))
                {
                    cooldown = attributes.GetValue(
                        AttributeType.WeaponCooldown,
                        weapon.Attributes[AttributeType.WeaponCooldown]);
                }
                _cooldowns[weapon.Entity] = (int) (cooldown * Settings.TicksPerSecond);

                // Consume our energy.
                energy.SetValue(energy.Value - energyConsumption);

                // Compute spawn offset.
                var offset = Vector2.Zero;
                var rotation = 0f;
                ((SpaceItemSlot) slot).Accumulate(ref offset, ref rotation);

                // Generate projectiles.
                foreach (var projectile in weapon.Projectiles)
                {
                    _projectilesToCreate.Add(
                        new PendingProjectile
                        {
                            Factory = projectile,
                            Entity = component.Entity,
                            Offset = XnaUnitConversion.ToSimulationUnits(offset),
                            Rotation = rotation,
                            Weapon = weapon,
                            Faction = faction.Value
                        });
                }

                // Play sound.
                var soundSystem = (SoundSystem) Manager.GetSystem(SoundSystem.TypeId);
                if (soundSystem != null)
                {
                    soundSystem.Play(weapon.Sound, component.Entity);
                }
            }
        }

        /// <summary>Called when a component is removed.</summary>
        /// <param name="component">The component.</param>
        public override void OnComponentRemoved(IComponent component)
        {
            base.OnComponentRemoved(component);

            if (component is Item)
            {
                // An item was removed, clear its cooldowns.
                _cooldowns.Remove(component.Entity);
            }
        }

        /// <summary>Handles item equipment to track cooldowns.</summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as ItemEquipped?;
                if (cm != null)
                {
                    var m = cm.Value;
                    if (Manager.GetComponent(m.Item, Weapon.TypeId) != null)
                    {
                        // Weapon was equipped, track a cooldown for it.
                        _cooldowns.Add(m.Item, 0);
                    }
                    return;
                }
            }
            {
                var cm = message as ItemUnequipped?;
                if (cm != null)
                {
                    var m = cm.Value;
                    if (Manager.GetComponent(m.Item, Weapon.TypeId) != null)
                    {
                        // Weapon was unequipped, stop tracking.
                        _cooldowns.Remove(m.Item);
                    }
                }
            }
        }

        #endregion

        #region Serialization

        /// <summary>Write the object's state to the given packet.</summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);

            packet.Write(_cooldowns.Count);
            foreach (var kv in _cooldowns)
            {
                packet.Write(kv.Key);
                packet.Write(kv.Value);
            }

            return packet;
        }

        /// <summary>Bring the object to the state in the given packet.</summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            _cooldowns.Clear();
            var cooldownsCount = packet.ReadInt32();
            for (var i = 0; i < cooldownsCount; i++)
            {
                var key = packet.ReadInt32();
                var value = packet.ReadInt32();
                _cooldowns.Add(key, value);
            }
        }

        /// <summary>Dumps the specified sb.</summary>
        /// <param name="w">The sb.</param>
        /// <param name="indent">The indent.</param>
        /// <returns></returns>
        public override StreamWriter Dump(StreamWriter w, int indent)
        {
            base.Dump(w, indent);

            w.AppendIndent(indent).Write("Cooldowns = {");
            foreach (var cooldown in _cooldowns)
            {
                w.AppendIndent(indent + 1).Write(cooldown.Key);
                w.Write(" = ");
                w.Write(cooldown.Value);
            }
            w.AppendIndent(indent).Write("}");

            return w;
        }

        #endregion

        #region Copying

        /// <summary>
        ///     Servers as a copy constructor that returns a new instance of the same type that is freshly initialized.
        ///     <para>This takes care of duplicating reference types to a new copy of that type (e.g. collections).</para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (WeaponControlSystem) base.NewInstance();

            copy._cooldowns = new Dictionary<int, int>();
            copy._random = new MersenneTwister(0);
            copy._projectilesToCreate = new List<PendingProjectile>(16);
            copy._reusableEntities = new List<int>();

            return copy;
        }

        /// <summary>
        ///     Creates a deep copy of the system. The passed system must be of the same type.
        ///     <para>
        ///         This clones any contained data types to return an instance that represents a complete copy of the one passed
        ///         in.
        ///     </para>
        /// </summary>
        /// <remarks>The manager for the system to copy into must be set to the manager into which the system is being copied.</remarks>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (WeaponControlSystem) into;

            copy._cooldowns.Clear();
            foreach (var item in _cooldowns)
            {
                copy._cooldowns.Add(item.Key, item.Value);
            }
        }

        #endregion

        #region Types

        /// <summary>Storage for pending projectile generation.</summary>
        private sealed class PendingProjectile
        {
            /// <summary>The projectile type to create (via its factory).</summary>
            public ProjectileFactory Factory;

            /// <summary>The entity that emitted the projectile.</summary>
            public int Entity;

            /// <summary>The offset of the projectile from its emitter.</summary>
            public Vector2 Offset;

            /// <summary>The rotation (angle) in which to emit the projectile.</summary>
            public float Rotation;

            /// <summary>The weapon that shot the projectile.</summary>
            public Weapon Weapon;

            /// <summary>The faction the shooter belongs to.</summary>
            public Factions Faction;
        }

        #endregion
    }
}