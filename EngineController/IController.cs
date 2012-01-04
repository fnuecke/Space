using System;
using Engine.Session;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Defines public functionality of a game controller.
    /// </summary>
    public interface IController<TSession> : IDisposable
        where TSession : ISession
    {
        /// <summary>
        /// The underlying session being used by this controller.
        /// </summary>
        TSession Session { get; }

        /// <summary>
        /// The actual current game speed, which may be influenced by clients
        /// not being able to keep up with the computations needed. This will
        /// be at maximum 1, meaning real-time.
        /// </summary>
        double CurrentSpeed { get; }

        /// <summary>
        /// Called when the controller needs to be updated.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Called when the controller needs to be rendered.
        /// </summary>
        void Draw();
    }
}
