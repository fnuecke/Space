using System;
using System.Text.RegularExpressions;
using Engine.Util;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;

namespace Engine.Serialization
{
    public abstract class AbstractIntervalSerializer<T> : ContentTypeSerializer<Interval<T>>
        where T : IComparable<T>, IEquatable<T>
    {
        private static readonly Regex IntervalPattern = new Regex(@"
            ^\s*            # Complete line, ignore leading whitespace.
            (?<low>         # Read the low value, which must be a number.
                -?[0-9]+
                (
                    \.[0-9]+        # Optionally a floating point value.
                )?
            )
            (               # Optionally read the high value, which must be a number.
            \s+to\s+
                (?<high>
                    -?[0-9]+
                    (
                        \.[0-9]+    # Optionally a floating point value.
                    )?
                )
            )?
            \s*$    # Skip trailing whitespace",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

        protected override void Serialize(IntermediateWriter output, Interval<T> value, ContentSerializerAttribute format)
        {
            if (value.Low.Equals(value.High))
            {
                output.Xml.WriteValue(value.Low);
            }
            else
            {
                output.Xml.WriteValue(value.Low + " to " + value.High);
            }
        }

        protected override Interval<T> Deserialize(IntermediateReader input, ContentSerializerAttribute format, Interval<T> existingInstance)
        {
            // Parse the content.
            var match = IntervalPattern.Match(input.Xml.ReadContentAsString());
            if (match.Success)
            {
                T low = Parse(match.Groups["low"].Value);
                T high = match.Groups["high"].Success ? Parse(match.Groups["high"].Value) : low;
                return new Interval<T>(low, high);
            }
            else
            {
                throw new ArgumentException("input");
            }
        }

        protected abstract T Parse(string value);
    }

    [ContentTypeSerializer]
    public sealed class DoubleIntervalSerializer : AbstractIntervalSerializer<double>
    {
        protected override double Parse(string value)
        {
            return double.Parse(value);
        }
    }

    [ContentTypeSerializer]
    public sealed class FloatIntervalSerializer : AbstractIntervalSerializer<float>
    {
        protected override float Parse(string value)
        {
            return float.Parse(value);
        }
    }

    [ContentTypeSerializer]
    public sealed class Int32IntervalSerializer : AbstractIntervalSerializer<int>
    {
        protected override int Parse(string value)
        {
            return int.Parse(value);
        }
    }
}
