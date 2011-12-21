using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Systems;
using Engine.Math;
using Microsoft.Xna.Framework.Audio;
using Engine.Session;
using Engine.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    class PlayerCenteredSoundSystem : SoundSystem
    {
        #region Fields
        IClientSession _session;
        #endregion
        public PlayerCenteredSoundSystem(SoundBank soundbank, IClientSession session)
            :base(soundbank)
        {
            _session = session;
        }
        protected override FPoint GetListenerPosition()
        {
            var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
            if (avatar != null)
            {
                return avatar.GetComponent<Transform>().Translation;
            }
            return FPoint.Zero;
        }

        protected override FPoint GetListenerVelocity()
        {
            var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
            if (avatar != null)
            {
                return avatar.GetComponent<Velocity>().Value;
            }
            return FPoint.Zero;
        }
    }
}
