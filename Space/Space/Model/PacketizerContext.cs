using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceData;

namespace Space.Model
{
    class PacketizerContext
    {
        public Game game;
        public Dictionary<string, ShipData> shipData = new Dictionary<string, ShipData>();
        public Dictionary<string, Texture2D> shipTextures = new Dictionary<string, Texture2D>();
    }
}
