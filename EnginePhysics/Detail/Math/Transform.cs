using Engine.Serialization;
using Engine.XnaExtensions;
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

namespace Engine.Physics.Detail.Math
{
    /// <summary>
    /// Represents a 2D-rotation.
    /// </summary>
    internal struct Rotation
    {
        /// <summary>
        /// Gets the identity rotation.
        /// </summary>
        public static Rotation Identity
        {
            get { return ImmutableIdentity; }
        }

        private static readonly Rotation ImmutableIdentity = new Rotation {Sin = 0, Cos = 1};

        /// <summary>
        /// Sine and cosine of the rotation.
        /// </summary>
        public float Sin, Cos;

        /// <summary>
        /// Set using an angle in radians.
        /// </summary>
        /// <param name="angle">the angle to set to.</param>
        public void Set(float angle)
        {
            Sin = (float)System.Math.Sin(angle);
            Cos = (float)System.Math.Cos(angle);
        }

        /// <summary>
        /// Get the angle of the rotation in radians.
        /// </summary>
        /// <returns>The rotation in radians.</returns>
        public float GetAngle()
        {
            return (float)System.Math.Atan2(Sin, Cos);
        }

        #region Operators

        /// <summary>
        /// Inverts the rotation. This may be used to apply an inverse rotation
        /// to a vector or other rotation. I.e. -q * r is the inverse of q * r.
        /// </summary>
        /// <param name="q">The rotation to invert.</param>
        /// <returns>
        /// The inverted rotation.
        /// </returns>
        public static Rotation operator -(Rotation q)
        {
            Rotation result;
            result.Sin = -q.Sin;
            result.Cos = q.Cos;
	        return result;
        }
        
        /// <summary>
        /// Rotate a vector.
        /// </summary>
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

        /// <summary>
        /// Multiply two rotations: q * r.
        /// </summary>
        /// <param name="q">The first rotation.</param>
        /// <param name="r">The other rotation.</param>
        /// <returns>
        /// The combined rotations.
        /// </returns>
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
    /// A transform contains translation and rotation. It is used to represent
    /// the position and orientation of rigid frames.
    /// </summary>
    internal struct LocalTransform
    {
        /// <summary>
        /// The translation in relative coordinates.
        /// </summary>
        public Vector2 Translation;

        /// <summary>
        /// The relative rotation.
        /// </summary>
        public Rotation Rotation;

        /// <summary>
        /// Transforms a local coordinate to another frame of reference.
        /// </summary>
        /// <param name="v">The local coordinate to transform.</param>
        /// <returns>The transformed coordinate (a global coordinate).</returns>
        public Vector2 ToOther(Vector2 v)
        {
            Vector2 result;
            result.X = (Rotation.Cos * v.X - Rotation.Sin * v.Y) + Translation.X;
            result.Y = (Rotation.Sin * v.X + Rotation.Cos * v.Y) + Translation.Y;
            return result;
        }

        /// <summary>
        /// Transforms a local coordinate from another frame of reference back
        /// to its original frame of reference.
        /// </summary>
        /// <param name="v">The world coordinate to apply the inverse transform to.</param>
        /// <returns>
        /// The result of the inverse transform (a local coordiante).
        /// </returns>
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
    /// A transform contains translation and rotation. It is used to represent
    /// the position and orientation of rigid frames.
    /// </summary>
    internal struct WorldTransform
    {
        /// <summary>
        /// Gets the identity transform.
        /// </summary>
        public static WorldTransform Identity
        {
            get { return ImmutableIdentity; }
        }

        private static readonly WorldTransform ImmutableIdentity = new WorldTransform
        {
            Translation = WorldPoint.Zero,
            Rotation = Rotation.Identity
        };

        /// <summary>
        /// The translation in world coordinates.
        /// </summary>
        public Vector2 Translation;

        /// <summary>
        /// The global rotation.
        /// </summary>
        public Rotation Rotation;

        /// <summary>
        /// Transforms a local coordinate to a global one.
        /// </summary>
        /// <param name="v">The local coordinate to transform.</param>
        /// <returns>The transformed coordinate (a global coordinate).</returns>
        public Vector2 ToGlobal(Vector2 v)
        {
            Vector2 result;
            result.X = (Rotation.Cos * v.X - Rotation.Sin * v.Y) + Translation.X;
            result.Y = (Rotation.Sin * v.X + Rotation.Cos * v.Y) + Translation.Y;
            return result;
        }

        /// <summary>
        /// Transforms a global coordinate to a local one.
        /// </summary>
        /// <param name="v">The world coordinate to apply the inverse transform to.</param>
        /// <returns>
        /// The result of the inverse transform (a local coordiante).
        /// </returns>
        public Vector2 ToLocal(Vector2 v)
        {
            var px = (float)(v.X - Translation.X);
            var py = (float)(v.Y - Translation.Y);

            Vector2 result;
            result.X = Rotation.Cos * px + Rotation.Sin * py;
            result.Y = -Rotation.Sin * px + Rotation.Cos * py;
            return result;
        }

        /// <summary>
        /// Combines this transform with the specified world transform, resulting
        /// in a transform that maps coordinates from tje ptjer transform's local
        /// frame of reference to the one defined by this transform.
        /// </summary>
        /// <param name="xf">The other world transform.</param>
        /// <returns>A transform mapping coordinates from the other frame
        /// of reference to this one.</returns>
        public LocalTransform MulT(WorldTransform xf)
        {
            // v2 = A.q' * (B.q * v1 + B.p - A.p)
            //    = A.q' * B.q * v1 + A.q' * (B.p - A.p)
            LocalTransform result;
	        result.Rotation = -Rotation * xf.Rotation;
	        result.Translation = -Rotation * (Vector2)(xf.Translation - Translation);
	        return result;
        }
    }

    /// <summary>
    /// A 2-by-2 matrix. Stored in column-major order.
    /// </summary>
    internal struct Mat22
    {
        public Vector2 ex, ey;

	    public Mat22 GetInverse()
	    {
	        var a = ex.X;
	        var b = ey.X;
	        var c = ex.Y;
            var d = ey.Y;
		    Mat22 B;
		    var det = a * d - b * c;
		    if (det != 0.0f)
		    {
			    det = 1.0f / det;
		    }
		    B.ex.X =  det * d;	B.ey.X = -det * b;
		    B.ex.Y = -det * c;	B.ey.Y =  det * a;
		    return B;
	    }

        /// Multiply a matrix times a vector. If a rotation matrix is provided,
        /// then this transforms the vector from one frame to another.
        public static Vector2 operator*(Mat22 A, Vector2 v)
	    {
	        Vector2 result;
	        result.X = A.ex.X * v.X + A.ey.X * v.Y;
	        result.Y = A.ex.Y * v.X + A.ey.Y * v.Y;
	        return result;
        }
    }

    /// <summary>
    /// Packet write and read methods for math types.
    /// </summary>
    internal static class PacketTransformExtensions
    {
        /// <summary>
        /// Writes the specified world transform value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Write(this Packet packet, WorldTransform data)
        {
            return packet.Write(data.Translation).Write(data.Rotation);
        }

        /// <summary>
        /// Reads a world transform value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static WorldTransform ReadWorldTransform(this Packet packet)
        {
            WorldTransform result;
            result.Translation = packet.ReadVector2();
            result.Rotation = packet.ReadRotation();
            return result;
        }

        /// <summary>
        /// Writes the specified rotation value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        private static Packet Write(this Packet packet, Rotation data)
        {
            return packet.Write(data.Sin).Write(data.Cos);
        }

        /// <summary>
        /// Reads a rotation value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        private static Rotation ReadRotation(this Packet packet)
        {
            Rotation result;
            result.Sin = packet.ReadSingle();
            result.Cos = packet.ReadSingle();
            return result;
        }
    }

    /// <summary>
    /// Hashing methods for transforms.
    /// </summary>
    internal static class HasherTransformExtensions
    {
        /// <summary>
        /// Put the specified value to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public static Hasher Put(this Hasher hasher, WorldTransform value)
        {
            return hasher
                .Put(value.Translation)
                .Put(value.Rotation.Sin)
                .Put(value.Rotation.Cos);
        }
    }
}
