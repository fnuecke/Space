using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Loads icons for detectable components.</summary>
    public sealed class DetectableSystem : AbstractComponentSystem<Detectable>, IDrawingSystem
    {
        #region Constants

        /// <summary>Index group to use for gravitational computations.</summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Logic

        /// <summary>Loads textures for detectables.</summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            foreach (var component in Components)
            {
                // Load our texture, if it's not set.
                if (component.Texture == null)
                {
                    var graphicsSystem = ((GraphicsDeviceSystem) Manager.GetSystem(GraphicsDeviceSystem.TypeId));
                    component.Texture = graphicsSystem.Content.Load<Texture2D>(component.TextureName);
                }
            }
        }
        
        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<GraphicsDeviceCreated>(OnGraphicsDeviceCreated);
        }

        private void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            foreach (var component in Components)
            {
                component.Texture = message.Content.Load<Texture2D>(component.TextureName);
            }
        }

        #endregion
    }
}