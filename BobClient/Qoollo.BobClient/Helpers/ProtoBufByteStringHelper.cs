using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Qoollo.BobClient.Helpers
{
    /// <summary>
    /// Helper to work with ByteString
    /// </summary>
    internal static class ProtoBufByteStringHelper
    {
        private delegate ByteString CreateFromByteArrayDelegate(byte[] array);
        private static CreateFromByteArrayDelegate _createFromByteArrayDelegate;


        private delegate void ExtractObjectIndexFromMemoryDelegate(ref ReadOnlyMemory<byte> mem, out object obj, out int index);
        private static ExtractObjectIndexFromMemoryDelegate _extractObjectIndexFromMemoryDelegate;
        private static System.Reflection.FieldInfo _readOnlyMemory_object;
        private static System.Reflection.FieldInfo _readOnlyMemory_index;
        private static int _canExtractByteArrayOptimized;

        private const int CanExtractByteArrayOptimized_NotInitialized = 0;
        private const int CanExtractByteArrayOptimized_Ok = 1;
        private const int CanExtractByteArrayOptimized_Unavailable = -1;

        private const int ExtractObjectIndexFromMemoryWithReflectionThreshold = 64;


        private static readonly object _syncObj = new object();


        // ===================

        /// <summary>
        /// Fallback if AttachBytes method is not available
        /// </summary>
        /// <param name="array">source array</param>
        /// <returns>Created ByteString</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ByteString CreateFromByteArrayFallback(byte[] array)
        {
            return ByteString.CopyFrom(array);
        }

        /// <summary>
        /// True if optimized creation from byte array is available
        /// </summary>
        /// <returns>True if optimized creation from byte array is available</returns>
        internal static bool CanCreateFromByteArrayOptimized()
        {
            return TryGetAttachBytesMethod() != null;
        }

        /// <summary>
        /// Attempts to get 'AttachBytes' method from 'ByteString'
        /// </summary>
        /// <returns>MethodInfo or null</returns>
        private static System.Reflection.MethodInfo TryGetAttachBytesMethod()
        {
            return typeof(ByteString).GetMethod("AttachBytes", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
                                                null, new Type[] { typeof(byte[]) }, null);
        }

        /// <summary>
        /// Creates delegate for AttachBytes method 
        /// </summary>
        /// <returns>Created delegate or null</returns>
        private static CreateFromByteArrayDelegate TryGenerateCreateFromByteArrayMethod()
        {
            var attachBytesMethod = TryGetAttachBytesMethod();
            if (attachBytesMethod != null)
                return (CreateFromByteArrayDelegate)attachBytesMethod.CreateDelegate(typeof(CreateFromByteArrayDelegate));

            return null;
        }


        /// <summary>
        /// Initialize method
        /// </summary>
        /// <returns>Delegate to initialized method</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CreateFromByteArrayDelegate InitCreateFromByteArrayMethod()
        {
            lock (_syncObj)
            {
                var result = Volatile.Read(ref _createFromByteArrayDelegate);
                if (result == null)
                {
                    result = TryGenerateCreateFromByteArrayMethod() ?? new CreateFromByteArrayDelegate(CreateFromByteArrayFallback);
                    Volatile.Write(ref _createFromByteArrayDelegate, result);
                }
                return result;
            }
        }

        /// <summary>
        /// Create ByteString from byte array without copy
        /// </summary>
        /// <param name="array">Source array</param>
        /// <returns>Created byte string</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ByteString CreateFromByteArrayOptimized(byte[] array)
        {
            var action = _createFromByteArrayDelegate ?? InitCreateFromByteArrayMethod();
            return action(array);
        }


        // =========================

        /// <summary>
        /// Extracts '_object' and '_index' fields from <see cref="ReadOnlyMemory{T}"/> with Reflection
        /// </summary>
        /// <param name="mem">Source memory struct</param>
        /// <param name="obj">Extracted '_object'</param>
        /// <param name="index">Extracted '_index'</param>
        private static void ExtractObjectIndexFromMemoryWithReflection(ref ReadOnlyMemory<byte> mem, out object obj, out int index)
        {
            object boxedMem = (object)mem;

            obj = _readOnlyMemory_object.GetValue(boxedMem);
            index = (int)_readOnlyMemory_index.GetValue(boxedMem);
        }

        /// <summary>
        /// Initialize fields, required for <see cref="ExtractObjectIndexFromMemoryWithReflection"/>
        /// </summary>
        private static void InitExtractObjectIndexFromMemoryReflectionData()
        {
            var memoryType = typeof(ReadOnlyMemory<byte>);
            Volatile.Write(ref _readOnlyMemory_object, memoryType.GetField("_object", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
            Volatile.Write(ref _readOnlyMemory_index, memoryType.GetField("_index", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
        }

        /// <summary>
        /// Checks whether the optimized array extraction is available
        /// </summary>
        /// <returns>True if available</returns>
        internal static bool CanExtractByteArrayOptimized()
        {
            var memoryProperty = typeof(ByteString).GetProperty("Memory", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (memoryProperty == null || memoryProperty.PropertyType != typeof(ReadOnlyMemory<byte>) || !memoryProperty.CanRead)
                return false;

            var memoryType = memoryProperty.PropertyType;

            var objectField = memoryType.GetField("_object", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (objectField == null || objectField.FieldType != typeof(object))
                return false;

            var indexField = memoryType.GetField("_index", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (indexField == null || indexField.FieldType != typeof(int))
                return false;

            var lengthField = memoryType.GetField("_length", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (lengthField == null || lengthField.FieldType != typeof(int))
                return false;

            return true;
        }

        /// <summary>
        /// Generates dynamic method to extract '_object' and '_index' fields from <see cref="ReadOnlyMemory{T}"/>
        /// </summary>
        /// <returns>Generated method or null</returns>
        private static ExtractObjectIndexFromMemoryDelegate TryGenerateExtractObjectIndexFromMemoryMethod()
        {
#if NETSTANDARD
            return null;
#else
            if (!CanExtractByteArrayOptimized())
                return null;

            var memoryType = typeof(ReadOnlyMemory<byte>);
            var objectField = memoryType.GetField("_object", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var indexField = memoryType.GetField("_index", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (objectField == null || indexField == null)
                return null;

            try
            {
                var method = new System.Reflection.Emit.DynamicMethod("ByteString_ExtractByteArray_" + Guid.NewGuid().ToString("N"),
                    null, new Type[] { typeof(ReadOnlyMemory<byte>).MakeByRefType(), typeof(object).MakeByRefType(), typeof(int).MakeByRefType() }, true);

                var ilGen = method.GetILGenerator();

                ilGen.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                ilGen.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                ilGen.Emit(System.Reflection.Emit.OpCodes.Ldfld, objectField);
                ilGen.Emit(System.Reflection.Emit.OpCodes.Stind_Ref);

                ilGen.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);
                ilGen.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                ilGen.Emit(System.Reflection.Emit.OpCodes.Ldfld, indexField);
                ilGen.Emit(System.Reflection.Emit.OpCodes.Stind_I4);

                ilGen.Emit(System.Reflection.Emit.OpCodes.Ret);

                return (ExtractObjectIndexFromMemoryDelegate)method.CreateDelegate(typeof(ExtractObjectIndexFromMemoryDelegate));
            }
            catch
            {
                return null;
            }
#endif
        }

        /// <summary>
        /// Initlaize all fields for <see cref="ExtractByteArrayOptimized"/>
        /// </summary>
        /// <returns>Value of <see cref="_canExtractByteArrayOptimized"/> after initialization</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int InitExtractByteArrayEnvironment()
        {
            lock (_syncObj)
            {
                var canExtractByteArrayOptimized = Volatile.Read(ref _canExtractByteArrayOptimized);
                if (canExtractByteArrayOptimized == CanExtractByteArrayOptimized_NotInitialized)
                {
                    if (CanCreateFromByteArrayOptimized())
                    {
                        canExtractByteArrayOptimized = CanExtractByteArrayOptimized_Ok;

                        InitExtractObjectIndexFromMemoryReflectionData();
                        var extractObjectIndexFromMemoryDelegate = TryGenerateExtractObjectIndexFromMemoryMethod();
                        Interlocked.Exchange(ref _extractObjectIndexFromMemoryDelegate, extractObjectIndexFromMemoryDelegate);
                    }
                    else
                    {
                        canExtractByteArrayOptimized = CanExtractByteArrayOptimized_Unavailable;
                    }

                    Interlocked.Exchange(ref _canExtractByteArrayOptimized, canExtractByteArrayOptimized);
                }

                return canExtractByteArrayOptimized;
            }
        }

        /// <summary>
        /// Extracts byte array form <see cref="ByteString"/> without array copy (if possible), otherwise fallback to array copy
        /// </summary>
        /// <param name="byteString">Source ByteString</param>
        /// <returns>Extracted byte array</returns>
        public static byte[] ExtractByteArrayOptimized(ByteString byteString)
        {
            int canExtractByteArrayOptimized = _canExtractByteArrayOptimized;
            if (canExtractByteArrayOptimized == CanExtractByteArrayOptimized_NotInitialized)
                canExtractByteArrayOptimized = InitExtractByteArrayEnvironment();

            if (canExtractByteArrayOptimized == CanExtractByteArrayOptimized_Ok)
            {
                var memory = byteString.Memory;
                object memObj = null;
                int memIndex = 0;

                var extractObjectIndexFromMemoryDelegate = _extractObjectIndexFromMemoryDelegate;
                if (extractObjectIndexFromMemoryDelegate != null)
                    extractObjectIndexFromMemoryDelegate(ref memory, out memObj, out memIndex);
                else if (byteString.Length >= ExtractObjectIndexFromMemoryWithReflectionThreshold)
                    ExtractObjectIndexFromMemoryWithReflection(ref memory, out memObj, out memIndex);

                if (memObj != null && memIndex == 0 && memObj.GetType() == typeof(byte[]))
                {
                    var result = (byte[])memObj;
                    if (result.Length == memory.Length)
                        return result;
                }
            }

            return byteString.ToByteArray();
        }
    }
}
