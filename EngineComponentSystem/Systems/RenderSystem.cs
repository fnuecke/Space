using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Basic implementation of a render system. This is kept generic to allow
    /// sub-classing with extended rendering configurations (see the particle
    /// render system).
    /// </summary>
    public class RenderSystem<TUpdateParameterization, TDrawParameterization>
        : AbstractComponentSystem<TUpdateParameterization, TDrawParameterization>
        where TUpdateParameterization : RendererUpdateParameterization, new()
        where TDrawParameterization : RendererDrawParameterization, new()
    {
        #region Fields

        /// <summary>
        /// The reusable parameterization.
        /// </summary>
        protected TUpdateParameterization _updateParameterization;

        /// <summary>
        /// The reusable parameterization.
        /// </summary>
        protected TDrawParameterization _drawParameterization;

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components.
        /// </summary>
        private List<AbstractComponent> _reusableComponentList = new List<AbstractComponent>(1024);

        #endregion

        #region Constructor
        
        public RenderSystem(Game game, SpriteBatch spriteBatch)
        {
            _updateParameterization = new TUpdateParameterization();
            _updateParameterization.Game = game;
            _updateParameterization.SpriteBatch = spriteBatch;

            _drawParameterization = new TDrawParameterization();
            _drawParameterization.SpriteBatch = spriteBatch;
            _drawParameterization.Transform = Matrix.Identity;
        }

        #endregion

        #region Logic

        public override void Update(long frame)
        {
            // Set current frame.
            _updateParameterization.Frame = frame;

            _reusableComponentList.AddRange(UpdateableComponents);
            foreach (var component in _reusableComponentList)
            {
                if (component.Enabled)
                {
                    component.Update(_updateParameterization);
                }
            }
            _reusableComponentList.Clear();
        }

        public override void Draw(GameTime gameTime, long frame)
        {
            // Get translation, which may be overridden.
            _drawParameterization.GameTime = gameTime;
            _drawParameterization.Transform.Translation = GetTranslation();

            _reusableComponentList.AddRange(DrawableComponents);
            foreach (var component in _reusableComponentList)
            {
                if (component.Enabled)
                {
                    component.Draw(_drawParameterization);
                }
            }
            _reusableComponentList.Clear();
        }

        /// <summary>
        /// Override in subclasses for specific translation of the view.
        /// </summary>
        /// <returns>the translation of the view to use when rendering.</returns>
        protected virtual Vector3 GetTranslation()
        {
            return Vector3.Zero;
        }

        #endregion

        #region Copying

        public override IComponentSystem DeepCopy(IComponentSystem into)
        {
            var copy = (RenderSystem<TUpdateParameterization, TDrawParameterization>)base.DeepCopy(into);

            if (copy != into)
            {
                copy._reusableComponentList = new List<AbstractComponent>(1024);
            }

            return copy;
        }

        #endregion
    }
}
