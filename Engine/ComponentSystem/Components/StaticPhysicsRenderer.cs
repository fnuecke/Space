﻿using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Implements a renderer for a simple physics object.
    /// 
    /// <para>
    /// Requires: <c>StaticPhysics</c>.
    /// </para>
    /// </summary>
    public class StaticPhysicsRenderer : AbstractRenderer
    {
        #region Logic

        /// <summary>
        /// Render a physics object at its location.
        /// </summary>
        /// <param name="parameterization">the parameterization to use.</param>
        public override void Update(object parameterization)
        {
            // Make sure we have our texture.
            base.Update(parameterization);

            // Get parameterization in proper type.
            var p = (RendererParameterization)parameterization;

            // The position and orientation we're rendering at and in.
            var sphysics = Entity.GetComponent<StaticPhysics>();

            // Draw the texture based on our physics component.
            p.SpriteBatch.Begin();
            p.SpriteBatch.Draw(texture,
                new Rectangle((int)sphysics.Position.X + (int)p.Translation.X,
                              (int)sphysics.Position.Y + (int)p.Translation.Y,
                              texture.Width, texture.Height),
                null, Color.White,
                (float)sphysics.Rotation,
                new Vector2(texture.Width / 2, texture.Height / 2),
                SpriteEffects.None, 0);
            p.SpriteBatch.End();
        }

        #endregion
    }
}
