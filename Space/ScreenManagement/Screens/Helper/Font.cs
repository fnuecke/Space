using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace Space.ScreenManagement.Screens.Helper
{

    /// <summary>
    /// A class that holds and handles all font related operations.
    /// </summary>
    public class Fonts
    {

        #region Constants

        /// <summary>
        /// Holds all available fonts that are currently supported by this class.
        /// </summary>
        public enum Types
        {
            Strasua24, ConsoleFont
        }

        #endregion

        #region Fields

        private ContentManager _content;
        private IngameScreen _ingame;
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// A Dictionary that holds all supported font types of this class
        /// and their correspondenting SpriteFont object.
        /// </summary>
        private Dictionary<Types, SpriteFont> _fontList;

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        public Fonts()
        {
            _fontList = new Dictionary<Types, SpriteFont>();
        }

        /// <summary>
        /// Loads the content.
        /// </summary>
        public void LoadContent(IngameScreen ingame, ContentManager content)
        {
            _ingame = ingame;
            _content = content;
            _spriteBatch = ingame.SpriteBatch;

            _fontList.Add(Types.Strasua24, _content.Load<SpriteFont>("Fonts/strasua_24"));
            _fontList.Add(Types.ConsoleFont, _content.Load<SpriteFont>("Fonts/ConsoleFont"));
        }

        #endregion

        #region Drawing Methods

        public void DrawString(Types font, String text, Vector2 position, Color color)
        {
            _spriteBatch.DrawString(GetSpriteFont(font), text, position, color);
        }

        #endregion

        #region Getter / Setter

        /// <summary>
        /// Returns the SpriteFont object of a specific font type.
        /// </summary>
        /// <param name="type">The font type.</param>
        /// <returns>The SpriteFont object of the font type.</returns>
        public SpriteFont GetSpriteFont(Types type)
        {
            return _fontList[type];
        }

        #endregion

    }
}
