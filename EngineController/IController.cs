using System;
using Engine.Session;

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
        float CurrentLoad { get; }

        /// <summary>
        /// Called when the controller needs to be updated.
        /// </summary>
        void Update();

        /// <summary>
        /// Called when the controller needs to be rendered.
        /// </summary>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        void Draw(float elapsedMilliseconds);
    }
}
