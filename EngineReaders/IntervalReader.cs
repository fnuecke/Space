using System;
using Engine.Math;
using Microsoft.Xna.Framework.Content;

namespace Engine.Serialization
{
    public abstract class AbstractIntervalReader<T> : ContentTypeReader<Interval<T>>
        where T : IComparable<T>, IEquatable<T>
    {
        protected override Interval<T> Read(ContentReader input, Interval<T> existingInstance)
        {
            existingInstance = existingInstance ?? NewInstance();

            var low = Read(input);
            var high = Read(input);

            existingInstance.SetTo(low, high);

            return existingInstance;
        }

        protected abstract T Read(ContentReader input);

        protected abstract Interval<T> NewInstance();
    }

    public sealed class DoubleIntervalReader : AbstractIntervalReader<double>
    {
        protected override double Read(ContentReader input)
        {
            return input.ReadDouble();
        }

        protected override Interval<double> NewInstance()
        {
            return new DoubleInterval();
        }
    }

    public sealed class FloatIntervalReader : AbstractIntervalReader<float>
    {
        protected override float Read(ContentReader input)
        {
            return input.ReadSingle();
        }

        protected override Interval<float> NewInstance()
        {
            return new FloatInterval();
        }
    }

    public sealed class Int32IntervalReader : AbstractIntervalReader<int>
    {
        protected override int Read(ContentReader input)
        {
            return input.ReadInt32();
        }

        protected override Interval<int> NewInstance()
        {
            return new IntInterval();
        }
    }
}