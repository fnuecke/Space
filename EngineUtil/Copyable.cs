using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Engine.Util
{
    #region Copying

    /// <summary>
    /// Use this attribute to mark properties or fields as to be ignored when
    /// copying the object to another instance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class CopyIgnoreAttribute : Attribute
    {
    }

    /// <summary>
    /// This package provides utilities for dynamically generating automatic
    /// member copying functions.
    /// </summary>
    public static class Copyable
    {
        /// <summary>
        /// Copies all public and private instance fields from the first object into
        /// the second. This somewhat behaves like <see cref="object.MemberwiseClone"/>,
        /// except that it does not create a new object but reuses an existing one.
        /// This will perform a shallow copy.
        /// 
        /// Fields marked with the <see cref="CopyIgnoreAttribute"/> will not be copied.
        /// </summary>
        /// <typeparam name="T">The type of the object to copy.</typeparam>
        /// <param name="from">The instance to copy from.</param>
        /// <param name="into">The instance to copy into.</param>
        public static void CopyInto<T>(T from, T into) where T : class, ICopyable<T>
        {
            // Skip on identity copy.
            if (from == into)
            {
                return;
            }
            // Make sure we have valid parameters.
            if (from == null)
            {
                throw new ArgumentNullException("from");
            }
            if (into == null)
            {
                throw new ArgumentNullException("into");
            }
            // Make sure the types match.
            if (!Equals(from.GetType().TypeHandle, into.GetType().TypeHandle))
            {
                throw new ArgumentException("Type mismatch.", "into");
            }
            // Copy the data.
            GetCopier(from.GetType())(from, into);
        }

        #endregion

        #region Copying internals

        /// <summary>
        /// Signature of a copying function.
        /// </summary>
        private delegate void Copier(object from, object into);

        /// <summary>
        /// Cached list of object copiers, to avoid rebuilding the methods over and over.
        /// </summary>
        private static readonly Dictionary<Type, Copier> CopierCache =
            new Dictionary<Type, Copier>();

        /// <summary>
        /// Gets the copier from the cache, or creates it if it doesn't exist yet
        /// and adds it to the cache.
        /// </summary>
        private static Copier GetCopier(Type type)
        {
            Copier copier;
            if (!CopierCache.ContainsKey(type))
            {
                copier = CreateCopier(type);
                CopierCache.Add(type, copier);
            }
            else
            {
                copier = CopierCache[type];
            }
            return copier;
        }

        /// <summary>
        /// Generates a function that will copy all public and private instance fields,
        /// including backing fields for auto properties, from one instance of a type
        /// into the other. It will skip any fields (and properties) that are marked
        /// with the <see cref="CopyIgnoreAttribute"/> attribute.
        /// </summary>
        /// <param name="type">The type to generate the method for.</param>
        /// <returns>
        /// A delegate for the generated method.
        /// </returns>
        private static Copier CreateCopier(Type type)
        {
            // Must not be null for the following. This is used to provide a context
            // for the generated method, which will avoid a number of costly security
            // checks, which could slow down the generated method immensly.
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            if (declaringType == null)
            {
                return null;
            }

            // Generate dynamic method for the specified type.
            var method = new DynamicMethod("CopyInto", null, new[] { typeof(object), typeof(object) }, declaringType, true);
            var generator = method.GetILGenerator();

            var copyable = Type.GetType("Engine.Util.ICopyable`1");
            if (copyable == null)
            {
                throw new InvalidOperationException("Copyable interface not found.");
            }

            // Copy all instance fields.
            foreach (var f in GetAllFields(type))
            {
                
                Type typedCopyable;
                if (f.FieldType.IsClass && f.FieldType.IsAssignableFrom(typedCopyable = copyable.MakeGenericType(f.FieldType)))
                {
                    // Got another copyable, call its CopyInto method in turn.
                    var copyInto = typedCopyable.GetMethod("CopyInto", new[] {f.FieldType, f.FieldType});
                    System.Diagnostics.Debug.Assert(copyInto.ReturnType == typeof(void));
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, f);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldfld, f);
                    generator.EmitCall(OpCodes.Callvirt, copyInto, null);
                }
                else
                {
                    // Normal field, just copy by value.
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, f);
                    generator.Emit(OpCodes.Stfld, f);
                }
            }

            // Finish our dynamic function by returning.
            generator.Emit(OpCodes.Ret);

            // Create an instance of our dynamic method (as a delegate) and return it.
            return (Copier)method.CreateDelegate(typeof(Copier));
        }

        /// <summary>
        /// Utility method the gets a list of all fields in a type, including
        /// this in its base classes all the way up the hierarchy. Fields with
        /// the <see cref="CopyIgnoreAttribute"/> are not returned. This will
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
                                    !f.IsDefined(typeof(CopyIgnoreAttribute), true) &&
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
                                    !p.IsDefined(typeof(CopyIgnoreAttribute), true) &&
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
