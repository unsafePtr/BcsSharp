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

One full run, pinned with `taskset -c 6-11,18-23` to one CCD so other load on the machine stayed off its cores and L3 cache.
The reports are BenchmarkDotNet's GitHub exports, unedited.

### User

```

BenchmarkDotNet v0.16.0-preview.2, Linux Ubuntu 26.04 LTS (Resolute Raccoon)
AMD Ryzen 9 7900 3.01GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 29.93 GB Total, 0.57 GB Available
.NET SDK 11.0.100-rc.2.26504.105
  [Host]     : .NET 11.0.0 (11.0.0-rc.2.26504.105, 11.0.26.50505), X64 RyuJIT x86-64-v4
  Job-IHFIKV : .NET 11.0.0 (11.0.0-rc.2.26504.105, 11.0.26.50505), X64 RyuJIT x86-64-v4

Runtime=.NET 11.0  

```
| Method      | Categories        | Mean     | Error    | StdDev   | Ratio | Size | Gen0   | Allocated | Alloc Ratio |
|------------ |------------------ |---------:|---------:|---------:|------:|-----:|-------:|----------:|------------:|
| BCS         | Deserialize       | 51.43 ns | 0.031 ns | 0.029 ns |  1.00 | 31 B | 0.0057 |      96 B |        1.00 |
| MessagePack | Deserialize       | 81.36 ns | 0.050 ns | 0.042 ns |  1.58 | 31 B | 0.0057 |      96 B |        1.00 |
|             |                   |          |          |          |       |      |        |           |             |
| BCS         | Serialize         | 61.07 ns | 0.032 ns | 0.030 ns |  1.00 | 31 B | 0.0033 |      56 B |        1.00 |
| MessagePack | Serialize         | 54.34 ns | 0.029 ns | 0.025 ns |  0.89 | 31 B | 0.0033 |      56 B |        1.00 |
|             |                   |          |          |          |       |      |        |           |             |
| BCS         | SerializeToBuffer | 42.72 ns | 0.041 ns | 0.036 ns |  1.00 | 31 B |      - |         - |          NA |
| MessagePack | SerializeToBuffer | 47.95 ns | 0.060 ns | 0.050 ns |  1.12 | 31 B |      - |         - |          NA |

### User ×100

```

BenchmarkDotNet v0.16.0-preview.2, Linux Ubuntu 26.04 LTS (Resolute Raccoon)
AMD Ryzen 9 7900 3.01GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 29.93 GB Total, 0.57 GB Available
.NET SDK 11.0.100-rc.2.26504.105
  [Host]     : .NET 11.0.0 (11.0.0-rc.2.26504.105, 11.0.26.50505), X64 RyuJIT x86-64-v4
  Job-IHFIKV : .NET 11.0.0 (11.0.0-rc.2.26504.105, 11.0.26.50505), X64 RyuJIT x86-64-v4

Runtime=.NET 11.0  

```
| Method      | Categories        | Mean     | Error     | StdDev    | Ratio | Size    | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| BCS         | Deserialize       | 5.641 μs | 0.0409 μs | 0.0383 μs |  1.00 | 4,400 B | 0.8698 | 0.0381 |   14568 B |        1.00 |
| MessagePack | Deserialize       | 6.804 μs | 0.0050 μs | 0.0047 μs |  1.21 | 4,321 B | 0.8698 | 0.0381 |   14568 B |        1.00 |
|             |                   |          |           |           |       |         |        |        |           |             |
| BCS         | Serialize         | 3.791 μs | 0.0036 μs | 0.0032 μs |  1.00 | 4,400 B | 0.2632 |      - |    4424 B |        1.00 |
| MessagePack | Serialize         | 3.881 μs | 0.0050 μs | 0.0046 μs |  1.02 | 4,321 B | 0.2594 |      - |    4352 B |        0.98 |
|             |                   |          |           |           |       |         |        |        |           |             |
| BCS         | SerializeToBuffer | 3.647 μs | 0.0021 μs | 0.0020 μs |  1.00 | 4,400 B |      - |      - |         - |          NA |
| MessagePack | SerializeToBuffer | 4.205 μs | 0.0031 μs | 0.0024 μs |  1.15 | 4,321 B |      - |      - |         - |          NA |

### GameData

```

BenchmarkDotNet v0.16.0-preview.2, Linux Ubuntu 26.04 LTS (Resolute Raccoon)
AMD Ryzen 9 7900 3.01GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 29.93 GB Total, 0.57 GB Available
.NET SDK 11.0.100-rc.2.26504.105
  [Host]     : .NET 11.0.0 (11.0.0-rc.2.26504.105, 11.0.26.50505), X64 RyuJIT x86-64-v4
  Job-IHFIKV : .NET 11.0.0 (11.0.0-rc.2.26504.105, 11.0.26.50505), X64 RyuJIT x86-64-v4

Runtime=.NET 11.0  

```
| Method      | Categories        | Mean     | Error   | StdDev  | Ratio | Size  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |------------------ |---------:|--------:|--------:|------:|------:|-------:|-------:|----------:|------------:|
| BCS         | Deserialize       | 669.8 ns | 2.84 ns | 2.66 ns |  1.00 | 414 B | 0.1364 | 0.0010 |    2296 B |        1.00 |
| MessagePack | Deserialize       | 962.3 ns | 1.22 ns | 1.14 ns |  1.44 | 374 B | 0.1545 |      - |    2600 B |        1.13 |
|             |                   |          |         |         |       |       |        |        |           |             |
| BCS         | Serialize         | 711.7 ns | 0.40 ns | 0.34 ns |  1.00 | 414 B | 0.0277 |      - |     472 B |        1.00 |
| MessagePack | Serialize         | 495.0 ns | 0.31 ns | 0.29 ns |  0.70 | 374 B | 0.0238 |      - |     400 B |        0.85 |
|             |                   |          |         |         |       |       |        |        |           |             |
| BCS         | SerializeToBuffer | 621.2 ns | 0.49 ns | 0.46 ns |  1.00 | 414 B |      - |      - |         - |          NA |
| MessagePack | SerializeToBuffer | 473.8 ns | 0.58 ns | 0.55 ns |  0.76 | 374 B |      - |      - |         - |          NA |

### GameData ×50

```

BenchmarkDotNet v0.16.0-preview.2, Linux Ubuntu 26.04 LTS (Resolute Raccoon)
AMD Ryzen 9 7900 3.01GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 29.93 GB Total, 0.57 GB Available
.NET SDK 11.0.100-rc.2.26504.105
  [Host]     : .NET 11.0.0 (11.0.0-rc.2.26504.105, 11.0.26.50505), X64 RyuJIT x86-64-v4
  Job-IHFIKV : .NET 11.0.0 (11.0.0-rc.2.26504.105, 11.0.26.50505), X64 RyuJIT x86-64-v4

Runtime=.NET 11.0  

```
| Method      | Categories        | Mean     | Error    | StdDev   | Ratio | Size     | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |------------------ |---------:|---------:|---------:|------:|---------:|-------:|-------:|----------:|------------:|
| BCS         | Deserialize       | 29.23 μs | 0.093 μs | 0.082 μs |  1.00 | 17,951 B | 6.0425 | 1.6785 |  101344 B |        1.00 |
| MessagePack | Deserialize       | 42.02 μs | 0.053 μs | 0.047 μs |  1.44 | 16,533 B | 6.9580 | 1.8311 |  116544 B |        1.15 |
|             |                   |          |          |          |       |          |        |        |           |             |
| BCS         | Serialize         | 27.61 μs | 0.013 μs | 0.011 μs |  1.00 | 17,951 B | 1.0681 |      - |   18008 B |        1.00 |
| MessagePack | Serialize         | 21.49 μs | 0.008 μs | 0.006 μs |  0.78 | 16,533 B | 0.9766 |      - |   16560 B |        0.92 |
|             |                   |          |          |          |       |          |        |        |           |             |
| BCS         | SerializeToBuffer | 27.41 μs | 0.026 μs | 0.024 μs |  1.00 | 17,951 B |      - |      - |         - |          NA |
| MessagePack | SerializeToBuffer | 21.29 μs | 0.007 μs | 0.007 μs |  0.78 | 16,533 B |      - |      - |         - |          NA |

### Map ×1000

```

BenchmarkDotNet v0.16.0-preview.2, Linux Ubuntu 26.04 LTS (Resolute Raccoon)
AMD Ryzen 9 7900 3.01GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 29.93 GB Total, 0.57 GB Available
.NET SDK 11.0.100-rc.2.26504.105
  [Host]     : .NET 11.0.0 (11.0.0-rc.2.26504.105, 11.0.26.50505), X64 RyuJIT x86-64-v4
  Job-IHFIKV : .NET 11.0.0 (11.0.0-rc.2.26504.105, 11.0.26.50505), X64 RyuJIT x86-64-v4

Runtime=.NET 11.0  

```
| Method      | Categories        | Mean     | Error    | StdDev   | Ratio | Size     | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |------------------ |---------:|---------:|---------:|------:|---------:|-------:|-------:|----------:|------------:|
| BCS         | Deserialize       | 25.20 μs | 0.028 μs | 0.026 μs |  1.00 | 20,002 B | 5.1880 | 1.2817 |   87016 B |        1.00 |
| MessagePack | Deserialize       | 24.49 μs | 0.040 μs | 0.037 μs |  0.97 | 20,839 B | 5.1880 | 1.2817 |   87016 B |        1.00 |
|             |                   |          |          |          |       |          |        |        |           |             |
| BCS         | Serialize         | 50.58 μs | 0.039 μs | 0.035 μs |  1.00 | 20,002 B | 1.1597 |      - |   20064 B |        1.00 |
| MessagePack | Serialize         | 13.09 μs | 0.030 μs | 0.025 μs |  0.26 | 20,839 B | 1.2360 |      - |   20864 B |        1.04 |
|             |                   |          |          |          |       |          |        |        |           |             |
| BCS         | SerializeToBuffer | 49.73 μs | 0.042 μs | 0.039 μs |  1.00 | 20,002 B |      - |      - |         - |          NA |
| MessagePack | SerializeToBuffer | 12.66 μs | 0.076 μs | 0.071 μs |  0.25 | 20,839 B |      - |      - |         - |          NA |

## Speed comparison

Values are BCS / MessagePack means from the reports above; the faster side is in bold, followed by how many times faster it is.

| Payload | Serialize → `byte[]` | Serialize → `IBufferWriter` | Deserialize |
|---|---|---|---|
| User | 61.07 / **54.34** ns · 1.12× | **42.72** / 47.95 ns · 1.12× | **51.43** / 81.36 ns · 1.58× |
| User ×100 | **3.791** / 3.881 µs · 1.02× | **3.647** / 4.205 µs · 1.15× | **5.641** / 6.804 µs · 1.21× |
| GameData | 711.7 / **495.0** ns · 1.44× | 621.2 / **473.8** ns · 1.31× | **669.8** / 962.3 ns · 1.44× |
| GameData ×50 | 27.61 / **21.49** µs · 1.28× | 27.41 / **21.29** µs · 1.29× | **29.23** / 42.02 µs · 1.44× |
| Map ×1000 | 50.58 / **13.09** µs · 3.86× | 49.73 / **12.66** µs · 3.93× | 25.20 / **24.49** µs · 1.03× |

Allocations: serializing to `byte[]` allocates the output array on both sides, plus 32 bytes per call on the BCS side for the payloads that contain maps.
Serializing into an `IBufferWriter` allocates nothing on either side.
Deserialization allocates the same on both sides, except GameData, where MessagePack allocates 13–15% more.
BenchmarkDotNet's error bars cover variation within one process, not between launches.

## Reading the results

- **Deserialization: BCS is 1.2–1.6× faster on objects** and level with MessagePack on the string map, where MessagePack is 3% ahead. BcsSharp reads straight from a span, while MessagePack's reader is built for multi-segment buffers and its generated code checks the object depth on every object.
- **Serialization: MessagePack is 1.3–1.4× faster on GameData and about 3.9× faster on the map.** BCS requires map entries in the order of their serialized key bytes, so `MapFormatter` rents a scratch buffer per map, encodes every entry into it and sorts them before writing. MessagePack writes maps in iteration order. GameData carries two small maps per object, so it pays that cost too. Without maps the two are close: BCS leads on User ×100 both ways and on User into an `IBufferWriter`, while MessagePack leads on User into `byte[]`.
- **Serializing into an `IBufferWriter` beats `byte[]` for BCS on every payload**, by 30% on User, 13% on GameData and 4% or less on the larger payloads. `BcsWriter` writes into the span it was last handed and asks the buffer writer for more only when it runs out, while the `byte[]` overload also rents a scratch buffer and copies the result out.
- **Size depends on the data.** BCS integers have the width of their declared type, while MessagePack sizes each integer by its value. GameData stores small inventory counts as `long`, 8 bytes in BCS against 1 in MessagePack, so BCS is about 10% larger there; the map's larger `int` values cost BCS 4 bytes against MessagePack's 5, so BCS is 4% smaller. A `UInt128` is 16 bytes in BCS and 18 in MessagePack.
