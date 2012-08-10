﻿using System.Globalization;
using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    /// This component defines in which layer to render an entity in a parallax render system.
    /// </summary>
    public sealed class Parallax : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The "layer" the component is in.
        /// </summary>
        /// <remarks>
        /// This directly translates to the offset used when rendering it, where <c>1.0f</c>
        /// means 1:1 mapping of coordinate to screen space. Lower values make objects
        /// "move slower"/appear further back, higher values do the opposite.</remarks>
        public float Layer = 1.0f;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Layer = ((Parallax)other).Layer;

            return this;
        }

        /// <summary>
        /// Initialize with the specified layer.
        /// </summary>
        /// <param name="layer">The layer.</param>
        public Parallax Initialize(float layer)
        {
            Layer = layer;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Layer = 1.0f;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Layer);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Layer = packet.ReadSingle();
        }

        /// <summary>
        /// Suppress hashing as this component has no influence on other
        /// components and actual simulation logic.
        /// </summary>
        /// <param name="hasher"></param>
        public override void Hash(Hasher hasher)
        {
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Layer=" + Layer.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}