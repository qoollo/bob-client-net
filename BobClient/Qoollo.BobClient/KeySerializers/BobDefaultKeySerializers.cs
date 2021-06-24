using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.KeySerializers
{
    /// <summary>
    /// Container for default key serializers
    /// </summary>
    internal static class BobDefaultKeySerializers
    {
        private static readonly Dictionary<Type, object> _serializers = new Dictionary<Type, object>()
        {
            { typeof(ulong), UInt64BobKeySerializer.Instance },
            { typeof(long), Int64BobKeySerializer.Instance },
            { typeof(uint), UInt32BobKeySerializer.Instance },
            { typeof(int), Int32BobKeySerializer.Instance },
            { typeof(Guid), GuidBobKeySerializer.Instance }
        };

        /// <summary>
        /// Attempts to get default <see cref="BobKeySerializer{TKey}"/> for a key of type <typeparamref name="TKey"/> 
        /// </summary>
        /// <typeparam name="TKey">Type of the key</typeparam>
        /// <param name="serializer">Extracted serializer (null if not found)</param>
        /// <returns>true if the <see cref="BobDefaultKeySerializers"/> contains serializer for key of type <typeparamref name="TKey"/></returns>
        public static bool TryGetKeySerializer<TKey>(out BobKeySerializer<TKey> serializer)
        {
            if (_serializers.TryGetValue(typeof(TKey), out object val))
            {
                serializer = (BobKeySerializer<TKey>)val;
                return true;
            }    
            else
            {
                serializer = null;
                return false;
            }
        }
    }
}
