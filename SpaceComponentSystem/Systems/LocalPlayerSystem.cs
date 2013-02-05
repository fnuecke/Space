using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Session;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    ///     Utility class providing information on who the local player is in this simulation (i.e. what his ship's entity
    ///     ID is). This allows a centralized lookup, and avoids having to store the session all over the place.
    /// </summary>
    [Packetizable(false)]
    public sealed class LocalPlayerSystem : AbstractSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the entity ID of the local player's avatar. This can be an invalid value (zero) in case the player
        ///     currently does not have an avatar, or the session was closed.
        /// </summary>
        [PacketizeIgnore]
        public int LocalPlayerAvatar { get; private set; }

        #endregion

        #region Fields

        /// <summary>The session this system belongs to, for fetching the local player.</summary>
        [PacketizeIgnore]
        private readonly IClientSession _session;

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="LocalPlayerSystem"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public LocalPlayerSystem(IClientSession session)
        {
            _session = session;
        }

        #endregion

        #region Logic

        /// <summary>Keep the cached avatar ID up-to-date</summary>
        [MessageCallback]
        public void OnUpdate(Update message)
        {
            DetermineLocalPlayer();
        }

        /// <summary>Called by the manager when the complete environment has been copied or depacketized.</summary>
        [MessageCallback]
        public void OnInitialize(Initialize message)
        {
            DetermineLocalPlayer();
        }

        private void DetermineLocalPlayer()
        {
            if (_session == null || _session.ConnectionState != ClientState.Connected)
            {
                LocalPlayerAvatar = 0;
                return;
            }
            var avatars = ((AvatarSystem) Manager.GetSystem(AvatarSystem.TypeId));
            LocalPlayerAvatar = avatars.GetAvatar(_session.LocalPlayer.Number);
        }

        #endregion
    }
}