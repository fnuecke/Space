using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Space.ComponentSystem.Messages;

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
        
        public CameraCenteredSoundSystem(SoundBank soundbank, IClientSession session)
            : base(soundbank)
        {
            _session = session;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Reacts to messages to fire sounds.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public override void Receive<T>(ref T message)
        {
            if (message is WeaponFired)
            {
                var weaponMessage = (WeaponFired)(ValueType)message;
                Play(weaponMessage.Weapon.Sound, weaponMessage.Weapon.Entity);
            }
        }

        /// <summary>
        /// Returns the position of the local player's avatar.
        /// </summary>
        protected override Vector2 GetListenerPosition()
        {
            var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
            return avatar.HasValue ? Manager.GetComponent<Transform>(avatar.Value).Translation : Vector2.Zero;
        }

        /// <summary>
        /// Returns the velocity of the local player's avatar.
        /// </summary>
        protected override Vector2 GetListenerVelocity()
        {
            var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
            return avatar.HasValue ? Manager.GetComponent<Velocity>(avatar.Value).Value : Vector2.Zero;
        }

        #endregion
    }
}
