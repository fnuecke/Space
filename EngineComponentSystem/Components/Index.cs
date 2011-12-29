﻿using System;
using Engine.ComponentSystem.Components.Messages;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Allows tracking a component with a position in the <c>IndexSystem</c>
    /// for quick nearest neighbor queries.
    /// </summary>
    public sealed class Index : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The bit mask of the index group this component will belong to.
        /// There are a total of 64 separate groups, via the 64 bits in a
        /// ulong.
        /// </summary>
        public ulong IndexGroups { get; set; }

        /// <summary>
        /// Whether the position of our entity changed since the last update.
        /// </summary>
        public bool PositionChanged { get; private set; }
        
        /// <summary>
        /// The position we had before a position change.
        /// </summary>
        public Vector2 PreviousPosition { get; private set; }

        #endregion

        #region Constructor

        public Index(ulong groups)
        {
            this.IndexGroups = groups;
        }

        public Index()
            : this(IndexSystem.DefaultIndexGroup)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Tells the index system whether our position changed.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            var args = (IndexParameterization)parameterization;

            if (PositionChanged)
            {
                args.IndexGroups = IndexGroups;
                args.PositionChanged = true;
                args.PreviousPosition = PreviousPosition;
            }

            PositionChanged = false;
        }

        /// <summary>
        /// Supports <c>IndexParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The type to check.</param>
        /// <returns>Whether it's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(IndexParameterization);
        }

        /// <summary>
        /// Uses <c>TranslationChanged</c> messages to set our changed flag.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        public override void HandleMessage(ValueType message)
        {
            if (message is TranslationChanged)
            {
                // Position changed. If this is the first, remember this as
                // our previous position.
                if (!PositionChanged)
                {
                    PreviousPosition = ((TranslationChanged)message).PreviousPosition;
                }
                PositionChanged = true;
            }
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(IndexGroups);
            packet.Write(PositionChanged);
            packet.Write(PreviousPosition);

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            IndexGroups = packet.ReadUInt64();
            PositionChanged = packet.ReadBoolean();
            PreviousPosition = packet.ReadVector2();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(IndexGroups));
            hasher.Put(BitConverter.GetBytes(PositionChanged));
            hasher.Put(BitConverter.GetBytes(PreviousPosition.X));
            hasher.Put(BitConverter.GetBytes(PreviousPosition.Y));
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return GetType().Name + ": " + PositionChanged.ToString() + ", " + PreviousPosition;
        }

        #endregion
    }
}
