using Engine.Data;
using Engine.Math;
using Engine.Serialization;
using Microsoft.Xna.Framework.Content;

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
        /// Basic attributes for the ship.
        /// </summary>
        public EntityAttribute<EntityAttributeType>[] BaseAttributes;

        /// <summary>
        /// Available slots for weapons.
        /// </summary>
        public byte WeaponSlots;

        /// <summary>
        /// Available equipment slots for other items.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public byte ItemSlots;

        public Packet Packetize(Packet packet)
        {
            packet
                .Write(Name)
                .Write(Radius)
                .Write(Texture)
                .Write(WeaponSlots)
                .Write(ItemSlots)
                .Write(BaseAttributes.Length);
            foreach (var attribute in BaseAttributes)
            {
                packet.Write(attribute);
            }
            return packet;
        }

        public void Depacketize(Packet packet)
        {
            Name = packet.ReadString();
            Radius = packet.ReadFixed();
            Texture = packet.ReadString();
            WeaponSlots = packet.ReadByte();
            ItemSlots = packet.ReadByte();
            var numAttributes = packet.ReadInt32();
            BaseAttributes = new EntityAttribute<EntityAttributeType>[numAttributes];
            for (int i = 0; i < numAttributes; i++)
            {
                BaseAttributes[i] = packet.ReadPacketizable(new EntityAttribute<EntityAttributeType>());
            }
        }
    }
}
