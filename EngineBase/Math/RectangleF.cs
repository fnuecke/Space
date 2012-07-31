using System.Globalization;
using Microsoft.Xna.Framework;

namespace Engine.Math
{
    /// <summary>
    /// A floating point rectangle, that is otherwise equivalent to the integer
    /// Rectangle from the XNA framework.
    /// </summary>
    public struct RectangleF
    {
        #region Constants

        /// <summary>
        /// An empty <see cref="RectangleF"/> at the origin.
        /// </summary>
        public static readonly RectangleF Empty = new RectangleF(0, 0, 0, 0);

        #endregion

        #region Properties

        /// <summary>
        /// Returns the x-coordinate of the left side of the rectangle.
        /// </summary>
        public float Left
        {
            get { return X; }
        }

        /// <summary>
        /// Returns the y-coordinate of the top of the rectangle.
        /// </summary>
        public float Top
        {
            get { return Y; }
        }

        /// <summary>
        /// Returns the x-coordinate of the right side of the rectangle.
        /// </summary>
        public float Right
        {
            get { return X + Width; }
        }

        /// <summary>
        /// Returns the y-coordinate of the bottom of the rectangle.
        /// </summary>
        public float Bottom
        {
            get { return Y + Height; }
        }

        /// <summary>
        /// Gets the Point that specifies the center of the rectangle.
        /// </summary>
        public Vector2 Center
        {
            get
            {
                Vector2 v;
                v.X = X + Width / 2;
                v.Y = Y + Height / 2;
                return v;
            }
        }

        /// <summary>
        /// Gets or sets the upper-left value of the <see cref="RectangleF"/>.
        /// </summary>
        public Vector2 Location
        {
            get
            {
                Vector2 v;
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
        /// Gets a value that indicates whether the <see cref="RectangleF"/> is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return Width == 0 && Height == 0 && X == 0 && Y == 0; }
        }

        #endregion

        #region Fields
        
        /// <summary>
        /// Specifies the x-coordinate of the rectangle.
        /// </summary>
        public float X;

        /// <summary>
        /// Specifies the y-coordinate of the rectangle.
        /// </summary>
        public float Y;

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
        /// Initializes a new instance of the <see cref="RectangleF"/> struct.
        /// </summary>
        /// <param name="x">The x-coordinate of the rectangle.</param>
        /// <param name="y">The y-coordinate of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Determines whether this <see cref="RectangleF"/> entirely contains a specified <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="value">The <see cref="RectangleF"/> to evaluate.</param>
        /// <returns>
        ///   <c>true</c> if the rectangle contains the specified value; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(RectangleF value)
        {
            return X <= value.X &&
                   (value.X + value.Width) <= (X + Width) &&
                   Y <= value.Y &&
                   (value.Y + value.Height) <= (Y + Height);
        }

        /// <summary>
        /// Determines whether this <see cref="RectangleF"/> contains a specified Vector2.
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
        /// Determines whether this <see cref="RectangleF"/> contains a specified point represented
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
        /// Determines whether this <see cref="RectangleF"/> entirely contains a specified <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="value">The <see cref="RectangleF"/> to evaluate.</param>
        /// <param name="result">On exit, is true if this <see cref="RectangleF"/> entirely contains the specified
        /// <see cref="RectangleF"/>, or false if not.</param>
        public void Contains(ref RectangleF value, out bool result)
        {
            result = X <= value.X &&
                     (value.X + value.Width) <= (X + Width) &&
                     Y <= value.Y &&
                     (value.Y + value.Height) <= (Y + Height);
        }

        /// <summary>
        /// Determines whether this <see cref="RectangleF"/> contains a specified Vector2.
        /// </summary>
        /// <param name="value">The Vector2 to evaluate.</param>
        /// <param name="result">true if the specified Vector2 is contained within this <see cref="RectangleF"/>;
        /// false otherwise.</param>
        public void Contains(ref Vector2 value, out bool result)
        {
            result = X <= value.X &&
                     value.X < (X + Width) &&
                     Y <= value.Y &&
                     value.Y < (Y + Height);
        }

        /// <summary>
        /// Pushes the edges of the <see cref="RectangleF"/> out by the horizontal and vertical values
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
        /// Determines whether a specified <see cref="RectangleF"/> intersects with this <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="other">The <see cref="RectangleF"/> to evaluate.</param>
        /// <returns>
        ///   <c>true</c> if the rectangles intersect; otherwise, <c>false</c>.
        /// </returns>
        public bool Intersects(RectangleF other)
        {
            return X < (other.X + other.Width) &&
                   other.X < (X + Width) &&
                   Y < (other.Y + other.Height) &&
                   other.Y < (Y + Height);
        }

        /// <summary>
        /// Determines whether a specified <see cref="RectangleF"/> intersects with this <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="other">The <see cref="RectangleF"/> to evaluate.</param>
        /// <param name="result">true if the specified <see cref="RectangleF"/> intersects with this one;
        /// false otherwise.</param>
        public void Intersects(RectangleF other, out bool result)
        {
            result = X < (other.X + other.Width) &&
                     other.X < (X + Width) &&
                     Y < (other.Y + other.Height) &&
                     other.Y < (Y + Height);
        }

        /// <summary>
        /// Changes the position of the <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="amount">The values to adjust the position of the <see cref="RectangleF"/> by.</param>
        public void Offset(Vector2 amount)
        {
            X += amount.X;
            Y += amount.Y;
        }

        /// <summary>
        /// Changes the position of the <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="offsetX">Change in the x-position.</param>
        /// <param name="offsetY">Change in the y-position.</param>
        public void Offset(float offsetX, float offsetY)
        {
            X += offsetX;
            Y += offsetY;
        }

        /// <summary>
        /// Creates a <see cref="RectangleF"/> defining the area where one rectangle overlaps with another
        /// rectangle.
        /// </summary>
        /// <param name="value1">The first <see cref="RectangleF"/> to compare.</param>
        /// <param name="value2">The second <see cref="RectangleF"/> to compare.</param>
        /// <returns></returns>
        public static RectangleF Intersect(RectangleF value1, RectangleF value2)
        {
            var right1 = value1.X + value1.Width;
            var right2 = value2.X + value2.Width;
            var bottom1 = value1.Y + value1.Height;
            var bottom2 = value2.Y + value2.Height;
            var left = (value1.X > value2.X) ? value1.X : value2.X;
            var top = (value1.Y > value2.Y) ? value1.Y : value2.Y;
            var right = (right1 < right2) ? right1 : right2;
            var bottom = (bottom1 < bottom2) ? bottom1 : bottom2;

            RectangleF result;
            if ((right > left) && (bottom > top))
            {
                result.X = left;
                result.Y = top;
                result.Width = right - left;
                result.Height = bottom - top;
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
        /// Creates a <see cref="RectangleF"/> defining the area where one rectangle overlaps with another
        /// rectangle.
        /// </summary>
        /// <param name="value1">The first <see cref="RectangleF"/> to compare.</param>
        /// <param name="value2">The second <see cref="RectangleF"/> to compare.</param>
        /// <param name="result">The area where the two first parameters overlap.</param>
        public static void Intersect(ref RectangleF value1, ref RectangleF value2, out RectangleF result)
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
                result.Width = right - left;
                result.Height = bottom - top;
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
        /// Creates a new <see cref="RectangleF"/> that exactly contains two other rectangles.
        /// </summary>
        /// <param name="value1">The first <see cref="RectangleF"/> to contain.</param>
        /// <param name="value2">The second <see cref="RectangleF"/> to contain.</param>
        /// <returns>The <see cref="RectangleF"/> that must be the union of the first two rectangles.</returns>
        public static RectangleF Union(RectangleF value1, RectangleF value2)
        {
            var right1 = value1.X + value1.Width;
            var right2 = value2.X + value2.Width;
            var bottom1 = value1.Y + value1.Height;
            var bottom2 = value2.Y + value2.Height;
            var left = (value1.X < value2.X) ? value1.X : value2.X;
            var top = (value1.Y < value2.Y) ? value1.Y : value2.Y;
            var right = (right1 > right2) ? right1 : right2;
            var bottom = (bottom1 > bottom2) ? bottom1 : bottom2;

            RectangleF result;
            result.X = left;
            result.Y = top;
            result.Width = right - left;
            result.Height = bottom - top;
            return result;
        }

        /// <summary>
        /// Creates a new <see cref="RectangleF"/> that exactly contains two other rectangles.
        /// </summary>
        /// <param name="value1">The first <see cref="RectangleF"/> to contain.</param>
        /// <param name="value2">The second <see cref="RectangleF"/> to contain.</param>
        /// <param name="result">The <see cref="RectangleF"/> that must be the union of the first two rectangles.</param>
        public static void Union(ref RectangleF value1, ref RectangleF value2, out RectangleF result)
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
            result.Width = right - left;
            result.Height = bottom - top;
        }

        #endregion

        #region Operators

        /// <summary>
        /// Compares two rectangles for equality.
        /// </summary>
        /// <param name="a">Source rectangle.</param>
        /// <param name="b">Source rectangle.</param>
        /// <returns>
        ///   <c>true</c> if the rectangles are equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(RectangleF a, RectangleF b)
        {
            return a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
        }

        /// <summary>
        /// Compares two rectangles for inequality.
        /// </summary>
        /// <param name="a">Source rectangle.</param>
        /// <param name="b">Source rectangle.</param>
        /// <returns>
        ///   <c>true</c> if the rectangles are inequal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(RectangleF a, RectangleF b)
        {
            return a.X != b.X || a.Y != b.Y || a.Width != b.Width || a.Height != b.Height;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Microsoft.Xna.Framework.Rectangle"/> to <see cref="Engine.Math.RectangleF"/>.
        /// </summary>
        /// <param name="rectangle">The rectangle to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator RectangleF(Rectangle rectangle)
        {
            RectangleF r;
            r.X = rectangle.X;
            r.Y = rectangle.Y;
            r.Width = rectangle.Width;
            r.Height = rectangle.Height;
            return r;
        }

        #endregion

        #region Equality Overrides
        
        /// <summary>
        /// Compares two rectangles for equality.
        /// </summary>
        /// <param name="other">The other rectangle.</param>
        /// <returns>
        ///   <c>true</c> if the rectangles are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(RectangleF other)
        {
            return (X == other.X) &&
                   (Y == other.Y) &&
                   (Width == other.Width) &&
                   (Height == other.Height);
        }

        /// <summary>
        /// Compares two rectangles for equality.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified value is a <see cref="RectangleF"/> and the rectangles
        /// are equal; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return (obj is Rectangle) && Equals((Rectangle)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
// ReSharper disable NonReadonlyFieldInGetHashCode XNA's Rectangle does the same thing
            return X.GetHashCode() + Y.GetHashCode() + Width.GetHashCode() + Height.GetHashCode();
// ReSharper restore NonReadonlyFieldInGetHashCode
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
            var currentCulture = CultureInfo.CurrentCulture;
            return string.Format(CultureInfo.CurrentCulture, "{{X:{0} Y:{1} Width:{2} Height:{3}}}", X.ToString(CultureInfo.CurrentCulture), Y.ToString(CultureInfo.CurrentCulture), Width.ToString(CultureInfo.CurrentCulture), Height.ToString(CultureInfo.CurrentCulture));
        }

        #endregion

        public Vector2 lowerBound
        {
            get
            {
                Vector2 v;
                v.X = X;
                v.Y = Y;
                return v;
            }
        }

        public Vector2 upperBound
        {
            get
            {
                Vector2 v;
                v.X = X + Width;
                v.Y = Y + Height;
                return v;
            }
        }

        public float GetPerimeter()
        {
            return Width * Height;
        }

    }
}
