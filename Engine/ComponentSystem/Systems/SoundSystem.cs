using System.Collections.Generic;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// System that manages sound components, querying them for cue names to play
    /// in a single update.
    /// </summary>
    public class SoundSystem : AbstractComponentSystem<SoundParameterization>
    {
        #region Constants

        /// <summary>
        /// Number of frames to wait before disallowing a sound to be played.
        /// </summary>
        private const int GracePeriod = 20;

        #endregion

        #region Fields

        /// <summary>
        /// The sound bank we use to get actual sounds for our cue names. This
        /// will be the same across all copies / clones.
        /// </summary>
        private SoundBank _soundBank;

        /// <summary>
        /// The sound listener to use for relative position. This will be the
        /// same across all copies / clones.
        /// </summary>
        private AudioListener _listener = new AudioListener();

        /// <summary>
        /// The sound emitter to use for emitted sound positioning. This will
        /// be the same across all copies / clones.
        /// </summary>
        private AudioEmitter _emitter = new AudioEmitter();

        /// <summary>
        /// A list of sounds not to play again int their respective time
        /// frames. This list is kept in addition to the sounds to play one,
        /// to avoid replaying sounds, e.g. on TSS rollbacks. This will be the
        /// same across all copies / clones.
        /// </summary>
        private Dictionary<long, List<SoundParameterization>> _recentlyPlayed =
            new Dictionary<long, List<SoundParameterization>>();

        /// <summary>
        /// The newest frame in which we were asked to played a sound. As an
        /// array as that's the simplest way to make it a reference type :P
        /// This will be the same across all copies / clones.
        /// </summary>
        private long[] _lastFrame = new long[] { 0 };

        /// <summary>
        /// A list of sounds to play in the next "Display" update. It's
        /// necessary to keep the frames here, too, to prune old sounds
        /// from states that might not be run through the display update
        /// regularly (TSS trailing states).
        /// </summary>
        private Dictionary<long, List<SoundParameterization>> _soundsToPlay =
            new Dictionary<long, List<SoundParameterization>>();

        #endregion

        #region Constructor

        public SoundSystem(SoundBank soundBank)
        {
            _soundBank = soundBank;
        }

        #endregion

        #region Logic

        public override void Update(ComponentSystemUpdateType updateType, long frame)
        {
            // Get new current frame.
            _lastFrame[0] = System.Math.Max(frame, _lastFrame[0]);

            // Remove sound events that are too old, and clean out the
            // "recently played" list.
            var eventHorizon = _lastFrame[0] - GracePeriod;
            foreach (var key in new List<long>(_soundsToPlay.Keys))
            {
                if (key < eventHorizon)
                {
                    _soundsToPlay.Remove(key);
                }
            }
            foreach (var key in new List<long>(_recentlyPlayed.Keys))
            {
                if (key < eventHorizon)
                {
                    _recentlyPlayed.Remove(key);
                }
            }

            if (updateType == ComponentSystemUpdateType.Logic)
            {
                // Don't play sounds before our grace period.
                if (frame < eventHorizon)
                {
                    return;
                }

                // Get a list of sounds that should be played this frame.
                foreach (var component in Components)
                {
                    // Get infos for this component.
                    var parameterization = new SoundParameterization();
                    component.Update(parameterization);
                    if (!string.IsNullOrEmpty(parameterization.SoundCueToPlay))
                    {
                        // Wants to play a sound.
                        Enqueue(parameterization, frame);
                    }
                }
            }
            else if (updateType == ComponentSystemUpdateType.Display)
            {
                // Get position and velocity of listener. We might be playing
                // some events from the past, where we were somewhere else, but
                // using that old position would be just as wrong, so this
                // wrong is simpler ;)
                _listener.Position = (Vector3)GetListenerPosition();
                _listener.Velocity = (Vector3)GetListenerVelocity();

                // Actually play the sounds that should be this update.
                foreach (var sounds in _soundsToPlay.Values)
                foreach (var sound in sounds)
                {
                    // Get position and velocity of emitter.
                    _emitter.Position = (Vector3)sound.Position;
                    _emitter.Velocity = (Vector3)sound.Velocity;

                    // Let there be sound!
                    _soundBank.PlayCue(sound.SoundCueToPlay, _listener, _emitter);
                }

                // Remove all sound from the list, we played them.
                _soundsToPlay.Clear();
            }
        }

        /// <summary>
        /// Get the position of the listener (e.g. player avatar).
        /// </summary>
        /// <returns>The position of the listener.</returns>
        protected virtual FPoint GetListenerPosition()
        {
            return FPoint.Zero;
        }

        /// <summary>
        /// Get the velocity of the listener (e.g. player avatar).
        /// </summary>
        /// <returns>The velocity of the listener.</returns>
        protected virtual FPoint GetListenerVelocity()
        {
            return FPoint.Zero;
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Stores a sound as "to play", if it hasn't been played before.
        /// </summary>
        /// <param name="p">The sound event information.</param>
        /// <param name="frame">The frame in which the sound was triggered.</param>
        private void Enqueue(SoundParameterization p, long frame)
        {
            // Check if we may play the sound, or if it already was, in some
            // other update.
            if (_recentlyPlayed.ContainsKey(frame))
            {
                // Got a list for this frame, check if the same sound was
                // played at the same position already.
                if (ContainsWithPosition(_recentlyPlayed[frame], p))
                {
                    // Yes, skip it.
                    return;
                }
            }
            else
            {
                // No entries for that frame yet, allocate for this one.
                _recentlyPlayed.Add(frame, new List<SoundParameterization>());
            }

            // Allocate list for that frame.
            if (!_soundsToPlay.ContainsKey(frame))
            {
                _soundsToPlay.Add(frame, new List<SoundParameterization>());
            }

            _soundsToPlay[frame].Add(p);
            _recentlyPlayed[frame].Add(p);
        }

        /// <summary>
        /// Checks if a sound event is in the given lists, tested by sound cue
        /// name, emitter position and emitter velocity.
        /// </summary>
        /// <param name="list">The list to look in.</param>
        /// <param name="p">The parameterization with the event info.</param>
        /// <returns>Whether such an event was already played.</returns>
        private bool ContainsWithPosition(ICollection<SoundParameterization> list, SoundParameterization p)
        {
            foreach (var item in list)
            {
                if (item.SoundCueToPlay.Equals(p.SoundCueToPlay) &&
                    item.Position == p.Position &&
                    item.Velocity == p.Velocity)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Cloning

        public override object Clone()
        {
            var copy = (SoundSystem)base.Clone();

            // Get own list of sounds to play.
            copy._soundsToPlay = new Dictionary<long, List<SoundParameterization>>();
            foreach (var entry in _soundsToPlay)
            {
                copy._soundsToPlay.Add(entry.Key, new List<SoundParameterization>(entry.Value));
            }
            
            // Note: keep the SAME list of recently played sounds across all
            // states, to avoid forgetting about played sounds when rolling
            // back in TSS.
            
            return copy;
        }

        #endregion
    }
}
