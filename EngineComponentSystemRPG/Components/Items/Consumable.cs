namespace Engine.ComponentSystem.RPG.Components.Items
{
    /// <summary>
    /// A consumable item will be destroyed in the process of using it. If the
    /// items is stackable, the stack size will be reduced by one.
    /// </summary>
    public abstract class Consumable : Usable
    {
        /// <summary>
        /// Use the item, have it trigger its logic. Consumes one item of a
        /// stack, destroys the item if only one is left (or it's not
        /// stackable, which is equivalent).
        /// </summary>
        public override void Use()
        {
            var stackable = Entity.GetComponent<Stackable>();
            if (stackable != null)
            {
                // We're part of a stack, reduce the size by one, if we're at
                // zero, destroy the item / stack.
                if (--stackable.Count == 0)
                {
                    Entity.Manager.RemoveEntity(Entity);
                }
            }
            else
            {
                // Normal item, just remove it.
                Entity.Manager.RemoveEntity(Entity);
            }
        }
    }
}
