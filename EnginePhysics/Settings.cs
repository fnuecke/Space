using Microsoft.Xna.Framework;

namespace Engine.Physics
{
    /// <summary>
    /// This class holds some global constants that control the behavior of
    /// our physics simulation.
    /// </summary>
    internal static class Settings
    {
        #region General

        /// <summary>
        /// An epsilon value used to cut off noise in a lot of places.
        /// </summary>
        public const float Epsilon = 1.192092896e-07f;

        /// <summary>
        /// Number of velocity iterations to use in the solver. The suggested
        /// value by Box2D is 8.
        /// Using fewer iterations increases performance but accuracy suffers.
        /// Likewise, using more iterations decreases performance but improves
        /// the quality of your simulation.
        /// </summary>
        public const int VelocityIterations = 8;

        /// <summary>
        /// Number of position iterations to use in the solver. The suggested
        /// value by Box2D is 3.
        /// Using fewer iterations increases performance but accuracy suffers.
        /// Likewise, using more iterations decreases performance but improves
        /// the quality of your simulation.
        /// </summary>
        public const int PositionIterations = 3;

        /// <summary>
        /// Number of position iterations to use in the TOI solver. The hardcoded
        /// value by Box2D is 20.
        /// Using fewer iterations increases performance but accuracy suffers.
        /// Likewise, using more iterations decreases performance but improves
        /// the quality of your simulation.
        /// </summary>
        public const int PositionIterationsTOI = 20;

        #endregion

        #region Collision

        /// <summary>
        /// The maximum number of vertices on a convex polygon. You cannot increase
        /// this too much because b2BlockAllocator has a maximum object size.
        /// </summary>
        public const int MaxPolygonVertices = 8;

        /// <summary>
        /// This is used to fatten AABBs in the dynamic tree. This allows proxies
        /// to move by a small amount without triggering a tree adjustment.
        /// This is in meters.
        /// </summary>
        public const float AabbExtension = 0.1f;

        /// <summary>
        /// This is used to fatten AABBs in the dynamic tree. This is used to predict
        /// the future position based on the current displacement.
        /// This is a dimensionless multiplier.
        /// </summary>
        public const float AabbMultiplier = 2;

        /// <summary>
        /// A small length used as a collision and constraint tolerance. Usually it is
        /// chosen to be numerically significant, but visually insignificant.
        /// </summary>
        public const float LinearSlop = 0.005f;

        /// <summary>
        /// A small angle used as a collision and constraint tolerance. Usually it is
        /// chosen to be numerically significant, but visually insignificant.
        /// </summary>
        public const float AngularSlop = 2f / 180f * MathHelper.Pi;

        /// <summary>
        /// The radius of the polygon/edge shape skin. This should not be modified. Making
        /// this smaller means polygons will have an insufficient buffer for continuous collision.
        /// Making it larger may create artifacts for vertex collision.
        /// </summary>
        public const float PolygonRadius = 2f * 0.005f;

        /// <summary>
        /// Maximum number of sub-steps per contact in continuous physics simulation.
        /// </summary>
        public const int MaxSubSteps = 8;

        #endregion

        #region Dynamics

        /// <summary>
        /// Maximum number of contacts to be handled to solve a TOI impact.
        /// </summary>
        public const int MaxTOIContacts = 32;

        /// <summary>
        /// A velocity threshold for elastic collisions. Any collision with a relative linear
        /// velocity below this threshold will be treated as inelastic.
        /// </summary>
        public const float VelocityThreshold = 1;

        /// <summary>
        /// The maximum linear position correction used when solving constraints. This helps to
        /// prevent overshoot.
        /// </summary>
        public const float MaxLinearCorrection = 0.2f;

        /// <summary>
        /// The maximum angular position correction used when solving constraints. This helps to
        /// prevent overshoot.
        /// </summary>
        public const float MaxAngularCorrection = 8f / 180f * MathHelper.Pi;

        /// <summary>
        /// The maximum linear velocity of a body. This limit is very large and is used
        /// to prevent numerical problems. You shouldn't need to adjust this.
        /// </summary>
        public const float MaxTranslation = 2, MaxTranslationSquared = MaxTranslation * MaxTranslation;

        /// <summary>
        /// The maximum angular velocity of a body. This limit is very large and is used
        /// to prevent numerical problems. You shouldn't need to adjust this.
        /// </summary>
        public const float MaxRotation = 0.5f * MathHelper.Pi, MaxRotationSquared = MaxRotation * MaxRotation;

        /// <summary>
        /// This scale factor controls how fast overlap is resolved. Ideally this would be 1 so
        /// that overlap is removed in one time step. However using values close to 1 often lead
        /// to overshoot.
        /// </summary>
        public const float Baumgarte = 0.2f, BaugarteTOI = 0.75f;

        #endregion

        #region Sleep

        /// <summary>
        /// The time that a body must be still before it will go to sleep.
        /// </summary>
        public const float TimeToSleep = 0.5f;

        /// <summary>
        /// A body cannot sleep if its linear velocity is above this tolerance.
        /// </summary>
        public const float LinearSleepTolerance = 0.01f;

        /// <summary>
        /// A body cannot sleep if its angular velocity is above this tolerance.
        /// </summary>
        public const float AngularSleepTolerance = 2f / 180f * MathHelper.Pi;

        #endregion
    }
}
