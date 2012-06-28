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

        public string SoundName;
        #endregion
        #region Constants

        /// <summary>
        /// Index group to use for gravitational computations.
        /// </summary>
        public static readonly ulong IndexGroup = 1ul << IndexSystem.GetGroup();
        #endregion

        public Sound Initialize(string soundname)
        {
            SoundName = soundname;
            return this;
        }
    }
}
