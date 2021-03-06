﻿using System.Collections.Generic;
using System.IO;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

namespace Space.ComponentSystem.Components
{
    /// <summary>Gives an entity the ability to spawn other entities in a regular interval.</summary>
    public sealed class ShipSpawner : Component
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

        #region Fields

        /// <summary>A list of stations this spawner may send ships to.</summary>
        [CopyIgnore, PacketizeIgnore]
        public readonly HashSet<int> Targets = new HashSet<int>();

        /// <summary>The interval in which new entities are being spawned, in ticks.</summary>
        public int SpawnInterval = 1000;

        /// <summary>Ticks to wait before sending the next wave.</summary>
        internal int Cooldown;

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherSpawner = (ShipSpawner) other;
            Targets.UnionWith(otherSpawner.Targets);

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Targets.Clear();
            SpawnInterval = 0;
            Cooldown = 0;
        }

        #endregion

        #region Serialization / Hashing

        [OnPacketize]
        public IWritablePacket Packetize(IWritablePacket packet)
        {
            packet.Write(Targets.Count);
            foreach (var item in Targets)
            {
                packet.Write(item);
            }

            return packet;
        }

        [OnPostDepacketize]
        public void Depacketize(IReadablePacket packet)
        {
            Targets.Clear();
            var targetCount = packet.ReadInt32();
            for (var i = 0; i < targetCount; i++)
            {
                Targets.Add(packet.ReadInt32());
            }
        }

        [OnStringify]
        public StreamWriter Dump(StreamWriter w, int indent)
        {
            w.AppendIndent(indent).Write("Targets = {");
            var first = true;
            foreach (var target in Targets)
            {
                if (!first)
                {
                    w.Write(", ");
                }
                first = false;
                w.Write(target);
            }
            w.Write("}");

            return w;
        }

        #endregion
    }
}