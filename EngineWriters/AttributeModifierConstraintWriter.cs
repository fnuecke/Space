using System;
using System.Text.RegularExpressions;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Constraints;
using Engine.Util;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading actual XML files in the content project.
    /// </summary>
    public abstract class AbstractAttributeModifierConstraintSerializer<TAttribute> : ContentTypeSerializer<AttributeModifierConstraint<TAttribute>>
        where TAttribute : struct
    {

        private static readonly Regex AttributePattern = new Regex(@"
            ^\s*            # Complete line, ignore leading whitespace.
            (?<minValue>    # Read the actual value, which must be a number.
                -?[0-9]+
                (
                    \.[0-9]+    # Optionally a floating point value.
                )?
            )
            (
                \s+to\s+    # Optionally a max value, which must be separated by a 'to'.
                (?<maxValue>
                    -?[0-9]+
                    (
                        \.[0-9]+    # Again, optionally as floating point.
                    )?
                )
            )?
            (?<percentual>  # Check if it's a percentual value.
                %
            )?
            \s+             # Separate attribute type with whitespace.
            (?<type>        # Read the attribute type.
                \w+
            )
            \s*$    # Skip trailing whitespace",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

        protected override void Serialize(IntermediateWriter output, AttributeModifierConstraint<TAttribute> value, ContentSerializerAttribute format)
        {
            string valueString;
            switch (value.ComputationType)
            {
                case AttributeComputationType.Additive:
                    if (value.Value.Low == value.Value.High)
                    {
                        valueString = value.Value.Low.ToString();
                    }
                    else
                    {
                        valueString = value.Value.Low.ToString() + " to " + value.Value.High.ToString();
                    }
                    output.Xml.WriteValue(valueString + " " + Enum.GetName(typeof(TAttribute), value.Type));
                    break;
                case AttributeComputationType.Multiplicative:
                    if (value.Value.Low == value.Value.High)
                    {
                        valueString = ((value.Value.Low - 1) * 100).ToString();
                    }
                    else
                    {
                        valueString = ((value.Value.Low - 1) * 100).ToString() + " to " + ((value.Value.High - 1) * 100).ToString();
                    }
                    output.Xml.WriteValue(valueString + "% " + Enum.GetName(typeof(TAttribute), value.Type));
                    break;
            }
        }

        protected override AttributeModifierConstraint<TAttribute> Deserialize(IntermediateReader input, ContentSerializerAttribute format, AttributeModifierConstraint<TAttribute> existingInstance)
        {
            // Parse the content.
            //bool round = input.Xml.GetAttribute("round") != null && bool.Parse(input.Xml.GetAttribute("round"));
            var match = AttributePattern.Match(input.Xml.ReadContentAsString());
            if (match.Success)
            {
                existingInstance = existingInstance ?? new AttributeModifierConstraint<TAttribute>();

                // Save rounding mode.
                existingInstance.Round = false;// round;

                // Pattern was OK, get the enum.
                existingInstance.Type = (TAttribute)Enum.Parse(typeof(TAttribute), match.Groups["type"].Value);

                // Now get the numeric value for the attribute.
                var minValue = float.Parse(match.Groups["minValue"].Value);
                var maxValue = match.Groups["maxValue"].Success ? float.Parse(match.Groups["maxValue"].Value) : minValue;

                // Check if it's a percentual value, and if so set mode to multiplicative.
                var percentual = match.Groups["percentual"];
                if (percentual.Success)
                {
                    minValue = (minValue / 100f) + 1;
                    maxValue = (maxValue / 100f) + 1;
                    existingInstance.ComputationType = AttributeComputationType.Multiplicative;
                }
                else
                {
                    existingInstance.ComputationType = AttributeComputationType.Additive;
                }

                // Set final value in our instance.
                existingInstance.Value = new Interval<float>(minValue, maxValue);

                return existingInstance;
            }
            else
            {
                throw new ArgumentException("input");
            }
        }
    }
}
