using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// System that manages sound components, querying them for cue names to play
    /// in a single update.
    /// </summary>
    public class SoundSystem : AbstractSystem
    {
        #region Fields

        /// <summary>
        /// The sound bank we use to get actual sounds for our cue names.
        /// </summary>
        private readonly SoundBank _soundBank;

        /// <summary>
        /// The sound listener to use for relative position.
        /// </summary>
        protected readonly AudioListener Listener = new AudioListener();

        /// <summary>
        /// The sound emitter to use for emitted sound positioning.
        /// </summary>
        protected readonly AudioEmitter Emitter = new AudioEmitter();

        /// <summary>
        /// Whether this is the sound system thats doing the actual "rendering"
        /// for the simulation the component system belongs to, i.e. whether
        /// Draw is called for this instance. Only that system may actually
        /// play sounds.
        /// </summary>
        protected bool IsDrawingInstance;

        #endregion

        #region Constructor

        protected SoundSystem(SoundBank soundBank)
        {
            _soundBank = soundBank;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Flags our system as the presenting instance.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        /// <param name="frame">The frame that should be rendered.</param>
        public sealed override void Draw(GameTime gameTime, long frame)
        {
            IsDrawingInstance = true;
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
            if (!IsDrawingInstance)
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

            Listener.Position = ToV3(ref listenerPosition);
            Listener.Velocity = ToV3(ref listenerVelocity);

            // Get position and velocity of emitter.
            Emitter.Position = ToV3(ref position);
            Emitter.Velocity = ToV3(ref velocity);

            // Let there be sound!
            var cue = _soundBank.GetCue(soundCue);
            cue.Apply3D(Listener, Emitter);
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
        protected static Vector3 ToV3(ref Vector2 v2)
        {
            Vector3 result;
            result.X = v2.X;
            result.Y = 0;
            result.Z = v2.Y;
            return result;
        }

        #endregion

        #region Copying

        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            var copy = (SoundSystem)base.DeepCopy(into);

            // Mark as secondary system.
            copy.IsDrawingInstance = false;

            return copy;
        }

        #endregion
    }
}
