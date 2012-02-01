using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.Control;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Space.ScreenManagement.Screens.Ingame.Interfaces
{

    /// <summary>
    /// An abstract class to guarantee that all GUI elements have the
    /// necessary methods and data.
    /// </summary>
    public abstract class AbstractGuiElement
    {

        #region Fields

        /// <summary>
        /// The local client, used to fetch player's position and radar range.
        /// </summary>
        protected readonly GameClient _client;

        #endregion

        #region Properties

        /// <summary>
        /// Status whether the GUI element is enabled or not.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Status whether the GUI element is displayed or not.
        /// </summary>
        public bool Visible { get; set; }

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The local client.</param>
        public AbstractGuiElement(GameClient client)
        {
            this._client = client;

            Enabled = false;
            Visible = false;
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public abstract void LoadContent(SpriteBatch spriteBatch, ContentManager content);

        #endregion

        #region Update & Draw

        /// <summary>
        /// Updates the data of the elements.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// Draws the element.
        /// </summary>
        public abstract void Draw();

        #endregion

        #region Mouse, Keyboard and Gameplay Listener

        public virtual bool HandleKeyPressed(Keys key)
        {
            return false;
        }

        public virtual bool HandleKeyReleased(Keys key)
        {
            return false;
        }

        public virtual bool HandleMousePressed(MouseButtons buttons)
        {
            return false;
        }

        public virtual bool HandleMouseReleased(MouseButtons buttons)
        {
            return false;
        }

        public virtual bool HandleMouseMoved(float x, float y)
        {
            return false;
        }

        public virtual bool HandleGamePadPressed(Buttons buttons)
        {
            return false;
        }

        public virtual bool HandleGamePadReleased(Buttons buttons)
        {
            return false;
        }

        #endregion

    }
}
