using Engine.ComponentSystem.Entities;
using Engine.Serialization;

namespace Space.ComponentSystem.Constraints
{
    public sealed class PlayerShip : ShipConstraints, IPacketizable
    {
        /// <summary>
        /// Saves the state of a player's ship.
        /// </summary>
        /// <param name="ship">The ship representing the player's avatar.</param>
        public void Store(Entity ship)
        {

        }

        #region Serialization

        public Packet Packetize(Packet packet)
        {
            return packet;
        }

        public void Depacketize(Packet packet)
        {
        }

        #endregion
    }
}
