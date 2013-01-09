using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Defines a sound system which uses the local player's avatar to determine the listener position.</summary>
    public sealed class CameraCenteredSoundSystem : SoundSystem
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="CameraCenteredSoundSystem"/> class.
        /// </summary>
        /// <param name="soundbank">The soundbank.</param>
        /// <param name="maxAudibleDistance">The maximum distance at which sound is heard.</param>
        public CameraCenteredSoundSystem(SoundBank soundbank, float maxAudibleDistance)
            : base(soundbank, maxAudibleDistance) {}

        #endregion

        #region Logic

        /// <summary>Returns the position of the local player's avatar.</summary>
        protected override FarPosition GetListenerPosition()
        {
            //var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);
            //camera.Transform.Translation;
            var avatar = ((LocalPlayerSystem) Manager.GetSystem(LocalPlayerSystem.TypeId)).LocalPlayerAvatar;
            return avatar > 0
                       ? ((Transform) Manager.GetComponent(avatar, Transform.TypeId)).Translation
                       : FarPosition.Zero;
        }

        /// <summary>Returns the velocity of the local player's avatar.</summary>
        protected override Vector2 GetListenerVelocity()
        {
            var avatar = ((LocalPlayerSystem) Manager.GetSystem(LocalPlayerSystem.TypeId)).LocalPlayerAvatar;
            return avatar > 0
                       ? ((Velocity) Manager.GetComponent(avatar, Velocity.TypeId)).Value
                       : Vector2.Zero;
        }

        #endregion
    }
}