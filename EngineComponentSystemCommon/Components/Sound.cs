using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Components
{
    public class Sound : Component
    {
        #region Fields
        /// <summary>
        /// The name of the sound
        /// </summary>
        public string SoundName;
        #endregion
        #region Constants

        /// <summary>
        /// Index group to use for sound computations.
        /// </summary>
        public static readonly ulong IndexGroup = 1ul << IndexSystem.GetGroup();
        #endregion

        /// <summary>
        /// Initialize the Sound with the given sound name
        /// </summary>
        /// <param name="soundname">The name of the sound to be played</param>
        /// <returns></returns>
        public Sound Initialize(string soundname)
        {
            SoundName = soundname;
            return this;
        }
    }
}
