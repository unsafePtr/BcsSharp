# BcsSharp

Binary Canonical Serialization (BCS) for .NET.
Byte-for-byte wire-compatible with Rust's `bcs` crate, with a zero-allocation hot path.
Built for Sui/Move and other BCS-based protocols.

## Requirements

The package needs .NET 11 or later at runtime.

Declaring `union` types needs the .NET 11 SDK RC1 or later, for the C# 15 compiler.
The `union` keyword is the recommended way to model Rust tagged enums; the attribute-based `[BcsEnum]` path produces identical bytes.

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
| `Vec<T>` | `List<T>` (non-null; not `T[]`) |
| `BTreeMap<K, V>` | `Dictionary<K, V>` (non-null; sorted by serialized key bytes) |
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

On the wire, None is `0x00` and Some is `0x01` followed by the payload, exactly as Rust writes `Option<T>`.

### Reference types are never optional by annotation

BCS has no null — `Option<T>` is the optional form for a string, a vector and a map alike.
C# nullable annotations are erased at runtime, so `string` and `string?` are the same type and the library cannot tell them apart.
Serializing a null `string`, `List<T>` or `Dictionary<K, V>` therefore throws `ArgumentNullException` rather than silently encoding it as empty:

```csharp
[BcsField(0)] public string Name { get; set; } = "";                          // required
[BcsField(1)] public Option<string> Email { get; set; } = None.Instance;      // optional
[BcsField(2)] public List<Tag> Tags { get; set; } = [];                       // required, may be empty
[BcsField(3)] public Option<List<Tag>> Aliases { get; set; } = None.Instance; // optional
```

`string? Email` is not a shorthand for `Option<string>`, nor is `List<Tag>?` one for `Option<List<Tag>>`.
A `string?` holding `"hi"` encodes as `02 68 69`, missing the `0x01` discriminant that Rust's `Option<String>` writes — a silent wire mismatch.
Only the absent case coincides (`0x00` for an empty string, an empty vector and `Option.None` alike), which is exactly what makes the mistake hard to spot.

Deserialization never returns null: a zero-length string decodes to `""` and a zero-length vector to an empty `List<T>`.

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

The variant index is the declaration order of the cases (Common = 0, Uncommon = 1, …), so reordering them breaks the wire.

## CLR enums (Rust's unit-only enums)

```csharp
public enum AssetType { Weapon, Armor, Consumable, Material, Currency }
```

On the wire a member is the ULEB128 of its declaration index, not of its value, so `AssetType.Material`, declared fourth, serializes as `0x03`.
Values need not ascend.
Two members with the same value are rejected, since the wire has one index per member.

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

`ByteArrayFormatter<T>` is the recommended base for fixed-length byte-array-backed types.
All its overrides are span-based, so deserializing through it allocates nothing.

### Imperative — `CustomFormatterResolver`

Register a formatter at startup for a type you can't (or won't) annotate:

```csharp
CustomFormatterResolver.Instance.Register<ThirdPartyType>(new ThirdPartyFormatter());
BcsSerializer.ClearFormatterCache();   // only needed if registering late
```

`CustomFormatterResolver` sits first in the resolver chain, so it overrides any built-in formatter.

### 256-bit integers (Sui `u256`)

These are not shipped, by design: BCS has no 256-bit type, and the format and serde's data model both stop at 128 bits.
Sui's Move `u256` delegates to a fixed `[u8; 32]` array, encoded as 32 bare little-endian bytes with no length prefix; as a `Vec<u8>` it would be 33.
That framing is an application convention, not BCS, so it lives in a consumer formatter:

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
| `Serialize<T>(IBufferWriter<byte>, T)` | 0 B |
| `Deserialize<T>(bytes)` value-type T | 0 B |
| `Deserialize<T>(bytes)` class T | 24 B (the class instance itself; intrinsic) |

For maximum throughput, use the `IBufferWriter<byte>` overload and reuse the buffer writer.

## Rules worth knowing

- Vectors are `List<T>`, mirroring Rust's `Vec<T>`; plain `T[]` arrays are rejected.
- `[BcsField]` numbers are the wire order and must run `0, 1, 2, …`.
  A duplicate or a gap is rejected when the type's formatter is built.
- Variant indices are part of the wire format.
  A `union` numbers its cases in declaration order, and a `[BcsEnum]` variant takes its index from `[BcsEnumVariant]`.
- Recursive types resolve.
  A struct or union may reference itself directly or through `Option<T>`, `List<T>`, `Dictionary<K,V>` or a tuple; deserialization bounds the nesting at `MaxContainerDepth`.
- Map keys are sorted by their serialized bytes, not by `IComparable`, matching the BCS spec and Rust's `bcs::ser::MapSerializer`.
- Null is rejected.
  BCS has no null string, vector or map, so a null `string`, `List<T>` or `Dictionary<K, V>` throws `ArgumentNullException` instead of encoding as empty.
  Use `Option<T>` for an optional value — `string?` and `List<T>?` are erased at runtime and encode without the `Option` discriminant.

## Untrusted input

- Structs and enums may nest at most `BcsSerializer.MaxContainerDepth` (500) deep; lists, maps, tuples and options don't count.
  `new BcsReader(bytes, maxContainerDepth: 64)` lowers the limit.
- Lengths above `BcsSerializer.MaxSequenceLength` (2^31 − 1) are rejected, and collections never pre-allocate more than the remaining input could fill.
- `Deserialize<T>(bytes)` requires exactly one value; the `ref BcsReader` overload leaves trailing bytes unread.
- Input must be canonical: non-minimal ULEB128, booleans other than 0 and 1, invalid UTF-8, and unsorted or duplicate map keys are rejected.
- Maps keyed by integers or enums use the default hash, which an attacker can make collide to turn decoding quadratic.
  Opt in to a keyed SipHash comparer with `CompositeResolver.Create(CollisionResistantMapResolver.Instance)`; string keys are already protected by .NET's randomized string hashing.

## Links

- Source, issues and full docs: <https://github.com/unsafePtr/BcsSharp>
- BCS specification: <https://github.com/diem/bcs>

## License

MIT
