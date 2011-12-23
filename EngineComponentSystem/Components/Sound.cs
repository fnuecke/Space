using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;

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
#if DEBUG
            base.Update(parameterization);
#endif
            var p = (SoundParameterization)parameterization;

            // Should we play? (Was a message fired for us?)
            if (_soundsToPlay.Count > 0)
            {
                // Yes, write the cue name.
                p.SoundCues.AddRange(_soundsToPlay);

                // Also check if we can fill in position and velocity.
                var transform = Entity.GetComponent<Transform>();
                var velocity = Entity.GetComponent<Velocity>();

                if (transform != null)
                {
                    p.Position = transform.Translation;
                }
                if (velocity != null)
                {
                    p.Velocity = velocity.Value;
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
        public override bool SupportsParameterization(Type parameterizationType)
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

        public override void HandleMessage(ValueType message)
        {
            var sound = GetSoundForMessage(message);
            if (!string.IsNullOrWhiteSpace(sound))
            {
                _soundsToPlay.Add(sound);
            }
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(_soundsToPlay.Count);
            foreach (var sound in _soundsToPlay)
            {
                packet.Write(sound);
            }

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _soundsToPlay.Clear();
            int numSounds = packet.ReadInt32();
            for (int i = 0; i < numSounds; ++i)
            {
                _soundsToPlay.Add(packet.ReadString());
            }
        }

        public override object Clone()
        {
            var copy = (AbstractSound)base.Clone();

            copy._soundsToPlay = new List<string>(_soundsToPlay);

            return copy;
        }

        #endregion
    }
}
