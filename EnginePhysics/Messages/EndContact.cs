using Engine.Physics.Systems;

namespace Engine.Physics.Messages
{
    /// <summary>
    /// Used to indicate a collision ended.
    /// </summary>
    public struct EndContact
    {
        /// <summary>
        /// The contact has has become inactive.
        /// </summary>
        public PhysicsSystem.IContact Contact;
    }
}
