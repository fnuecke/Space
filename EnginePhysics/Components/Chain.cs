using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.Math.RectangleF;
#endif

namespace Engine.Physics.Components
{
    /// <summary>
    /// As opposed to Box2D, here a chain is not a fixture. It is merely a component
    /// that tracks edge fixtures.
    /// </summary>
    public sealed class Chain : Component
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
        
	    /// Create a loop. This automatically adjusts connectivity.
	    /// @param vertices an array of vertices, these are copied
	    /// @param count the vertex count
	    public void CreateLoop(IList<Vector2> vertices)
	    {
	        
	    }

	    /// Create a chain with isolated end vertices.
	    /// @param vertices an array of vertices, these are copied
	    /// @param count the vertex count
        public void CreateChain(IList<Vector2> vertices)
	    {
	        
	    }
    }
}
