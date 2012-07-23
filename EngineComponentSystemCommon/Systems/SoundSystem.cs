using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// System that manages sound components, querying them for cue names to play
    /// in a single update.
    /// </summary>
    public class SoundSystem : AbstractSystem
    {
        #region Constants

        /// <summary>
        /// Index group to use for sound computations.
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        /// <summary>
        /// The maximum distance from which sounds can be heard.
        /// </summary>
        private const float MaxSoundDistance = 5000;

        #endregion

        #region Fields

        /// <summary>
        /// The sound bank we use to get actual sounds for our cue names.
        /// </summary>
        private readonly SoundBank _soundBank;

        /// <summary>
        /// The sound listener to use for relative position.
        /// </summary>
        private readonly AudioListener _listener = new AudioListener();

        /// <summary>
        /// The sound emitter to use for emitted sound positioning.
        /// </summary>
        private readonly AudioEmitter _emitter = new AudioEmitter();

        /// <summary>
        /// Whether this is the sound system thats doing the actual "rendering"
        /// for the simulation the component system belongs to, i.e. whether
        /// Draw is called for this instance. Only that system may actually
        /// play sounds.
        /// </summary>
        private bool _isDrawingInstance;

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
        private readonly List<int> _reusableNeighborList = new List<int>();

        /// <summary>
        /// Used to swap between this dict and the one assigned to _playingSounds
        /// to avoid reallocating each update.
        /// </summary>
        private Dictionary<int, Cue> _reusablePlayingSounds = new Dictionary<int, Cue>();

        #endregion

        #region Constructor

        public SoundSystem(SoundBank soundBank)
        {
            _soundBank = soundBank;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Check for sound in range and play.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Update(long frame)
        {
            if (!_isDrawingInstance)
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
            _listener.Position = ToV3(ref position);
            var velocity = GetListenerVelocity();
            _listener.Velocity = ToV3(ref velocity);

            // Iterate all sounds in range of the listener. All sounds remaining
            // in the current list of sounds playing will be stopped, as they are
            // out of range. The ones in range will be removed from that list and
            // added to our reusable list.
            ICollection<int> neighbors = _reusableNeighborList;
            index.Find(position, MaxSoundDistance, ref neighbors, IndexGroupMask);
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
                        _emitter.Position = ToV3(ref emitterPosition);
                        _emitter.Velocity = ToV3(ref emitterVelocity);

                        // Apply new surround effect.
                        cue.Apply3D(_listener, _emitter);

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
        /// Flags our system as the presenting instance.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        public sealed override void Draw(long frame)
        {
            _isDrawingInstance = true;
        }

        #endregion

        #region Playback

        /// <summary>
        /// Plays a sound cue with the specified name at the specified location
        /// and with the specified emitter velocity.
        /// </summary>
        /// <param name="soundCue">The name of the sound cue to play.</param>
        /// <param name="position">The position at which to emit the sound.</param>
        /// <param name="velocity">The velocity of the sound's emitter.</param>
        public Cue Play(string soundCue, ref Vector2 position, ref Vector2 velocity)
        {
            // Do not play sounds if this isn't the main sound system thats
            // used for the presentation.
            if (!_isDrawingInstance || _soundBank == null)
            {
                return null;
            }

            // Get position and velocity of listener. We might be playing
            // some events from the past, where we were somewhere else, but
            // using that old position would be just as wrong, so this
            // wrong is simpler ;)
            var listenerPosition = GetListenerPosition();
            var listenerVelocity = GetListenerVelocity();

            // Skip if too far away.
            if (Vector2.Distance(listenerPosition, position) > 5000.0f)
            {
                return null;
            }

            _listener.Position = ToV3(ref listenerPosition);
            _listener.Velocity = ToV3(ref listenerVelocity);

            // Get position and velocity of emitter.
            _emitter.Position = ToV3(ref position);
            _emitter.Velocity = ToV3(ref velocity);

            // Let there be sound!
            var cue = _soundBank.GetCue(soundCue);
            cue.Apply3D(_listener, _emitter);
            cue.Play();

            // Return cue for further usage.
            return cue;
        }

        /// <summary>
        /// Plays a sound cue with the specified name at the specified location.
        /// </summary>
        /// <param name="soundCue">The name of the sound cue to play.</param>
        /// <param name="position">The position at which to emit the sound.</param>
        public Cue Play(string soundCue, ref Vector2 position)
        {
            var zero = Vector2.Zero;
            return Play(soundCue, ref  position, ref zero);
        }

        /// <summary>
        /// Plays a sound cue with the specified name as if it were emitted by
        /// the specified entity.
        /// </summary>
        /// <remarks>
        /// The entity must have a <c>Transform</c> component. A <c>Velocity</c>
        /// component is optional.
        /// </remarks>
        /// <param name="soundCue">The name of the sound cue to play.</param>
        /// <param name="entity">The entity that emits the sound.</param>
        public Cue Play(string soundCue, int entity)
        {
            var position = Manager.GetComponent<Transform>(entity).Translation;
            var velocity = Vector2.Zero;
            var velocityComponent = Manager.GetComponent<Velocity>(entity);
            if (velocityComponent != null)
            {
                velocity = velocityComponent.Value;
            }
            return Play(soundCue, ref position, ref velocity);
        }

        #endregion

        #region Overrides for positional sound systems

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
        /// Converts a 2D vector to a XACT compatible 3D vector.
        /// </summary>
        /// <param name="v2">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        private static Vector3 ToV3(ref Vector2 v2)
        {
            Vector3 result;
            result.X = v2.X;
            result.Y = 0;
            result.Z = v2.Y;
            return result;
        }

        #endregion

        #region Copying

        /// <summary>
        /// Servers as a copy constructor that returns a new instance of the same
        /// type that is freshly initialized.
        /// 
        /// <para>
        /// This takes care of duplicating reference types to a new copy of that
        /// type (e.g. collections).
        /// </para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (SoundSystem)base.NewInstance();

            // Mark as secondary system.
            copy._isDrawingInstance = false;

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// 
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <remarks>The manager for the system to copy into must be set to the
        /// manager into which the system is being copied.</remarks>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (SoundSystem)into;

            // Mark as secondary system.
            copy._isDrawingInstance = false;
        }

        #endregion
    }
}
