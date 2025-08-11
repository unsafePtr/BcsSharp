using System.Runtime.InteropServices;

namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// High-performance array formatter for primitive types using vectorized operations
    /// </summary>
    public sealed class PrimitiveArrayFormatter<T> : IBcsFormatter<T[]>
        where T : unmanaged
    {
        private static readonly Dictionary<Type, object> _instances = new();

        public Type TargetType => typeof(T[]);

        public static PrimitiveArrayFormatter<T> GetInstance()
        {
            if (!_instances.TryGetValue(typeof(T), out var instance))
            {
                instance = new PrimitiveArrayFormatter<T>();
                _instances[typeof(T)] = instance;
            }

            return (PrimitiveArrayFormatter<T>)instance;
        }

        public void Serialize(ref BcsWriter writer, T[] value)
        {
            if (value == null)
            {
                writer.WriteULEB(0u);
                return;
            }

            writer.WriteULEB((uint)value.Length);
            writer.WritePrimitiveArray<T>(value.AsSpan());
        }

        public T[] Deserialize(ref BcsReader reader)
        {
            var length = reader.ReadULEB32();
            if (length == 0)
                return Array.Empty<T>();

            var result = new T[length];
            reader.ReadPrimitiveArray(result.AsSpan());
            return result;
        }

        public int? GetSerializedSize(T[] value)
        {
            if (value == null)
                return GetULEBSize(0);

            var elementSize = Marshal.SizeOf<T>();
            var size = GetULEBSize((uint)value.Length);
            size += value.Length * elementSize;
            return size;
        }

        private static int GetULEBSize(uint value)
        {
            if (value < 0x80) return 1;
            if (value < 0x4000) return 2;
            if (value < 0x200000) return 3;
            if (value < 0x10000000) return 4;
            return 5;
        }
    }

    /// <summary>
    /// High-performance list formatter for primitive types using vectorized operations
    /// </summary>
    public sealed class PrimitiveListFormatter<T> : IBcsFormatter<List<T>> where T : unmanaged
    {
        private static readonly Dictionary<Type, object> _instances = new();

        public Type TargetType => typeof(List<T>);

        public static PrimitiveListFormatter<T> GetInstance()
        {
            if (!_instances.TryGetValue(typeof(T), out var instance))
            {
                instance = new PrimitiveListFormatter<T>();
                _instances[typeof(T)] = instance;
            }

            return (PrimitiveListFormatter<T>)instance;
        }

        public void Serialize(ref BcsWriter writer, List<T> value)
        {
            if (value == null)
            {
                writer.WriteULEB(0u);
                return;
            }

            writer.WriteULEB((uint)value.Count);
            // Convert List to Span for vectorized operations
            var span = CollectionsMarshal.AsSpan(value);
            writer.WritePrimitiveArray<T>(span);
        }

        public List<T> Deserialize(ref BcsReader reader)
        {
            var length = reader.ReadULEB32();
            if (length == 0)
                return [];

            var result = new List<T>((int)length);
            var span = CollectionsMarshal.AsSpan(result);
            reader.ReadPrimitiveArray(span);
            return result;
        }

        public int? GetSerializedSize(List<T> value)
        {
            if (value == null)
                return GetULEBSize(0);

            var elementSize = Marshal.SizeOf<T>();
            var size = GetULEBSize((uint)value.Count);
            size += value.Count * elementSize;
            return size;
        }

        private static int GetULEBSize(uint value)
        {
            if (value < 0x80) return 1;
            if (value < 0x4000) return 2;
            if (value < 0x200000) return 3;
            if (value < 0x10000000) return 4;
            return 5;
        }
    }
}