using Engine.Serialization;
using Space.Data;

namespace Space.Session
{
    /// <summary>
    /// Represents some information about a single player.
    /// </summary>
    public class PlayerData : IPacketizable
    {
        #region Properties
        
        /// <summary>
        /// The ship of the player.
        /// </summary>
        public ShipData Ship { get; set; }

        #endregion

        #region Serialization

        public Packet Packetize(Packet packet)
        {
            return packet.Write(Ship);
        }

        public void Depacketize(Packet packet)
        {
            Ship = packet.ReadPacketizable<ShipData>();
        }

        #endregion
    }
}
