using Engine.ComponentSystem.Entities;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Class used to generate consumable items.
    /// </summary>
    public static class ConsumableFactory
    {
        public static Entity CreateRepairKit()
        {
            var entity = new Entity();

            entity.AddComponent(new RepairKit("Repair Kit", "Textures/Icons/Buffs/default", 10));

            return entity;
        }
    }
}
