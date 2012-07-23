using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Loads icons for detectable components.
    /// </summary>
    public sealed class DetectableSystem : AbstractComponentSystem<Detectable>
    {
        #region Constants

        /// <summary>
        /// Index group to use for gravitational computations.
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Fields
        
        /// <summary>
        /// Used for loading detectable icons.
        /// </summary>
        private readonly ContentManager _content;

        #endregion

        #region Constructor
        
        public  DetectableSystem(ContentManager content)
        {
            _content = content;
        }

        #endregion

        #region Logic
        
        protected override void UpdateComponent(long frame, Detectable component)
        {
            // Load our texture, if it's not set.
            if (component.Texture == null)
            {
                component.Texture = _content.Load<Texture2D>(component.TextureName);
            }
        }

        #endregion
    }
}
