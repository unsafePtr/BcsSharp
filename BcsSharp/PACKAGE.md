# BcsSharp

Binary Canonical Serialization (BCS) for .NET. Byte-for-byte wire-compatible with Rust's `bcs` crate. Zero-allocation hot path. Built for Sui/Move and other BCS-based protocols.

## Requirements

- **Runtime**: .NET 10.0 or later
- **Building** types that use `union` (C# 15 keyword): **.NET 11 SDK or later** + `<LangVersion>preview</LangVersion>`. The C# 15 `union` keyword is the recommended way to model Rust tagged enums. If you're stuck on the .NET 10 SDK, the `[BcsEnum]` attribute-based path produces identical bytes.

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
| `u8`/`u16`/`u32`/`u64`/`u128`/`u256` | `byte`, `ushort`, `uint`, `ulong`, `UInt128`, `UInt256` |
| `i8`/`i16`/`i32`/`i64`/`i128` | `sbyte`, `short`, `int`, `long`, `Int128` |
| `bool`, `String` | `bool`, `string` |
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

## Links

- **Source / issues / full docs**: <https://github.com/yourusername/BcsSharp>
- **BCS specification**: <https://github.com/diem/bcs>

## License

Apache-2.0
