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
        #region Constants

        /// <summary>
        /// The maximum distance from which sounds can be heard.
        /// </summary>
        private const float MaxSoundDistance = 5000;

        #endregion

        #region Fields

        /// <summary>
        /// The session this system belongs to, for fetching the local player.
        /// </summary>
        private readonly IClientSession _session;

        /// <summary>
        /// All Currently playing sounds mapped to the entry id
        /// </summary>
        private Dictionary<int, Cue> _playingSounds = new Dictionary<int, Cue>();

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components. As its only used by the drawing
        /// instance we don't need to clone it, so it can be readonly.
        /// </summary>
        private readonly HashSet<int> _reusableNeighborList = new HashSet<int>();

        /// <summary>
        /// Used to swap between this dict and the one assigned to _playingSounds
        /// to avoid reallocating each update.
        /// </summary>
        private Dictionary<int, Cue> _reusablePlayingSounds = new Dictionary<int, Cue>();

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
        /// Check for sound in range and play.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Update(GameTime gameTime, long frame)
        {
            if (!IsDrawingInstance)
            {
                return;
            }

            var index = Manager.GetSystem<IndexSystem>();
            if (index == null)
            {
                return;
            }

            // Update listener information.
            var position = GetListenerPosition();
            Listener.Position = ToV3(ref position);
            var velocity = GetListenerVelocity();
            Listener.Velocity = ToV3(ref velocity);

            // Iterate all sounds in range of the listener. All sounds remaining
            // in the current list of sounds playing will be stopped, as they are
            // out of range. The ones in range will be removed from that list and
            // added to our reusable list.
            ICollection<int> neighbors = _reusableNeighborList;
            index.Find(position, MaxSoundDistance, ref neighbors, Sound.IndexGroup);
            foreach (var neighbor in neighbors)
            {
                // Get the sound component of the neighbor.
                var sound = Manager.GetComponent<Sound>(neighbor);

                // Skip this neighbor if its sound is not enabled.
                if (!sound.Enabled)
                {
                    continue;
                }

                // Get sound position and velocity.
                var emitterPosition = Manager.GetComponent<Transform>(neighbor).Translation;
                // The velocity is optional, so we must check if it exists.
                var neighborVelocity = Manager.GetComponent<Velocity>(neighbor);
                var emitterVelocity = neighborVelocity != null ? neighborVelocity.Value : Vector2.Zero;
               
                // Check whether to update or start playing.
                if (_playingSounds.ContainsKey(neighbor))
                {
                    // We already know this one so just apply 3d effect.
                    var cue = _playingSounds[neighbor];

                    // Make sure cue is not stopped (how ever that may have happened...)
                    if (!cue.IsStopped)
                    {
                        // Do not stop it.
                        _playingSounds.Remove(neighbor);

                        // Get position and velocity of emitter.
                        Emitter.Position = ToV3(ref emitterPosition);
                        Emitter.Velocity = ToV3(ref emitterVelocity);

                        // Apply new surround effect.
                        cue.Apply3D(Listener, Emitter);

                        // Add it to the new list of playing sounds.
                        _reusablePlayingSounds.Add(neighbor, cue);
                    }
                    else
                    {
                        // Dispose it. It will be restarted in the next update,
                        // if still in range.
                        cue.Dispose();
                        // Don't dispose it again.
                        _playingSounds.Remove(neighbor);
                    }
                }
                else
                {
                    // Sound is not yet playing, start it.
                    var cue = Play(sound.SoundName, ref emitterPosition, ref emitterVelocity);
                    if (cue != null)
                    {
                        _reusablePlayingSounds.Add(neighbor, cue);
                    }
                }
            }

            // Clear for next update.
            _reusableNeighborList.Clear();

            // Stop all sound thats not in range.
            foreach (var cue in _playingSounds)
            {
                cue.Value.Stop(AudioStopOptions.Immediate);
                cue.Value.Dispose();
            }
            _playingSounds.Clear();

            // Swap the two sound dictionaries.
            var tmp = _reusablePlayingSounds;
            _reusablePlayingSounds = _playingSounds;
            _playingSounds = tmp;
        }

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

        #endregion
    }
}
