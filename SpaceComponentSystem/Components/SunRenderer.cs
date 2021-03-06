﻿using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components
{
    /// <summary>Represents sun visuals.</summary>
    public sealed class SunRenderer : Component
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

        /// <summary>Surface rotation of the sun.</summary>
        public Vector2 SurfaceRotation;

        /// <summary>Rotational direction of primary surface turbulence.</summary>
        public Vector2 PrimaryTurbulenceRotation;

        /// <summary>Rotational direction of secondary surface turbulence.</summary>
        public Vector2 SecondaryTurbulenceRotation;

        #endregion

        #region Initialization

        /// <summary>Initialize with the specified radius.</summary>
        /// <param name="radius">The radius of the sun.</param>
        /// <param name="surfaceRotation">Surface rotation of the sun.</param>
        /// <param name="primaryTurbulenceRotation">Rotational direction of primary surface turbulence.</param>
        /// <param name="secondaryTurbulenceRotation">Rotational direction of secondary surface turbulence.</param>
        /// <param name="tint"> </param>
        /// <returns></returns>
        public SunRenderer Initialize(
            float radius,
            Vector2 surfaceRotation,
            Vector2 primaryTurbulenceRotation,
            Vector2 secondaryTurbulenceRotation,
            Color tint)
        {
            Radius = radius;
            Tint = tint;
            SurfaceRotation = surfaceRotation;
            PrimaryTurbulenceRotation = primaryTurbulenceRotation;
            SecondaryTurbulenceRotation = secondaryTurbulenceRotation;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Radius = 0;
            Tint = Color.White;
            SurfaceRotation = Vector2.Zero;
            PrimaryTurbulenceRotation = Vector2.Zero;
            SecondaryTurbulenceRotation = Vector2.Zero;
        }

        #endregion
    }
}