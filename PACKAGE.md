# BcsSharp

Binary Canonical Serialization (BCS) for .NET. Byte-for-byte wire-compatible with Rust's `bcs` crate. Zero-allocation hot path. Built for Sui/Move and other BCS-based protocols.

## Requirements

- **Runtime**: .NET 11 or later
- **Building** types that use `union` (C# 15 keyword): **.NET 11 SDK** RC1 or later. The C# 15 `union` keyword is the recommended way to model Rust tagged enums; the `[BcsEnum]` attribute-based path produces identical bytes.

## Quick start

```csharp
using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Unions;
using System.Buffers;

[BcsStruct]
public class User
{
    [BcsField(0)] public ulong Id { get; set; }
    [BcsField(1)] public string Name { get; set; } = "";
    [BcsField(2)] public Option<Address> Address { get; set; } = None.Instance;
}

[BcsStruct]
public class Address
{
    [BcsField(0)] public string City { get; set; } = "";
}

var user = new User { Id = 12345, Name = "Alice", Address = new Address { City = "Plovdiv" } };

// Allocating overload — returns a fresh byte[]
byte[] bytes = BcsSerializer.Serialize(user);
User back = BcsSerializer.Deserialize<User>(bytes);

// Zero-allocation overload — writes into caller-owned buffer
var bw = new ArrayBufferWriter<byte>(64);
BcsSerializer.Serialize(bw, user);
ReadOnlySpan<byte> output = bw.WrittenSpan;
```

## Type mapping (Rust BCS ↔ C#)

| Rust | C# |
|---|---|
| `u8`/`u16`/`u32`/`u64`/`u128` | `byte`, `ushort`, `uint`, `ulong`, `UInt128` |
| `i8`/`i16`/`i32`/`i64`/`i128` | `sbyte`, `short`, `int`, `long`, `Int128` |
| `bool`, `String` | `bool`, `string` (non-null — see below) |
| `Vec<T>` | `List<T>` (not `T[]`) |
| `BTreeMap<K, V>` | `Dictionary<K, V>` (sorted by serialized key bytes) |
| `Option<T>` (value-type `T`) | `T?` (Nullable) |
| `Option<T>` (reference-type `T`) | `Option<T>` from `BcsSharp.Core.Unions` |
| Unit-only enum `Foo { A, B, C }` | `enum Foo { A, B, C }` (CLR enum) |
| Tagged enum with payloads | `union Foo(VariantA, VariantB, ...)` (C# 15) |
| `(T1, T2, T3)` | `(T1, T2, T3)` (ValueTuple up to arity 4) |
| `struct Foo { … }` | `[BcsStruct] class Foo { [BcsField(N)] … }` |

## Options

```csharp
// Value-type optional → Nullable<T>
[BcsField(0)] public uint? Age { get; set; }

// Reference-type optional → Option<T> union
[BcsField(1)] public Option<Address> Home { get; set; } = None.Instance;

// Direct assignment via implicit conversion
Option<Address> some = new Address { City = "Sofia" };
Option<Address> none = None.Instance;
```

Wire: `0x00` = None, `0x01 + payload` = Some. Identical to Rust's `Option<T>`.

### Strings are never optional by annotation

BCS has no null string — `Option<String>` is the optional form. C# nullable annotations are
erased at runtime, so `string` and `string?` are the *same* type and the library cannot tell
them apart. Serializing a null string therefore throws `ArgumentNullException` rather than
silently encoding it as empty:

```csharp
[BcsField(0)] public string Name { get; set; } = "";                      // required
[BcsField(1)] public Option<string> Email { get; set; } = None.Instance;  // optional
```

`string? Email` is **not** a shorthand for `Option<string>`. It encodes `"hi"` as
`02 68 69`, missing the `0x01` discriminant that Rust's `Option<String>` writes — a silent
wire mismatch. Only the absent case coincides (`0x00` for both a zero-length string and
`Option.None`), which is exactly what makes the mistake hard to spot.

Deserialization never returns null: a zero-length string decodes to `""`.

## Tagged unions

```csharp
[BcsStruct] public sealed record class Uncommon { [BcsField(0)] public uint Value { get; set; } }
[BcsStruct] public sealed record class Rare    { [BcsField(0)] public List<uint> Values { get; set; } = []; }
[BcsStruct] public sealed record class Epic    { [BcsField(0)] public string Name { get; set; } = ""; }
public sealed record class Common;   // unit variant — no fields

public union Rarity(Common, Uncommon, Rare, Epic);

Rarity r = new Epic { Name = "Sword" };
byte[] bytes = BcsSerializer.Serialize(r);
// → [0x03, 0x05, 'S', 'w', 'o', 'r', 'd']  (variant 3 + ULEB-prefixed UTF-8)
```

Variant index = declaration order of constructors (Common=0, Uncommon=1, …). Re-ordering variants is a wire-breaking change.

## CLR enums (Rust's unit-only enums)

```csharp
public enum AssetType { Weapon, Armor, Consumable, Material, Currency }
```

Wire: ULEB128 of the **declaration index**, not the explicit discriminant value. `AssetType.Material` (4th declared) serializes as `0x03`.

## Custom formatters

### Declarative — `[BcsFormatter]` on the target type

```csharp
[BcsFormatter(typeof(SuiAddressFormatter))]
public readonly struct SuiAddress(ReadOnlySpan<byte> bytes) { /* … */ }

public sealed class SuiAddressFormatter : ByteArrayFormatter<SuiAddress>
{
    public override int GetLength() => 32;
    public override ReadOnlySpan<byte> GetBytes(SuiAddress v) => v.Bytes;
    public override SuiAddress GetFromBytes(ReadOnlySpan<byte> bytes) => new(bytes);
}
```

`ByteArrayFormatter<T>` is the recommended base for fixed-length byte-array-backed types. All overrides are span-based → zero-alloc deserialize.

### Imperative — `CustomFormatterResolver`

Register a formatter at startup for a type you can't (or won't) annotate:

```csharp
CustomFormatterResolver.Instance.Register<ThirdPartyType>(new ThirdPartyFormatter());
BcsSerializer.ClearFormatterCache();   // only needed if registering late
```

`CustomFormatterResolver` sits first in the resolver chain — overrides any built-in formatter.

### 256-bit integers (Sui `u256`)

Not shipped, by design: BCS has no 256-bit type — the format and serde's data model both stop at 128 bits. Sui's Move `u256` delegates to a fixed `[u8; 32]` array, encoded as **32 bare little-endian bytes with no length prefix** (as a `Vec<u8>` it would be 33). That framing is an application convention, not BCS, so it lives in a consumer formatter:

```csharp
public void Serialize(ref BcsWriter writer, UInt256 value)   // Nethermind.Int256
{
    Span<byte> littleEndian = stackalloc byte[32];
    value.ToLittleEndian(littleEndian);
    writer.WriteBytes(littleEndian);
}

public UInt256 Deserialize(ref BcsReader reader)
    => new(reader.ReadBytesAsSpan(32), isBigEndian: false);
```

Register it as above; `UInt256?`, `List<UInt256>` and `[BcsStruct]` fields then work unchanged.

## Allocation profile

Measured for a `[BcsStruct]` with two `uint` fields:

| Path | Bytes per op |
|---|---|
| `Serialize<T>(T) → byte[]` | 32 B (output array only) |
| `Serialize<T>(IBufferWriter<byte>, T)` | **0 B** |
| `Deserialize<T>(bytes)` value-type T | **0 B** |
| `Deserialize<T>(bytes)` class T | 24 B (the class instance itself; intrinsic) |

For maximum throughput, use the `IBufferWriter<byte>` overload and reuse the buffer writer.

## Rules

- **Use `List<T>` for vectors.** Plain `T[]` arrays are rejected — `Vec<T>` maps to `List<T>`.
- **`[BcsField]` numbers must be sequential** (`0, 1, 2, …`). They define the wire order.
- **Map keys are sorted by serialized bytes**, not by `IComparable`. Matches the BCS spec and Rust's `bcs::ser::MapSerializer`.
- **Variant index = declaration order** for both `union` and `[BcsEnum]` paths.
- **A null `string` is rejected.** BCS has no null string, so `WriteString` throws `ArgumentNullException` instead of coercing to `""`. Use `Option<string>` for an optional string — `string?` is erased to `string` at runtime and encodes without the `Option` discriminant.

## Links

- **Source / issues / full docs**: <https://github.com/yourusername/BcsSharp>
- **BCS specification**: <https://github.com/diem/bcs>

## License

MIT
