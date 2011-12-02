using System;

namespace Engine.Math
{
    /// <summary>
    /// Represents a fixed-point number.
    /// </summary>
    /// <see cref="http://stackoverflow.com/questions/605124/fixed-point-math-in-c"/>
    public struct Fixed
    {
        #region Constants

        public static readonly Fixed PI = Fixed.Create(System.Math.PI);

        #endregion

        #region Private constants

        private const int SHIFT_AMOUNT = 12; //12 is 4096
        private const long One = 1 << SHIFT_AMOUNT;
        private const int OneI = 1 << SHIFT_AMOUNT;
        private static Fixed OneF = Fixed.Create(1, true);
        private static Fixed InternalPI = Fixed.Create(12868, false); // PI x 2^12
        private static Fixed TwoPIF = InternalPI * 2; // radian equivalent of 360 degrees
        private static Fixed PIOver180F = InternalPI / (Fixed)180; // PI / 180

        #endregion

        // Actual value holder of this instance.
        public long RawValue;

        #region Constructors

        public static Fixed Create(long startingRawValue, bool useMultiple)
        {
            Fixed fInt;
            fInt.RawValue = startingRawValue;
            if (useMultiple)
                fInt.RawValue = fInt.RawValue << SHIFT_AMOUNT;
            return fInt;
        }

        public static Fixed Create(double value)
        {
            Fixed fInt;
            value *= (double)One;
            fInt.RawValue = (int)System.Math.Round(value);
            return fInt;
        }

        #endregion

        #region Conversions

        public int IntValue
        {
            get { return (int)(this.RawValue >> SHIFT_AMOUNT); }
        }

        public double DoubleValue
        {
            get { return (double)this.RawValue / (double)One; }
        }

        #endregion

        #region FromParts

        /// <summary>
        /// Create a fixed-int number from parts.  For example, to create 1.5 pass in 1 and 500.
        /// </summary>
        /// <param name="PreDecimal">The number above the decimal.  For 1.5, this would be 1.</param>
        /// <param name="PostDecimal">The number below the decimal, to three digits.  
        /// For 1.5, this would be 500. For 1.005, this would be 5.</param>
        /// <returns>A fixed-int representation of the number parts</returns>
        public static Fixed FromParts(int preDecimal, int postDecimal)
        {
            Fixed f = Fixed.Create(preDecimal, true);
            if (postDecimal != 0)
                f.RawValue += (Fixed.Create(postDecimal) / 1000).RawValue;

            return f;
        }

        #endregion

        #region *

        public static Fixed operator *(Fixed one, Fixed other)
        {
            Fixed fInt;
            fInt.RawValue = (one.RawValue * other.RawValue) >> SHIFT_AMOUNT;
            return fInt;
        }

        public static Fixed operator *(Fixed one, int multi)
        {
            return one * (Fixed)multi;
        }

        public static Fixed operator *(int scalar, Fixed one)
        {
            return one * (Fixed)scalar;
        }

        #endregion

        #region /

        public static Fixed operator /(Fixed one, Fixed other)
        {
            Fixed fInt;
            fInt.RawValue = (one.RawValue << SHIFT_AMOUNT) / (other.RawValue);
            return fInt;
        }

        public static Fixed operator /(Fixed one, int divisor)
        {
            return one / (Fixed)divisor;
        }

        public static Fixed operator /(int divisor, Fixed one)
        {
            return (Fixed)divisor / one;
        }

        #endregion

        #region %

        public static Fixed operator %(Fixed one, Fixed other)
        {
            Fixed fInt;
            fInt.RawValue = (one.RawValue) % (other.RawValue);
            return fInt;
        }

        public static Fixed operator %(Fixed one, int divisor)
        {
            return one % (Fixed)divisor;
        }

        public static Fixed operator %(int divisor, Fixed one)
        {
            return (Fixed)divisor % one;
        }

        #endregion

        #region +

        public static Fixed operator +(Fixed one, Fixed other)
        {
            Fixed fInt;
            fInt.RawValue = one.RawValue + other.RawValue;
            return fInt;
        }

        public static Fixed operator +(Fixed one, int other)
        {
            return one + (Fixed)other;
        }

        public static Fixed operator +(int other, Fixed one)
        {
            return one + (Fixed)other;
        }

        #endregion

        #region -

        public Fixed Inverse
        {
            get { return Fixed.Create(-this.RawValue, false); }
        }

        public static Fixed operator -(Fixed one)
        {
            return one.Inverse;
        }

        public static Fixed operator -(Fixed one, Fixed other)
        {
            Fixed fInt;
            fInt.RawValue = one.RawValue - other.RawValue;
            return fInt;
        }

        public static Fixed operator -(Fixed one, int other)
        {
            return one - (Fixed)other;
        }

        public static Fixed operator -(int other, Fixed one)
        {
            return (Fixed)other - one;
        }

        #endregion

        #region ==

        public static bool operator ==(Fixed one, Fixed other)
        {
            return one.RawValue == other.RawValue;
        }

        public static bool operator ==(Fixed one, int other)
        {
            return one == (Fixed)other;
        }

        public static bool operator ==(int other, Fixed one)
        {
            return (Fixed)other == one;
        }

        #endregion

        #region !=

        public static bool operator !=(Fixed one, Fixed other)
        {
            return one.RawValue != other.RawValue;
        }

        public static bool operator !=(Fixed one, int other)
        {
            return one != (Fixed)other;
        }

        public static bool operator !=(int other, Fixed one)
        {
            return (Fixed)other != one;
        }

        #endregion

        #region >=

        public static bool operator >=(Fixed one, Fixed other)
        {
            return one.RawValue >= other.RawValue;
        }

        public static bool operator >=(Fixed one, int other)
        {
            return one >= (Fixed)other;
        }

        public static bool operator >=(int other, Fixed one)
        {
            return (Fixed)other >= one;
        }

        #endregion

        #region <=

        public static bool operator <=(Fixed one, Fixed other)
        {
            return one.RawValue <= other.RawValue;
        }

        public static bool operator <=(Fixed one, int other)
        {
            return one <= (Fixed)other;
        }

        public static bool operator <=(int other, Fixed one)
        {
            return (Fixed)other <= one;
        }

        #endregion

        #region >

        public static bool operator >(Fixed one, Fixed other)
        {
            return one.RawValue > other.RawValue;
        }

        public static bool operator >(Fixed one, int other)
        {
            return one > (Fixed)other;
        }

        public static bool operator >(int other, Fixed one)
        {
            return (Fixed)other > one;
        }

        #endregion

        #region <

        public static bool operator <(Fixed one, Fixed other)
        {
            return one.RawValue < other.RawValue;
        }

        public static bool operator <(Fixed one, int other)
        {
            return one < (Fixed)other;
        }

        public static bool operator <(int other, Fixed one)
        {
            return (Fixed)other < one;
        }

        #endregion

        #region Casting

        public static explicit operator int(Fixed src)
        {
            return (int)(src.RawValue >> SHIFT_AMOUNT);
        }

        public static explicit operator Fixed(int src)
        {
            return Fixed.Create(src, true);
        }

        public static explicit operator Fixed(long src)
        {
            return Fixed.Create(src, true);
        }

        public static explicit operator Fixed(ulong src)
        {
            return Fixed.Create((long)src, true);
        }

        #endregion

        #region Bitshifts

        public static Fixed operator <<(Fixed one, int amount)
        {
            return Fixed.Create(one.RawValue << amount, false);
        }

        public static Fixed operator >>(Fixed one, int amount)
        {
            return Fixed.Create(one.RawValue >> amount, false);
        }

        #endregion

        #region Sqrt

        public static Fixed Sqrt(Fixed f, int numberOfIterations)
        {
            if (f.RawValue < 0) //NaN in Math.Sqrt
                throw new ArithmeticException("Input Error");
            if (f.RawValue == 0)
                return (Fixed)0;
            Fixed k = f + Fixed.OneF >> 1;
            for (int i = 0; i < numberOfIterations; i++)
                k = (k + (f / k)) >> 1;

            if (k.RawValue < 0)
                throw new ArithmeticException("Overflow");
            else
                return k;
        }

        public static Fixed Sqrt(Fixed f)
        {
            byte numberOfIterations = 8;
            if (f.RawValue > 0x64000)
                numberOfIterations = 12;
            if (f.RawValue > 0x3e8000)
                numberOfIterations = 16;
            return Sqrt(f, numberOfIterations);
        }

        #endregion

        #region Sin

        public static Fixed Sin(Fixed f)
        {
            Fixed j = (Fixed)0;
            for (; f < 0; f += Fixed.Create(25736, false)) ;
            if (f > Fixed.Create(25736, false))
                f %= Fixed.Create(25736, false);
            Fixed k = (f * Fixed.Create(10, false)) / Fixed.Create(714, false);
            if (f != 0 && f != Fixed.Create(6434, false) && f != Fixed.Create(12868, false) &&
                f != Fixed.Create(19302, false) && f != Fixed.Create(25736, false))
                j = (f * Fixed.Create(100, false)) / Fixed.Create(714, false) - k * Fixed.Create(10, false);
            if (k <= Fixed.Create(90, false))
                return sin_lookup(k, j);
            if (k <= Fixed.Create(180, false))
                return sin_lookup(Fixed.Create(180, false) - k, j);
            if (k <= Fixed.Create(270, false))
                return sin_lookup(k - Fixed.Create(180, false), j).Inverse;
            else
                return sin_lookup(Fixed.Create(360, false) - k, j).Inverse;
        }

        private static Fixed sin_lookup(Fixed i, Fixed j)
        {
            if (j > 0 && j < Fixed.Create(10, false) && i < Fixed.Create(90, false))
                return Fixed.Create(SIN_TABLE[i.RawValue], false) +
                    ((Fixed.Create(SIN_TABLE[i.RawValue + 1], false) - Fixed.Create(SIN_TABLE[i.RawValue], false)) /
                    Fixed.Create(10, false)) * j;
            else
                return Fixed.Create(SIN_TABLE[i.RawValue], false);
        }

        private static int[] SIN_TABLE = {
            0, 71, 142, 214, 285, 357, 428, 499, 570, 641, 
            711, 781, 851, 921, 990, 1060, 1128, 1197, 1265, 1333, 
            1400, 1468, 1534, 1600, 1665, 1730, 1795, 1859, 1922, 1985, 
            2048, 2109, 2170, 2230, 2290, 2349, 2407, 2464, 2521, 2577, 
            2632, 2686, 2740, 2793, 2845, 2896, 2946, 2995, 3043, 3091, 
            3137, 3183, 3227, 3271, 3313, 3355, 3395, 3434, 3473, 3510, 
            3547, 3582, 3616, 3649, 3681, 3712, 3741, 3770, 3797, 3823, 
            3849, 3872, 3895, 3917, 3937, 3956, 3974, 3991, 4006, 4020, 
            4033, 4045, 4056, 4065, 4073, 4080, 4086, 4090, 4093, 4095, 
            4096
        };

        #endregion

        #region Cos, Tan, Asin

        public static Fixed Cos(Fixed f)
        {
            return Sin(f + Fixed.Create(6435, false));
        }

        public static Fixed Tan(Fixed f)
        {
            return Sin(f) / Cos(f);
        }

        public static Fixed Asin(Fixed f)
        {
            bool isNegative = f < 0;
            f = Abs(f);

            if (f > Fixed.OneF)
                throw new ArithmeticException("Bad Asin Input:" + f.DoubleValue);

            Fixed f1 = mul(mul(mul(mul(Fixed.Create(145103 >> Fixed.SHIFT_AMOUNT, false), f) -
                Fixed.Create(599880 >> Fixed.SHIFT_AMOUNT, false), f) +
                Fixed.Create(1420468 >> Fixed.SHIFT_AMOUNT, false), f) -
                Fixed.Create(3592413 >> Fixed.SHIFT_AMOUNT, false), f) +
                Fixed.Create(26353447 >> Fixed.SHIFT_AMOUNT, false);
            Fixed f2 = InternalPI / Fixed.Create(2, true) - (Sqrt(Fixed.OneF - f) * f1);

            return isNegative ? f2.Inverse : f2;
        }

        private static Fixed mul(Fixed F1, Fixed F2)
        {
            return F1 * F2;
        }

        #endregion

        #region ATan, ATan2

        public static Fixed Atan(Fixed f)
        {
            return Asin(f / Sqrt(Fixed.OneF + (f * f)));
        }

        public static Fixed Atan2(Fixed f1, Fixed f2)
        {
            if (f2.RawValue == 0 && f1.RawValue == 0)
                return (Fixed)0;

            Fixed result = (Fixed)0;
            if (f2 > 0)
                result = Atan(f1 / f2);
            else if (f2 < 0)
            {
                if (f1 >= 0)
                    result = (InternalPI - Atan(Abs(f1 / f2)));
                else
                    result = (InternalPI - Atan(Abs(f1 / f2))).Inverse;
            }
            else
                result = (f1 >= 0 ? InternalPI : InternalPI.Inverse) / Fixed.Create(2, true);

            return result;
        }

        #endregion

        #region Abs

        public static Fixed Abs(Fixed f)
        {
            if (f < 0)
                return f.Inverse;
            else
                return f;
        }

        #endregion

        #region MaxMin

        public static Fixed Max(Fixed f1, Fixed f2)
        {
            if (f1 > f2)
                return f1;
            else
                return f2;
        }

        public static Fixed Min(Fixed f1, Fixed f2)
        {
            if (f1 < f2)
                return f1;
            else
                return f2;
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is Fixed)
            {
                return ((Fixed)obj).RawValue == this.RawValue;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return RawValue.GetHashCode();
        }

        public override string ToString()
        {
            return this.RawValue.ToString();
        }
    }

    public struct FPoint
    {
        public static readonly FPoint Zero = FPoint.Create((Fixed)0, (Fixed)0);

        public Fixed X;

        public Fixed Y;

        public static FPoint Create(Fixed x, Fixed y)
        {
            FPoint fp;
            fp.X = x;
            fp.Y = y;
            return fp;
        }

        public Fixed DistanceTo(FPoint f)
        {
            return Fixed.Sqrt((this - f).Length);
        }

        public Fixed Norm { get { return X * X + Y * Y; } }

        public Fixed Length { get { return Fixed.Sqrt(X * X + Y * Y); } }

        #region Vector Operations

        internal static Fixed Dot(FPoint f1, FPoint f2)
        {
            return f1.X * f2.X + f1.Y * f2.Y;
        }

        public static FPoint Rotate(FPoint f, Fixed angle)
        {
            FPoint result;
            Fixed cos = Fixed.Cos(angle);
            Fixed sin = Fixed.Sin(angle);
            result.X = f.X * cos - f.Y * sin;
            result.Y = f.X * sin + f.Y * cos;
            return result;
        }

        #region +
        public static FPoint operator +(FPoint point1, FPoint point2)
        {
            FPoint result;
            result.X = point1.X + point2.X;
            result.Y = point1.Y + point2.Y;
            return result;
        }
        #endregion

        #region -
        public static FPoint operator -(FPoint point1, FPoint point2)
        {
            FPoint result;
            result.X = point1.X - point2.X;
            result.Y = point1.Y - point2.Y;
            return result;
        }
        #endregion

        #region *
        public static FPoint operator *(FPoint point, Fixed scalar)
        {
            FPoint result;
            result.X = point.X * scalar;
            result.Y = point.Y * scalar;
            return result;
        }
        public static FPoint operator *(FPoint point, int scalar)
        {
            return point * (Fixed)scalar;
        }
        public static FPoint operator *(int scalar, FPoint point)
        {
            return point * (Fixed)scalar;
        }
        #endregion

        #region /
        public static FPoint operator /(FPoint point, Fixed scalar)
        {
            FPoint result;
            result.X = point.X / scalar;
            result.Y = point.Y / scalar;
            return result;
        }
        public static FPoint operator /(FPoint point, int scalar)
        {
            return point / (Fixed)scalar;
        }
        #endregion

        #endregion

        public override string ToString()
        {
            return String.Format("{0:f}, {1:f}", X.DoubleValue, Y.DoubleValue);
        }
    }

    public struct FRectangle
    {
        public FPoint TopLeft;
        public FPoint BottomRight;

        public static FRectangle Create(FPoint topLeft, FPoint bottomRight)
        {
            FRectangle result;
            result.TopLeft = topLeft;
            result.BottomRight = bottomRight;
            return result;
        }
        public static FRectangle Create(FPoint topLeft, Fixed width, Fixed height)
        {
            FRectangle result;
            result.TopLeft = topLeft;
            result.BottomRight = FPoint.Create(topLeft.X + width, topLeft.Y + height);
            return result;
        }
        public static FRectangle Create(Fixed x, Fixed y, Fixed width, Fixed height)
        {
            FRectangle result;
            result.TopLeft = FPoint.Create(x, y);
            result.BottomRight = FPoint.Create(x + width, y + height);
            return result;
        }
        public static FRectangle Create(FPoint size)
        {
            FRectangle result;
            result.TopLeft = FPoint.Create((Fixed)0, (Fixed)0);
            result.BottomRight = FPoint.Create(size.X, size.Y);
            return result;
        }
        public static FRectangle Create(Fixed width, Fixed height)
        {
            FRectangle result;
            result.TopLeft = FPoint.Create((Fixed)0, (Fixed)0);
            result.BottomRight = FPoint.Create(width, height);
            return result;
        }

        public Fixed Width { get { return BottomRight.X - TopLeft.X; } }
        public Fixed Height { get { return BottomRight.Y - TopLeft.Y; } }
        public Fixed Top { get { return TopLeft.Y; } }
        public Fixed Right { get { return BottomRight.X; } }
        public Fixed Bottom { get { return BottomRight.Y; } }
        public Fixed Left { get { return TopLeft.X; } }

        public bool Intersects(FRectangle other)
        {
            return other.Left < Right && other.Right > Left && other.Top < Bottom && other.Bottom > Top;
        }
    }
}