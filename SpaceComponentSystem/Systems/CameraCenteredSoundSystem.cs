using System;
using System.Collections.Generic;
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

        /// <summary>
        /// All Currently playing sounds mapped to the entry id
        /// </summary>
        private Dictionary<int, Cue> _playingSounds;

        #endregion

        #region Constants


        /// <summary>
        /// The Maximum Distance from which the sound shall be heard
        /// </summary>
        private const float Maxsounddistance = 5000;

        #endregion
        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components.
        /// </summary>
        private HashSet<int> _reusableNeighborList = new HashSet<int>();

        #endregion
        #region Constructor

        public CameraCenteredSoundSystem(SoundBank soundbank, IClientSession session)
            : base(soundbank)
        {
            _session = session;
            _playingSounds = new Dictionary<int, Cue>();
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
                Play(weaponMessage.Weapon.Sound, weaponMessage.ShipEntity);
            }
        }

        /// <summary>
        /// Returns the position of the local player's avatar.
        /// </summary>
        protected override Vector2 GetListenerPosition()
        {
            if (_session.ConnectionState == ClientState.Connected)
            {
                var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
                if (avatar.HasValue)
                {
                    return Manager.GetComponent<Transform>(avatar.Value).Translation;
                }
            }
            return Vector2.Zero;
        }

        /// <summary>
        /// Returns the velocity of the local player's avatar.
        /// </summary>
        protected override Vector2 GetListenerVelocity()
        {
            if (_session.ConnectionState == ClientState.Connected)
            {
                var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
                if (avatar.HasValue)
                {
                    return Manager.GetComponent<Velocity>(avatar.Value).Value;
                }
            }
            return Vector2.Zero;
        }

        /// <summary>
        /// Check for sound in range and play
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="frame"></param>
        public override void Update(GameTime gameTime, long frame)
        {
            if (!_isDrawingInstance)
                return;

            var position = GetListenerPosition();
            var index = Manager.GetSystem<IndexSystem>();
            if (index == null)
            {
                return;
            }
            _listener.Position = ToV3(ref position);
            var tmp = GetListenerVelocity();
            _listener.Velocity = ToV3(ref tmp);
            var newDict = new Dictionary<int, Cue>();
            foreach (var neigbor in index.
                    RangeQuery(ref position, Maxsounddistance, Sound.IndexGroup, _reusableNeighborList))
            {
                var neigborTransform = Manager.GetComponent<Transform>(neigbor);
                var neigborPosition = neigborTransform.Translation;
                var neigborVelocity = Manager.GetComponent<Velocity>(neigbor);
                var nvel = neigborVelocity != null ? neigborVelocity.Value : Vector2.Zero;
                if (_playingSounds.ContainsKey(neigbor))//we already know this one so just apply 3d effect
                {
                    var cue = _playingSounds[neigbor];

                    if (cue != null && !cue.IsStopped)//make sure cue is not stoped (how ever that may have happened...)
                    {
                        _playingSounds.Remove(neigbor);
                        // Get position and velocity of emitter.
                        _emitter.Position = ToV3(ref neigborPosition);
                        _emitter.Velocity = ToV3(ref nvel);
                        cue.Apply3D(_listener, _emitter);//apply new 3d effect
                        newDict.Add(neigbor, cue);
                    }
                }
                else
                {
                    var neigborSound = Manager.GetComponent<Sound>(neigbor);
                    //var cue = _soundBank.GetCue(neigborSound.SoundName);
                    var cue = Play(neigborSound.SoundName, ref neigborPosition, ref nvel);
                    //cue.Play();
                    newDict.Add(neigbor, cue);
                }


            }
            foreach (var cue in _playingSounds)//stop all sound thats not in range
            {
                cue.Value.Stop(AudioStopOptions.Immediate);
            }
            _playingSounds = newDict;
        }
        #endregion
    }
}
