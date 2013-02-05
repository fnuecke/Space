namespace Engine.ComponentSystem.Messages
{
    /// <summary>
    ///     Sent by the <see cref="Manager"/> to notify systems that they should re-initialize after the manager was copied or
    ///     deserialized.
    /// </summary>
    public struct Initialize {}
}