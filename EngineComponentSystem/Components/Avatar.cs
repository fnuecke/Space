using System;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Part of entities that represent a player's presence in a game.
    /// </summary>
    public sealed class Avatar : AbstractComponent
    {
        #region Fields

        /// <summary>
        /// The number of player whose avatar this is.
        /// </summary>
        public int PlayerNumber;

        #endregion

        #region Constructor

        public Avatar(int playerNumber)
        {
            this.PlayerNumber = playerNumber;
        }

        public Avatar()
            : this(-1)
        {
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(PlayerNumber);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            PlayerNumber = packet.ReadInt32();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(PlayerNumber));
        }

        #endregion

        #region Copying

        protected override bool ValidateType(AbstractComponent instance)
        {
            return instance is Avatar;
        }

        protected override void CopyFields(AbstractComponent into, bool isShallowCopy)
        {
            base.CopyFields(into, isShallowCopy);

            if (!isShallowCopy)
            {
                var copy = (Avatar)into;

                copy.PlayerNumber = PlayerNumber;
            }
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return GetType().Name + ": " + PlayerNumber.ToString();
        }

        #endregion
    }
}
