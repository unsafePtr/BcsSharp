using System;
using System.Numerics;

namespace BcsSharp.Core.Types
{
    /// <summary>
    /// BCS type for signed 8-bit integers
    /// </summary>
    public class I8Type : BcsType<sbyte>
    {
        public I8Type() : base("i8") { }

        public override sbyte Read(BcsReader reader) => reader.ReadI8();

        public override void Write(sbyte value, BcsWriter writer) => writer.WriteI8(value);

        public override int? SerializedSize(sbyte value) => 1;
    }

    /// <summary>
    /// BCS type for signed 16-bit integers
    /// </summary>
    public class I16Type : BcsType<short>
    {
        public I16Type() : base("i16") { }

        public override short Read(BcsReader reader) => reader.ReadI16();

        public override void Write(short value, BcsWriter writer) => writer.WriteI16(value);

        public override int? SerializedSize(short value) => 2;
    }

    /// <summary>
    /// BCS type for signed 32-bit integers
    /// </summary>
    public class I32Type : BcsType<int>
    {
        public I32Type() : base("i32") { }

        public override int Read(BcsReader reader) => reader.ReadI32();

        public override void Write(int value, BcsWriter writer) => writer.WriteI32(value);

        public override int? SerializedSize(int value) => 4;
    }

    /// <summary>
    /// BCS type for signed 64-bit integers
    /// </summary>
    public class I64Type : BcsType<long>
    {
        public I64Type() : base("i64") { }

        public override long Read(BcsReader reader) => reader.ReadI64();

        public override void Write(long value, BcsWriter writer) => writer.WriteI64(value);

        public override int? SerializedSize(long value) => 8;
    }

    /// <summary>
    /// BCS type for signed 128-bit integers
    /// </summary>
    public class I128Type : BcsType<Int128>
    {
        public I128Type() : base("i128") { }

        public override Int128 Read(BcsReader reader) => reader.ReadI128();

        public override void Write(Int128 value, BcsWriter writer) => writer.WriteI128(value);

        public override int? SerializedSize(Int128 value) => 16;
    }
}