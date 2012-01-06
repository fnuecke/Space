using System;
using System.Linq;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Data;
using Space.Data.Modules;

namespace Space.ComponentSystem.Components
{
    class Radar : AbstractComponent
    {
        #region Fields

        private Texture2D arrow;

        private SpriteFont font;
        
        #endregion

        #region Constructor

        public Radar()
        {
            DrawOrder = 2;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Draws the 
        /// </summary>
        /// <param name="parameterization">The parameterization to use for this update.</param>
        public override void Draw(object parameterization)
        {
            // Get parameterization in proper type.
            var args = (RendererParameterization)parameterization;
            if (arrow == null)
            {
                arrow = args.Content.Load<Texture2D>("Textures/arrow");
                font = args.Content.Load<SpriteFont>("Fonts/ConsoleFont");
            }
            
            var transform = Entity.GetComponent<Transform>();
            if (transform == null) return;
            var x = transform.Translation.X;
            var y = transform.Translation.Y;
            //get Index System
            var index = Entity.Manager.SystemManager.GetSystem<IndexSystem>();
            if (index == null)
            {
                return;
            }
            var modules = Entity.GetComponent<EntityModules<EntityAttributeType>>();

            if (modules == null) return;
            //get radar modules
            var radar = modules.GetModules<SensorModule>();
            float range = 0;
            if (radar != null)
            {   //get sum of radars todo check if we need multiple
                range += radar.Sum(radarModule => radarModule.Range);
            }
            //calculate distance
            range = modules.GetValue(EntityAttributeType.SensorRange, range);
            var count = 0;
            foreach (var neigbour in index.GetNeighbors(Entity, range, Detectable.IndexGroup))
            {
                count++;
                if (neigbour == null || neigbour.GetComponent<Transform>() == null || neigbour.GetComponent<AstronomicBody>() == null) continue;
                //get color of faction
                var color = Color.White;
                var faction = neigbour.GetComponent<Faction>();
                if (faction != null)
                    color = faction.Value.ToColor();

                args.SpriteBatch.DrawString(font, "das ist ein test"+color, new Vector2(200, 200), color);
                
                var position = neigbour.GetComponent<Transform>().Translation;
                var detectable = neigbour.GetComponent<Detectable>();
                if (detectable == null) continue;
                
                if (detectable.Texture == null)
                {
                    // But only if we have a name, set, else return.
                    if (string.IsNullOrWhiteSpace(detectable.TextureName))
                    {
                        continue;
                    }
                    detectable.Texture = args.Content.Load<Texture2D>(detectable.TextureName);
                }
                var vba = (position - transform.Translation);
                vba.Normalize();
                var distX = Math.Abs((double)position.X - (double)x);
                var distY = Math.Abs((double)position.Y - (double)y);
                var viewWidth = args.SpriteBatch.GraphicsDevice.Viewport.Width/2f;
                var viewHeight = args.SpriteBatch.GraphicsDevice.Viewport.Height / 2f;
                var distance = Math.Sqrt(Math.Pow((double)position.Y - (double)y, 2) +
                                         Math.Pow((double)position.X - (double)x, 2));

                var phi = Math.Atan2((double)position.Y - (double)y, (double)position.X - (double)x);
                var texturePos = new Vector2(args.SpriteBatch.GraphicsDevice.Viewport.Width / 2.0f,
                                           args.SpriteBatch.GraphicsDevice.Viewport.Height / 2.0f);
                //object is outside x bounds
                
                texturePos.X += viewWidth*vba.X;
                texturePos.Y += viewHeight*vba.Y;
                //texturePos.X += args.SpriteBatch.GraphicsDevice.Viewport.Height / 2.0f * (float)Math.Cos(phi);

                //texturePos.Y += args.SpriteBatch.GraphicsDevice.Viewport.Height / 2.0f * (float)Math.Sin(phi);
                //Console.WriteLine(arrowPos);
                
                if (distX > args.SpriteBatch.GraphicsDevice.Viewport.Width / 2.0 || distY > args.SpriteBatch.GraphicsDevice.Viewport.Height / 2.0)
                    args.SpriteBatch.Draw(detectable.Texture, texturePos, null, color, (float)phi,
                                          new Vector2(arrow.Width / 2.0f, arrow.Height / 2.0f),1f,
                                          SpriteEffects.None, 1);
                args.SpriteBatch.DrawString(font," texturePos "+texturePos+ ", width: " + viewWidth + " height: " + viewHeight, new Vector2(200, 200+count*20), color);
                //spriteBatch.Draw(rocketTexture, rocketPosition, null, players[currentPlayer].Color, rocketAngle, new Vector2(42, 240), 0.1f, SpriteEffects.None, 1);
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsDrawParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(RendererParameterization) ||
                parameterizationType.IsSubclassOf(typeof(RendererParameterization));
        }
        #endregion
    }
}
