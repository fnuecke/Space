﻿using Engine.Simulation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.Simulation
{
    interface IGameObject : IPhysicsSteppable<IGameObject>
    {
        void Draw(GameTime gameTime, Vector2 translation, SpriteBatch spriteBatch);
    }
}
