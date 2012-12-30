using Engine.Physics.Systems;

namespace Engine.Physics.Messages
{
    /// <summary>
    /// Used to indicate a collision occurred.
    /// </summary>
    public struct BeginContact
    {
        /// <summary>
        /// The contact has has become active.
        /// </summary>
        public PhysicsSystem.IContact Contact;
    }
}
