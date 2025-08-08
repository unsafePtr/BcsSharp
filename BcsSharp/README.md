# BCS Sharp - Binary Canonical Serialization for C#

A C# implementation of Binary Canonical Serialization (BCS), compatible with the TypeScript BCS implementation used in the Sui blockchain ecosystem.

## Features

- **Basic Types**: u8, u16, u32, u64, u128, u256, bool, string
- **Complex Types**: vectors (arrays), options (nullable), enums, structs
- **Reader/Writer Classes**: Low-level BcsReader and BcsWriter for direct byte manipulation
- **Type Safety**: Strong typing with compile-time type checking
- **Compatibility**: Designed to be compatible with existing BCS implementations

## Quick Start

### Basic Usage

```csharp
using BcsSharp.Core;

// Serialize basic types
var u32Value = 123456u;
var serialized = Bcs.U32.Serialize(u32Value);
var deserialized = Bcs.U32.Parse(serialized);

// Serialize strings
var text = "Hello, BCS!";
var textBytes = Bcs.String.Serialize(text);
var deserializedText = Bcs.String.Parse(textBytes);
```

### Working with Vectors

```csharp
var vectorType = Bcs.Vector(Bcs.U32);
var numbers = new uint[] { 1, 2, 3, 4, 5 };

var serialized = vectorType.Serialize(numbers);
var deserialized = vectorType.Parse(serialized);
```

### Working with Enums

```csharp
var colorEnum = BcsEnum.Create("Color")
    .AddVariant("Red")
    .AddVariant("Green")
    .AddVariant("Blue")
    .Build();

var redVariant = colorEnum.CreateVariant("Red");
var serialized = colorEnum.Serialize(redVariant);
var deserialized = colorEnum.Parse(serialized);

// Enums with associated data
var shapeEnum = BcsEnum.Create("Shape")
    .AddVariant("Circle", Bcs.U32)  // radius
    .AddVariant("Point")
    .Build();

var circle = shapeEnum.CreateVariant("Circle", 10u);
var circleBytes = shapeEnum.Serialize(circle);
```

### Low-level Reader/Writer

```csharp
// Writing
var writer = new BcsWriter();
writer.WriteString("Alice")
      .Write32(30)
      .WriteBool(true);

var bytes = writer.ToBytes();

// Reading
var reader = new BcsReader(bytes);
var name = reader.ReadString();
var age = reader.Read32();
var isActive = reader.ReadBool();
```

## Project Structure

- **BcsSharp.Core**: Main library containing all BCS types and functionality
- **BcsSharp.Tests**: Comprehensive unit tests
- **BcsSharp.Example**: Example usage demonstrating various features

## Available Types

### Basic Types
- `Bcs.U8` - 8-bit unsigned integer
- `Bcs.U16` - 16-bit unsigned integer
- `Bcs.U32` - 32-bit unsigned integer
- `Bcs.U64` - 64-bit unsigned integer
- `Bcs.U128` - 128-bit unsigned integer (as string)
- `Bcs.U256` - 256-bit unsigned integer (as string)
- `Bcs.Bool` - Boolean values
- `Bcs.String` - UTF-8 strings

### Complex Types
- `Bcs.Vector<T>(elementType)` - Arrays/vectors
- `Bcs.Option<T>(innerType)` - Optional values
- `BcsEnum.Create(name)...` - Tagged unions/enums
- `BcsStruct.Create<T>(name)...` - Structured data (planned)

## Building and Testing

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run the example
cd BcsSharp.Example
dotnet run
```

## Compatibility

This implementation is designed to be compatible with:
- Move language BCS serialization
- Sui TypeScript SDK BCS implementation
- Other standard BCS implementations

## Technical Details

- Uses little-endian byte order for multi-byte integers
- ULEB128 encoding for variable-length integers and length prefixes
- Supports BigInteger for u128/u256 operations
- Strong type safety with C# generic constraints

## Requirements

- .NET 9.0 or later
- C# 12.0 language features

## License

Same as parent project (Apache-2.0)