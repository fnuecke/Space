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
        #region Fields
        
        /// <summary>
        /// A list of sounds to play in this update.
        /// </summary>
        private List<SoundParameterization> _sounds = new List<SoundParameterization>();
        private Dictionary<string ,int > playedSounds = new Dictionary<string, int>();

        /// <summary>
        /// Time in which the sound shall not be played again
        /// </summary>
        private const int Cooldown = 5;
        protected SoundBank SoundBank;
        #endregion


        #region Constructor
        public SoundSystem(SoundBank soundBank)
        {
            SoundBank = soundBank;
        }
        #endregion
        #region Logic

        public override void Update(ComponentSystemUpdateType updateType)
        {
            if (updateType == ComponentSystemUpdateType.Logic)
            {
                // Get a list of sounds that should be played this frame.
                _sounds.Clear();
                foreach (var component in Components)
                {
                    var parameterization = new SoundParameterization();
                    component.Update(parameterization);
                    if (!string.IsNullOrEmpty(parameterization.SoundCueToPlay))
                    {
                        _sounds.Add(parameterization);
                    }
                }
            }
            else if (updateType == ComponentSystemUpdateType.Display)
            {


                foreach (var key in playedSounds.Keys)
                {
                    if (--playedSounds[key] == 0)
                        playedSounds.Remove(key);
                    
                }
                // Get position of listener, i.e. what the sounds are relative to.
                FPoint listenerPosition = GetListenerPosition();
                FPoint listenerVelocity = GetListenerVelocity();

                var listener = new AudioListener();
                //Listener
                //Position
                Vector3 positionListener;
                positionListener.X = (float)listenerPosition.X;
                positionListener.Y = (float)listenerPosition.Y;
                positionListener.Z = 0;
                listener.Position = positionListener;
                //Velocity
                Vector3 velocityListener;
                velocityListener.X = (float)listenerVelocity.X;
                velocityListener.Y = (float)listenerVelocity.Y;
                velocityListener.Z = 0;
                listener.Velocity = velocityListener;

                // Actually play the sounds that should be this update.
                foreach (var sound in _sounds)
                {
                    //sound was not played in the last clicks
                    if (!playedSounds.ContainsKey(sound.SoundCueToPlay))
                    {
                        playedSounds.Add(sound.SoundCueToPlay,Cooldown);
                        var emitter = new AudioEmitter();
                        

                        //Emitter

                        //Velocity
                        Vector3 velocity;
                        velocity.X = (float)sound.Velocity.X;
                        velocity.Y = (float)sound.Velocity.Y;
                        velocity.Z = 0;
                        emitter.Velocity = velocity;
                        
                        //Position
                        Vector3 positionEmitter;
                        positionEmitter.X = (float)sound.Position.X;
                        positionEmitter.Y = (float)sound.Position.Y;
                        positionEmitter.Z = 0;
                        emitter.Position = positionEmitter;
                        SoundBank.PlayCue(sound.SoundCueToPlay,listener,emitter);
                    }
                    
                }

            }
        }

        /// <summary>
        /// Get the position of the listener (e.g. player avatar).
        /// </summary>
        /// <returns></returns>
        protected FPoint GetListenerPosition()
        {
            return FPoint.Zero;
        }
        /// <summary>
        /// Get the position of the listener (e.g. player avatar).
        /// </summary>
        /// <returns></returns>
        protected FPoint GetListenerVelocity()
        {
            return FPoint.Zero;
        }
        #endregion

        #region Cloning

        public override object Clone()
        {
            var copy = (SoundSystem)base.Clone();

            // Get own list of sounds to play.
            copy._sounds = new List<SoundParameterization>(_sounds);

            return copy;
        }

        #endregion
    }
}
