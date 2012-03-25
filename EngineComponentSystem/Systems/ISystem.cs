using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Interface for generic systems (which implement self-contained logic).
    /// </summary>
    public interface ISystem : ICopyable<ISystem>, IPacketizable, IHashable
    {
        #region Properties

        /// <summary>
        /// The component system manager this system is part of.
        /// </summary>
        IManager Manager { get; set; }

        #endregion

        #region Logic

        /// <summary>
        /// Update all components in this system.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        void Update(GameTime gameTime, long frame);

        #endregion

        #region Messaging

        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        void Receive<T>(ref T message) where T : struct;

        #endregion
    }
}
