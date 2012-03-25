using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

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
        public readonly HashSet<int> Targets = new HashSet<int>();

        /// <summary>
        /// The interval in which new entities are being spawned, in ticks.
        /// </summary>
        public int SpawnInterval = 1000;

        /// <summary>
        /// Ticks to wait before sending the next wave.
        /// </summary>
        internal int Cooldown;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherSpawner = (ShipSpawner)other;
            Targets.UnionWith(otherSpawner.Targets);
            SpawnInterval = otherSpawner.SpawnInterval;
            Cooldown = otherSpawner.Cooldown;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Targets.Clear();
            SpawnInterval = 0;
            Cooldown = 0;
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
                .Write(Cooldown);

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
            Cooldown = packet.ReadInt32();
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
            hasher.Put(BitConverter.GetBytes(Cooldown));
        }

        #endregion
    }
}
