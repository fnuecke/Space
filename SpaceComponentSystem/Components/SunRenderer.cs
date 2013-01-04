using System.Globalization;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents sun visuals.
    /// </summary>
    public sealed class SunRenderer : Component
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
        /// The size of the sun.
        /// </summary>
        public float Radius;

        /// <summary>
        /// The color tint for this sun.
        /// </summary>
        public Color Tint;

        /// <summary>
        /// Surface rotation of the sun.
        /// </summary>
        public Vector2 SurfaceRotation;

        /// <summary>
        /// Rotational direction of primary surface turbulence.
        /// </summary>
        public Vector2 PrimaryTurbulenceRotation;

        /// <summary>
        /// Rotational direction of secondary surface turbulence.
        /// </summary>
        public Vector2 SecondaryTurbulenceRotation;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherSun = (SunRenderer)other;
            Radius = otherSun.Radius;
            SurfaceRotation = otherSun.SurfaceRotation;
            PrimaryTurbulenceRotation = otherSun.PrimaryTurbulenceRotation;
            SecondaryTurbulenceRotation = otherSun.SecondaryTurbulenceRotation;

            return this;
        }

        /// <summary>
        /// Initialize with the specified radius.
        /// </summary>
        /// <param name="radius">The radius of the sun.</param>
        /// <param name="surfaceRotation">Surface rotation of the sun.</param>
        /// <param name="primaryTurbulenceRotation">Rotational direction of primary surface turbulence.</param>
        /// <param name="secondaryTurbulenceRotation">Rotational direction of secondary surface turbulence.</param>
        /// <param name="tint"> </param>
        /// <returns></returns>
        public SunRenderer Initialize(float radius, Vector2 surfaceRotation, Vector2 primaryTurbulenceRotation, Vector2 secondaryTurbulenceRotation, Color tint)
        {
            Radius = radius;
            Tint = tint;
            SurfaceRotation = surfaceRotation;
            PrimaryTurbulenceRotation = primaryTurbulenceRotation;
            SecondaryTurbulenceRotation = secondaryTurbulenceRotation;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
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

        #region Serialization

        /// <summary>
        /// Suppress hashing as this component has no influence on other
        /// components and actual simulation logic.
        /// </summary>
        /// <param name="hasher"></param>
        public override void Hash(Hasher hasher)
        {
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Radius=" + Radius.ToString(CultureInfo.InvariantCulture) + ", Color=" + Tint +
                ", SurfaceRotation=" + SurfaceRotation.X.ToString(CultureInfo.InvariantCulture) + ":" + SurfaceRotation.Y.ToString(CultureInfo.InvariantCulture) +
                ", PrimaryTurbulenceRotation=" + PrimaryTurbulenceRotation.X.ToString(CultureInfo.InvariantCulture) + ":" + PrimaryTurbulenceRotation.Y.ToString(CultureInfo.InvariantCulture) +
                ", SecondaryTurbulenceRotation=" + SecondaryTurbulenceRotation.X.ToString(CultureInfo.InvariantCulture) + ":" + SecondaryTurbulenceRotation.Y.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
