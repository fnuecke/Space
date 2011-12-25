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
