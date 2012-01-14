using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// A sound component, which can be configured to trigger on a given
    /// set of component messages.
    /// 
    /// <para>
    /// Optional: <c>Transform</c>, <c>Velocity</c>.
    /// </para>
    /// </summary>
    public abstract class AbstractSound : AbstractComponent
    {
        #region Fields

        /// <summary>
        /// The sounds to play in the next update.
        /// </summary>
        private List<string> _soundsToPlay = new List<string>();

        #endregion

        #region Logic

        /// <summary>
        /// Checks if any sounds should be played this update, and if yes writes
        /// the names. If possible, it also writes a position and velocity of
        /// the sound's emitter.
        /// </summary>
        /// <param name="parameterization"></param>
        public override void Update(object parameterization)
        {
            var args = (SoundParameterization)parameterization;

            // Should we play? (Was a message fired for us?)
            if (_soundsToPlay.Count > 0)
            {
                // Yes, write the cue name.
                args.SoundCues.AddRange(_soundsToPlay);

                // Also check if we can fill in position and velocity.
                var transform = Entity.GetComponent<Transform>();
                if (transform != null)
                {
                    args.Position = transform.Translation;
                }

                var velocity = Entity.GetComponent<Velocity>();
                if (velocity != null)
                {
                    args.Velocity = velocity.Value;
                }
            }

            // We played, unset for next update.
            _soundsToPlay.Clear();
        }

        /// <summary>
        /// Accepts parameterizations of type <c>SoundParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The type to check.</param>
        /// <returns>Whether the type is supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(SoundParameterization);
        }

        /// <summary>
        /// To be overridden by subclasses, to tell if a message should result
        /// in a sound being played, and if yes, which.
        /// </summary>
        /// <param name="message">The message to check.</param>
        /// <returns>The sound to be played.</returns>
        protected abstract string GetSoundForMessage(ValueType message);

        #endregion

        #region Message handling

        public override void HandleMessage<T>(ref T message)
        {
            var sound = GetSoundForMessage(message);
            if (!string.IsNullOrWhiteSpace(sound))
            {
                _soundsToPlay.Add(sound);
            }
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (AbstractSound)base.DeepCopy(into);

            if (copy == into)
            {
                copy._soundsToPlay.Clear();
                copy._soundsToPlay.AddRange(_soundsToPlay);
            }
            else
            {
                copy._soundsToPlay = new List<string>(_soundsToPlay);
            }

            return copy;
        }

        #endregion
    }
}
