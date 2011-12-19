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
    public class Sound : AbstractComponent
    {
        #region Properties
        
        /// <summary>
        /// The component messages this sound should be played on.
        /// </summary>
        public List<Type> TriggeringMessages { get; private set; }

        /// <summary>
        /// The name of the XACT sound cue to play.
        /// </summary>
        public string SoundCue { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Whether to play the sound this update or not.
        /// </summary>
        private bool _play;

        #endregion

        #region Constructor

        public Sound()
        {
            TriggeringMessages = new List<Type>();
        }

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
            if (_play)
            {
                // Yes, write the cue name.
                p.SoundCueToPlay = SoundCue;

                // Also check if we can fill in position and velocity.
                var transform = Entity.GetComponent<Transform>();
                var velocity = Entity.GetComponent<Velocity>();

                if (transform != null)
                {
                    p.Position = transform.Translation;
                }
                if (velocity != null)
                {
                    p.velocity = velocity.Value;
                }
            }

            // We played, unset for next update.
            _play = false;
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

        #endregion

        #region Message handling

        public override void HandleMessage(object message)
        {
            if (TriggeringMessages.Contains(message.GetType()))
            {
                _play = true;
            }
        }

        #endregion

        #region Serialization / Hashing

        public override void Packetize(Packet packet)
        {
            packet.Write(TriggeringMessages.Count);
            foreach (var messageType in TriggeringMessages)
            {
                packet.Write(messageType.FullName);
            }
            packet.Write(SoundCue);
            packet.Write(_play);
        }

        public override void Depacketize(Packet packet)
        {
            int numTriggeringMessages = packet.ReadInt32();
            for (int i = 0; i < numTriggeringMessages; ++i)
            {
                TriggeringMessages.Add(Type.GetType(packet.ReadString()));
            }
            SoundCue = packet.ReadString();
            _play = packet.ReadBoolean();
        }

        public override object Clone()
        {
            var copy = (Sound)base.Clone();
            copy.TriggeringMessages = new List<Type>(TriggeringMessages);
            return copy;
        }

        #endregion
    }
}
