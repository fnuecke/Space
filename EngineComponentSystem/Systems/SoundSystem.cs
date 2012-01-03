using System.Collections.Generic;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// System that manages sound components, querying them for cue names to play
    /// in a single update.
    /// </summary>
    public class SoundSystem : AbstractComponentSystem<SoundParameterization, SoundParameterization>
    {
        #region Constants

        /// <summary>
        /// Number of frames to wait before disallowing a sound to be played.
        /// </summary>
        private const int GracePeriod = 60;

        /// <summary>
        /// Number of frames to check into the past if a sound has been played.
        /// </summary>
        private const int DeadPeriod = 15;

        /// <summary>
        /// Epsilon range (norm, not distance) in which sound events of the
        /// same cue type are considered equal (avoid multi-play due to late
        /// command altering position or velocity slightly).
        /// </summary>
        private static readonly float Epsilon = 4;

        #endregion

        #region Fields

        /// <summary>
        /// The sound bank we use to get actual sounds for our cue names.
        /// </summary>
        private SoundBank _soundBank;

        /// <summary>
        /// The sound listener to use for relative position.
        /// </summary>
        private AudioListener _listener = new AudioListener();

        /// <summary>
        /// The sound emitter to use for emitted sound positioning.
        /// </summary>
        private AudioEmitter _emitter = new AudioEmitter();

        /// <summary>
        /// Reused parameterization.
        /// </summary>
        private SoundParameterization _parameterization = new SoundParameterization();

        /// <summary>
        /// A list of sounds not to play again int their respective time
        /// frames. This list is kept in addition to the sounds to play one,
        /// to avoid replaying sounds, e.g. on TSS rollbacks.
        /// </summary>
        private Dictionary<long, List<SoundEvent>> _recentlyPlayed =
            new Dictionary<long, List<SoundEvent>>();

        /// <summary>
        /// The newest frame in which we were asked to played a sound. As an
        /// array as that's the simplest way to make it a reference type :P
        /// </summary>
        private long[] _lastFrame = new long[] { 0 };

        /// <summary>
        /// A list of sounds to play in the next "Display" update. It's
        /// necessary to keep the frames here, too, to prune old sounds
        /// from states that might not be run through the display update
        /// regularly (TSS trailing states).
        /// </summary>
        private Dictionary<long, List<SoundEvent>> _soundsToPlay =
            new Dictionary<long, List<SoundEvent>>();

        #endregion

        #region Constructor

        public SoundSystem(SoundBank soundBank)
        {
            this._soundBank = soundBank;

        }

        #endregion

        #region Logic

        public override void Update(long frame)
        {
            if (SetLastFrame(frame)) { return; }

            // Get a list of sounds that should be played this frame.
            foreach (var component in UpdateableComponents)
            {
                _parameterization.SoundCues.Clear();
                _parameterization.Position = Vector2.Zero;
                _parameterization.Velocity = Vector2.Zero;
                // Get infos for this component.
                if (component.Enabled)
                {
                    component.Update(_parameterization);
                }
                foreach (var soundCue in _parameterization.SoundCues)
                {
                    // Enqueue play a sound.
                    Enqueue(new SoundEvent(soundCue, _parameterization), frame);
                }
            }
        }

        public override void Draw(long frame)
        {
            if (SetLastFrame(frame)) { return; }

            // Get position and velocity of listener. We might be playing
            // some events from the past, where we were somewhere else, but
            // using that old position would be just as wrong, so this
            // wrong is simpler ;)
            _listener.Position = ToV3(GetListenerPosition());
            _listener.Velocity = ToV3(GetListenerVelocity());

            // Actually play the sounds that should be this update.
            foreach (var sounds in _soundsToPlay.Values)
                foreach (var sound in sounds)
                {
                    // Get position and velocity of emitter.
                    _emitter.Position = ToV3(sound.Position);
                    _emitter.Velocity = ToV3(sound.Velocity);

                    // Let there be sound!
                    _soundBank.PlayCue(sound.SoundCue, _listener, _emitter);
                }

            // Remove all sound from the list, we played them.
            _soundsToPlay.Clear();
        }

        private bool SetLastFrame(long frame)
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

            // Don't play sounds before our grace period.
            return frame < eventHorizon;
        }

        private static Vector3 ToV3(Vector2 v2)
        {
            Vector3 result;
            result.X = v2.X;
            result.Y = 0;
            result.Z = v2.Y;
            return result;
        }

        /// <summary>
        /// Get the position of the listener (e.g. player avatar).
        /// </summary>
        /// <returns>The position of the listener.</returns>
        protected virtual Vector2 GetListenerPosition()
        {
            return Vector2.Zero;
        }

        /// <summary>
        /// Get the velocity of the listener (e.g. player avatar).
        /// </summary>
        /// <returns>The velocity of the listener.</returns>
        protected virtual Vector2 GetListenerVelocity()
        {
            return Vector2.Zero;
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Stores a sound as "to play", if it hasn't been played before.
        /// </summary>
        /// <param name="p">The sound event information.</param>
        /// <param name="frame">The frame in which the sound was triggered.</param>
        private void Enqueue(SoundEvent p, long frame)
        {
            // Check if we may play the sound, or if it already was, in some
            // other update.
            if (WasRecentlyPlayed(p, frame))
            {
                // Yes, skip it.
                return;
            }
            else if (!_recentlyPlayed.ContainsKey(frame))
            {
                // No entries for that frame yet, allocate for this one.
                _recentlyPlayed.Add(frame, new List<SoundEvent>());
            }

            // Allocate list for that frame.
            if (!_soundsToPlay.ContainsKey(frame))
            {
                _soundsToPlay.Add(frame, new List<SoundEvent>());
            }

            _soundsToPlay[frame].Add(p);
            _recentlyPlayed[frame].Add(p);
        }

        /// <summary>
        /// Test if a certain sound was in the recent past.
        /// </summary>
        /// <param name="e">The sound information to check.</param>
        /// <param name="frame">The frame the sound would play in.</param>
        /// <returns>Whether the sound has been recently played or not.</returns>
        private bool WasRecentlyPlayed(SoundEvent e, long frame)
        {
            for (long testFrame = frame - DeadPeriod; testFrame <= frame; ++testFrame)
            {
                if (_recentlyPlayed.ContainsKey(testFrame))
                {
                    // Got a list for this frame, check if the same sound was
                    // played at the same position already.
                    if (ContainsWithPosition(_recentlyPlayed[testFrame], e))
                    {
                        // Yes, skip it.
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a sound event is in the given lists, tested by sound cue
        /// name, emitter position and emitter velocity.
        /// </summary>
        /// <param name="list">The list to look in.</param>
        /// <param name="e">The parameterization with the event info.</param>
        /// <returns>Whether such an event was already played.</returns>
        private bool ContainsWithPosition(ICollection<SoundEvent> list, SoundEvent e)
        {
            foreach (var item in list)
            {
                if (item.SoundCue.Equals(e.SoundCue) &&
                    (item.Position == e.Position ||
                    System.Math.Abs((item.Position - e.Position).LengthSquared()) < Epsilon) &&
                    (item.Velocity == e.Velocity ||
                    System.Math.Abs((item.Velocity - e.Velocity).LengthSquared()) < Epsilon))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Utility class

        private class SoundEvent
        {
            public string SoundCue { get; set; }
            public Vector2 Position { get; set; }
            public Vector2 Velocity { get; set; }
            public SoundEvent(string soundCue, SoundParameterization p)
            {
                this.SoundCue = soundCue;
                this.Position = p.Position;
                this.Velocity = p.Velocity;
            }
        }

        #endregion
    }
}
