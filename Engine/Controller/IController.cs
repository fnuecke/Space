using Engine.Session;

namespace Engine.Controller
{
    /// <summary>
    /// Defines public functionality of a game controller.
    /// </summary>
    public interface IController<TSession>
        where TSession : ISession
    {
        /// <summary>
        /// The underlying session being used by this controller.
        /// </summary>
        TSession Session { get; }
    }
}
