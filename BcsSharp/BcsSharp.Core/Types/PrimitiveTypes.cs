using System;

namespace BcsSharp.Core.Types
{
    /// <summary>
    /// BCS type for boolean values
    /// </summary>
    public class BoolType : BcsType<bool>
    {
        public BoolType() : base("bool") { }

        public override bool Read(BcsReader reader) => reader.ReadBool();

        public override void Write(bool value, BcsWriter writer) => writer.WriteBool(value);

        public override int? SerializedSize(bool value) => 1;
    }

    /// <summary>
    /// BCS type for string values
    /// </summary>
    public class StringType : BcsType<string>
    {
        public StringType() : base("string") { }

        public override string Read(BcsReader reader) => reader.ReadString();

        public override void Write(string value, BcsWriter writer) => writer.WriteString(value ?? string.Empty);

        public override int? SerializedSize(string value)
        {
            if (value == null) return null;
            var byteLength = System.Text.Encoding.UTF8.GetByteCount(value);
            // ULEB128 size calculation is complex, so return null for dynamic sizing
            return null;
        }
    }
}