using Engine.ComponentSystem.Systems;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.FarseerPhysics.Systems
{
    public sealed class PhysicsSystem : AbstractSystem, IUpdatingSystem
    {
        private World _world = new World(Vector2.Zero);

        public void Update(long frame)
        {
            _world.Step(1 / 20f);
        }
    }
}
