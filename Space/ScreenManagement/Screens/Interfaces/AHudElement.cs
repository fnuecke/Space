using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;
using Space.ScreenManagement.Screens.Helper;

namespace Space.ScreenManagement.Screens.Interfaces
{
    /// <summary>
    /// An abstract class that offers the basic elements and methods that
    /// are necessary for the hud elements.
    /// </summary>
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

        #region Getter / Setter

        /// <summary>
        /// Returns the height of the element.
        /// </summary>
        /// <returns>The height of the element.</returns>
        public virtual int GetHeight()
        {
            return _height;
        }

        /// <summary>
        /// Set the height of the element.
        /// </summary>
        /// <param name="height">The height of the element.</param>
        public virtual void SetHeight(int height)
        {
            _height = height;
        }

        /// <summary>
        /// Returns the width of the element.
        /// </summary>
        /// <returns>The width of the element.</returns>
        public virtual int GetWidth()
        {
            return _width;
        }

        /// <summary>
        /// Set the width of the element.
        /// </summary>
        /// <param name="width">The width of the element.</param>
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

        /// <summary>
        /// Returns the size of the element.
        /// </summary>
        /// <returns>The size of the element</returns>
        public virtual Point GetSize()
        {
            return new Point(GetWidth(), GetHeight());
        }

        /// <summary>
        /// Set the size of the elemenet.
        /// </summary>
        /// <param name="size">The size of the element.</param>
        public virtual void SetSize(Point size)
        {
            SetWidth(size.X);
            SetHeight(size.Y);
        }

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
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


            // set some standard values
            SetPosition(new Point(0, 0));
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD header with the current values.
        /// </summary>
        public abstract void Draw();

        #endregion

    }
}
