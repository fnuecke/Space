using System;
using System.Text.RegularExpressions;
using Engine.ComponentSystem.Modules;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading actual XML files in the content project.
    /// </summary>
    public abstract class AbstractEntityAttributeSerializer<TModifier> : ContentTypeSerializer<Modifier<TModifier>>
        where TModifier : struct
    {
        
        private static readonly Regex AttributePattern = new Regex(@"^\s*(?<type>[\+\-])?(?<value>[0-9]*(\.[0-9]+)?)(?<percentual>%)?\s+(?<class>\w+)\s*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        protected override void Serialize(IntermediateWriter output, Modifier<TModifier> value, ContentSerializerAttribute format)
        {
            switch (value.ComputationType)
            {
                case ModifierComputationType.Additive:
                    output.Xml.WriteValue(value.Value.ToString() + " " + value.Type.ToString());
                    break;
                case ModifierComputationType.Multiplicative:
                    output.Xml.WriteValue((value.Value - 1).ToString() + "% " + value.Type.ToString());
                    break;
            }
        }

        protected override Modifier<TModifier> Deserialize(IntermediateReader input, ContentSerializerAttribute format, Modifier<TModifier> existingInstance)
        {
            // Parse the content.
            var match = AttributePattern.Match(input.Xml.ReadContentAsString());
            if (match.Success)
            {
                if (existingInstance == null)
                {
                    existingInstance = new Modifier<TModifier>();
                }

                // Pattern was OK, get the enum.
                existingInstance.Type = (TModifier)Enum.Parse(typeof(TModifier), match.Groups["class"].Value);

                // Now get the numeric value for the attribute.
                var value = float.Parse(match.Groups["value"].Value);

                // Check if it's a negative value.
                var type = match.Groups["type"];
                if (type.Success && type.Value.Equals("-"))
                {
                    value = -value;
                }

                // Check if it's a percentual value, and if so set mode to multiplicative.
                var percentual = match.Groups["percentual"];
                if (percentual.Success)
                {
                    value = 1 + value;
                    existingInstance.ComputationType = ModifierComputationType.Multiplicative;
                }
                else
                {
                    existingInstance.ComputationType = ModifierComputationType.Additive;
                }

                // Set final value in our instance.
                existingInstance.Value = value;

                return existingInstance;
            }
            else
            {
                throw new ArgumentException("input");
            }
        }
    }

    /// <summary>
    /// This is for writing data back in binary format.
    /// </summary>
    public abstract class AbstractEntityAttributeWriter<TAttribute> : ContentTypeWriter<Modifier<TAttribute>>
        where TAttribute : struct
    {
        protected override void Write(ContentWriter output, Modifier<TAttribute> value)
        {
            output.Write(Enum.GetName(typeof(TAttribute), value.Type));
            output.Write((byte)value.ComputationType);
            output.Write(value.Value);
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(Modifier<TAttribute>).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(EntityAttributeReader<TAttribute>).AssemblyQualifiedName;
        }
    }
}