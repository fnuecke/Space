using Engine.Simulation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Commands;

namespace Space.Model
{
    interface IGameObject : IPhysicsSteppable<GameState, IGameObject, GameCommandType>
    {
        void Draw(GameTime gameTime, Vector2 translation, SpriteBatch spriteBatch);
    }
}
