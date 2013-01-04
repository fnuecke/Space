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
    /// This provides factory methods for generating dynamic functions taking
    /// care of the most common serialization cases.
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
                return data.Packetize(GetPacketizer(data.GetType())(packet, data));
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
                return data.Packetize(GetPacketizer(type)(packet, data));
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
                result.PreDepacketize(packet);
                GetDepacketizer(typeof(T))(packet, result);
                result.PostDepacketize(packet);
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
                result.PreDepacketize(packet);
                GetDepacketizer(type)(packet, result);
                result.PostDepacketize(packet);
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
                result.PreDepacketize(packet);
                GetDepacketizer(result.GetType())(packet, result);
                result.PostDepacketize(packet);
                return packet;
            }
            throw new InvalidOperationException("Cannot read 'null' into existing instance.");
        }

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
        /// This function may indirectly recurse. It calls the <see cref="IPacketizable.Packetize"/>
        /// method of any <see cref="IPacketizable"/>s it encounteres, which may in turn
        /// call a realization of this function. If a value does not implement the
        /// interface, it will only be handled if there is a known <c>Write</c> overload
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
            var writeInt32 = typeof(Packet).GetMethod("Write", new[] {typeof(int)});
            var readInt32 = typeof(Packet).GetMethod("Read", new[] {typeof(int).MakeByRefType()});
            var writePacketizable = typeof(Packetizable).GetMethod("Write").MakeGenericMethod(type);
            var readPacketizable = typeof(Packetizable).GetMethod("ReadPacketizableInto", new[] { typeof(Packet), typeof(IPacketizable) });

            // Generate dynamic methods for the specified type.
            var packetizeMethod = new DynamicMethod(
                "Packetize", typeof(Packet), new[] { typeof(Packet), typeof(IPacketizable) }, declaringType, true);
            var packetizeGenerator = packetizeMethod.GetILGenerator();
            var depacketizeMethod = new DynamicMethod(
                "Depacketize", typeof(Packet), new[] { typeof(Packet), typeof(IPacketizable) }, declaringType, true);
            var depacketizeGenerator = depacketizeMethod.GetILGenerator();

            // Load packet as onto the stack. This will always remain the lowest entry on
            // the stack of our packetizer. This is an optimization the compiler could not
            // even do, because the returned packet reference may theoretically differ. In
            // practice it never will/must, though.
            packetizeGenerator.Emit(OpCodes.Ldarg_0);
            // -> 1: packet
            depacketizeGenerator.Emit(OpCodes.Ldarg_0);
            // -> 1: packet

#if DEBUG
            //packetizeGenerator.EmitWriteLine("BEGIN Packetize " + type.Name);
            //depacketizeGenerator.EmitWriteLine("BEGIN Depacketize " + type.Name);
#endif

            // Handle all instance fields.
            foreach (var f in GetAllFields(type, typeof(PacketizerIgnoreAttribute)))
            {
                // Find a write and read function for the type.
                if (typeof(IPacketizable).IsAssignableFrom(f.FieldType))
                {
                    // Build serializer part.
                    // -> 1: packet -- already on the stack.
                    packetizeGenerator.Emit(OpCodes.Ldarg_1);
                    // -> 2: object -- to get field.
                    packetizeGenerator.Emit(OpCodes.Ldfld, f);
                    // -> 2: field -- as argument for call.
                    packetizeGenerator.EmitCall(OpCodes.Call, writePacketizable, null);
                    // -> 1: packet

                    // Build deserializer part.
                    // -> 1: packet -- already on the stack.
                    depacketizeGenerator.Emit(OpCodes.Ldarg_1);
                    // -> 2: object -- to get field to set after call.
                    depacketizeGenerator.Emit(OpCodes.Ldfld, f);
                    // -> 2: field ref
                    depacketizeGenerator.EmitCall(OpCodes.Call, readPacketizable, null);
                    // -> 1: packet
                }
                else if (f.FieldType.IsEnum)
                {
                    // Special treatment for enums. Convert the to integer.

                    // Build serializer part.
                    // -> 1: packet -- already on the stack.
                    packetizeGenerator.Emit(OpCodes.Ldarg_1);
                    // -> 2: object -- to get field.
                    packetizeGenerator.Emit(OpCodes.Ldfld, f);
                    // -> 2: field -- now convert enum to integer.
                    packetizeGenerator.Emit(OpCodes.Conv_I4);
                    // -> 2: int -- as argument for call.
                    packetizeGenerator.EmitCall(OpCodes.Call, writeInt32, null);
                    // -> 1: packet

                    // Build deserializer part.
                    // -> 1: packet -- already on the stack.
                    depacketizeGenerator.Emit(OpCodes.Ldarg_1);
                    // -> 2: object -- to get field to set after call.
                    depacketizeGenerator.Emit(OpCodes.Ldflda, f);
                    // -> 2: field ref
                    depacketizeGenerator.EmitCall(OpCodes.Call, readInt32, null);
                    // -> 1: packet
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
                    // -> 1: packet -- already on the stack.
                    packetizeGenerator.Emit(OpCodes.Ldarg_1);
                    // -> 2: object -- to get field.
                    packetizeGenerator.Emit(OpCodes.Ldfld, f);
                    // -> 2: field -- as argument for call.
                    packetizeGenerator.EmitCall(OpCodes.Call, writeType, null);
                    // -> 1: packet

                    // Build deserializer part.
                    // -> 1: packet -- already on the stack.
                    depacketizeGenerator.Emit(OpCodes.Ldarg_1);
                    // -> 2: object -- to get field to set after call.
                    depacketizeGenerator.Emit(OpCodes.Ldflda, f);
                    // -> 2: field ref
                    depacketizeGenerator.EmitCall(OpCodes.Call, readType, null);
                    // -> 1: packet
                }
            }

#if DEBUG
            //var totalBytes = packetizeGenerator.DeclareLocal(typeof(int));
            //packetizeGenerator.Emit(OpCodes.Dup);
            //// -> 2: packet
            //packetizeGenerator.EmitCall(OpCodes.Call, typeof(Packet).GetProperty("Length").GetGetMethod(), null);
            //// -> 2: length
            //packetizeGenerator.Emit(OpCodes.Stloc, totalBytes);
            //// -> 1: packet
            //packetizeGenerator.Emit(OpCodes.Ldstr, "Total bytes written: {0}");
            //// -> 2: format string
            //packetizeGenerator.Emit(OpCodes.Ldloc, totalBytes);
            //// -> 3: length
            //packetizeGenerator.Emit(OpCodes.Box, typeof(int));
            //// -> 3: boxed length
            //packetizeGenerator.EmitCall(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new[] { typeof(string), typeof(object) }), null);
            //// -> 1: packet

            //var packet = depacketizeGenerator.DeclareLocal(typeof(Packet));
            //var readBytes = depacketizeGenerator.DeclareLocal(typeof(int));
            //depacketizeGenerator.Emit(OpCodes.Stloc, packet);
            //depacketizeGenerator.Emit(OpCodes.Ldloc, packet);
            //depacketizeGenerator.Emit(OpCodes.Dup);
            //// -> 2: packet
            //depacketizeGenerator.EmitCall(OpCodes.Call, typeof(Packet).GetProperty("Length").GetGetMethod(), null);
            //// -> 2: length
            //depacketizeGenerator.Emit(OpCodes.Ldloc, packet);
            //// -> 3: packet
            //depacketizeGenerator.EmitCall(OpCodes.Call, typeof(Packet).GetProperty("Available").GetGetMethod(), null);
            //// -> 3: available
            //depacketizeGenerator.Emit(OpCodes.Sub);
            //// -> 2: length - available := read
            //depacketizeGenerator.Emit(OpCodes.Stloc, readBytes);
            //// -> 1: packet
            //depacketizeGenerator.Emit(OpCodes.Ldstr, "Total bytes read: {0}");
            //// -> 2: format
            //depacketizeGenerator.Emit(OpCodes.Ldloc, readBytes);
            //// -> 3: read
            //depacketizeGenerator.Emit(OpCodes.Box, typeof(int));
            //// -> 3: boxed read
            //depacketizeGenerator.EmitCall(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new[] { typeof(string), typeof(object) }), null);
            //// -> 1: packet

            //packetizeGenerator.EmitWriteLine("END Packetize " + type.Name);
            //depacketizeGenerator.EmitWriteLine("END Depacketize " + type.Name);
#endif

            // Finish our dynamic functions by returning.
            packetizeGenerator.Emit(OpCodes.Ret);
            // -> 0
            depacketizeGenerator.Emit(OpCodes.Ret);
            // -> 0

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
            var packetizer = typeof(Packet).GetMethod("Write", new[] { type });
            if (packetizer != null && packetizer.ReturnType == typeof(Packet))
            {
                return packetizer;
            }
            // Look for extension methods.
            foreach (var group in Packetizers)
            {
                packetizer = group.GetMethod("Write", new[] {typeof(Packet), type});
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
            var depacketizer = typeof(Packet).GetMethod("Read", new[] { type.MakeByRefType() });
            if (depacketizer != null)
            {
                return depacketizer;
            }
            // Look for extension methods.
            foreach (var group in Packetizers)
            {
                depacketizer = group.GetMethod("Read", new[] {typeof(Packet), type.MakeByRefType()});
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
                                    !f.IsDefined(ignoreMarker, false) &&
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
                                    !p.IsDefined(ignoreMarker, false) &&
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
