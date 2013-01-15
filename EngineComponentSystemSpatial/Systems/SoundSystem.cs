using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Spatial.Systems
{
    /// <summary>System that manages sound components, querying them for cue names to play in a single update.</summary>
    public abstract class SoundSystem : AbstractSystem, IDrawingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Constants

        /// <summary>Index group to use for sound computations.</summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();
        
        /// <summary>
        /// Get the interface's type id once, for performance.
        /// </summary>
        private static readonly int TransformTypeId = ComponentSystem.Manager.GetComponentTypeId<ITransform>();
        
        /// <summary>
        /// Get the interface's type id once, for performance.
        /// </summary>
        private static readonly int VelocityTypeId = ComponentSystem.Manager.GetComponentTypeId<IVelocity>();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should perform updates and react to events.</summary>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>The sound bank we use to get actual sounds for our cue names.</summary>
        private readonly SoundBank _soundBank;

        /// <summary>The squared maximum range ate which sound can be heard.</summary>
        private readonly float _maxAudibleDistance;

        /// <summary>The squared maximum range ate which sound can be heard.</summary>
        private readonly float _maxAudibleDistanceSquared;

        /// <summary>The sound listener to use for relative position.</summary>
        private readonly AudioListener _listener = new AudioListener();

        /// <summary>The sound emitter to use for emitted sound positioning.</summary>
        private readonly AudioEmitter _emitter = new AudioEmitter();

        /// <summary>All Currently playing sounds mapped to the entry id</summary>
        private Dictionary<int, Cue> _playingSounds = new Dictionary<int, Cue>();

        #endregion

        #region Single-Allocation

        /// <summary>
        ///     Reused for iterating components. As its only used by the drawing instance we don't need to clone it, so it can
        ///     be readonly.
        /// </summary>
        private ISet<int> _reusableNeighborList = new HashSet<int>();

        /// <summary>Used to swap between this dict and the one assigned to _playingSounds to avoid reallocating each update.</summary>
        private Dictionary<int, Cue> _reusablePlayingSounds = new Dictionary<int, Cue>();

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoundSystem"/> class.
        /// </summary>
        /// <param name="soundBank">The sound bank.</param>
        /// <param name="maxAudibleDistance">The maximum distance at which sound is heard.</param>
        protected SoundSystem(SoundBank soundBank, float maxAudibleDistance)
        {
            _soundBank = soundBank;
            _maxAudibleDistance = maxAudibleDistance;
            _maxAudibleDistanceSquared = maxAudibleDistance * maxAudibleDistance;
        }

        #endregion

        #region Logic

        /// <summary>Check for sound in range and play.</summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Bail if we don't have a soundbank.
            if (_soundBank == null)
            {
                Enabled = false;
                return;
            }

            // No need to check *all* the time... this saves quite some performance
            // if a lot of sound emitting objects are in range.
            if (frame % 3 != 0)
            {
                return;
            }

            var index = (IndexSystem) Manager.GetSystem(IndexSystem.TypeId);
            Debug.Assert(index != null);

            // Update listener information.
            var listenerPosition = GetListenerPosition();
            var listenerVelocity = GetListenerVelocity();
            _listener.Velocity = ToV3(ref listenerVelocity);

            // Iterate all sounds in range of the listener. All sounds remaining
            // in the current list of sounds playing will be stopped, as they are
            // out of range. The ones in range will be removed from that list and
            // added to our reusable list.
            index.Find(listenerPosition, _maxAudibleDistance, _reusableNeighborList, IndexGroupMask);
            foreach (IIndexable neighbor in _reusableNeighborList.Select(Manager.GetComponentById))
            {
                // Get the sound component of the neighbor.
                var sound = (Sound) Manager.GetComponent(neighbor.Entity, Sound.TypeId);

                // Skip this neighbor if its sound is not enabled.
                if (!sound.Enabled)
                {
                    continue;
                }

                // Get sound position and velocity.
                var emitterPosition = ((ITransform) Manager.GetComponent(neighbor.Entity, TransformTypeId)).Position;

                // The velocity is optional, so we must check if it exists.
                var neighborVelocity = (IVelocity) Manager.GetComponent(neighbor.Entity, VelocityTypeId);
                var emitterVelocity = neighborVelocity != null ? neighborVelocity.LinearVelocity : Vector2.Zero;

                // Check whether to update or start playing.
                if (_playingSounds.ContainsKey(neighbor.Entity))
                {
                    // We already know this one so just apply 3d effect.
                    var cue = _playingSounds[neighbor.Entity];

                    // Make sure cue is not stopped (how ever that may have happened...)
                    if (!cue.IsStopped)
                    {
                        // Do not stop it.
                        _playingSounds.Remove(neighbor.Entity);

                        // We make the emitter position relative to the listener, which is
                        // equivalent to having the listener at the actual origin at all
                        // times, so we don't have to to update its position.
                        var relativeEmitterPosition = (Vector2) (emitterPosition - listenerPosition);

                        // Get position and velocity of emitter.
                        _emitter.Position = ToV3(ref relativeEmitterPosition);
                        _emitter.Velocity = ToV3(ref emitterVelocity);

                        // Apply new surround effect.
                        cue.Apply3D(_listener, _emitter);

                        // Add it to the new list of playing sounds.
                        _reusablePlayingSounds.Add(neighbor.Entity, cue);
                    }
                    else
                    {
                        // Dispose it. It will be restarted in the next update,
                        // if still in range.
                        cue.Dispose();
                        // Don't dispose it again.
                        _playingSounds.Remove(neighbor.Entity);
                    }
                }
                else
                {
                    // Sound is not yet playing, start it.
                    var cue = Play(sound.SoundName, ref emitterPosition, ref emitterVelocity);
                    if (cue != null)
                    {
                        _reusablePlayingSounds.Add(neighbor.Entity, cue);
                    }
                }
            }

            // Clear for next update.
            _reusableNeighborList.Clear();

            // Stop all sound that's not in range.
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

        #endregion

        #region Playback

        /// <summary>Plays a sound cue with the specified name at the specified location and with the specified emitter velocity.</summary>
        /// <param name="soundCue">The name of the sound cue to play.</param>
        /// <param name="position">The position at which to emit the sound.</param>
        /// <param name="velocity">The velocity of the sound's emitter.</param>
        public Cue Play(string soundCue, ref WorldPoint position, ref Vector2 velocity)
        {
            // Only if we have a sound bank.
            if (_soundBank == null)
            {
                return null;
            }

            // Get position and velocity of listener. We might be playing
            // some events from the past, where we were somewhere else, but
            // using that old position would be just as wrong, so this
            // wrong is simpler ;)
            var listenerPosition = GetListenerPosition();

            // Transform all sounds so as to be relative to the origin. This way
            // listener is implicitly always at the center, so we don't have to
            // adjust its position.
            var relativePosition = (Vector2) (position - listenerPosition);

            // Skip if too far away.
            if (relativePosition.LengthSquared() > _maxAudibleDistanceSquared)
            {
                return null;
            }

            // We're in range, so get the listener velocity, too.
            var listenerVelocity = GetListenerVelocity();

            // Lock for sound playback.
            lock (this)
            {
                _listener.Velocity = ToV3(ref listenerVelocity);

                // Get position and velocity of emitter.
                _emitter.Position = ToV3(ref relativePosition);
                _emitter.Velocity = ToV3(ref velocity);

                // Let there be sound!
                var cue = _soundBank.GetCue(soundCue);
                cue.Apply3D(_listener, _emitter);
                cue.Play();

                // Return cue for further usage.
                return cue;
            }
        }

        /// <summary>Plays a sound cue with the specified name at the specified location.</summary>
        /// <param name="soundCue">The name of the sound cue to play.</param>
        /// <param name="position">The position at which to emit the sound.</param>
        public Cue Play(string soundCue, ref WorldPoint position)
        {
            var zero = Vector2.Zero;
            return Play(soundCue, ref position, ref zero);
        }

        /// <summary>Plays a sound cue with the specified name as if it were emitted by the specified entity.</summary>
        /// <remarks>
        ///     The entity must have a <c>Transform</c> component. A <c>Velocity</c>
        ///     component is optional.
        /// </remarks>
        /// <param name="soundCue">The name of the sound cue to play.</param>
        /// <param name="entity">The entity that emits the sound.</param>
        public Cue Play(string soundCue, int entity)
        {
            var position = ((ITransform) Manager.GetComponent(entity, TransformTypeId)).Position;
            var velocity = Vector2.Zero;
            var velocityComponent = (IVelocity) Manager.GetComponent(entity, VelocityTypeId);
            if (velocityComponent != null)
            {
                velocity = velocityComponent.LinearVelocity;
            }
            return Play(soundCue, ref position, ref velocity);
        }

        #endregion

        #region Overrides for positional sound systems

        /// <summary>Get the position of the listener (e.g. player avatar).</summary>
        /// <returns>The position of the listener.</returns>
        protected virtual WorldPoint GetListenerPosition()
        {
            return WorldPoint.Zero;
        }

        /// <summary>Get the velocity of the listener (e.g. player avatar).</summary>
        /// <returns>The velocity of the listener.</returns>
        protected virtual Vector2 GetListenerVelocity()
        {
            return Vector2.Zero;
        }

        #endregion

        #region Utility methods

        /// <summary>Converts a 2D vector to a XACT compatible 3D vector.</summary>
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
    }
}