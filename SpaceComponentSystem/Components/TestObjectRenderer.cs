using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components
{
    public class TestObjectRenderer : Component
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>The size of the sun.</summary>
        public float Radius;

        /// <summary>The color tint for this sun.</summary>
        public Color Tint;

        #endregion

        public TestObjectRenderer Initialize(float radius, Color tint)
        {
            Radius = radius;
            Tint = tint;

            return this;
        }
    }
}