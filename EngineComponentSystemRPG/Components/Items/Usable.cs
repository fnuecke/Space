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
        #region Fields

        /// <summary>
        /// The type of response triggered when activated.
        /// </summary>
        public TResponse Response;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new usable item with the specified parameters.
        /// </summary>
        /// <param name="response">The response triggered when activated.</param>
        public Usable(TResponse response)
        {
            this.Response = response;
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public Usable()
        {
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

        #region Copying

        public override Component DeepCopy(Component into)
        {
            var copy = (Usable<TResponse>)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Response = Response;
            }

            return copy;
        }

        #endregion
    }
}
