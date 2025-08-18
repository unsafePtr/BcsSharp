namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// List/Vector formatter for generic types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ListFormatter<T> : IBcsFormatter<List<T>>
    {
        private readonly IBcsFormatter<T> _elementFormatter;

        public Type TargetType => typeof(List<T>);

        public ListFormatter(IBcsFormatter<T> elementFormatter)
        {
            _elementFormatter = elementFormatter;
        }

        public static ListFormatter<T> GetInstance(IBcsFormatter<T> elementFormatter)
        {
            return FormatterCache.GetOrAddFormatter(typeof(List<T>), _ => new ListFormatter<T>(elementFormatter));
        }

        public void Serialize(ref BcsWriter writer, List<T> value)
        {
            if (value == null)
            {
                writer.WriteULEB(0u);
                return;
            }

            writer.WriteULEB((uint)value.Count);
            foreach (var item in value)
            {
                _elementFormatter.Serialize(ref writer, item);
            }
        }

        public List<T> Deserialize(ref BcsReader reader)
        {
            var length = reader.ReadULEB32();
            if (length == 0)
                return [];

            var result = new List<T>((int)length);
            for (int i = 0; i < length; i++)
            {
                result.Add(_elementFormatter.Deserialize(ref reader));
            }
            return result;
        }

        public int? GetSerializedSize(List<T> value)
        {
            if (value == null)
                return GetULEBSize(0);

            var size = GetULEBSize((uint)value.Count);
            foreach (var item in value)
            {
                var itemSize = _elementFormatter.GetSerializedSize(item);
                if (itemSize == null) return null;
                size += itemSize.Value;
            }
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