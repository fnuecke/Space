using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Space.Control;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ScreenManagement.Screens.Helper;

namespace Space.ScreenManagement.Screens.Interfaces
{
    public abstract class AHudElement
    {

        #region Fields

        /// <summary>
        /// The local client, used to fetch player's position and radar range.
        /// </summary>
        protected readonly GameClient _client;

        /// <summary>
        /// The current content manager.
        /// </summary>
        protected ContentManager _content;

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        protected SpriteBatch _spriteBatch;

        /// <summary>
        /// Helper class for drawing basic forms.
        /// </summary>
        protected BasicForms _basicForms;

        /// <summary>
        /// The width of the element.
        /// </summary>
        protected int _width;

        /// <summary>
        /// The height of the element.
        /// </summary>
        protected int _height;

        /// <summary>
        /// The top-left position of the element.
        /// </summary>
        protected Point _position;

        #endregion

        #region Initialisation
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client"></param>
        public AHudElement(GameClient client)
        {
            this._client = client;
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public virtual void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            this._spriteBatch = spriteBatch;
            this._content = content;
            
            _basicForms = new BasicForms(_spriteBatch, _client);
        }

        #endregion

        #region Getter / Setter

        public virtual int GetHeight()
        {
            return _height;
        }

        public virtual void SetHeight(int height)
        {
            _height = height;
        }

        public virtual int GetWidth()
        {
            return _width;
        }

        public virtual void SetWidth(int width)
        {
            _width = width;
        }

        /// <summary>
        /// Updates the position of the element and of all (!) child elements.
        /// </summary>
        /// <param name="newPosition">The top-left new position of the parent element.</param>
        public virtual void SetPosition(Point position)
        {
            _position = position;
        }

        /// <summary>
        /// Returns the position of the element.
        /// </summary>
        /// <return>The top-left position of the element.</return>
        public virtual Point GetPosition()
        {
            return _position;
        }

        #endregion

        /// <summary>
        /// Render the HUD header with the current values.
        /// </summary>
        public abstract void Draw();

    }
}
