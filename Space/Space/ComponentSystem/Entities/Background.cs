using Engine.ComponentSystem.Entities;

namespace Space.ComponentSystem.Entities
{
    public class Background : AbstractEntity
    {
        public Background()
        {
            
        }

        public override object Clone()
        {
            return this;
        }
    }
}
