using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Allows assigning entities to factions.
    /// </summary>
    public class Faction : AbstractComponent
    {
        #region Properties
        
        /// <summary>
        /// The faction this component's entity belongs to.
        /// </summary>
        public Factions Value { get; set; }

        #endregion

        #region Constructor

        public Faction(Factions factions)
        {
            this.Value = factions;
        }

        public Faction()
            : this(Factions.None)
        {
        }

        #endregion

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write((byte)Value);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = (Factions)packet.ReadByte();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put((byte)Value);
        }

        #endregion
    }
}
