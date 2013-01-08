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
    /// packetizing or depacketzing an object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PacketizerIgnoreAttribute : Attribute
    {
    }

    /// <summary>
    /// Use this attribute to mark properties or fields as to be created when
    /// depacketzing an object. Normally packetizables are read using
    /// <see cref="Packetizable.ReadPacketizableInto"/> to minimize allocations,
    /// but sometimes it cannot be guaranteed that an instance will already
    /// exist. In these cases specify this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PacketizerCreateAttribute : Attribute
    {
    }

    /// <summary>
    /// Use this attribute to mark a method that should be called after
    /// packetizing an object, for example to allow specialized packetizing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnPacketizeAttribute : Attribute
    {
    }

    /// <summary>
    /// Use this attribute to mark a method that should be called before
    /// depacketizing an object, for example to allow cleanup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnPreDepacketizeAttribute : Attribute
    {
    }

    /// <summary>
    /// Use this attribute to mark a method that should be called after
    /// depacketizing an object, for example to allow specialized depacketizing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnPostDepacketizeAttribute : Attribute
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
        public static IWritablePacket Write<T>(this IWritablePacket packet, T data) where T : class, IPacketizable
        {
            if (data != null)
            {
                // Flag that we have something.
                packet.Write(true);
                // Packetize all fields, then give the object a chance to do manual
                // serialization, e.g. of collections and such.
                try
                {
                    GetPacketizer(data.GetType())(packet, data);
                }
                catch (Exception ex)
                {
                    throw new PacketException("Failed serializing " + typeof(T).Name, ex);
                }
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
        public static IWritablePacket WriteWithTypeInfo<T>(this IWritablePacket packet, T data) where T : class, IPacketizable
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
                try
                {
                    GetPacketizer(type)(packet, data);
                }
                catch (Exception ex)
                {
                    throw new PacketException("Failed serializing " + type.Name, ex);
                }
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
        public static IReadablePacket Read<T>(this IReadablePacket packet, out T data) where T : class, IPacketizable, new()
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
        public static T ReadPacketizable<T>(this IReadablePacket packet) where T : class, IPacketizable, new()
        {
            // See if we have anything at all, or if the written value was null.
            if (packet.ReadBoolean())
            {
                // Read all fields, then give the object a chance to do manual
                // deserialization, e.g. for collections.
                var result = new T();
                try
                {
                    GetDepacketizer(typeof(T))(packet, result);
                }
                catch (Exception ex)
                {
                    throw new PacketException("Failed deserializing " + typeof(T).Name, ex);
                }
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
        public static T ReadPacketizableWithTypeInfo<T>(this IReadablePacket packet) where T : class, IPacketizable
        {
            // Get the type.
            var type = packet.ReadType();
            if (type != null)
            {
                // Read all fields, then give the object a chance to do manual
                // deserialization, e.g. for collections.
                var result = (T)Activator.CreateInstance(type);
                try
                {
                    GetDepacketizer(type)(packet, result);
                }
                catch (Exception ex)
                {
                    throw new PacketException("Failed deserializing " + type.Name, ex);
                }
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
        public static IReadablePacket ReadPacketizableInto(this IReadablePacket packet, IPacketizable result)
        {
            // We need something to write to.
            if (result == null)
            {
                throw new ArgumentNullException("result", "Cannot depacketize into null reference.");
            }

            // See if we have anything at all, or if the written value was null.
            if (packet.ReadBoolean())
            {
                // Read all fields, then give the object a chance to do manual
                // deserialization, e.g. for collections.
                try
                {
                    GetDepacketizer(result.GetType())(packet, result);
                }
                catch (Exception ex)
                {
                    throw new PacketException("Failed deserializing packetizable", ex);
                }
                return packet;
            }
            throw new InvalidOperationException("Cannot read 'null' into existing instance.");
        }

        #endregion

        #region Internals

        /// <summary>
        /// Signature of a packetizing function.
        /// </summary>
        private delegate IWritablePacket Packetizer(IWritablePacket packet, IPacketizable data);

        /// <summary>
        /// Signature of a depacketizing function.
        /// </summary>
        private delegate IReadablePacket Depacketizer(IReadablePacket packet, IPacketizable data);

        /// <summary>
        /// Cached list of type packetizers, to avoid rebuilding the methods over and over.
        /// </summary>
        private static readonly Dictionary<Type, Tuple<Packetizer, Depacketizer>> PacketizerCache =
            new Dictionary<Type, Tuple<Packetizer, Depacketizer>>();

        /// <summary>
        /// List of types providing serialization/deserialization methods for the
        /// <see cref="Packet"/> class.
        /// </summary>
        private static readonly HashSet<Type> Packetizers = new HashSet<Type>
        {
            typeof(WritablePacketExtensions),
            typeof(ReadablePacketExtensions)
        };

        /// <summary>
        /// Gets the packetizer from the cache, or creates it if it doesn't exist yet
        /// and adds it to the cache.
        /// </summary>
        private static Packetizer GetPacketizer(Type type)
        {
            Tuple<Packetizer, Depacketizer> pair;
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
        private static Depacketizer GetDepacketizer(Type type)
        {
            Tuple<Packetizer, Depacketizer> pair;
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
        /// <see cref="OnPacketizeAttribute"/> attribute after serialization, and any
        /// methods with the <see cref="OnPreDepacketizeAttribute"/> before as well as
        /// those with the <see cref="OnPostDepacketizeAttribute"/> after depacketizing
        /// from a packet, respectively. If a value does not implement <see cref="IPacketizable"/>,
        /// it will only be handled if there is a known <c>Write</c> overload
        /// for <see cref="Packet"/>. Otherwise an exception is thrown.
        /// </summary>
        /// <param name="type">The type to generate the packetizer for.</param>
        /// <returns>
        /// Two delegates for the generated methods.
        /// </returns>
        private static Tuple<Packetizer, Depacketizer> CreatePacketizer(Type type)
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
            var writeInt32 = typeof(IWritablePacket)
                .GetMethod("Write", new[] { typeof(int) });
            var readInt32 = typeof(ReadablePacketExtensions)
                .GetMethod("Read", new[] { typeof(IReadablePacket), typeof(int).MakeByRefType() });
            var writePacketizable = typeof(Packetizable)
                .GetMethod("Write").MakeGenericMethod(type);
            var readPacketizable = typeof(Packetizable)
                .GetMethod("ReadPacketizable", new[] {typeof(IReadablePacket)});
            var readPacketizableInto = typeof(Packetizable)
                .GetMethod("ReadPacketizableInto", new[] { typeof(IReadablePacket), typeof(IPacketizable) });

            System.Diagnostics.Debug.Assert(writeInt32 != null);
            System.Diagnostics.Debug.Assert(readInt32 != null);
            System.Diagnostics.Debug.Assert(writePacketizable != null);
            System.Diagnostics.Debug.Assert(readPacketizable != null);
            System.Diagnostics.Debug.Assert(readPacketizableInto != null);

            // Generate dynamic methods for the specified type.
            var packetizeMethod = new DynamicMethod(
                "Packetize", typeof(IWritablePacket), new[] { typeof(IWritablePacket), typeof(IPacketizable) }, declaringType, true);
            var depacketizeMethod = new DynamicMethod(
                "Depacketize", typeof(IReadablePacket), new[] { typeof(IReadablePacket), typeof(IPacketizable) }, declaringType, true);

            // Get the code generators.
            var packetizeGenerator = packetizeMethod.GetILGenerator();
            var depacketizeGenerator = depacketizeMethod.GetILGenerator();

            // Call pre-depacketize method for depacketization if a callback exists, to
            // allow some cleanup where necessary.
            foreach (var callback in type
                .GetMethods(BindingFlags.Instance |
                            BindingFlags.Public)
                .Where(m => m.IsDefined(typeof(OnPreDepacketizeAttribute), true)))
            {
                if (callback.GetParameters().Length != 0)
                {
                    throw new ArgumentException(string.Format("PreDepacketize callback {0}.{1} has invalid signature, must be (void => ?).", type.Name, callback.Name));
                }
                depacketizeGenerator.Emit(OpCodes.Ldarg_1);
                depacketizeGenerator.EmitCall(OpCodes.Callvirt, callback, null);
                if (callback.ReturnType != typeof(void))
                {
                    packetizeGenerator.Emit(OpCodes.Pop);
                }
            }

            // Load packet as onto the stack. This will always remain the lowest entry on
            // the stack of our packetizer. This is an optimization the compiler could not
            // even do, because the returned packet reference may theoretically differ. In
            // practice it never will/must, though.
            packetizeGenerator.Emit(OpCodes.Ldarg_0);
            depacketizeGenerator.Emit(OpCodes.Ldarg_0);

            // Handle all instance fields.
            foreach (var f in GetAllFields(type))
            {
                // Skip functions (event handlers in particular) because we have no
                // way of serializing them.
                if (typeof(Delegate).IsAssignableFrom(f.FieldType))
                {
                    continue;
                }

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
                    if (f.IsDefined(typeof(PacketizerCreateAttribute), true))
                    {
                        // Data should be read by creating a new instance.
                        depacketizeGenerator.Emit(OpCodes.Ldarg_0);
                        depacketizeGenerator.EmitCall(OpCodes.Call, readPacketizable.MakeGenericMethod(f.FieldType), null);
                        depacketizeGenerator.Emit(OpCodes.Stfld, f);
                    }
                    else
                    {
                        // Data should be read into existing instance.
                        depacketizeGenerator.Emit(OpCodes.Ldfld, f);
                        depacketizeGenerator.EmitCall(OpCodes.Call, readPacketizableInto, null);
                    }
                }
                else if (f.FieldType.IsEnum || f.FieldType == typeof(Enum))
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
                        throw new ArgumentException(string.Format("Cannot build packetizer for type {0}, could not find write method for field '{1}' of type '{2}'.", type.Name, f.Name, f.FieldType.Name));
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
                .Where(m => m.IsDefined(typeof(OnPacketizeAttribute), true)))
            {
                if (callback.GetParameters().Length != 1 ||
                    callback.GetParameters()[0].ParameterType != typeof(IWritablePacket))
                {
                    throw new ArgumentException(string.Format("Packetize callback {0}.{1} has invalid signature, must be (IWritablePacket => ?).", type.Name, callback.Name));
                }
                packetizeGenerator.Emit(OpCodes.Ldarg_1);
                packetizeGenerator.Emit(OpCodes.Ldarg_0);
                packetizeGenerator.EmitCall(OpCodes.Callvirt, callback, null);
                if (callback.ReturnType != typeof(void))
                {
                    packetizeGenerator.Emit(OpCodes.Pop);
                }
            }

            // Call post-depacketize method for depacketization if a callback exists, to
            // allow some specialized depacketization where necessary.
            foreach (var callback in type
                .GetMethods(BindingFlags.Instance |
                            BindingFlags.Public)
                .Where(m => m.IsDefined(typeof(OnPostDepacketizeAttribute), true)))
            {
                if (callback.GetParameters().Length != 1 ||
                    callback.GetParameters()[0].ParameterType != typeof(IReadablePacket))
                {
                    throw new ArgumentException(string.Format("PostDepacketize callback {0}.{1} has invalid signature, must be (IReadablePacket => ?).", type.Name, callback.Name));
                }
                depacketizeGenerator.Emit(OpCodes.Ldarg_1);
                depacketizeGenerator.Emit(OpCodes.Ldarg_0);
                depacketizeGenerator.EmitCall(OpCodes.Callvirt, callback, null);
                if (callback.ReturnType != typeof(void))
                {
                    depacketizeGenerator.Emit(OpCodes.Pop);
                }
            }

            // Finish our dynamic functions by returning.
            packetizeGenerator.Emit(OpCodes.Ret);
            depacketizeGenerator.Emit(OpCodes.Ret);

            // Create an instances of our dynamic methods (as delegates) and return them.
            var packetizer = (Packetizer)packetizeMethod.CreateDelegate(typeof(Packetizer));
            var depacketizer = (Depacketizer)depacketizeMethod.CreateDelegate(typeof(Depacketizer));
            return Tuple.Create(packetizer, depacketizer);
        }

        /// <summary>
        /// Used to find a Packet.Write overload for the specified type.
        /// </summary>
        private static MethodInfo FindWriteMethod(Type type)
        {
            // Look for built-in methods.
            {
                var packetizer = typeof(IWritablePacket).GetMethod("Write", new[] {type});
                if (packetizer != null && packetizer.ReturnType == typeof(IWritablePacket))
                {
                    return packetizer;
                }
            }
            // Look for extension methods.
            foreach (var group in Packetizers)
            {
                var packetizer = group.GetMethod("Write", new[] {typeof(IWritablePacket), type});
                if (packetizer != null && packetizer.IsStatic && packetizer.ReturnType == typeof(IWritablePacket))
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
                var depacketizer = typeof(IReadablePacket).GetMethod("Read", new[] {type.MakeByRefType()});
                if (depacketizer != null && depacketizer.ReturnType == typeof(IReadablePacket))
                {
                    return depacketizer;
                }
            }
            // Look for extension methods.
            foreach (var group in Packetizers)
            {
                var depacketizer = group.GetMethod("Read", new[] {typeof(IReadablePacket), type.MakeByRefType()});
                if (depacketizer != null && depacketizer.IsStatic && depacketizer.ReturnType == typeof(IReadablePacket))
                {
                    return depacketizer;
                }
            }
            return null;
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
                        //   when we parse the properties.
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

            // And we're done.
            return result;
        }

        #endregion
    }
}
