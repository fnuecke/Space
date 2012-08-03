using Microsoft.Xna.Framework;

namespace Engine.FarMath
{
    /// <summary>
    /// A model-view transformation for <see cref="FarPosition"/>s.
    /// </summary>
    public struct FarTransform
    {
        #region Constants

        /// <summary>
        /// Gets the identity transform, that when applied does nothing.
        /// </summary>
        public static FarTransform Identity { get { return ConstIdentity; } }

        /// <summary>
        /// Keep as private field to avoid manipulation.
        /// </summary>
        private static readonly FarTransform ConstIdentity = new FarTransform(FarPosition.Zero, Matrix.Identity);

        #endregion

        #region Fields

        /// <summary>
        /// Gets or sets the origin of this transform. This is the point
        /// operations should be relative to for best precision.
        /// </summary>
        /// <remarks>
        /// Normally, this will be the position of the camera.
        /// </remarks>
        public FarPosition Translation;

        /// <summary>
        /// The actual transform matrix, as it would normally be used, minus
        /// the precomputed translation from the origin.
        /// </summary>
        public Matrix Matrix;

        #endregion

        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FarTransform"/> struct.
        /// </summary>
        /// <param name="translation">The initial translation.</param>
        /// <param name="matrix">The initial matrix.</param>
        public FarTransform(FarPosition translation, Matrix matrix)
        {
            Translation = translation;
            Matrix = matrix;
        }

        #endregion
    }
}
