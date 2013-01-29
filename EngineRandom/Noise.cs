using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Random
{
    /// <summary>Noise generating methods.</summary>
    [PublicAPI]
    public sealed class Noise
    {
        /// <summary>Table of permuted interval [0, 255].</summary>
        private readonly int[] _permutation = new int[512];

        /// <summary>Gradient directions.</summary>
        private readonly float[,] _gradients = new float[256,2];

        /// <summary>Initialized permutation table.</summary>
        /// <param name="seed">The seed value.</param>
        public Noise(ulong seed = 0)
        {
            Reseed(seed);
        }

        /// <summary>Reseeds the noise lookup tables.</summary>
        /// <param name="seed">The seed value.</param>
        [PublicAPI]
        public void Reseed(ulong seed)
        {
            // Seed table.
            for (var i = 0; i < 256; ++i)
            {
                _permutation[i] = i;
            }

            // Shuffle it.
            var random = new MersenneTwister(seed);
            _permutation.Shuffle(0, 256, random);

            // Copy to upper range to avoid having to handle wrapping.
            for (var i = 0; i < 256; i++)
            {
                _permutation[i + 256] = _permutation[i];
            }

            // Seed gradients.
            for (var i = 0; i < 256; i++)
            {
                var theta = random.NextDouble(0, Math.PI * 2);
                _gradients[i, 0] = (float) Math.Cos(theta);
                _gradients[i, 1] = (float) Math.Sin(theta);
            }
        }
        
        /// <summary>Computes Perlin noise in 2D.</summary>
        /// <param name="x">The x-offset.</param>
        /// <param name="y">The y-offset.</param>
        /// <returns>A value in the interval of [-1, 1].</returns>
        [PublicAPI]
        public float Perlin(float x, float y)
        {
            var ix = (int) x;
            var iy = (int) y;
            
            x = x - ix;
            y = y - iy;
            ix &= 0xFF;
            iy &= 0xFF;

            int g00 = _permutation[ix + _permutation[iy]],
                g10 = _permutation[ix + 1 + _permutation[iy]],
                g01 = _permutation[ix + _permutation[iy + 1]],
                g11 = _permutation[ix + 1 + _permutation[iy + 1]];

            float n00 = Dot(g00, x, y),
                  n10 = Dot(g10, x - 1f, y),
                  n01 = Dot(g01, x, y - 1f),
                  n11 = Dot(g11, x - 1f, y - 1f);

            var fx = Fade(x);
            return Lerp(Lerp(n00, n10, fx), Lerp(n01, n11, fx), Fade(y));
        }

        /// <summary>Fills the specified texture with Perlin noise.</summary>
        /// <param name="texture">The texture to write to.</param>
        /// <param name="octaves">The number of octaves.</param>
        /// <param name="frequency">The frequency.</param>
        /// <param name="amplitude">The amplitude.</param>
        [PublicAPI]
        public void Perlin(Texture2D texture, int octaves, float frequency = 0.5f, float amplitude = 1f)
        {
            if (texture == null)
            {
                throw new ArgumentNullException("texture");
            }
            if (octaves < 1)
            {
                throw new ArgumentException("Must generate at least one octave.", "octaves");
            }

            var noise = new float[texture.Width * texture.Height];
            var min = float.MaxValue;
            var max = float.MinValue;
            for (var octave = 0; octave < octaves; octave++, frequency += frequency, amplitude *= 0.5f)
            {
                var fx = frequency / texture.Width;
                var fy = frequency / texture.Height;
                var u = amplitude;
                Parallel.For(0, noise.Length, i =>
                {
                    var x = i % texture.Width;
                    var y = i / texture.Height;
                    var n = noise[i] += u * Perlin(x * fx, y * fy);
                    if (n < min) min = n;
                    if (n > max) max = n;
                });
            }

            texture.SetData(
                (noise.Select(f => (f - min) / (max - min))
                  .Select(value => new Color(value, value, value, 1f))).ToArray());
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private float Dot(int i, float x, float y)
        {
            return _gradients[i, 0] * x + _gradients[i, 1] * y;
        }

        private static float Lerp(float x, float y, float t)
        {
            return x * (1f - t) + y * t;
        }
    }
}