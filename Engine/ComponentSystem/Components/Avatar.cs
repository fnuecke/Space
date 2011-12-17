using System;
using Engine.ComponentSystem.Entities;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Part of entities that represent a player's presence in a game.
    /// </summary>
    public class Avatar : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The number of player whose avatar this is.
        /// </summary>
        public int PlayerNumber { get; set; }

        /// <summary>
        /// The entity that serves as the player's avatar.
        /// </summary>
        public IEntity Entity { get; private set; }

        #endregion

        public Avatar(IEntity entity)
        {
            this.Entity = entity;
        }

        public override void Packetize(Serialization.Packet packet)
        {
            packet.Write(PlayerNumber);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            PlayerNumber = packet.ReadInt32();
        }

        public override void Hash(Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(PlayerNumber));
        }
    }
}
