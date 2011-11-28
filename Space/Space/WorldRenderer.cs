using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;
using Space.Commands;
using Space.Model;

namespace Space
{
    class WorldRenderer : DrawableGameComponent
    {
        private Player<PlayerInfo> player;
        private IState<GameState, IGameObject, GameCommandType> world;

        public WorldRenderer(Game game, Player<PlayerInfo> player, IState<GameState, IGameObject, GameCommandType> world)
            : base(game)
        {
            this.player = player;
            this.world = world;
        }

        public override void Draw(GameTime gameTime)
        {


            base.Draw(gameTime);
        }
    }
}
