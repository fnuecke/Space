using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Session;

namespace Space.ComponentSystem.Systems

{
    /// <summary>
    /// Utility class providing information on who the local player is
    /// in this simulation (i.e. what his ship's entity ID is). This
    /// allows a centralized lookup, and avoids having to store the
    /// session all over the place.
    /// </summary>
    public sealed class LocalPlayerSystem : AbstractSystem, IUpdatingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the entity ID of the local player's avatar. This can be
        /// an invalid value (zero) in case the player currently does not
        /// have an avatar, or the session was closed.
        /// </summary>
        public int LocalPlayerAvatar { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// The session this system belongs to, for fetching the local player.
        /// </summary>
        private readonly IClientSession _session;

        #endregion
        
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalPlayerSystem"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public LocalPlayerSystem(IClientSession session)
        {
            _session = session;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the system.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Update(long frame)
        {
            // Keep the cached avatar ID up-to-date.
            if (_session == null || _session.ConnectionState != ClientState.Connected)
            {
                LocalPlayerAvatar = 0;
                return;
            }
            LocalPlayerAvatar = ((AvatarSystem)Manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(_session.LocalPlayer.Number);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Called by the manager when the complete environment has been
        /// copied from another manager.
        /// </summary>
        public override void OnCopied()
        {
            base.OnCopied();

            Update(0);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Called by the manager when the complete environment has been
        /// depacketized.
        /// </summary>
        public override void OnDepacketized()
        {
            base.OnDepacketized();

            Update(0);
        }

        #endregion
    }
}
