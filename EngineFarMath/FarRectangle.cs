using System.Globalization;
using Engine.Math;
using Microsoft.Xna.Framework;

namespace Engine.FarMath
{
    /// <summary>
    /// A "far rectangle", a rectangle based on <see cref="FarValue"/>s. It is otherwise
    /// pretty much equivalent to the <see cref="Rectangle"/> from the XNA framework.
    /// </summary>
    public struct FarRectangle
    {
        #region Constants

        /// <summary>
        /// An empty <see cref="FarRectangle"/> at the origin.
        /// </summary>
        public static FarRectangle Empty
        {
            get { return ConstEmpty; }
        }

        /// <summary>
        /// Keep as private field to avoid manipulation.
        /// </summary>
        private static readonly FarRectangle ConstEmpty = new FarRectangle(0, 0, 0, 0);

        #endregion

        #region Properties

        /// <summary>
        /// Returns the x-coordinate of the left side of the rectangle.
        /// </summary>
        public FarValue Left
        {
            get { return X; }
        }

        /// <summary>
        /// Returns the y-coordinate of the top of the rectangle.
        /// </summary>
        public FarValue Top
        {
            get { return Y; }
        }

        /// <summary>
        /// Returns the x-coordinate of the right side of the rectangle.
        /// </summary>
        public FarValue Right
        {
            get { return X + Width; }
        }

        /// <summary>
        /// Returns the y-coordinate of the bottom of the rectangle.
        /// </summary>
        public FarValue Bottom
        {
            get { return Y + Height; }
        }

        /// <summary>
        /// Gets the Point that specifies the center of the rectangle.
        /// </summary>
        public FarPosition Center
        {
            get
            {
                FarPosition v;
                v.X = X + Width / 2;
                v.Y = Y + Height / 2;
                return v;
            }
        }

        /// <summary>
        /// Gets or sets the upper-left value of the <see cref="FarRectangle"/>.
        /// </summary>
        public FarPosition Location
        {
            get
            {
                FarPosition v;
                v.X = X;
                v.Y = Y;
                return v;
            }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="FarRectangle"/> is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return Width == 0f && Height == 0f && X == 0 && Y == 0; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Specifies the x-coordinate of the rectangle.
        /// </summary>
        public FarValue X;

        /// <summary>
        /// Specifies the y-coordinate of the rectangle.
        /// </summary>
        public FarValue Y;

        /// <summary>
        /// Specifies the width of the rectangle.
        /// </summary>
        public float Width;

        /// <summary>
        /// Specifies the height of the rectangle.
        /// </summary>
        public float Height;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FarRectangle"/> struct.
        /// </summary>
        /// <param name="x">The x-coordinate of the rectangle.</param>
        /// <param name="y">The y-coordinate of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public FarRectangle(FarValue x, FarValue y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FarRectangle"/> struct.
        /// </summary>
        /// <param name="x">The x-coordinate of the rectangle.</param>
        /// <param name="y">The y-coordinate of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public FarRectangle(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FarRectangle"/> struct.
        /// </summary>
        /// <param name="x">The x-coordinate of the rectangle.</param>
        /// <param name="y">The y-coordinate of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public FarRectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Determines whether this <see cref="FarRectangle"/> contains the specified <see cref="FarPosition"/>.
        /// </summary>
        /// <param name="value">The <see cref="FarPosition"/> to evaluate.</param>
        /// <returns>
        ///   <c>true</c> if the rectangle contains the specified value; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(FarPosition value)
        {
            return X <= value.X &&
                   value.X < (X + Width) &&
                   Y <= value.Y &&
                   value.Y < (Y + Height);
        }

        /// <summary>
        /// Determines whether this <see cref="FarRectangle"/> contains the specified <see cref="FarPosition"/>.
        /// </summary>
        /// <param name="value">The <see cref="FarPosition"/> to evaluate.</param>
        /// <param name="result">On exit, is true if this <see cref="FarRectangle"/> entirely contains the specified
        /// <see cref="FarPosition"/>, or false if not.</param>
        public void Contains(ref FarPosition value, out bool result)
        {
            result = X <= value.X &&
                     value.X < (X + Width) &&
                     Y <= value.Y &&
                     value.Y < (Y + Height);
        }

        /// <summary>
        /// Determines whether this <see cref="FarRectangle"/> entirely contains a specified <see cref="FarRectangle"/>.
        /// </summary>
        /// <param name="value">The <see cref="FarRectangle"/> to evaluate.</param>
        /// <returns>
        ///   <c>true</c> if the rectangle contains the specified value; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(FarRectangle value)
        {
            return X <= value.X &&
                   (value.X + value.Width) <= (X + Width) &&
                   Y <= value.Y &&
                   (value.Y + value.Height) <= (Y + Height);
        }

        /// <summary>
        /// Determines whether this <see cref="FarRectangle"/> entirely contains a specified <see cref="FarRectangle"/>.
        /// </summary>
        /// <param name="value">The <see cref="FarRectangle"/> to evaluate.</param>
        /// <param name="result">On exit, is true if this <see cref="FarRectangle"/> entirely contains the specified
        /// <see cref="FarRectangle"/>, or false if not.</param>
        public void Contains(ref FarRectangle value, out bool result)
        {
            result = X <= value.X &&
                     (value.X + value.Width) <= (X + Width) &&
                     Y <= value.Y &&
                     (value.Y + value.Height) <= (Y + Height);
        }

        /// <summary>
        /// Determines whether this <see cref="FarRectangle"/> contains a specified point represented
        //  by its x- and y-coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the specified point.</param>
        /// <param name="y">The y-coordinate of the specified point.</param>
        /// <returns>
        ///   <c>true</c> if the rectangle contains the specified point; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(float x, float y)
        {
            return X <= x &&
                   x < (X + Width) &&
                   Y <= y &&
                   y < (Y + Height);
        }

        /// <summary>
        /// Determines whether this <see cref="FarRectangle"/> contains a specified Vector2.
        /// </summary>
        /// <param name="value">The Vector2 to evaluate.</param>
        /// <returns>
        ///   <c>true</c> if the rectangle contains the specified value; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(Vector2 value)
        {
            return X <= value.X &&
                   value.X < (X + Width) &&
                   Y <= value.Y &&
                   value.Y < (Y + Height);
        }

        /// <summary>
        /// Determines whether this <see cref="FarRectangle"/> contains a specified Vector2.
        /// </summary>
        /// <param name="value">The Vector2 to evaluate.</param>
        /// <param name="result">true if the specified Vector2 is contained within this <see cref="FarRectangle"/>;
        /// false otherwise.</param>
        public void Contains(ref Vector2 value, out bool result)
        {
            result = X <= value.X &&
                     value.X < (X + Width) &&
                     Y <= value.Y &&
                     value.Y < (Y + Height);
        }

        /// <summary>
        /// Pushes the edges of the <see cref="FarRectangle"/> out by the horizontal and vertical values
        /// specified.
        /// </summary>
        /// <param name="horizontalAmount">Value to push the sides out by.</param>
        /// <param name="verticalAmount">Value to push the top and bottom out by.</param>
        public void Inflate(float horizontalAmount, float verticalAmount)
        {
            X -= horizontalAmount;
            Y -= verticalAmount;
            Width += horizontalAmount + horizontalAmount;
            Height += verticalAmount + verticalAmount;
        }

        /// <summary>
        /// Determines whether a specified <see cref="FarRectangle"/> intersects with this <see cref="FarRectangle"/>.
        /// </summary>
        /// <param name="other">The <see cref="FarRectangle"/> to evaluate.</param>
        /// <returns>
        ///   <c>true</c> if the rectangles intersect; otherwise, <c>false</c>.
        /// </returns>
        public bool Intersects(FarRectangle other)
        {
            return X < (other.X + other.Width) &&
                   other.X < (X + Width) &&
                   Y < (other.Y + other.Height) &&
                   other.Y < (Y + Height);
        }

        /// <summary>
        /// Determines whether a specified <see cref="FarRectangle"/> intersects with this <see cref="FarRectangle"/>.
        /// </summary>
        /// <param name="other">The <see cref="FarRectangle"/> to evaluate.</param>
        /// <param name="result">true if the specified <see cref="FarRectangle"/> intersects with this one;
        /// false otherwise.</param>
        public void Intersects(FarRectangle other, out bool result)
        {
            result = X < (other.X + other.Width) &&
                     other.X < (X + Width) &&
                     Y < (other.Y + other.Height) &&
                     other.Y < (Y + Height);
        }

        /// <summary>
        /// Changes the position of the <see cref="FarRectangle"/>.
        /// </summary>
        /// <param name="amount">The value to adjust the position of the <see cref="FarRectangle"/> by.</param>
        public void Offset(FarPosition amount)
        {
            X += amount.X;
            Y += amount.Y;
        }

        /// <summary>
        /// Changes the position of the <see cref="FarRectangle"/>.
        /// </summary>
        /// <param name="amount">The value to adjust the position of the <see cref="FarRectangle"/> by.</param>
        public void Offset(Vector2 amount)
        {
            X += amount.X;
            Y += amount.Y;
        }

        /// <summary>
        /// Changes the position of the <see cref="FarRectangle"/>.
        /// </summary>
        /// <param name="offsetX">Change in the x-position.</param>
        /// <param name="offsetY">Change in the y-position.</param>
        public void Offset(float offsetX, float offsetY)
        {
            X += offsetX;
            Y += offsetY;
        }

        /// <summary>
        /// Creates a <see cref="FarRectangle"/> defining the area where one rectangle overlaps with another
        /// rectangle.
        /// </summary>
        /// <param name="value1">The first <see cref="FarRectangle"/> to compare.</param>
        /// <param name="value2">The second <see cref="FarRectangle"/> to compare.</param>
        /// <returns></returns>
        public static FarRectangle Intersect(FarRectangle value1, FarRectangle value2)
        {
            var right1 = value1.X + value1.Width;
            var right2 = value2.X + value2.Width;
            var bottom1 = value1.Y + value1.Height;
            var bottom2 = value2.Y + value2.Height;
            var left = (value1.X > value2.X) ? value1.X : value2.X;
            var top = (value1.Y > value2.Y) ? value1.Y : value2.Y;
            var right = (right1 < right2) ? right1 : right2;
            var bottom = (bottom1 < bottom2) ? bottom1 : bottom2;

            FarRectangle result;
            if ((right > left) && (bottom > top))
            {
                result.X = left;
                result.Y = top;
                result.Width = (float)(right - left);
                result.Height = (float)(bottom - top);
            }
            else
            {
                result.X = 0;
                result.Y = 0;
                result.Width = 0;
                result.Height = 0;
            }
            return result;
        }

        /// <summary>
        /// Creates a <see cref="FarRectangle"/> defining the area where one rectangle overlaps with another
        /// rectangle.
        /// </summary>
        /// <param name="value1">The first <see cref="FarRectangle"/> to compare.</param>
        /// <param name="value2">The second <see cref="FarRectangle"/> to compare.</param>
        /// <param name="result">The area where the two first parameters overlap.</param>
        public static void Intersect(ref FarRectangle value1, ref FarRectangle value2, out FarRectangle result)
        {
            var right1 = value1.X + value1.Width;
            var right2 = value2.X + value2.Width;
            var bottom1 = value1.Y + value1.Height;
            var bottom2 = value2.Y + value2.Height;
            var left = (value1.X > value2.X) ? value1.X : value2.X;
            var top = (value1.Y > value2.Y) ? value1.Y : value2.Y;
            var right = (right1 < right2) ? right1 : right2;
            var bottom = (bottom1 < bottom2) ? bottom1 : bottom2;

            if ((right > left) && (bottom > top))
            {
                result.X = left;
                result.Y = top;
                result.Width = (float)(right - left);
                result.Height = (float)(bottom - top);
            }
            else
            {
                result.X = 0;
                result.Y = 0;
                result.Width = 0;
                result.Height = 0;
            }
        }

        /// <summary>
        /// Creates a new <see cref="FarRectangle"/> that exactly contains two other rectangles.
        /// </summary>
        /// <param name="value1">The first <see cref="FarRectangle"/> to contain.</param>
        /// <param name="value2">The second <see cref="FarRectangle"/> to contain.</param>
        /// <returns>The <see cref="FarRectangle"/> that must be the union of the first two rectangles.</returns>
        public static FarRectangle Union(FarRectangle value1, FarRectangle value2)
        {
            var right1 = value1.X + value1.Width;
            var right2 = value2.X + value2.Width;
            var bottom1 = value1.Y + value1.Height;
            var bottom2 = value2.Y + value2.Height;
            var left = (value1.X < value2.X) ? value1.X : value2.X;
            var top = (value1.Y < value2.Y) ? value1.Y : value2.Y;
            var right = (right1 > right2) ? right1 : right2;
            var bottom = (bottom1 > bottom2) ? bottom1 : bottom2;

            FarRectangle result;
            result.X = left;
            result.Y = top;
            result.Width = (float)(right - left);
            result.Height = (float)(bottom - top);
            return result;
        }

        /// <summary>
        /// Creates a new <see cref="FarRectangle"/> that exactly contains two other rectangles.
        /// </summary>
        /// <param name="value1">The first <see cref="FarRectangle"/> to contain.</param>
        /// <param name="value2">The second <see cref="FarRectangle"/> to contain.</param>
        /// <param name="result">The <see cref="FarRectangle"/> that must be the union of the first two rectangles.</param>
        public static void Union(ref FarRectangle value1, ref FarRectangle value2, out FarRectangle result)
        {
            var right1 = value1.X + value1.Width;
            var right2 = value2.X + value2.Width;
            var bottom1 = value1.Y + value1.Height;
            var bottom2 = value2.Y + value2.Height;
            var left = (value1.X < value2.X) ? value1.X : value2.X;
            var top = (value1.Y < value2.Y) ? value1.Y : value2.Y;
            var right = (right1 > right2) ? right1 : right2;
            var bottom = (bottom1 > bottom2) ? bottom1 : bottom2;

            result.X = left;
            result.Y = top;
            result.Width = (float)(right - left);
            result.Height = (float)(bottom - top);
        }

        #endregion

        #region Operators

        /// <summary>
        /// Compares the two specified <see cref="FarRectangle"/>s for equality.
        /// </summary>
        /// <param name="a">Source rectangle.</param>
        /// <param name="b">Source rectangle.</param>
        /// <returns>
        ///   <c>true</c> if the rectangles are equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(FarRectangle a, FarRectangle b)
        {
            return a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
        }

        /// <summary>
        /// Compares the two specified <see cref="FarRectangle"/>s for unequality.
        /// </summary>
        /// <param name="a">Source rectangle.</param>
        /// <param name="b">Source rectangle.</param>
        /// <returns>
        ///   <c>true</c> if the rectangles are different; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(FarRectangle a, FarRectangle b)
        {
            return a.X != b.X || a.Y != b.Y || a.Width != b.Width || a.Height != b.Height;
        }

        #endregion

        #region Type Conversion (Casts)

        /// <summary>
        /// Performs an implicit conversion from <see cref="Microsoft.Xna.Framework.Rectangle"/> to <see cref="Engine.FarMath.FarRectangle"/>.
        /// </summary>
        /// <param name="rectangle">The <see cref="Microsoft.Xna.Framework.Rectangle"/> to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator FarRectangle(Rectangle rectangle)
        {
            FarRectangle r;
            r.X = rectangle.X;
            r.Y = rectangle.Y;
            r.Width = rectangle.Width;
            r.Height = rectangle.Height;
            return r;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Engine.FarMath.FarRectangle"/> to <see cref="Microsoft.Xna.Framework.Rectangle"/>.
        /// </summary>
        /// <param name="rectangle">The <see cref="Engine.FarMath.FarRectangle"/> to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator Rectangle(FarRectangle rectangle)
        {
            Rectangle r;
            r.X = (int)rectangle.X;
            r.Y = (int)rectangle.Y;
            r.Width = (int)rectangle.Width;
            r.Height = (int)rectangle.Height;
            return r;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Engine.Math.RectangleF"/> to <see cref="Engine.FarMath.FarRectangle"/>.
        /// </summary>
        /// <param name="rectangle">The <see cref="Engine.Math.RectangleF"/> to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator FarRectangle(RectangleF rectangle)
        {
            FarRectangle r;
            r.X = rectangle.X;
            r.Y = rectangle.Y;
            r.Width = rectangle.Width;
            r.Height = rectangle.Height;
            return r;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Engine.FarMath.FarRectangle"/> to <see cref="Microsoft.Xna.Framework.Rectangle"/>.
        /// </summary>
        /// <param name="rectangle">The <see cref="Engine.FarMath.FarRectangle"/> to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator RectangleF(FarRectangle rectangle)
        {
            RectangleF r;
            r.X = (float)rectangle.X;
            r.Y = (float)rectangle.Y;
            r.Width = rectangle.Width;
            r.Height = rectangle.Height;
            return r;
        }

        #endregion

        #region Equality Overrides

        /// <summary>
        /// Compares the two specified <see cref="FarRectangle"/>s for equality.
        /// </summary>
        /// <param name="other">The other <see cref="FarRectangle"/>.</param>
        /// <returns>
        ///   <c>true</c> if the <see cref="FarRectangle"/>s are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(FarRectangle other)
        {
            return (X == other.X) &&
                   (Y == other.Y) &&
                   (Width == other.Width) &&
                   (Height == other.Height);
        }

        /// <summary>
        /// Compares the two specified <see cref="FarRectangle"/>s for equality.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified value is a <see cref="FarRectangle"/> and the <see cref="FarRectangle"/>s
        /// are equal; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return (obj is FarRectangle) && Equals((FarRectangle)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode() + Width.GetHashCode() + Height.GetHashCode();
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
            return string.Format(CultureInfo.InvariantCulture, "{{X:{0} Y:{1} Width:{2} Height:{3}}}", X, Y, Width, Height);
        }

        #endregion
    }
}
