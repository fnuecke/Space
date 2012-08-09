using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.FarMath;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Defines a sound system which uses the local player's avatar to
    /// determine the listener position.
    /// </summary>
    public sealed class CameraCenteredSoundSystem : SoundSystem
    {
        #region Fields

        /// <summary>
        /// The session this system belongs to, for fetching the local player.
        /// </summary>
        private readonly IClientSession _session;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraCenteredSoundSystem"/> class.
        /// </summary>
        /// <param name="soundbank">The soundbank.</param>
        /// <param name="maxAudibleDistance">The maximum distance at which sound is heard.</param>
        /// <param name="session">The session.</param>
        public CameraCenteredSoundSystem(SoundBank soundbank, float maxAudibleDistance, IClientSession session)
            : base(soundbank, maxAudibleDistance)
        {
            _session = session;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Returns the position of the local player's avatar.
        /// </summary>
        protected override FarPosition GetListenerPosition()
        {
            //var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);
            //camera.Transform.Translation;
            if (_session.ConnectionState == ClientState.Connected)
            {
                var avatar = ((AvatarSystem)Manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(_session.LocalPlayer.Number);
                if (avatar.HasValue)
                {
                    return ((Transform)Manager.GetComponent(avatar.Value, Transform.TypeId)).Translation;
                }
            }
            return FarPosition.Zero;
        }

        /// <summary>
        /// Returns the velocity of the local player's avatar.
        /// </summary>
        protected override Vector2 GetListenerVelocity()
        {
            if (_session.ConnectionState == ClientState.Connected)
            {
                var avatar = ((AvatarSystem)Manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(_session.LocalPlayer.Number);
                if (avatar.HasValue)
                {
                    return ((Velocity)Manager.GetComponent(avatar.Value, Velocity.TypeId)).Value;
                }
            }
            return Vector2.Zero;
        }

        #endregion
    }
}
