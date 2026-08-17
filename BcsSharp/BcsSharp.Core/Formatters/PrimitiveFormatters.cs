
namespace BcsSharp.Core.Formatters;

/// <summary>
/// Formatters for primitive types
/// </summary>
public sealed class ByteFormatter : IBcsFormatter<byte>
{
    public static readonly ByteFormatter Instance = new();
    public Type TargetType => typeof(byte);

    public void Serialize(ref BcsWriter writer, byte value) => writer.Write(value);
    public byte Deserialize(ref BcsReader reader) => reader.Read8();
}

public sealed class SByteFormatter : IBcsFormatter<sbyte>
{
    public static readonly SByteFormatter Instance = new();
    public Type TargetType => typeof(sbyte);

    public void Serialize(ref BcsWriter writer, sbyte value) => writer.Write(value);
    public sbyte Deserialize(ref BcsReader reader) => reader.ReadI8();
}

public sealed class UInt16Formatter : IBcsFormatter<ushort>
{
    public static readonly UInt16Formatter Instance = new();
    public Type TargetType => typeof(ushort);

    public void Serialize(ref BcsWriter writer, ushort value) => writer.Write(value);
    public ushort Deserialize(ref BcsReader reader) => reader.Read16();
}

public sealed class Int16Formatter : IBcsFormatter<short>
{
    public static readonly Int16Formatter Instance = new();
    public Type TargetType => typeof(short);

    public void Serialize(ref BcsWriter writer, short value) => writer.Write(value);
    public short Deserialize(ref BcsReader reader) => reader.ReadI16();
}

public sealed class UInt32Formatter : IBcsFormatter<uint>
{
    public static readonly UInt32Formatter Instance = new();
    public Type TargetType => typeof(uint);

    public void Serialize(ref BcsWriter writer, uint value) => writer.Write(value);
    public uint Deserialize(ref BcsReader reader) => reader.Read32();
}

public sealed class Int32Formatter : IBcsFormatter<int>
{
    public static readonly Int32Formatter Instance = new();
    public Type TargetType => typeof(int);

    public void Serialize(ref BcsWriter writer, int value) => writer.Write(value);
    public int Deserialize(ref BcsReader reader) => reader.ReadI32();
}

public sealed class UInt64Formatter : IBcsFormatter<ulong>
{
    public static readonly UInt64Formatter Instance = new();
    public Type TargetType => typeof(ulong);

    public void Serialize(ref BcsWriter writer, ulong value) => writer.Write(value);
    public ulong Deserialize(ref BcsReader reader) => reader.Read64();
}

public sealed class Int64Formatter : IBcsFormatter<long>
{
    public static readonly Int64Formatter Instance = new();
    public Type TargetType => typeof(long);

    public void Serialize(ref BcsWriter writer, long value) => writer.Write(value);
    public long Deserialize(ref BcsReader reader) => reader.ReadI64();
}

public sealed class UInt128Formatter : IBcsFormatter<UInt128>
{
    public static readonly UInt128Formatter Instance = new();
    public Type TargetType => typeof(UInt128);

    public void Serialize(ref BcsWriter writer, UInt128 value) => writer.Write(value);
    public UInt128 Deserialize(ref BcsReader reader) => reader.Read128();
}

public sealed class Int128Formatter : IBcsFormatter<Int128>
{
    public static readonly Int128Formatter Instance = new();
    public Type TargetType => typeof(Int128);

    public void Serialize(ref BcsWriter writer, Int128 value) => writer.Write(value);
    public Int128 Deserialize(ref BcsReader reader) => reader.ReadI128();
}

public sealed class BoolFormatter : IBcsFormatter<bool>
{
    public static readonly BoolFormatter Instance = new();
    public Type TargetType => typeof(bool);

    public void Serialize(ref BcsWriter writer, bool value) => writer.WriteBool(value);
    public bool Deserialize(ref BcsReader reader) => reader.ReadBool();
}

public sealed class StringFormatter : IBcsFormatter<string?>
{
    public static readonly StringFormatter Instance = new();
    public Type TargetType => typeof(string);

    public void Serialize(ref BcsWriter writer, string? value) => writer.WriteString(value);
    public string Deserialize(ref BcsReader reader) => reader.ReadString();
}
