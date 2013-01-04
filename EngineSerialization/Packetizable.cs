using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Engine.Serialization
{
    /// <summary>
    /// Use this attribute to mark properties or fields as to be ignored when
    /// copying the object to another instance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class CopyIgnoreAttribute : Attribute
    {
    }

    /// <summary>
    /// Use this attribute to mark properties or fields as to be ignored when
    /// packetizing or depacketzing an object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PacketizerIgnoreAttribute : Attribute
    {
    }

    /// <summary>
    /// Use this attribute to mark a method that should be called after
    /// packetizing an object, for example to allow specialized packetizing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PacketizeAttribute : Attribute
    {
    }

    /// <summary>
    /// Use this attribute to mark a method that should be called before
    /// depacketizing an object, for example to allow cleanup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PreDepacketizeAttribute : Attribute
    {
    }

    /// <summary>
    /// Use this attribute to mark a method that should be called after
    /// depacketizing an object, for example to allow specialized depacketizing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PostDepacketizeAttribute : Attribute
    {
    }

    /// <summary>
    /// The serialization framework works in its core by generating dynamic
    /// functions for each type that it is called for. These functions are
    /// cached. This way the expensive process of analyzing what to serialize
    /// via reflection is only performed once.
    /// 
    /// There are two ways for packetizing an object, which depends on whether
    /// it is a value type or a class type. Value type objects must be written
    /// to a packet using overloads of the <c>Packet.Write</c> function, and
    /// read back using overloads of the <c>Packet.Read</c> function or the
    /// <c>Packet.Read[TypeName]</c> functions. For the dynamic packetizer to
    /// recognize a type it must provide the first two overloads as extension
    /// methods for <see cref="Packet"/>.
    /// 
    /// Class types can be written if they implement <see cref="IPacketizable"/>.
    /// In that case, when written with <see cref="Write{T}"/>, their dynamic
    /// packetizer function will be used, which will in turn check for member
    /// fields that are <see cref="IPacketizable"/> (and thus recurse), or
    /// value types for which an overload is known. If a member is either a
    /// value type with for which no overload was found, or a class type that
    /// is not packetizable an exception will be thrown.
    /// </summary>
    public static class Packetizable
    {
        /// <summary>
        /// Generates a function that will copy all public and private instance fields,
        /// including backing fields for auto properties, from one instance of a type
        /// into the other. It will skip any fields (and properties) that are marked
        /// with the <see cref="CopyIgnoreAttribute"/> attribute.
        /// </summary>
        /// <typeparam name="T">The type to generate the method for.</typeparam>
        /// <returns>
        /// A delegate for the generated method.
        /// </returns>
        public static Action<T, T> CopyInto<T>()
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
            var m = new DynamicMethod("CopyInto", null, new[] {typeof(T), typeof(T)}, declaringType, true);
            var g = m.GetILGenerator();

            // Copy all instance fields.
            foreach (var f in GetAllFields(typeof(T), typeof(CopyIgnoreAttribute)))
            {
                g.Emit(OpCodes.Ldarg_1);
                g.Emit(OpCodes.Ldarg_0);
                g.Emit(OpCodes.Ldfld, f);
                g.Emit(OpCodes.Stfld, f);
            }

            // Finish our dynamic function by returning.
            g.Emit(OpCodes.Ret);

            // Create an instance of our dynamic method (as a delegate) and return it.
            return (Action<T, T>)m.CreateDelegate(typeof(Action<T, T>));
        }

        #region Serialization

        /// <summary>
        /// Adds a namespace with <c>Packet.Write</c> and <c>Packet.Read</c> overloads
        /// for handling one or more value types. This allows the automatic packetizer
        /// logic to handle third party value types.
        /// </summary>
        /// <param name="type">The type.</param>
        public static void AddValueTypeOverloads(Type type)
        {
            Packetizers.Add(type);
        }

        /// <summary>
        /// Writes the specified packetizable. This will work with <c>null</c> values.
        /// The reader must have knowledge about the type of packetizable to expect,
        /// i.e. this can only be read with <see cref="Read{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the packetizable. Note that the actual
        /// type will be used for serialization, and <typeparamref name="T"/> may
        /// be a supertype.</typeparam>
        /// <param name="packet">The packet to write to.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>
        /// The packet, for call chaining.
        /// </returns>
        public static Packet Write<T>(this Packet packet, T data) where T : class, IPacketizable
        {
            if (data != null)
            {
                // Flag that we have something.
                packet.Write(true);
                // Packetize all fields, then give the object a chance to do manual
                // serialization, e.g. of collections and such.
                GetPacketizer(data.GetType())(packet, data);
                //data.Packetize(packet);
                return packet;
            }
            return packet.Write(false);
        }

        /// <summary>
        /// Writes the specified packetizable with its type info. This will work with
        /// <c>null</c> values. The reader does not have to know the actual underlying
        /// type when reading, i.e. this can only be read with <see cref="ReadPacketizableWithTypeInfo{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the packetizable. Note that the actual
        /// type will be used for serialization, and <typeparamref name="T"/> may
        /// be a supertype.</typeparam>
        /// <param name="packet">The packet to write to.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>
        /// This packet, for call chaining.
        /// </returns>
        public static Packet WriteWithTypeInfo<T>(this Packet packet, T data) where T : class, IPacketizable
        {
            if (data != null)
            {
                // Get the underlying type.
                var type = data.GetType();

                // Make sure we have a parameterless public constructor for deserialization.
                System.Diagnostics.Debug.Assert(type.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null);

                // Store the type, which also tells us the value us not null.
                packet.Write(type);

                // Packetize all fields, then give the object a chance to do manual
                // serialization, e.g. of collections and such.
                GetPacketizer(type)(packet, data);
                //data.Packetize(packet);
                return packet;
            }
            return packet.Write((Type)null);
        }

        /// <summary>
        /// Reads a new packetizable of a known type. This may yield in a <c>null</c>
        /// value. The type must match the actual type of the object previously written
        /// using the <see cref="Write{T}"/> method.
        /// 
        /// For example, let <c>A</c> and <c>B</c> be two classes, where <b>B</b> extends
        /// <c>A</c>. While <c>Write&lt;A&gt;()</c> and <c>Write&lt;B&gt;()</c> are
        /// equivalent, even if a <b>B</b> was written using <c>Write&lt;A&gt;()</c>,
        /// it must always be read back using  <c>Read&lt;B&gt;()</c>.
        /// 
        /// If the type cannot be predicted, use <see cref="WriteWithTypeInfo{T}"/>
        /// and <see cref="ReadPacketizableWithTypeInfo{T}"/> instead.
        /// </summary>
        /// <typeparam name="T">The type of the packetizable to read.</typeparam>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static Packet Read<T>(this Packet packet, out T data) where T : class, IPacketizable, new()
        {
            data = packet.ReadPacketizable<T>();
            return packet;
        }

        /// <summary>
        /// Reads a new packetizable of a known type. This may return a <c>null</c>
        /// value. The type must match the actual type of the object previously written
        /// using the <see cref="Write{T}"/> method.
        /// 
        /// For example, let <c>A</c> and <c>B</c> be two classes, where <b>B</b> extends
        /// <c>A</c>. While <c>Write&lt;A&gt;()</c> and <c>Write&lt;B&gt;()</c> are
        /// equivalent, even if a <b>B</b> was written using <c>Write&lt;A&gt;()</c>,
        /// it must always be read back using  <c>Read&lt;B&gt;()</c>.
        /// 
        /// If the type cannot be predicted, use <see cref="WriteWithTypeInfo{T}"/>
        /// and <see cref="ReadPacketizableWithTypeInfo{T}"/> instead.
        /// </summary>
        /// <typeparam name="T">The type of the packetizable to read.</typeparam>
        /// <param name="packet">The packet.</param>
        /// <returns>The read data.</returns>
        public static T ReadPacketizable<T>(this Packet packet) where T : class, IPacketizable, new()
        {
            // See if we have anything at all, or if the written value was null.
            if (packet.ReadBoolean())
            {
                // Read all fields, then give the object a chance to do manual
                // deserialization, e.g. for collections.
                var result = new T();
                //result.PreDepacketize(packet);
                GetDepacketizer(typeof(T))(packet, result);
                //result.PostDepacketize(packet);
                return result;
            }
            return null;
        }

        /// <summary>
        /// Reads a packetizable of an arbitrary type, which should be a subtype of the
        /// specified type parameter <typeparamref name="T"/>. This may return <c>null</c>
        /// if the written value was <c>null</c>.
        /// </summary>
        /// <typeparam name="T">Supertype of the type actually being read.</typeparam>
        /// <param name="packet">The packet.</param>
        /// <returns>
        /// The read value.
        /// </returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static T ReadPacketizableWithTypeInfo<T>(this Packet packet) where T : class, IPacketizable
        {
            // Get the type.
            var type = packet.ReadType();
            if (type != null)
            {
                // Read all fields, then give the object a chance to do manual
                // deserialization, e.g. for collections.
                var result = (T)Activator.CreateInstance(type);
                //result.PreDepacketize(packet);
                GetDepacketizer(type)(packet, result);
                //result.PostDepacketize(packet);
                return result;
            }
            return null;
        }

        /// <summary>
        /// Reads a packetizable of a known type (from an existing instance).
        /// 
        /// <para>
        /// May yield <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        /// <param name="result">The object to write read data to.</param>
        public static Packet ReadPacketizableInto(this Packet packet, IPacketizable result)
        {
            // See if we have anything at all, or if the written value was null.
            if (packet.ReadBoolean())
            {
                // Read all fields, then give the object a chance to do manual
                // deserialization, e.g. for collections.
                //result.PreDepacketize(packet);
                GetDepacketizer(result.GetType())(packet, result);
                //result.PostDepacketize(packet);
                return packet;
            }
            throw new InvalidOperationException("Cannot read 'null' into existing instance.");
        }

        #endregion

        #region Internals

        /// <summary>
        /// Cached list of type packetizers, to avoid rebuilding the methods over and over.
        /// </summary>
        private static readonly Dictionary<Type, Tuple<Packetize, Depacketize>> PacketizerCache =
            new Dictionary<Type, Tuple<Packetize, Depacketize>>();

        /// <summary>
        /// Signature of a packetizing function.
        /// </summary>
        private delegate Packet Packetize(Packet packet, IPacketizable data);

        /// <summary>
        /// Signature of a depacketizing function.
        /// </summary>
        private delegate Packet Depacketize(Packet packet, IPacketizable data);

        /// <summary>
        /// Gets the packetizer from the cache, or creates it if it doesn't exist yet
        /// and adds it to the cache.
        /// </summary>
        private static Packetize GetPacketizer(Type type)
        {
            Tuple<Packetize, Depacketize> pair;
            if (!PacketizerCache.ContainsKey(type))
            {
                pair = CreatePacketizer(type);
                PacketizerCache.Add(type, pair);
            }
            else
            {
                pair = PacketizerCache[type];
            }
            return pair.Item1;
        }

        /// <summary>
        /// Gets the depacketizer from the cache, or creates it if it doesn't exist yet
        /// and adds it to the cache.
        /// </summary>
        private static Depacketize GetDepacketizer(Type type)
        {
            Tuple<Packetize, Depacketize> pair;
            if (!PacketizerCache.ContainsKey(type))
            {
                pair = CreatePacketizer(type);
                PacketizerCache.Add(type, pair);
            }
            else
            {
                pair = PacketizerCache[type];
            }
            return pair.Item2;
        }

        /// <summary>
        /// Generates two function of which one will packetize all public and private
        /// instance fields, including backing fields for auto properties, into the
        /// specified packet. The other will read the written data back into the fields
        /// they came from. It will skip any fields (and properties) that are marked
        /// with the <see cref="PacketizerIgnoreAttribute"/> attribute.
        /// This function may indirectly recurse. It calls any methods with the
        /// <see cref="PacketizeAttribute"/> attribute after serialization, and any
        /// methods with the <see cref="PreDepacketizeAttribute"/> before as well as
        /// those with the <see cref="PostDepacketizeAttribute"/> after depacketizing
        /// from a packet, respectively. If a value does not implement <see cref="IPacketizable"/>,
        /// it will only be handled if there is a known <c>Write</c> overload
        /// for <see cref="Packet"/>. Otherwise an exception is thrown.
        /// </summary>
        /// <param name="type">The type to generate the packetizer for.</param>
        /// <returns>
        /// Two delegates for the generated methods.
        /// </returns>
        private static Tuple<Packetize, Depacketize> CreatePacketizer(Type type)
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
            var writeInt32 = typeof(Packet)
                .GetMethod("Write", new[] { typeof(int) });
            var readInt32 = typeof(Packet)
                .GetMethod("Read", new[] { typeof(int).MakeByRefType() });
            var writePacketizable = typeof(Packetizable)
                .GetMethod("Write").MakeGenericMethod(type);
            var readPacketizable = typeof(Packetizable)
                .GetMethod("ReadPacketizableInto", new[] { typeof(Packet), typeof(IPacketizable) });

            // Generate dynamic methods for the specified type.
            var packetizeMethod = new DynamicMethod(
                "Packetize", typeof(Packet), new[] { typeof(Packet), typeof(IPacketizable) }, declaringType, true);
            var depacketizeMethod = new DynamicMethod(
                "Depacketize", typeof(Packet), new[] { typeof(Packet), typeof(IPacketizable) }, declaringType, true);

            // Get the code generators.
            var packetizeGenerator = packetizeMethod.GetILGenerator();
            var depacketizeGenerator = depacketizeMethod.GetILGenerator();

            // Call pre-depacketize method for depacketization if a callback exists, to
            // allow some cleanup where necessary.
            foreach (var callback in type
                .GetMethods(BindingFlags.Instance |
                            BindingFlags.Public)
                .Where(m => m.IsDefined(typeof(PreDepacketizeAttribute), true) &&
                            m.GetParameters().Length == 0 &&
                            m.ReturnType == typeof(void)))
            {
                depacketizeGenerator.Emit(OpCodes.Ldarg_1);
                depacketizeGenerator.EmitCall(OpCodes.Callvirt, callback, null);
            }

            // Load packet as onto the stack. This will always remain the lowest entry on
            // the stack of our packetizer. This is an optimization the compiler could not
            // even do, because the returned packet reference may theoretically differ. In
            // practice it never will/must, though.
            packetizeGenerator.Emit(OpCodes.Ldarg_0);
            depacketizeGenerator.Emit(OpCodes.Ldarg_0);

            // Handle all instance fields.
            foreach (var f in GetAllFields(type, typeof(PacketizerIgnoreAttribute)))
            {
                // Find a write and read function for the type.
                if (typeof(IPacketizable).IsAssignableFrom(f.FieldType))
                {
                    // Got a packetizable, call our own write method with it.

                    // Build serializer part.
                    packetizeGenerator.Emit(OpCodes.Ldarg_1);
                    packetizeGenerator.Emit(OpCodes.Ldfld, f);
                    packetizeGenerator.EmitCall(OpCodes.Call, writePacketizable, null);

                    // Build deserializer part.
                    depacketizeGenerator.Emit(OpCodes.Ldarg_1);
                    depacketizeGenerator.Emit(OpCodes.Ldfld, f);
                    depacketizeGenerator.EmitCall(OpCodes.Call, readPacketizable, null);
                }
                else if (f.FieldType.IsEnum)
                {
                    // Special treatment for enums -- treat them as integer.

                    // Build serializer part.
                    packetizeGenerator.Emit(OpCodes.Ldarg_1);
                    packetizeGenerator.Emit(OpCodes.Ldfld, f);
                    packetizeGenerator.EmitCall(OpCodes.Call, writeInt32, null);

                    // Build deserializer part.
                    depacketizeGenerator.Emit(OpCodes.Ldarg_1);
                    depacketizeGenerator.Emit(OpCodes.Ldflda, f);
                    depacketizeGenerator.EmitCall(OpCodes.Call, readInt32, null);
                }
                else
                {
                    // Not a packetizable. Let's look for overloads that support this type.
                    var writeType = FindWriteMethod(f.FieldType);
                    var readType = FindReadMethod(f.FieldType);

                    // Make sure we can handle this type.
                    if (writeType == null || readType == null)
                    {
                        throw new ArgumentException(string.Format("Cannot build serializer for this type, could not find write method for field '{0}' of type '{1}'.", f.Name, f.FieldType.Name));
                    }

                    // Build serializer part.
                    packetizeGenerator.Emit(OpCodes.Ldarg_1);
                    packetizeGenerator.Emit(OpCodes.Ldfld, f);
                    packetizeGenerator.EmitCall(OpCodes.Call, writeType, null);

                    // Build deserializer part.
                    depacketizeGenerator.Emit(OpCodes.Ldarg_1);
                    depacketizeGenerator.Emit(OpCodes.Ldflda, f);
                    depacketizeGenerator.EmitCall(OpCodes.Call, readType, null);
                }
            }

            // Call post-packetize method for packetization if a callback exists, to
            // allow some specialized packetization where necessary.
            foreach (var callback in type
                .GetMethods(BindingFlags.Instance |
                            BindingFlags.Public)
                .Where(m => m.IsDefined(typeof(PacketizeAttribute), true) &&
                            m.GetParameters().Length == 1 &&
                            m.GetParameters()[0].ParameterType == typeof(Packet) &&
                            m.ReturnType == typeof(Packet)))
            {
                packetizeGenerator.Emit(OpCodes.Ldarg_1);
                packetizeGenerator.Emit(OpCodes.Ldarg_0);
                packetizeGenerator.EmitCall(OpCodes.Callvirt, callback, null);
                packetizeGenerator.Emit(OpCodes.Pop);
            }

            // Call post-depacketize method for depacketization if a callback exists, to
            // allow some specialized depacketization where necessary.
            foreach (var callback in type
                .GetMethods(BindingFlags.Instance |
                            BindingFlags.Public)
                .Where(m => m.IsDefined(typeof(PostDepacketizeAttribute), true) &&
                            m.GetParameters().Length == 1 &&
                            m.GetParameters()[0].ParameterType == typeof(Packet) &&
                            m.ReturnType == typeof(void)))
            {
                depacketizeGenerator.Emit(OpCodes.Ldarg_1);
                depacketizeGenerator.Emit(OpCodes.Ldarg_0);
                depacketizeGenerator.EmitCall(OpCodes.Callvirt, callback, null);
            }

            // Finish our dynamic functions by returning.
            packetizeGenerator.Emit(OpCodes.Ret);
            depacketizeGenerator.Emit(OpCodes.Ret);

            // Create an instances of our dynamic methods (as delegates) and return them.
            var packetizer = (Packetize)packetizeMethod.CreateDelegate(typeof(Packetize));
            var depacketizer = (Depacketize)depacketizeMethod.CreateDelegate(typeof(Depacketize));
            return Tuple.Create(packetizer, depacketizer);
        }

        /// <summary>
        /// List of types providing serialization/deserialization methods for the
        /// <see cref="Packet"/> class.
        /// </summary>
        private static readonly HashSet<Type> Packetizers = new HashSet<Type>();

        /// <summary>
        /// Used to find a Packet.Write overload for the specified type.
        /// </summary>
        private static MethodInfo FindWriteMethod(Type type)
        {
            // Look for built-in methods.
            {
                var packetizer = typeof(Packet).GetMethod("Write", new[] {type});
                if (packetizer != null && packetizer.ReturnType == typeof(Packet))
                {
                    return packetizer;
                }
            }
            // Look for extension methods.
            foreach (var group in Packetizers)
            {
                var packetizer = group.GetMethod("Write", new[] {typeof(Packet), type});
                if (packetizer != null && packetizer.ReturnType == typeof(Packet))
                {
                    return packetizer;
                }

            }
            return null;
        }

        /// <summary>
        /// Used to find a Packet.Read overload for the specified type.
        /// </summary>
        private static MethodInfo FindReadMethod(Type type)
        {
            // Look for built-in methods.
            {
                var depacketizer = typeof(Packet).GetMethod("Read", new[] {type.MakeByRefType()});
                if (depacketizer != null)
                {
                    return depacketizer;
                }
            }
            // Look for extension methods.
            foreach (var group in Packetizers)
            {
                var depacketizer = group.GetMethod("Read", new[] {typeof(Packet), type.MakeByRefType()});
                if (depacketizer != null)
                {
                    return depacketizer;
                }
            }
            return null;
        }

        /// <summary>
        /// Utility method the gets a list of all fields in a type, including
        /// this in its base classes all the way up the hierarchy. Fields with
        /// the specified <paramref name="ignoreMarker"/> attribute are not
        /// returned. This will also include automatially generated field
        /// backing properties, unless the property has said attribute.
        /// </summary>
        /// <param name="type">The type to start parsing at.</param>
        /// <param name="ignoreMarker">The attribute type used to mark fields
        /// as ignored.</param>
        /// <returns>The list of all relevant fields.</returns>
        private static IEnumerable<FieldInfo> GetAllFields(Type type, Type ignoreMarker)
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
                                    !f.IsDefined(ignoreMarker, true) &&
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
                                    !p.IsDefined(ignoreMarker, true) &&
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
