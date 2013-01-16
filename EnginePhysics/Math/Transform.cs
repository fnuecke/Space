using Engine.Serialization;
using Microsoft.Xna.Framework;

#if FARMATH
using Engine.FarMath;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using Engine.XnaExtensions;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Math
{
    /// <summary>Represents a 2D-rotation.</summary>
    internal struct Rotation
    {
        /// <summary>Gets the identity rotation.</summary>
        public static Rotation Identity
        {
            get { return ImmutableIdentity; }
        }

        private static readonly Rotation ImmutableIdentity = new Rotation {Sin = 0, Cos = 1};

        /// <summary>Sine and cosine of the rotation.</summary>
        public float Sin, Cos;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Rotation"/> struct.
        /// </summary>
        /// <param name="angle">The angle.</param>
        public Rotation(float angle)
        {
            Sin = (float) System.Math.Sin(angle);
            Cos = (float) System.Math.Cos(angle);
        }

        /// <summary>Set using an angle in radians.</summary>
        /// <param name="angle">the angle to set to.</param>
        public void Set(float angle)
        {
            Sin = (float) System.Math.Sin(angle);
            Cos = (float) System.Math.Cos(angle);
        }

        #region Operators

        /// <summary>
        ///     Inverts the rotation. This may be used to apply an inverse rotation to a vector or other rotation. I.e. -q * r
        ///     is the inverse of q * r.
        /// </summary>
        /// <param name="q">The rotation to invert.</param>
        /// <returns>The inverted rotation.</returns>
        public static Rotation operator -(Rotation q)
        {
            Rotation result;
            result.Sin = -q.Sin;
            result.Cos = q.Cos;
            return result;
        }

        /// <summary>Rotate a vector.</summary>
        /// <param name="q">The rotation to rotate the vector by.</param>
        /// <param name="v">The vector to rotate.</param>
        /// <returns>The rotated vector.</returns>
        public static Vector2 operator *(Rotation q, Vector2 v)
        {
            Vector2 result;
            result.X = q.Cos * v.X - q.Sin * v.Y;
            result.Y = q.Sin * v.X + q.Cos * v.Y;
            return result;
        }

        /// <summary>Multiply two rotations: q * r.</summary>
        /// <param name="q">The first rotation.</param>
        /// <param name="r">The other rotation.</param>
        /// <returns>The combined rotations.</returns>
        public static Rotation operator *(Rotation q, Rotation r)
        {
            // [qc -qs] * [rc -rs] = [qc*rc-qs*rs -qc*rs-qs*rc]
            // [qs  qc]   [rs  rc]   [qs*rc+qc*rs -qs*rs+qc*rc]
            // s = qs * rc + qc * rs
            // c = qc * rc - qs * rs
            Rotation qr;
            qr.Sin = q.Sin * r.Cos + q.Cos * r.Sin;
            qr.Cos = q.Cos * r.Cos - q.Sin * r.Sin;
            return qr;
        }

        #endregion
    };

    /// <summary>
    ///     A transform contains translation and rotation. It is used to represent the position and orientation of rigid
    ///     frames.
    /// </summary>
    internal struct LocalTransform
    {
        /// <summary>The translation in relative coordinates.</summary>
        public Vector2 Translation;

        /// <summary>The relative rotation.</summary>
        public Rotation Rotation;

        /// <summary>Transforms a local coordinate to another frame of reference.</summary>
        /// <param name="v">The local coordinate to transform.</param>
        /// <returns>The transformed coordinate (a global coordinate).</returns>
        public Vector2 ToOther(Vector2 v)
        {
            Vector2 result;
            result.X = (Rotation.Cos * v.X - Rotation.Sin * v.Y) + Translation.X;
            result.Y = (Rotation.Sin * v.X + Rotation.Cos * v.Y) + Translation.Y;
            return result;
        }

        /// <summary>Transforms a local coordinate from another frame of reference back to its original frame of reference.</summary>
        /// <param name="v">The world coordinate to apply the inverse transform to.</param>
        /// <returns>The result of the inverse transform (a local coordinate).</returns>
        public Vector2 FromOther(Vector2 v)
        {
            var px = v.X - Translation.X;
            var py = v.Y - Translation.Y;

            Vector2 result;
            result.X = Rotation.Cos * px + Rotation.Sin * py;
            result.Y = -Rotation.Sin * px + Rotation.Cos * py;
            return result;
        }
    }

    /// <summary>
    ///     A transform contains translation and rotation. It is used to represent the position and orientation of rigid
    ///     frames.
    /// </summary>
    internal struct WorldTransform
    {
        /// <summary>Gets the identity transform.</summary>
        public static WorldTransform Identity
        {
            get { return ImmutableIdentity; }
        }

        private static readonly WorldTransform ImmutableIdentity = new WorldTransform
        {
            Translation = WorldPoint.Zero,
            Rotation = Rotation.Identity
        };

        /// <summary>The translation in world coordinates.</summary>
        public WorldPoint Translation;

        /// <summary>The global rotation.</summary>
        public Rotation Rotation;

        /// <summary>Transforms a local coordinate to a global one.</summary>
        /// <param name="v">The local coordinate to transform.</param>
        /// <returns>The transformed coordinate (a global coordinate).</returns>
        public WorldPoint ToGlobal(Vector2 v)
        {
            WorldPoint result;
            result.X = (Rotation.Cos * v.X - Rotation.Sin * v.Y) + Translation.X;
            result.Y = (Rotation.Sin * v.X + Rotation.Cos * v.Y) + Translation.Y;
            return result;
        }

        /// <summary>Transforms a global coordinate to a local one.</summary>
        /// <param name="v">The world coordinate to apply the inverse transform to.</param>
        /// <returns>The result of the inverse transform (a local coordinate).</returns>
        public Vector2 ToLocal(WorldPoint v)
        {
// ReSharper disable RedundantCast Necessary for FarPhysics.
            var px = (float) (v.X - Translation.X);
            var py = (float) (v.Y - Translation.Y);
// ReSharper restore RedundantCast

            Vector2 result;
            result.X = Rotation.Cos * px + Rotation.Sin * py;
            result.Y = -Rotation.Sin * px + Rotation.Cos * py;
            return result;
        }

        /// <summary>
        ///     Combines this transform with the specified world transform, resulting in a transform that maps coordinates
        ///     from the other transform's local frame of reference to the one defined by this transform.
        /// </summary>
        /// <param name="xf">The other world transform.</param>
        /// <returns>A transform mapping coordinates from the other frame of reference to this one.</returns>
        public LocalTransform MulT(WorldTransform xf)
        {
            // v2 = A.q' * (B.q * v1 + B.p - A.p)
            //    = A.q' * B.q * v1 + A.q' * (B.p - A.p)
            LocalTransform result;
            result.Rotation = -Rotation * xf.Rotation;
// ReSharper disable RedundantCast Necessary for FarPhysics.
            result.Translation = -Rotation * (Vector2) (xf.Translation - Translation);
// ReSharper restore RedundantCast
            return result;
        }
    }

    /// <summary>A 2-by-2 matrix. Stored in column-major order.</summary>
    internal struct Matrix22
    {
        /// <summary>Gets the zero matrix.</summary>
        public static Matrix22 Zero
        {
            get { return ImmutableZero; }
        }

        private static readonly Matrix22 ImmutableZero = new Matrix22
        {
            Column1 = Vector2.Zero,
            Column2 = Vector2.Zero
        };

        /// <summary>The columns of this matrix.</summary>
        public Vector2 Column1, Column2;

        /// <summary>Computes the inverse of this matrix.</summary>
        /// <returns>The inverse matrix.</returns>
        public Matrix22 GetInverse()
        {
            var a = Column1.X;
            var b = Column2.X;
            var c = Column1.Y;
            var d = Column2.Y;

            var det = a * d - b * c;
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            if (det == 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                return Zero;
            }
            det = 1.0f / det;

            Matrix22 result;
            result.Column1.X = det * d;
            result.Column2.X = -det * b;
            result.Column1.Y = -det * c;
            result.Column2.Y = det * a;
            return result;
        }

        /// <summary>
        ///     Solve <c>A * x = v</c>, where <paramref name="v"/> is a column vector. This is more efficient than computing the
        ///     inverse in one-shot cases.
        /// </summary>
        public Vector2 Solve(Vector2 v)
        {
            var a11 = Column1.X;
            var a12 = Column2.X;
            var a21 = Column1.Y;
            var a22 = Column2.Y;
            var det = a11 * a22 - a12 * a21;
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            if (det != 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                det = 1.0f / det;
            }
            Vector2 result;
            result.X = det * (a22 * v.X - a12 * v.Y);
            result.Y = det * (a11 * v.Y - a21 * v.X);
            return result;
        }

        /// <summary>
        ///     Multiply a matrix times a vector. If a rotation matrix is provided, then this transforms the vector from one
        ///     frame to another.
        /// </summary>
        /// <param name="xf">The transformation matrix.</param>
        /// <param name="v">The vector to transform.</param>
        public static Vector2 operator *(Matrix22 xf, Vector2 v)
        {
            Vector2 result;
            result.X = xf.Column1.X * v.X + xf.Column2.X * v.Y;
            result.Y = xf.Column1.Y * v.X + xf.Column2.Y * v.Y;
            return result;
        }
    }

    /// <summary>A 3-by-3 matrix. Stored in column-major order.</summary>
    internal struct Matrix33
    {
        /// <summary>The columns of this matrix.</summary>
        public Vector3 Column1, Column2, Column3;

        /// <summary>
        ///     Solve <c>A * x = v</c>, where <paramref name="v"/> is a column vector. This is more efficient than computing the
        ///     inverse in one-shot cases.
        /// </summary>
        public Vector3 Solve33(Vector3 v)
        {
            var det = Vector3.Dot(Column1, Vector3.Cross(Column2, Column3));
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            if (det != 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                det = 1.0f / det;
            }
            Vector3 result;
            result.X = det * Vector3.Dot(v, Vector3.Cross(Column2, Column3));
            result.Y = det * Vector3.Dot(Column1, Vector3.Cross(v, Column3));
            result.Z = det * Vector3.Dot(Column1, Vector3.Cross(Column2, v));
            return result;
        }

        /// <summary>
        ///     Solve <c>A * x = v</c>, where <paramref name="v"/> is a column vector. This is more efficient than computing the
        ///     inverse in one-shot cases. Solve only the upper 2-by-2 matrix equation.
        /// </summary>
        public Vector2 Solve22(Vector2 v)
        {
            var a11 = Column1.X;
            var a12 = Column2.X;
            var a21 = Column1.Y;
            var a22 = Column2.Y;
            var det = a11 * a22 - a12 * a21;
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            if (det != 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                det = 1.0f / det;
            }
            Vector2 result;
            result.X = det * (a22 * v.X - a12 * v.Y);
            result.Y = det * (a11 * v.Y - a21 * v.X);
            return result;
        }

        /// <summary>Get the inverse of this matrix as a 2-by-2. Returns the zero matrix if singular.</summary>
        public void GetInverse22(out Matrix33 m)
        {
            var a = Column1.X;
            var b = Column2.X;
            var c = Column1.Y;
            var d = Column2.Y;
            var det = a * d - b * c;
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
            if (det != 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                det = 1.0f / det;
            }

            m.Column1.X = det * d;
            m.Column2.X = -det * b;
            m.Column1.Z = 0.0f;
            m.Column1.Y = -det * c;
            m.Column2.Y = det * a;
            m.Column2.Z = 0.0f;
            m.Column3.X = 0.0f;
            m.Column3.Y = 0.0f;
            m.Column3.Z = 0.0f;
        }

        /// <summary>Get the symmetric inverse of this matrix as a 3-by-3. Returns the zero matrix if singular.</summary>
        public void GetSymInverse33(out Matrix33 m)
        {
            var det = Vector3.Dot(Column1, Vector3.Cross(Column2, Column3));
// ReSharper disable CompareOfFloatsByEqualityOperator
            if (det != 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
            {
                det = 1.0f / det;
            }

            var a11 = Column1.X;
            var a12 = Column2.X;
            var a13 = Column3.X;
            var a22 = Column2.Y;
            var a23 = Column3.Y;
            var a33 = Column3.Z;

            m.Column1.X = det * (a22 * a33 - a23 * a23);
            m.Column1.Y = det * (a13 * a23 - a12 * a33);
            m.Column1.Z = det * (a12 * a23 - a13 * a22);

            m.Column2.X = m.Column1.Y;
            m.Column2.Y = det * (a11 * a33 - a13 * a13);
            m.Column2.Z = det * (a13 * a12 - a11 * a23);

            m.Column3.X = m.Column1.Z;
            m.Column3.Y = m.Column2.Z;
            m.Column3.Z = det * (a11 * a22 - a12 * a12);
        }

        /// <summary>Multiply a matrix times a vector.</summary>
        /// <param name="m">The matrix.</param>
        /// <param name="v">The vector.</param>
        /// <returns>The result of the multiplication.</returns>
        public static Vector3 operator *(Matrix33 m, Vector3 v)
        {
            return v.X * m.Column1 + v.Y * m.Column2 + v.Z * m.Column3;
        }

        /// <summary>Multiply a matrix times a vector.</summary>
        /// <param name="m">The matrix.</param>
        /// <param name="v">The vector.</param>
        /// <returns>The result of the multiplication.</returns>
        public static Vector2 operator *(Matrix33 m, Vector2 v)
        {
            return new Vector2(
                m.Column1.X * v.X + m.Column2.X * v.Y,
                m.Column1.Y * v.X + m.Column2.Y * v.Y);
        }
    }

    /// <summary>Packet write and read methods for math types.</summary>
    internal static class PacketTransformExtensions
    {
        /// <summary>Writes the specified world transform value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static IWritablePacket Write(this IWritablePacket packet, WorldTransform data)
        {
            return packet.Write(data.Translation).Write(data.Rotation);
        }

        /// <summary>Reads a world transform value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out WorldTransform data)
        {
            data = packet.ReadWorldTransform();
            return packet;
        }

        /// <summary>Reads a world transform value.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static WorldTransform ReadWorldTransform(this IReadablePacket packet)
        {
            WorldTransform result;
#if FARMATH
            result.Translation = packet.ReadFarPosition();
#else
            result.Translation = packet.ReadVector2();
#endif
            result.Rotation = packet.ReadRotation();
            return result;
        }

        /// <summary>Writes the specified rotation value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        private static IWritablePacket Write(this IWritablePacket packet, Rotation data)
        {
            return packet.Write(data.Sin).Write(data.Cos);
        }

        /// <summary>Reads a rotation value.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        private static Rotation ReadRotation(this IReadablePacket packet)
        {
            Rotation result;
            result.Sin = packet.ReadSingle();
            result.Cos = packet.ReadSingle();
            return result;
        }
    }
}