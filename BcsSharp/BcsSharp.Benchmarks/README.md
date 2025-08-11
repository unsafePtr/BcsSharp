# BcsSharp vs MessagePack Benchmarks

This project benchmarks BcsSharp against MessagePack for serialization performance and size efficiency.

## Running Benchmarks

```bash
# Run both performance and size benchmarks (default)
dotnet run

# Performance benchmarks only
dotnet run -- serialization

# Size comparison benchmarks only
dotnet run -- size

# Run all benchmarks
dotnet run -- all
```

## Benchmark Categories

### Performance Benchmarks (`SerializationBenchmarks`)

**BCS Tests:**
- User serialization/deserialization
- User list serialization (100 users)
- GameData (complex nested objects)
- Large dictionary serialization (1000 key-value pairs)
- GameData list serialization (50 objects)

**MessagePack Tests:**
- Same test cases as BCS for direct comparison

### Size Benchmarks (`SizeBenchmarks`)

**Separate size measurements for:**
- BCS: User, User List, GameData, Large Map
- MessagePack: User, User List, GameData, Large Map

Each benchmark returns `(int Length, byte[] bytes)` for detailed analysis.

## Test Data

### User Model
- ID (ulong)
- Name (string)
- Email (nullable string)
- IsVerified (bool)  
- Balance (UInt128)

### GameData Model (Complex)
- PlayerId (string)
- Inventory (Dictionary<string, long>)
- Achievements (List<Achievement> with UInt128 timestamps)
- Stats (PlayerStats with nested Dictionary<string, short>)

## Expected Characteristics

### BCS Strengths
- **Deterministic**: Same input always produces identical output
- **Cross-language compatible**: Works with Rust, Move, etc.
- **Canonical**: Required for blockchain/cryptographic applications

### MessagePack Strengths  
- **Compact**: Generally smaller serialized size
- **Fast**: Optimized for performance
- **Mature**: Well-established ecosystem

## Output

Benchmarks generate:
- Console output with results
- `BenchmarkDotNet.Artifacts/` folder with detailed reports
- CSV, Markdown, and HTML exports