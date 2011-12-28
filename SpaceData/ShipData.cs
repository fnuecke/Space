using Engine.Serialization;
using Microsoft.Xna.Framework.Content;
using Space.Data.Modules;

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
        public HullModule[] Hulls = new HullModule[0];

        /// <summary>
        /// Slots, occupied or not, of hulls for this ship.
        /// </summary>
        public ReactorModule[] Reactors = new ReactorModule[0];

        /// <summary>
        /// Slots, occupied or not, of hulls for this ship.
        /// </summary>
        public ThrusterModule[] Thrusters = new ThrusterModule[0];

        /// <summary>
        /// Slots, occupied or not, of hulls for this ship.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public ShieldModule[] Shields = new ShieldModule[0];

        /// <summary>
        /// Slots, occupied or not, of hulls for this ship.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public WeaponModule[] Weapons = new WeaponModule[0];

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
                .Write(Weapons);
        }

        public void Depacketize(Packet packet)
        {
            Name = packet.ReadString();
            CollisionRadius = packet.ReadSingle();
            Texture = packet.ReadString();
            Hulls = packet.ReadPacketizables<HullModule>();
            Reactors = packet.ReadPacketizables<ReactorModule>();
            Thrusters = packet.ReadPacketizables<ThrusterModule>();
            Shields = packet.ReadPacketizables<ShieldModule>();
            Weapons = packet.ReadPacketizables<WeaponModule>();
        }

        #endregion
    }
}
