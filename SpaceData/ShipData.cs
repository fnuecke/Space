using System.Collections.Generic;
using Engine.Math;
using Engine.Serialization;

namespace Space.Data
{
    public class ShipData : IPacketizable
    {
        /// <summary>
        /// The name of the ship, which serves as a unique type identifier.
        /// </summary>
        public string Name;

        /// <summary>
        /// The collision radius of the ship.
        /// </summary>
        public Fixed Radius;

        /// <summary>
        /// The texture to use for rendering the ship.
        /// </summary>
        public string Texture;

        /// <summary>
        /// Available module slots per type for this ship.
        /// </summary>
        public Dictionary<ShipModuleType, int> Slots = new Dictionary<ShipModuleType, int>();

        /// <summary>
        /// Default modules for required slots.
        /// </summary>
        public ShipModule[] BaseModules = new ShipModule[0];

        public Packet Packetize(Packet packet)
        {
            packet
                .Write(Name)
                .Write(Radius)
                .Write(Texture)
                .Write(Slots.Count);
            foreach (var slot in Slots)
            {
                packet.Write((byte)slot.Key);
                packet.Write(slot.Value);
            }
            packet.Write(BaseModules.Length);
            foreach (var module in BaseModules)
            {
                packet.Write(module);
            }
            return packet;
        }

        public void Depacketize(Packet packet)
        {
            Name = packet.ReadString();
            Radius = packet.ReadFixed();
            Texture = packet.ReadString();

            Slots.Clear();
            var numSlots = packet.ReadInt32();
            for (int i = 0; i < numSlots; i++)
            {
                var key = (ShipModuleType)packet.ReadByte();
                var value = packet.ReadInt32();
                Slots.Add(key, value);
            }
            var numModules = packet.ReadInt32();
            BaseModules = new ShipModule[numModules];
            for (int i = 0; i < numModules; i++)
            {
                BaseModules[i] = packet.ReadPacketizable(new ShipModule());
            }
        }
    }
}
