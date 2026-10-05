# BcsSharp benchmarks

BcsSharp against [MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp) on .NET 11: serialize to `byte[]`, serialize into an `IBufferWriter<byte>`, deserialize, and the encoded size of each payload.

## Running

```powershell
dotnet run -c Release --project BcsSharp.Benchmarks -- --filter '*'

# One payload
dotnet run -c Release --project BcsSharp.Benchmarks -- --filter '*GameDataBenchmarks*'
```

Reports land in `BenchmarkDotNet.Artifacts/results/`.
Each payload is its own benchmark class, with BCS as the baseline in every category.

## Setup

- **MessagePack 3.1.11, as its README recommends.** Models carry `[MessagePackObject]` and `[Key(n)]`, so objects encode as arrays with no member names. Calls use the default options, whose `StandardResolver` picks up the source-generated formatters. No compression.
- **Identical data on both sides.** Each payload is built from a fixed seed in `Payloads.cs`, and the two model sets differ only in the optional email: `Option<string>` for BCS, `string?` for MessagePack, each library's idiomatic optional.
- **BCS deserialization includes the input guards**: the container depth limit, length-prefix checks and the leftover-bytes check.

## Payloads

| Payload | Contents |
|---|---|
| User | `ulong` id, `string` name, optional email (absent for a third of users), `bool`, `UInt128` balance |
| User ×100 | `List` of 100 users |
| GameData | `string` id, `Dictionary<string, long>` inventory (5–15 entries), 3–7 achievements of two strings and a `UInt128`, stats of two `int`s and a `Dictionary<string, short>` |
| GameData ×50 | `List` of 50 GameData |
| Map ×1000 | `Dictionary<string, int>` with 1000 entries and 15-character keys |

## Results

```
BenchmarkDotNet v0.16.0-nightly.20260816.607, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-13700KF 3.42GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 11.0.0 (11.0.0-rc.1.26425.128), X64 RyuJIT x86-64-v3
```

Values are BCS / MessagePack; the faster side is in bold.

| Payload | Encoded size | Serialize → `byte[]` | Serialize → `IBufferWriter` | Deserialize |
|---|---|---|---|---|
| User | 31 / 31 B | 46.4 / **40.2** ns | 32.5 / 32.5 ns | **36.9** / 61.5 ns |
| User ×100 | 4,400 / 4,321 B | 3.14 / **2.66** µs | **2.51** / 3.64 µs | **4.26** / 6.26 µs |
| GameData | 414 / 374 B | 595 / **444** ns | 517 / **389** ns | **494** / 809 ns |
| GameData ×50 | 17,951 / 16,533 B | 24.3 / **20.1** µs | 21.5 / **18.9** µs | **23.0** / 37.7 µs |
| Map ×1000 | 20,002 / 20,839 B | 41.5 / **14.4** µs | 41.2 / **13.7** µs | **20.5** / 21.3 µs |

Allocations: serializing to `byte[]` allocates only the output array on both sides, and serializing into an `IBufferWriter` allocates nothing.
Deserialization allocates the same on both sides, except GameData, where MessagePack allocates 13–15% more.

The serialization columns come from a run after `BcsWriter` started holding on to its buffer writer's span; the deserialization columns are from the run before it, as no reading code changed.
BenchmarkDotNet's error bars cover variation within one process, not between launches.
In the newer run MessagePack took 3.64 µs on User ×100 into an `IBufferWriter`, against 2.46 µs the launch before, so that row is not a BCS win.

## Reading the results

- **Deserialization: BCS is 1.5–1.7× faster on objects** and level with MessagePack on the string map. BcsSharp reads straight from a span, while MessagePack's reader is built for multi-segment buffers and its generated code checks the object depth on every object.
- **Serialization: MessagePack is faster on objects by 1.1–1.3×**, except User into an `IBufferWriter`, which is level, and about 3× faster on the map.
  BCS requires map entries in the order of their serialized key bytes, so `MapFormatter` rents a scratch buffer per map, encodes every entry into it and sorts them before writing.
  MessagePack writes maps in iteration order.
  GameData carries two small maps per object, so it pays that cost too.
- **Serializing into an `IBufferWriter` beats `byte[]`** on every payload: `BcsWriter` writes into the span it was last handed and asks the buffer writer for more only when it runs out, while the `byte[]` overload also rents a scratch buffer and copies the result out.
- **Size depends on the data.** BCS integers have the width of their declared type, while MessagePack sizes each integer by its value. GameData stores small inventory counts as `long`, 8 bytes in BCS against 1 in MessagePack, so BCS is about 10% larger there; the map's larger `int` values cost BCS 4 bytes against MessagePack's 5, so BCS is 4% smaller. A `UInt128` is 16 bytes in BCS and 18 in MessagePack.
