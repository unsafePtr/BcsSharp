using System.Runtime.CompilerServices;
using BcsSharp.Core;
using BcsSharp.Core.Resolvers;
using Nethermind.Int256;

namespace BcsSharp.Tests.Extensibility;

/// <summary>
/// Consumer-side formatter for 256-bit unsigned integers, following Sui/Move semantics.
/// </summary>
/// <remarks>
/// <para>
/// 256-bit integers are <b>not</b> a BCS type — the format (and serde's data model, which the reference Rust <c>bcs</c> crate is built on) stops at 128 bits.
/// Sui's Move <c>u256</c> gets its encoding by delegating to a fixed <c>[u8; 32]</c> array, which BCS encodes as a fixed-size sequence: <b>32 bare little-endian bytes with no length prefix</b>.
/// A <c>Vec&lt;u8&gt;</c> of the same 32 bytes would instead be 33 bytes (ULEB128 <c>0x20</c> + data) — see <see cref="SuiUInt256FormatterTests.Serialize_HasNoLengthPrefix_UnlikeByteSequence"/>.
/// </para>
/// <para>
/// That is why BcsSharp.Core does not ship 256-bit support: the encoding is an application-level convention, not a BCS primitive, and a different chain could just as legitimately pick big-endian or a length-prefixed sequence.
/// This class doubles as the worked example for consumers who need Sui's flavour — copy it as-is.
/// </para>
/// </remarks>
public sealed class SuiUInt256Formatter : IBcsFormatter<UInt256>
{
    public static readonly SuiUInt256Formatter Instance = new();

    public void Serialize(ref BcsWriter writer, UInt256 value)
    {
        // 32 bare little-endian bytes, no ULEB length prefix — Sui's `[u8; 32]` shape.
        Span<byte> littleEndian = stackalloc byte[32];
        value.ToLittleEndian(littleEndian);
        writer.WriteBytes(littleEndian);
    }

    // Zero-copy: ReadBytesAsSpan slices the input buffer rather than allocating.
    public UInt256 Deserialize(ref BcsReader reader) => new(reader.ReadBytesAsSpan(32), isBigEndian: false);
}

/// <summary>
/// Registers <see cref="SuiUInt256Formatter"/> for the whole test assembly, mirroring what a consuming application does at startup.
/// </summary>
/// <remarks>
/// A module initializer is used rather than per-test registration because <see cref="CompositeResolver"/> memoises misses: once a lookup for a type has failed, registering later appears to do nothing until <see cref="BcsSerializer.ClearFormatterCache"/> is called.
/// Registering before any test body runs sidesteps that ordering hazard entirely.
/// </remarks>
internal static class SuiFormatterRegistration
{
    [ModuleInitializer]
    internal static void Register()
    {
        CustomFormatterResolver.Instance.Register(SuiUInt256Formatter.Instance);
    }
}
