namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Interface for logic implementing systems using messages.
    /// </summary>
    public interface IMessagingSystem
    {
        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        void Receive<T>(ref T message) where T : struct;
    }
}
