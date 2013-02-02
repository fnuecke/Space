using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework.Content;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>This system is used to centrally provide content.</summary>
    public class ContentSystem : AbstractSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion
        
        #region Properties

        /// <summary>Gets the content manager used for loading assets.</summary>
        public ContentManager Content
        {
            get { return _content; }
        }

        #endregion
        
        #region Fields

        /// <summary>The content manager used to load our assets.</summary>
        private readonly ContentManager _content;

        #endregion
        
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="GraphicsDeviceSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager to use for loading.</param>
        public ContentSystem(ContentManager content)
        {
            _content = content;
        }

        #endregion
    }
}
