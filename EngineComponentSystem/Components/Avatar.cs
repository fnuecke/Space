﻿using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Part of entities that represent a player's presence in a game.
    /// </summary>
    public sealed class Avatar : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The number of player whose avatar this is.
        /// </summary>
        public int PlayerNumber { get; set; }

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

        #region Logic

        /// <summary>
        /// Supports <c>AvatarParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The parameterization to check.</param>
        /// <returns>Whether it is supported.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(AvatarParameterization);
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

        #region ToString

        public override string ToString()
        {
            return GetType().Name + ": " + PlayerNumber.ToString();
        }

        #endregion
    }
}
