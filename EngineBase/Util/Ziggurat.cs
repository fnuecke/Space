using System;
using System.Diagnostics;

namespace Engine.Util
{
    /// <summary>
    /// Implementation of a ziggurat sampler for producing gaussian
    /// distributions from uniform ones.
    /// </summary>
    public sealed class Ziggurat : IGaussianRandom
    {
        #region Static Fields [Defaults]

        /// <summary>
        /// Number of blocks.
        /// </summary>
        const int BlockCount = 128;
        /// <summary>
        /// Right hand x coord of the base rectangle, thus also the left hand x coord of the tail 
        /// (pre-determined/computed for 128 blocks).
        /// </summary>
        const double R = 3.442619855899;
        /// <summary>
        /// Area of each rectangle (pre-determined/computed for 128 blocks).
        /// </summary>
        const double A = 9.91256303526217e-3;
        /// <summary>
        /// Scale factor for converting a UInt with range [0,0xffffffff] to a double with range [0,1].
        /// </summary>
        const double UIntToU = 1.0 / uint.MaxValue;

        #endregion

        #region Instance Fields

        readonly IUniformRandom _rng;

        // _x[i] and _y[i] describe the top-right position ox rectangle i.
        readonly double[] _x;
        readonly double[] _y;

        // The proportion of each segment that is entirely within the distribution, expressed as uint where 
        // a value of 0 indicates 0% and uint.MaxValue 100%. Expressing this as an integer allows some floating
        // points operations to be replaced with integer ones.
        readonly uint[] _xComp;

        // Useful precomputed values.
        // Area A divided by the height of B0. Note. This is *not* the same as _x[i] because the area 
        // of B0 is __A minus the area of the distribution tail.
        readonly double _aDivY0;

        #endregion

        #region Constructors

        /// <summary>
        /// Construct with a default RNG source.
        /// </summary>
        public Ziggurat() 
            : this(new MersenneTwister())
        {
        }

        /// <summary>
        /// Construct with the specified RNG seed.
        /// </summary>
        public Ziggurat(ulong seed) 
            : this(new MersenneTwister(seed))
        {
        }

        /// <summary>
        /// Construct with the provided RNG source.
        /// </summary>
        public Ziggurat(IUniformRandom rng)
        {
            _rng = rng;

            // Initialise rectangle position data. 
            // _x[i] and _y[i] describe the top-right position ox Box i.

            // Allocate storage. We add one to the length of _x so that we have an entry at _x[_blockCount], this avoids having 
            // to do a special case test when sampling from the top box.
            _x = new double[BlockCount + 1];
            _y = new double[BlockCount];

            // Determine top right position of the base rectangle/box (the rectangle with the Gaussian tale attached). 
            // We call this Box 0 or B0 for short.
            // Note. x[0] also describes the right-hand edge of B1. (See diagram).
            _x[0] = R; 
            _y[0] = GaussianPdfDenorm(R);

            // The next box (B1) has a right hand X edge the same as B0. 
            // Note. B1's height is the box area divided by its width, hence B1 has a smaller height than B0 because
            // B0's total area includes the attached distribution tail.
            _x[1] = R;
            _y[1] =  _y[0] + (A / _x[1]);

            // Calc positions of all remaining rectangles.
            for(var i=2; i<BlockCount; i++)
            {
                _x[i] = GaussianPdfDenormInv(_y[i-1]);
                _y[i] = _y[i-1] + (A / _x[i]);   
            }

            // For completeness we define the right-hand edge of a notional box 6 as being zero (a box with no area).
            _x[BlockCount] = 0.0;

            // Useful precomputed values.
            _aDivY0 = A / _y[0];
            _xComp = new uint[BlockCount];

            // Special case for base box. _xComp[0] stores the area of B0 as a proportion of __R 
            // (recalling that all segments have area __A, but that the base segment is the combination of B0 and the distribution tail).
            // Thus -xComp[0[ is the probability that a sample point is within the box part of the segment.
            _xComp[0] = (uint)(((R * _y[0]) / A) * uint.MaxValue);

            for(var i=1; i<BlockCount-1; i++) {
                _xComp[i] = (uint)((_x[i+1] / _x[i]) * uint.MaxValue);
            }
            _xComp[BlockCount-1] = 0;  // Shown for completeness.

            // Sanity check. Test that the top edge of the topmost rectangle is at y=1.0.
            // Note. We expect there to be a tiny drift away from 1.0 due to the inexactness of floating
            // point arithmetic.
            Debug.Assert(Math.Abs(1.0 - _y[BlockCount-1]) < 1e-10);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get the next sample value from the gaussian distribution.
        /// </summary>
        public double NextSample()
        {
            for(;;)
            {
                // Select box at random.
                var u = (byte)_rng.NextUInt32();
                var i = u & 0x7F;
                var sign = ((u & 0x80) == 0) ? -1.0 : 1.0;

                // Generate uniform random value with range [0,0xffffffff].
                var u2 = _rng.NextUInt32();

                // Special case for the base segment.
                if(0 == i)
                {
                    if(u2 < _xComp[0]) 
                    {   // Generated x is within R0.
                        return u2 * UIntToU * _aDivY0 * sign;
                    }
                    // Generated x is in the tail of the distribution.
                    return SampleTail() * sign;
                }

                // All other segments.
                if(u2 < _xComp[i]) 
                {   // Generated x is within the rectangle.
                    return u2 * UIntToU * _x[i] * sign;
                }

                // Generated x is outside of the rectangle.
                // Generate a random y coordinate and test if our (x,y) is within the distribution curve.
                // This execution path is relatively slow/expensive (makes a call to Math.Exp()) but relatively rarely executed,
                // although more often than the 'tail' path (above).
                double x = u2 * UIntToU * _x[i];
                if(_y[i-1] + ((_y[i] - _y[i-1]) * _rng.NextDouble()) < GaussianPdfDenorm(x) ) {
                    return x * sign;
                }
            }
        }

        /// <summary>
        /// Get the next sample value from the gaussian distribution.
        /// </summary>
        /// <param name="mu">The distribution's mean.</param>
        /// <param name="sigma">The distribution's standard deviation.</param>
        public double NextSample(double mu, double sigma)
        {
            return mu + (NextSample() * sigma);
        }

        /// <summary>
        /// Get the next sample value from the gaussian distribution, but
        /// clamp it to the 2*sigma interval around mu.
        /// </summary>
        /// <param name="mu">The distribution's mean.</param>
        /// <param name="sigma">The distribution's standard deviation.</param>
        public double NextSampleClamped(double mu, double sigma)
        {
            var sample = NextSample();
            if (sample < -1)
            {
                sample = -1;
            }
            else if (sample > 1)
            {
                sample = 1;
            }
            return mu + sample * sigma;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sample from the distribution tail (defined as having x >= __R).
        /// </summary>
        /// <returns></returns>
        private double SampleTail()
        {
            double x, y;
            do
            {
                x = -Math.Log(_rng.NextDouble()) / R;
                y = -Math.Log(_rng.NextDouble());
            }
            while(y+y < x*x);
            return R + x;
        }

        /// <summary>
        /// Gaussian probability density function, denormalised, that is, y = e^-(x^2/2).
        /// </summary>
        private static double GaussianPdfDenorm(double x)
        {
            return Math.Exp(-(x*x / 2.0));
        }

        /// <summary>
        /// Inverse function of GaussianPdfDenorm(x)
        /// </summary>
        private static double GaussianPdfDenormInv(double y)
        {   
            // Operates over the y range (0,1], which happens to be the y range of the pdf, 
            // with the exception that it does not include y=0, but we would never call with 
            // y=0 so it doesn't matter. Remember that a Gaussian effectively has a tail going
            // off into x == infinity, hence asking what is x when y=0 is an invalid question
            // in the context of this class.
            return Math.Sqrt(-2.0 * Math.Log(y));
        }

        #endregion
    }
}
