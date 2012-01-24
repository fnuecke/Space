using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Update types for a component system, based on the different update
    /// stages of a game loop (normally at least update + draw).
    /// </summary>
    public enum ComponentSystemUpdateType
    {
        /// <summary>
        /// Performs a game logic pass.
        /// </summary>
        Logic,

        /// <summary>
        /// Performs rendering pass (also plays sounds).
        /// </summary>
        Display
    }

    /// <summary>
    /// Interface for generic systems (which implement self-contained logic).
    /// </summary>
    public interface ISystem : ICopyable<ISystem>, IPacketizable, IHashable
    {
        #region Properties

        /// <summary>
        /// The component system manager this system is part of.
        /// </summary>
        ISystemManager Manager { get; set; }

        /// <summary>
        /// Tells if this system should be packetized and sent via
        /// the network (server to client). This should only be true for logic
        /// related systems, that affect functionality that has to work exactly
        /// the same on both server and client.
        /// 
        /// <para>
        /// If the game has no network functionality, this flag is irrelevant.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Note to implementors: a state should never send its list of
        /// components, nor depend on it for its state, as that list will
        /// always be dynamically rebuilt on the client by deserializing
        /// entities.
        /// </remarks>
        bool ShouldSynchronize { get; }

        #endregion

        #region Logic

        /// <summary>
        /// Update all components in this system.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        void Update(long frame);

        /// <summary>
        /// Draw all components in this system.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        void Draw(GameTime gameTime, long frame);

        #endregion

        #region Messaging

        /// <summary>
        /// Inform a system of a message that was sent by another system.
        /// 
        /// <para>
        /// Note that systems will also receive the messages they send themselves.
        /// </para>
        /// </summary>
        /// <param name="message">The sent message.</param>
        void HandleMessage<T>(ref T message) where T : struct;

        #endregion
    }
}
