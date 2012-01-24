using Engine.Serialization;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Modules;

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
        public float CollisionRadius;

        /// <summary>
        /// The texture to use for rendering the ship.
        /// </summary>
        public string Texture;

        /// <summary>
        /// Slots, occupied or not, of hulls for this ship.
        /// </summary>
        public Hull[] Hulls = new Hull[0];

        /// <summary>
        /// Slots, occupied or not, of reactors for this ship.
        /// </summary>
        public Reactor[] Reactors = new Reactor[0];

        /// <summary>
        /// Slots, occupied or not, of thrusters for this ship.
        /// </summary>
        public Thruster[] Thrusters = new Thruster[0];

        /// <summary>
        /// Slots, occupied or not, of shields for this ship.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Shield[] Shields = new Shield[0];

        /// <summary>
        /// Slots, occupied or not, of weapons for this ship.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Weapon[] Weapons = new Weapon[0];


        /// <summary>
        /// Slot, occupied or not, of sensors for this ship.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Sensor[] Sensors = new Sensor[0];

        #region Serialization

        public Packet Packetize(Packet packet)
        {
            return packet
                .Write(Name)
                .Write(CollisionRadius)
                .Write(Texture)
                .Write(Hulls)
                .Write(Reactors)
                .Write(Thrusters)
                .Write(Shields)
                .Write(Weapons)
                .Write(Sensors);
        }

        public void Depacketize(Packet packet)
        {
            Name = packet.ReadString();
            CollisionRadius = packet.ReadSingle();
            Texture = packet.ReadString();
            Hulls = packet.ReadPacketizables<Hull>();
            Reactors = packet.ReadPacketizables<Reactor>();
            Thrusters = packet.ReadPacketizables<Thruster>();
            Shields = packet.ReadPacketizables<Shield>();
            Weapons = packet.ReadPacketizables<Weapon>();
            Sensors = packet.ReadPacketizables<Sensor>();
        }

        #endregion
    }
}
