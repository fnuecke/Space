using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Part of entities that represent a player's presence in a game.
    /// </summary>
    public class Avatar : AbstractComponent
    {
        #region Packetizer registration

        static Avatar()
        {
            Packetizer.Register<Avatar>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The number of player whose avatar this is.
        /// </summary>
        public int PlayerNumber { get; set; }

        #endregion

        #region Logic
        
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType.Equals(typeof(AvatarParameterization));
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return packet.Write(PlayerNumber);
        }

        public override void Depacketize(Packet packet)
        {
            PlayerNumber = packet.ReadInt32();
        }

        public override void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(PlayerNumber));
        }

        #endregion
    }
}
