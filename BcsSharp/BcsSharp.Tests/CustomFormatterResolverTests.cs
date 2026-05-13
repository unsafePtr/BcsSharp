using BcsSharp.Core;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Resolvers;

namespace BcsSharp.Tests;

/// <summary>
/// Covers runtime registration of formatters via <see cref="CustomFormatterResolver"/>.
/// </summary>
public class CustomFormatterResolverTests
{
    [Fact]
    public void RegisterBeforeFirstUse_Works()
    {
        CustomFormatterResolver.Instance.Register<UnattributedAddress>(new UnattributedAddressFormatter());
        BcsSerializer.ClearFormatterCache();

        try
        {
            var addr = new UnattributedAddress(0xCD);

            var bytes = BcsSerializer.Serialize(addr);
            var back = BcsSerializer.Deserialize<UnattributedAddress>(bytes);

            Assert.Equal(addr, back);
        }
        finally
        {
            CustomFormatterResolver.Instance.Unregister<UnattributedAddress>();
            BcsSerializer.ClearFormatterCache();
        }
    }

    [Fact]
    public void OverridesBuiltInForSameType()
    {
        // The built-in int formatter writes a little-endian 4-byte payload. FakeIntFormatter
        // emits a single 0xFF byte regardless of input — observing that proves the manual
        // registration beats StandardResolver in the chain.
        CustomFormatterResolver.Instance.Register<int>(new FakeIntFormatter());
        BcsSerializer.ClearFormatterCache();

        try
        {
            var bytes = BcsSerializer.Serialize(123);
            Assert.Equal(new byte[] { 0xFF }, bytes);
        }
        finally
        {
            CustomFormatterResolver.Instance.Unregister<int>();
            BcsSerializer.ClearFormatterCache();
        }
    }

    public readonly struct UnattributedAddress : IEquatable<UnattributedAddress>
    {
        public readonly byte FirstByte;
        public UnattributedAddress(byte firstByte) => FirstByte = firstByte;
        public bool Equals(UnattributedAddress other) => FirstByte == other.FirstByte;
        public override bool Equals(object? obj) => obj is UnattributedAddress a && Equals(a);
        public override int GetHashCode() => FirstByte;
    }

    public sealed class UnattributedAddressFormatter : ByteArrayFormatter<UnattributedAddress>
    {
        public override int GetLength() => 32;
        public override ReadOnlySpan<byte> GetBytes(UnattributedAddress value)
        {
            var buf = new byte[32];
            buf[0] = value.FirstByte;
            return buf;
        }
        public override UnattributedAddress GetFromBytes(ReadOnlySpan<byte> bytes) => new(bytes[0]);
    }

    private sealed class FakeIntFormatter : IBcsFormatter<int>
    {
        private static readonly byte[] Sentinel = { 0xFF };
        public void Serialize(ref BcsWriter writer, int value) => writer.WriteBytes(Sentinel);
        public int Deserialize(ref BcsReader reader) => reader.ReadBytesAsSpan(1)[0];
    }
}
