using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;
using Space.ScreenManagement.Screens.Ingame.Interfaces;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    /// <summary>
    /// A sample file that can be used as a kind of template to create a new
    /// object for a new HUD element.
    /// 
    /// It is NOT intended to be used in the hud!
    /// </summary>
    class HudSample : AbstractHudElement
    {

        #region Fields

        // create fields for the single elements here

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudSample(GameClient client)
            : base(client)
        {
            // initialise the several elements here
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

            // call the LoadContent method of all elements here
            // and do the settings for the elements here
        }

        #endregion

        #region Getter & Setter

        public override void SetPosition(Point position)
        {
            base.SetPosition(position);

            // set the positions of all sub elements here - all positions should be committed
            // dependant of the position of the previous elements
        }

        public override int GetHeight()
        {
            int height = 0;

            // add the height of the single elements to the variable here

            return height;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD box with the current values.
        /// </summary>
        public override void Draw()
        {
            // call the Draw method of all elements
        }

        #endregion
    }
}
