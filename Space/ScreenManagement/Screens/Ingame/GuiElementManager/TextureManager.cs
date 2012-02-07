using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;

namespace Space.ScreenManagement.Screens.Ingame.GuiElementManager
{

    /// <summary>
    /// A helper class that is used so manage images that are used in the game.
    /// 
    /// All images that have been already loaded are saved in the dictionary. They
    /// can be fetched by using the path as a key. If the image is not included to
    /// the dictionary when getting it, it will be initialized and added to the
    /// dictionary before returning it.
    /// </summary>
    class TextureManager
    {

        /// <summary>
        /// Holds the Texture2D objects which can be fetched by using the path as key.
        /// </summary>
        private Dictionary<string, Texture2D> _dictionary;

        /// <summary>
        /// The local client, used to fetch player's position and radar range.
        /// </summary>
        private GameClient _client;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The local client.</param>
        public TextureManager(GameClient client)
        {
            _client = client;
            Reset();
        }

        /// <summary>
        /// Returns the Texture2D object of the corresponding path which is
        /// saved in the dictionary. If the not already initialized yet, it will be
        /// initialized and added to the dictionary before.
        /// </summary>
        /// <param name="path">The path of the image.</param>
        /// <returns>The Texture2D object of the image.</returns>
        public Texture2D Get(string path)
        {
            if (path == null)
            {
                return null;
            }
            if (_dictionary == null)
                Console.WriteLine("DICTIONARY == NULL");
            if (!_dictionary.ContainsKey(path))
            {
                _dictionary.Add(path, _client.Game.Content.Load<Texture2D>(path));
            }
            return _dictionary[path];
        }

        /// <summary>
        /// Resets the dictionary by replacing it with a new object.
        /// </summary>
        public void Reset()
        {
            _dictionary = new Dictionary<string, Texture2D>();
        }

    }
}
