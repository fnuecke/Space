using Engine.ComponentSystem.Components;
using FarseerPhysics.Dynamics;

namespace Engine.ComponentSystem.FarseerPhysics.Components
{
    public abstract class PhysicsBody : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The body representing the entity of this component in the physics engine.
        /// </summary>
        public Body Value;

        #endregion

        /// <summary>
        /// Creates the body, fixture and shape representing the entity this component belongs to.
        /// </summary>
        /// <param name="world">The world to create the parts in.</param>
        protected abstract void CreateBody(World world);
    }
}
