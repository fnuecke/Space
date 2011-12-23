using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;

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
        /// Whether to play the sound this update or not.
        /// </summary>
        private List<string> _play = new List<string>();

        #endregion

        #region Logic

        /// <summary>
        /// Checks if this sound should be played this update, and if yes writes
        /// the name. If possible, it also writes a position and velocity of
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
            if (_play.Count > 0)
            {
                // Yes, write the cue name.
                p.SoundCues.AddRange(_play);

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
            _play.Clear();
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

        protected abstract string GetSoundForMessage(ValueType message);

        #endregion

        #region Message handling

        public override void HandleMessage(ValueType message)
        {
            var sound = GetSoundForMessage(message);
            if (!string.IsNullOrWhiteSpace(sound))
            {
                _play.Add(sound);
            }
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(_play.Count);
            foreach (var sound in _play)
            {
                packet.Write(sound);
            }

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _play.Clear();
            int numSounds = packet.ReadInt32();
            for (int i = 0; i < numSounds; ++i)
            {
                _play.Add(packet.ReadString());
            }
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            foreach (var sound in _play)
            {
               // hasher.Put(Encoding.UTF8.GetBytes(sound));
            }
        }

        public override object Clone()
        {
            var copy = (AbstractSound)base.Clone();

            copy._play = new List<string>(_play);

            return copy;
        }

        #endregion
    }
}
