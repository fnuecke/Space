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
    /// Use this attribute to mark array properties or fields of which a deep
    /// copy should be made. If set, this will ensure the target array is a
    /// different instance than the source array, and will copy each element
    /// over into the target array. If the array elements are <see cref="ICopyable{T}"/>
    /// they will in turn have they their <see cref="ICopyable{T}.CopyInto"/>
    /// method invoked after being cloned using <see cref="ICopyable{T}.NewInstance"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class DeepCopyAttribute : Attribute
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
            var method = new DynamicMethod(
                "CopyInto", null, new[] { typeof(object), typeof(object) }, declaringType, true);
            var generator = method.GetILGenerator();

            // Loop invariant type shortcuts.
            var copyable = Type.GetType("Engine.Util.ICopyable`1");
            var prepareForCopy = typeof(Copyable).GetMethod(
                "PrepareArray", BindingFlags.Static | BindingFlags.NonPublic);
            var deepArrayCopy = typeof(Copyable).GetMethod(
                "DeepCopy", BindingFlags.Static | BindingFlags.NonPublic);
            var flatArrayCopy = typeof(Array).GetMethod(
                "Copy", new[] {typeof(Array), typeof(Array), typeof(int)});
            var smartCopy = typeof(Copyable).GetMethod(
                "CopyOrNull", BindingFlags.Static | BindingFlags.NonPublic);

            System.Diagnostics.Debug.Assert(copyable != null, "ICopyable not found.");
            System.Diagnostics.Debug.Assert(prepareForCopy != null, "Copyable.PrepareArray not found.");
            System.Diagnostics.Debug.Assert(deepArrayCopy != null, "Copyable.DeepCopy not found.");
            System.Diagnostics.Debug.Assert(flatArrayCopy != null, "Array.Copy not found.");
            System.Diagnostics.Debug.Assert(smartCopy != null, "Copyable.CopyOrNull not found.");

            // Copy all instance fields we're allowed to.
            foreach (var f in GetAllFields(type))
            {
                // Find a way to copy the type.
                if (f.FieldType.IsArray && f.IsDefined(typeof(DeepCopyAttribute), true))
                {
                    // Got an array type, get the type stored in it.
                    var elementType = f.FieldType.GetElementType();

                    // Make sure the target one has the right length and is its own instance.
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, f);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldflda, f);
                    generator.EmitCall(OpCodes.Call, prepareForCopy.MakeGenericMethod(elementType), null);

                    // Skip the rest if we have a null value.
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldfld, f);
                    var end = generator.DefineLabel();
                    generator.Emit(OpCodes.Brfalse_S, end);

                    // Copy each element. If we have an array of copyables we create new instances
                    // and copy them in turn via a CopyInto call, otherwise we can use the inbuilt
                    // Array.Copy and do a shallow copy of the array.
                    if (elementType.IsClass && copyable.MakeGenericType(elementType).IsAssignableFrom(elementType))
                    {
                        // Got copyables, use own copier which creates new instances.
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldfld, f);
                        generator.Emit(OpCodes.Ldarg_1);
                        generator.Emit(OpCodes.Ldfld, f);
                        generator.EmitCall(OpCodes.Call, deepArrayCopy.MakeGenericMethod(elementType), null);
                    }
                    else
                    {
                        // Normal fields, just copy by value.
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldfld, f);
                        generator.Emit(OpCodes.Ldarg_1);
                        generator.Emit(OpCodes.Ldfld, f);
                        generator.Emit(OpCodes.Dup);
                        generator.Emit(OpCodes.Ldlen);
                        generator.Emit(OpCodes.Conv_I4);
                        generator.EmitCall(OpCodes.Call, flatArrayCopy, null);
                    }

                    // Done.
                    generator.MarkLabel(end);
                }
                else if (f.FieldType.IsClass && copyable.MakeGenericType(f.FieldType).IsAssignableFrom(f.FieldType))
                {
                    // Got another copyable, set to null or use CopyInto.
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, f);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldflda, f);
                    generator.EmitCall(OpCodes.Call, smartCopy.MakeGenericMethod(f.FieldType), null);
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

        /// <summary>Prepares the target array for copying.</summary>
        /// <typeparam name="T">The data type.</typeparam>
        /// <param name="source">The source array.</param>
        /// <param name="target">The target array.</param>
// ReSharper disable UnusedMember.Local Used via reflection.
        private static void PrepareArray<T>(T[] source, ref T[] target)
// ReSharper restore UnusedMember.Local
        {
            // See if we have something.
            if (source == null)
            {
                // Nope, set the target to null, too.
                target = null;
            }
            else if (target == source || target == null || target.Length != source.Length)
            {
                // Target must be adjusted, create a new array of required length.
                target = new T[source.Length];
            }
        }

        /// <summary>Utility method for creating a deep copy of an array of copyables.</summary>
        /// <typeparam name="T">The data type.</typeparam>
        /// <param name="source">The source array.</param>
        /// <param name="target">The target array.</param>
// ReSharper disable UnusedMember.Local Used via reflection.
        private static void DeepCopy<T>(T[] source, T[] target) where T : class, ICopyable<T>
// ReSharper restore UnusedMember.Local
        {
            // Do a normal copyable copy for each array element.
            for (var i = 0; i < target.Length; ++i)
            {
                CopyOrNull(source[i], ref target[i]);
            }
        }

        /// <summary>Copies a copyable if it is not null, otherwise sets the target to null.</summary>
        /// <typeparam name="T">The data type.</typeparam>
        /// <param name="source">The source instance or null.</param>
        /// <param name="target">The target instance.</param>
        private static void CopyOrNull<T>(T source, ref T target) where T : class, ICopyable<T>
        {
            // Check if we have anything at all.
            if (source == null)
            {
                // Nope, just set the target to null, too.
                target = null;
            }
            else
            {
                // Make sure we have an instance to copy to and that it's of the right type.
                if (target == null || target.GetType() != source.GetType())
                {
                    target = source.NewInstance();
                }
                // Perform type specific copy operations.
                source.CopyInto(target);
            }
        }

        #endregion

    }
}
