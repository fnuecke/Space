using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceData;

namespace Space.Model
{
    class GameObjectFactory : DrawableGameComponent, IGameObjectFactory
    {
        private Dictionary<string, ShipData> ships = new Dictionary<string, ShipData>();

        private Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        public GameObjectFactory(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IGameObjectFactory), this);
        }

        protected override void LoadContent()
        {
            var shipdata = Game.Content.Load<ShipData[]>("Data/ships");
            foreach (var ship in shipdata)
            {
                ships[ship.Name] = ship;
                textures[ship.Name] = Game.Content.Load<Texture2D>(ship.Texture);
            }

            base.LoadContent();
        }

        public Ship CreateShip(string name, int player)
        {
            return new Ship(ships[name], textures[name], player);
        }
    }
}
