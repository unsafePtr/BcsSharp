using System.Runtime.InteropServices;

namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// List/Vector formatter for primitive types using vectorized operations
    /// </summary>
    public sealed class PrimitiveListFormatter<T> : IBcsFormatter<List<T>> where T : unmanaged
    {
        public Type TargetType => typeof(List<T>);

        public static PrimitiveListFormatter<T> GetInstance()
        {
            return FormatterCache.GetOrAddFormatter(typeof(List<T>), _ => new PrimitiveListFormatter<T>());
        }

        public void Serialize(ref BcsWriter writer, List<T> value)
        {
            if (value == null)
            {
                writer.WriteULEB(0u);
                return;
            }

            writer.WriteULEB((uint)value.Count);
            var span = CollectionsMarshal.AsSpan(value);
            writer.WritePrimitiveArray<T>(span);
        }

        public List<T> Deserialize(ref BcsReader reader)
        {
            var length = reader.ReadULEB32();
            if (length == 0)
                return [];

            var count = (int)length;
            var result = new List<T>(count);
            CollectionsMarshal.SetCount(result, count);
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