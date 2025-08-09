using System;
using System.Buffers;
using BcsSharp.Core.Resolvers;

namespace BcsSharp.Core
{
    /// <summary>
    /// Static serializer API similar to MessagePackSerializer
    /// </summary>
    public static class BcsSerializer
    {
        private static IFormatterResolver _defaultResolver = StandardResolver.Instance;
        
        /// <summary>
        /// Default resolver used when none is specified
        /// </summary>
        public static IFormatterResolver DefaultResolver
        {
            get => _defaultResolver;
            set => _defaultResolver = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        /// <summary>
        /// Serialize value to byte array
        /// </summary>
        public static byte[] Serialize<T>(T value, IFormatterResolver? resolver = null)
        {
            var writer = new BcsWriter();
            Serialize(ref writer, value, resolver);
            return writer.ToBytes();
        }
        
        /// <summary>
        /// Serialize value to BcsWriter
        /// </summary>
        public static void Serialize<T>(ref BcsWriter writer, T value, IFormatterResolver? resolver = null)
        {
            resolver ??= _defaultResolver;
            var formatter = resolver.GetFormatter<T>();
            
            if (formatter == null)
                throw new InvalidOperationException($"No formatter found for type {typeof(T)}");
            
            formatter.Serialize(ref writer, value);
        }
        
        /// <summary>
        /// Serialize value to IBufferWriter
        /// </summary>
        public static void Serialize<T>(IBufferWriter<byte> bufferWriter, T value, IFormatterResolver? resolver = null)
        {
            var writer = new BcsWriter(bufferWriter);
            Serialize(ref writer, value, resolver);
        }
        
        /// <summary>
        /// Deserialize from byte array
        /// </summary>
        public static T Deserialize<T>(byte[] data, IFormatterResolver? resolver = null)
        {
            var reader = new BcsReader(data);
            return Deserialize<T>(ref reader, resolver);
        }
        
        /// <summary>
        /// Deserialize from ReadOnlyMemory
        /// </summary>
        public static T Deserialize<T>(ReadOnlyMemory<byte> data, IFormatterResolver? resolver = null)
        {
            var reader = new BcsReader(data);
            return Deserialize<T>(ref reader, resolver);
        }
        
        /// <summary>
        /// Deserialize from BcsReader
        /// </summary>
        public static T Deserialize<T>(ref BcsReader reader, IFormatterResolver? resolver = null)
        {
            resolver ??= _defaultResolver;
            var formatter = resolver.GetFormatter<T>();
            
            if (formatter == null)
                throw new InvalidOperationException($"No formatter found for type {typeof(T)}");
            
            return formatter.Deserialize(ref reader);
        }
        
        /// <summary>
        /// Get serialized size for value if deterministic
        /// </summary>
        public static int? GetSerializedSize<T>(T value, IFormatterResolver? resolver = null)
        {
            resolver ??= _defaultResolver;
            var formatter = resolver.GetFormatter<T>();
            
            if (formatter == null)
                throw new InvalidOperationException($"No formatter found for type {typeof(T)}");
            
            return formatter.GetSerializedSize(value);
        }
        
        /// <summary>
        /// Serialize to hex string
        /// </summary>
        public static string SerializeToHex<T>(T value, IFormatterResolver? resolver = null)
        {
            var writer = new BcsWriter();
            Serialize(ref writer, value, resolver);
            return writer.ToHex();
        }
        
        /// <summary>
        /// Serialize to base64 string
        /// </summary>
        public static string SerializeToBase64<T>(T value, IFormatterResolver? resolver = null)
        {
            var writer = new BcsWriter();
            Serialize(ref writer, value, resolver);
            return writer.ToBase64();
        }
        
        /// <summary>
        /// Deserialize from hex string
        /// </summary>
        public static T DeserializeFromHex<T>(string hex, IFormatterResolver? resolver = null)
        {
            var bytes = Convert.FromHexString(hex);
            return Deserialize<T>(bytes, resolver);
        }
        
        /// <summary>
        /// Deserialize from base64 string
        /// </summary>
        public static T DeserializeFromBase64<T>(string base64, IFormatterResolver? resolver = null)
        {
            var bytes = Convert.FromBase64String(base64);
            return Deserialize<T>(bytes, resolver);
        }
    }
}