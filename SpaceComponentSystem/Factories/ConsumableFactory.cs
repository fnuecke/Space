using Engine.ComponentSystem;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Components.Items.Consumables;

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
