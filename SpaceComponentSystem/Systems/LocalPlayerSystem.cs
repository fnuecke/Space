using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Session;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Utility class providing information on who the local player is
    /// in this simulation (i.e. what his ship's entity ID is). This
    /// allows a centralized lookup, and avoids having to store the
    /// session all over the place.
    /// </summary>
    public sealed class LocalPlayerSystem : AbstractSystem, IDrawingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets the entity ID of the local player's avatar. This can be
        /// an invalid value (zero) in case the player currently does not
        /// have an avatar, or the session was closed.
        /// </summary>
        [PacketizerIgnore]
        public int LocalPlayerAvatar { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// The session this system belongs to, for fetching the local player.
        /// </summary>
        [PacketizerIgnore]
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
        /// Draws the system.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
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

            Draw(0, 0);
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

            Draw(0, 0);
        }

        #endregion
    }
}
