using Nethermind.Int256;

namespace BcsSharp.Core.Types
{
    /// <summary>
    /// BCS type for unsigned 8-bit integers
    /// </summary>
    public class U8Type : BcsType<byte>
    {
        public U8Type() : base("u8") { }

        public override byte Read(BcsReader reader) => reader.Read8();

        public override void Write(byte value, BcsWriter writer) => writer.Write(value);

        public override int? SerializedSize(byte value) => 1;
    }

    /// <summary>
    /// BCS type for unsigned 16-bit integers
    /// </summary>
    public class U16Type : BcsType<ushort>
    {
        public U16Type() : base("u16") { }

        public override ushort Read(BcsReader reader) => reader.Read16();

        public override void Write(ushort value, BcsWriter writer) => writer.Write(value);

        public override int? SerializedSize(ushort value) => 2;
    }

    /// <summary>
    /// BCS type for unsigned 32-bit integers
    /// </summary>
    public class U32Type : BcsType<uint>
    {
        public U32Type() : base("u32") { }

        public override uint Read(BcsReader reader) => reader.Read32();

        public override void Write(uint value, BcsWriter writer) => writer.Write(value);

        public override int? SerializedSize(uint value) => 4;
    }

    /// <summary>
    /// BCS type for unsigned 64-bit integers
    /// </summary>
    public class U64Type : BcsType<ulong>
    {
        public U64Type() : base("u64") { }

        public override ulong Read(BcsReader reader) => reader.Read64();

        public override void Write(ulong value, BcsWriter writer) => writer.Write(value);

        public override int? SerializedSize(ulong value) => 8;
    }

    /// <summary>
    /// BCS type for unsigned 128-bit integers
    /// </summary>
    public class U128Type : BcsType<UInt128>
    {
        public U128Type() : base("u128") { }

        public override UInt128 Read(BcsReader reader) => reader.Read128();

        public override void Write(UInt128 value, BcsWriter writer) => writer.Write(value);

        public override int? SerializedSize(UInt128 value) => 16;
    }

    /// <summary>
    /// BCS type for unsigned 256-bit integers
    /// </summary>
    public class U256Type : BcsType<UInt256>
    {
        public U256Type() : base("u256") { }

        public override UInt256 Read(BcsReader reader) => reader.Read256();

        public override void Write(UInt256 value, BcsWriter writer) => writer.Write(value);

        public override int? SerializedSize(UInt256 value) => 32;
    }
}