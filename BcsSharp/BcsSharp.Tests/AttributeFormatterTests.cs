using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Formatters;

namespace BcsSharp.Tests;

/// <summary>
/// Covers <see cref="BcsFormatterAttribute"/>-driven formatter discovery for types
/// declared in (or routed through) a separate assembly.
/// </summary>
public class AttributeFormatterTests
{
    [Fact]
    public void AttributeFormatter_ResolvesAndRoundtrips()
    {
        var addr = new SuiAddress(0xAB);

        var bytes = BcsSerializer.Serialize(addr);
        var back = BcsSerializer.Deserialize<SuiAddress>(bytes);

        Assert.Equal(SuiAddress.Length, bytes.Length);
        Assert.Equal(0xAB, bytes[0]);
        Assert.Equal(addr, back);
    }

    [Fact]
    public void AttributeFormatter_UsedInsideBcsStruct()
    {
        var holder = new AddressHolder { Address = new SuiAddress(0x42) };

        var bytes = BcsSerializer.Serialize(holder);
        var back = BcsSerializer.Deserialize<AddressHolder>(bytes);

        Assert.Equal(holder.Address, back.Address);
    }

    [BcsFormatter(typeof(SuiAddressFormatter))]
    public readonly struct SuiAddress : IEquatable<SuiAddress>
    {
        public const int Length = 32;
        private readonly byte _firstByte;
        public SuiAddress(byte firstByte) => _firstByte = firstByte;
        public byte FirstByte => _firstByte;
        public bool Equals(SuiAddress other) => _firstByte == other._firstByte;
        public override bool Equals(object? obj) => obj is SuiAddress a && Equals(a);
        public override int GetHashCode() => _firstByte;
    }

    public sealed class SuiAddressFormatter : ByteArrayFormatter<SuiAddress>
    {
        public static readonly SuiAddressFormatter Instance = new();
        public override int GetLength() => SuiAddress.Length;
        public override ReadOnlySpan<byte> GetBytes(SuiAddress value)
        {
            var buf = new byte[SuiAddress.Length];
            buf[0] = value.FirstByte;
            return buf;
        }
        public override SuiAddress GetFromBytes(ReadOnlySpan<byte> bytes) => new(bytes[0]);
    }

    [BcsStruct]
    public sealed class AddressHolder
    {
        [BcsField(0)]
        public SuiAddress Address { get; set; }
    }
}
