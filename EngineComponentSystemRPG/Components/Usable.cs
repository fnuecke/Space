using System;
using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// A usable entity can be 'activated', triggering some response. An example
    /// would be buff scrolls or healing potions for items.
    /// </summary>
    /// <typeparam name="TResponse">The possible responses triggered when a usable
    /// Item is activated.</typeparam>
    public abstract class Usable<TResponse> : Component
        where TResponse : struct
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
        /// The type of response triggered when activated.
        /// </summary>
        public TResponse Response;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Response = ((Usable<TResponse>)other).Response;

            return this;
        }

        /// <summary>
        /// Initialize with the specified parameters.
        /// </summary>
        /// <param name="response">The response triggered when activated.</param>
        public virtual Usable<TResponse> Initialize(TResponse response)
        {
            this.Response = response;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Response = default(TResponse);
        }

        #endregion

        #region Serialization

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
                .Write(Enum.GetName(typeof(TResponse), Response));
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Response = (TResponse)Enum.Parse(typeof(TResponse), packet.ReadString());
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
            return base.ToString() + ", Response=" + Response;
        }

        #endregion
    }
}