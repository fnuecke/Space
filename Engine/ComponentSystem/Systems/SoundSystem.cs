using System.Collections.Generic;
using Engine.ComponentSystem.Parameterizations;

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
                // Actually play the sounds that should be this update.
                foreach (var sound in _sounds)
                {
                    // TODO play
                    // TODO only play sounds again after a certain timeout (50ms or so) if XACT doesn't do that itself (which it probably doesn't?)
                }
            }
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
