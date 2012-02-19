using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Gives an entity the ability to spawn other entities in a regular
    /// interval.
    /// </summary>
    public sealed class ShipSpawner : Component
    {
        #region Fields
        
        /// <summary>
        /// A list of stations this spawner may send ships to.
        /// </summary>
        public HashSet<int> Targets = new HashSet<int>();

        /// <summary>
        /// The interval in which new entities are being spawned, in ticks.
        /// </summary>
        public int SpawnInterval = 1000;

        /// <summary>
        /// Ticks to wait before sending the next wave.
        /// </summary>
        private int _cooldown;

        #endregion

        #region Logic
        
        /// <summary>
        /// Decrements the remaining cooldown until the next spawn wave, and
        /// spawns some ships, if it expired, then resets it.
        /// </summary>
        /// <param name="parameterization"></param>
        public override void Update(object parameterization)
        {
            if (--_cooldown <= 0)
            {
                var shipSpawnSystem = Entity.Manager.SystemManager.GetSystem<ShipsSpawnSystem>();
                var faction = Entity.GetComponent<Faction>();
                var tranform = Entity.GetComponent<Transform>();
                foreach (var target in Targets)
                {
                    // shipSpawnSystem.CreateAttackingShip(ref tranform.Translation, target, faction.Value);
                }

                _cooldown = SpawnInterval;
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The parameterization to check.</param>
        /// <returns>Whether it's supported.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Removes potential targets from our list when they are removed from
        /// the simulation.
        /// </summary>
        /// <typeparam name="T">The type of message.</typeparam>
        /// <param name="message">The message to handle.</param>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is EntityRemoved)
            {
                var removedMessage = (EntityRemoved)(ValueType)(message);
                Targets.Remove(removedMessage.Entity.UID);
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

            packet.Write(Targets.Count);
            foreach (var item in Targets)
            {
                packet.Write(item);
            }

            packet
                .Write(SpawnInterval)
                .Write(_cooldown);

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            int numTargets = packet.ReadInt32();
            Targets.Clear();
            for (int i = 0; i < numTargets; i++)
            {
                Targets.Add(packet.ReadInt32());
            }

            SpawnInterval = packet.ReadInt32();
            _cooldown = packet.ReadInt32();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(SpawnInterval));
            hasher.Put(BitConverter.GetBytes(_cooldown));
        }

        #endregion

        #region Copying

        public override Component DeepCopy(Component into)
        {
            var copy = (ShipSpawner)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Targets.Clear();
                foreach (var item in Targets)
                {
                    copy.Targets.Add(item);
                }
                copy.SpawnInterval = SpawnInterval;
                copy._cooldown = _cooldown;
            }
            else
            {
                copy.Targets = new HashSet<int>(Targets);
            }

            return copy;
        }

        #endregion
    }
}
