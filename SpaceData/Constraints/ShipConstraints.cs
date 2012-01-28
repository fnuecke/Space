using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;

namespace Space.Data.Constraints
{
    /// <summary>
    /// Basic descriptor for a single ship class.
    /// </summary>
    public sealed class ShipConstraints : IPacketizable
    {
        #region General

        /// <summary>
        /// The name of the ship class, which serves as a unique type
        /// identifier.
        /// </summary>
        public string Name;

        /// <summary>
        /// The base texture to use for rendering the ship class.
        /// </summary>
        public string Texture;

        /// <summary>
        /// The base collision radius of the ship class.
        /// </summary>
        public float CollisionRadius;

        /// <summary>
        /// List of basic stats for this ship class.
        /// </summary>
        public AttributeModifier<AttributeType>[] Attributes;

        #endregion

        #region Equipment slots

        /// <summary>
        /// The number of sensor slots available for this ship class.
        /// </summary>
        public int SensorSlots;

        /// <summary>
        /// The number of hull slots available for this ship class.
        /// </summary>
        public int HullSlots;

        /// <summary>
        /// The number of reactor slots available for this ship class.
        /// </summary>
        public int ReactorSlots;

        /// <summary>
        /// The number of shield slots available for this ship class.
        /// </summary>
        public int ShieldSlots;

        /// <summary>
        /// The number of thruster slots available for this ship class.
        /// </summary>
        public int ThrusterSlots;

        /// <summary>
        /// The number of weapon slots available for this ship class.
        /// </summary>
        public int WeaponSlots;

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public Packet Packetize(Packet packet)
        {
            packet.Write(Name);
            packet.Write(Texture);
            packet.Write(CollisionRadius);
            packet.Write(Attributes);
            packet.Write(SensorSlots);
            packet.Write(HullSlots);
            packet.Write(ReactorSlots);
            packet.Write(ShieldSlots);
            packet.Write(ThrusterSlots);
            packet.Write(WeaponSlots);

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            Name = packet.ReadString();
            Texture = packet.ReadString();
            CollisionRadius = packet.ReadSingle();
            Attributes = packet.ReadPacketizables<AttributeModifier<AttributeType>>();
            SensorSlots = packet.ReadInt32();
            HullSlots = packet.ReadInt32();
            ReactorSlots = packet.ReadInt32();
            ShieldSlots = packet.ReadInt32();
            ThrusterSlots = packet.ReadInt32();
            WeaponSlots = packet.ReadInt32();
        }

        #endregion
    }
}
