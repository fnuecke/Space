using System;
using Engine.Session;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Defines public functionality of a game controller.
    /// </summary>
    public interface IController<out TSession> : IDisposable
        where TSession : ISession
    {
        /// <summary>
        /// The underlying session being used by this controller.
        /// </summary>
        TSession Session { get; }

        /// <summary>
        /// The current 'load', i.e. how much of the available time is actually
        /// needed to perform an update.
        /// </summary>
        double CurrentLoad { get; }

        /// <summary>
        /// Called when the controller needs to be updated.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Called when the controller needs to be rendered.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        void Draw(GameTime gameTime);
    }
}
