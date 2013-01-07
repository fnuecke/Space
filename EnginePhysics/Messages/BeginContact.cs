using Engine.Physics.Contacts;

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
        public Contact Contact;
    }
}
