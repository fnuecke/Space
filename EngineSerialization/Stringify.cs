using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Engine.Serialization
{
    /// <summary>
    /// Use this attribute to mark a method that should be called after
    /// stringifying an object, for example to allow specialized writing.
    /// This should emit a string representation of what the method marked
    /// with the <see cref="OnPacketizeAttribute"/> writes.
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnStringifyAttribute : Attribute
    {
    }

    /// <summary>
    /// This class can be used to dump a string representation of an object.
    /// It uses the packetizer attributes to determine which objects to dump
    /// and which not to. It'll recurse into other packetizables and simply
    /// call <see cref="object.ToString"/> for everything else.
    /// </summary>
    public static class Stringify
    {
        /// <summary>
        /// How many spaces one indent level adds in front of a line.
        /// </summary>
        private const int IndentAmount = 2;

        /// <summary>Appends the dump.</summary>
        /// <param name="sb">The sb.</param>
        /// <param name="value">The data.</param>
        /// <param name="indent">The initial indent.</param>
        /// <returns>The string builder, for call chaining.</returns>
        public static StringBuilder Dump(this StringBuilder sb, object value, int indent = 0)
        {
            // If we have a value type just call its tostring.
            if (value is ValueType)
            {
                return sb.Append(value);
            }
            else
            {
                return sb.AppendValue(value, indent);
            }
        }

        /// <summary>Adds a new line and the specified indent depth to the string builder.</summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="indent">The indent depth.</param>
        /// <returns>The string builder, for call chaining.</returns>
        public static StringBuilder AppendIndent(this StringBuilder sb, int indent)
        {
            return sb.AppendLine().Append(' ', indent * IndentAmount);
        }

        /// <summary>Appends the specified object to the string builder.</summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="value">The value to append.</param>
        /// <param name="indent">The indent of the output.</param>
        /// <returns>The string builder, for call chaining.</returns>
        private static StringBuilder AppendValue(this StringBuilder sb, object value, int indent)
        {
            // Most simple case is if we have null...
            if (value == null)
            {
                return sb.Append("null");
            }

            // We're still here, get a writing function for the object.
            sb.Append('{');
            GetAppender(value.GetType())(sb, value, indent + 1)
                .AppendIndent(indent).Append('}');

            return sb;
        }

        #region Internals

        /// <summary>
        /// Signature of an appending function.
        /// </summary>
        private delegate StringBuilder Appender(StringBuilder sb, object data, int indent);

        /// <summary>
        /// Cached list of type packetizers, to avoid rebuilding the methods over and over.
        /// </summary>
        private static readonly Dictionary<Type, Appender> AppenderCache =
            new Dictionary<Type, Appender>();

        /// <summary>
        /// Gets the packetizer from the cache, or creates it if it doesn't exist yet
        /// and adds it to the cache.
        /// </summary>
        private static Appender GetAppender(Type type)
        {
            Appender result;
            if (!AppenderCache.ContainsKey(type))
            {
                result = CreateAppender(type);
                AppenderCache.Add(type, result);
            }
            else
            {
                result = AppenderCache[type];
            }
            return result;
        }

        private static readonly Regex BackingFieldRegex = new Regex("^<([^>]+)>k__BackingField$");
        
        private static Appender CreateAppender(Type type)
        {
            // Must not be null for the following. This is used to provide a context
            // for the generated method, which will avoid a number of costly security
            // checks, which could slow down the generated method immensly.
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            if (declaringType == null)
            {
                return null;
            }

            // Invariant method shortcuts.
            var appendValue = typeof(Stringify).GetMethod("AppendValue", BindingFlags.Static | BindingFlags.NonPublic);
            var appendIndent = typeof(Stringify).GetMethod("AppendIndent");
            var appendString = typeof(StringBuilder).GetMethod("Append", new[] {typeof(string)});
            var appendObject = typeof(StringBuilder).GetMethod("Append", new[] {typeof(object)});
            
            System.Diagnostics.Debug.Assert(appendValue != null);
            System.Diagnostics.Debug.Assert(appendIndent != null);
            System.Diagnostics.Debug.Assert(appendString != null);
            System.Diagnostics.Debug.Assert(appendObject != null);

            // Generate dynamic methods for the specified type.
            var appendMethod = new DynamicMethod(
                "Append", typeof(StringBuilder), new[] { typeof(StringBuilder), typeof(object), typeof(int) }, declaringType, true);

            // Get the code generators.
            var appendGenerator = appendMethod.GetILGenerator();

            // Load builder as onto the stack. This will always remain the lowest entry on
            // the stack of our appender. This is an optimization the compiler could not
            // even do, because the returned packet reference may theoretically differ. In
            // practice it never will/must, though.
            appendGenerator.Emit(OpCodes.Ldarg_0);

            // Handle all instance fields.
            foreach (var f in GetAllFields(type))
            {
                // Skip functions (event handlers in particular).
                if (typeof(Delegate).IsAssignableFrom(f.FieldType))
                {
                    continue;
                }
                
                appendGenerator.Emit(OpCodes.Ldarg_2);
                appendGenerator.EmitCall(OpCodes.Call, appendIndent, null);
                appendGenerator.Emit(OpCodes.Ldstr, BackingFieldRegex.Replace(f.Name, "$1"));
                appendGenerator.EmitCall(OpCodes.Call, appendString, null);
                appendGenerator.Emit(OpCodes.Ldstr, " = ");
                appendGenerator.EmitCall(OpCodes.Call, appendString, null);

                appendGenerator.Emit(OpCodes.Ldarg_1);
                appendGenerator.Emit(OpCodes.Ldfld, f);
                if (f.FieldType.IsValueType)
                {
                    var appendType = typeof(StringBuilder).GetMethod("Append", new[] {f.FieldType});
                    if (appendType != null && appendType.GetParameters()[0].ParameterType == f.FieldType)
                    {
                        appendGenerator.EmitCall(OpCodes.Call, appendType, null);
                    }
                    else
                    {
                        appendGenerator.Emit(OpCodes.Box, f.FieldType);
                        appendGenerator.EmitCall(OpCodes.Call, appendObject, null);
                    }
                }
                else
                {
                    appendGenerator.Emit(OpCodes.Ldarg_2);
                    appendGenerator.EmitCall(OpCodes.Call, appendValue, null);
                }
            }
            
            // Call post-stringify method for writing if a callback exists, to
            // allow some specialized output where necessary.
            foreach (var callback in type
                .GetMethods(BindingFlags.Instance |
                            BindingFlags.Public)
                .Where(m => m.IsDefined(typeof(OnStringifyAttribute), true)))
            {
                if (callback.GetParameters().Length != 2 ||
                    callback.GetParameters()[0].ParameterType != typeof(StringBuilder) ||
                    callback.GetParameters()[1].ParameterType != typeof(int))
                {
                    throw new ArgumentException(string.Format("Stringify callback {0}.{1} has invalid signature, must be ((StringBuilder, int) => ?).", type.Name, callback.Name));
                }
                appendGenerator.Emit(OpCodes.Ldarg_1);
                appendGenerator.Emit(OpCodes.Ldarg_0);
                appendGenerator.Emit(OpCodes.Ldarg_2);
                appendGenerator.EmitCall(OpCodes.Callvirt, callback, null);
                if (callback.ReturnType != typeof(void))
                {
                    appendGenerator.Emit(OpCodes.Pop);
                }
            }
            
            // Finish our dynamic functions by returning.
            appendGenerator.Emit(OpCodes.Ret);

            // Create an instances of our dynamic methods (as delegates) and return them.
            return (Appender)appendMethod.CreateDelegate(typeof(Appender));
        }
        
        /// <summary>
        /// Utility method the gets a list of all fields in a type, including
        /// this in its base classes all the way up the hierarchy. Fields with
        /// the <see cref="PacketizerIgnoreAttribute"/> are not returned. This will
        /// also include automatially generated field backing properties, unless
        /// the property has said attribute.
        /// </summary>
        /// <param name="type">The type to start parsing at.</param>
        /// <returns>The list of all relevant fields.</returns>
        private static IEnumerable<FieldInfo> GetAllFields(Type type)
        {
            // Start with an empty set, then chain as we walk up the hierarchy.
            var result = Enumerable.Empty<FieldInfo>();
            while (type != null)
            {
                // For closures, to avoid referencing the wrong thing on evaluation.
                var t = type;

                // Look for normal, non-backing fields.
                result = result.Union(
                    // Get all public and private fields.
                    type.GetFields(BindingFlags.Public |
                                   BindingFlags.NonPublic |
                                   BindingFlags.Instance)
                        // Ignore:
                        // - fields that are declared in parent types.
                        // - fields that should be ignored via attribute.
                        // - fields that are compiler generated. We will scan for them below,
                        // when we parse the properties.
                        .Where(f => f.DeclaringType == t &&
                                    !f.IsDefined(typeof(PacketizerIgnoreAttribute), true) &&
                                    !f.IsDefined(typeof(CompilerGeneratedAttribute), false)));

                // Look for properties with automatically generated backing fields.
                result = result.Union(
                    type.GetProperties(BindingFlags.Public |
                                       BindingFlags.NonPublic |
                                       BindingFlags.Instance)
                        // Ignore:
                        // - properties that are declared in parent types.
                        // - properties that should be ignored via attribute.
                        // - properties that do not have an automatically generated backing field
                        //   (which we can deduce from the getter/setter being compiler generated).
                        .Where(p => p.DeclaringType == t &&
                                    !p.IsDefined(typeof(PacketizerIgnoreAttribute), true) &&
                                    (p.GetGetMethod(true) ?? p.GetSetMethod(true))
                                        .IsDefined(typeof(CompilerGeneratedAttribute), false))
                        // Get the backing field. There is no "hard link" we can follow, but the
                        // backing fields do follow a naming convention we can make use of.
                        .Select(p => t.GetField(string.Format("<{0}>k__BackingField", p.Name),
                                                BindingFlags.NonPublic | BindingFlags.Instance)));

                // Continue with the parent.
                type = type.BaseType;
            }

            // After we reach the top, filter out any duplicates (due to visibility
            // in sub-classes some field may have been registered more than once).
            return result;
        }

        #endregion
    }
}
