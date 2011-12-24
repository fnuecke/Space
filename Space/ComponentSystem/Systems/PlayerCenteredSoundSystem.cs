using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Math;
using Engine.Session;
using Microsoft.Xna.Framework.Audio;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Defines a sound system which uses the local player's avatar to
    /// determine the listener position.
    /// </summary>
    class PlayerCenteredSoundSystem : SoundSystem
    {
        #region Fields

        /// <summary>
        /// The session this system belongs to, for fetching the local player.
        /// </summary>
        IClientSession _session;

        #endregion

        #region Constructor
        
        public PlayerCenteredSoundSystem(SoundBank soundbank, IClientSession session)
            : base(soundbank)
        {
            this._session = session;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Returns the position of the local player's avatar.
        /// </summary>
        protected override FPoint GetListenerPosition()
        {
            var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
            if (avatar != null)
            {
                return avatar.GetComponent<Transform>().Translation;
            }
            return FPoint.Zero;
        }

        /// <summary>
        /// Returns the velocity of the local player's avatar.
        /// </summary>
        protected override FPoint GetListenerVelocity()
        {
            var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
            if (avatar != null)
            {
                return avatar.GetComponent<Velocity>().Value;
            }
            return FPoint.Zero;
        }

        #endregion
    }
}
