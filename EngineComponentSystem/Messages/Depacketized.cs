namespace Engine.ComponentSystem.Messages
{
    /// <summary>
    /// Sent by entity managers when they were deserialized. This allows
    /// systems to perform post-deserialization actions, e.g. for clients
    /// to fill in data that only concerns presentation (and was therefore
    /// not sent by the server).
    /// </summary>
    public struct Depacketized
    {
    }
}
