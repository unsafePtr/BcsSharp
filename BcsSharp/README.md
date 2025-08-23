# BcsSharp - Binary Canonical Serialization for C#

A high-performance C# implementation of Binary Canonical Serialization (BCS), fully compatible with Rust BCS and the Sui blockchain ecosystem.

## Features

✅ **Full BCS Compliance**: 100% compatible with Rust BCS implementation  
✅ **Complete Type Support**: All BCS types including primitives, vectors, options, enums, and structs  
✅ **High Performance**: Zero-allocation readers/writers with ref struct optimization  
✅ **Type Safety**: Compile-time type checking with C# attributes  
✅ **Rust Compatible**: Byte-for-byte identical serialization with Rust BCS  

## Supported Types

### Primitive Types
- **Integers**: `byte`, `ushort`, `uint`, `ulong`, `UInt128`, `UInt256`
- **Signed**: `sbyte`, `short`, `int`, `long`, `Int128`
- **Other**: `bool`, `string` (UTF-8)

### Complex Types  
- **Vectors**: `List<T>` (variable-length arrays)
- **Options**: `T?` (nullable types), `OneOf<None, T>` (explicit options)
- **Structs**: Classes with `[BcsStruct]` and `[BcsField]`
- **Enums**: Tagged unions with `[BcsEnum]` and `[BcsEnumVariant]`

## Quick Start

### Installation

```bash
# Add to your project
dotnet add package BcsSharp
```

### Basic Serialization

```csharp
using BcsSharp.Core;

// Serialize primitive types
var data = BcsSerializer.Serialize(42u);
var value = BcsSerializer.Deserialize<uint>(data);

// Serialize strings
var textData = BcsSerializer.Serialize("Hello BCS!");
var text = BcsSerializer.Deserialize<string>(textData);

// Serialize collections
var numbers = new List<uint> { 1, 2, 3, 4, 5 };
var listData = BcsSerializer.Serialize(numbers);
var deserializedList = BcsSerializer.Deserialize<List<uint>>(listData);
```

### Working with Structs

```csharp
using BcsSharp.Core.Attributes;

[BcsStruct]
public class User
{
    [BcsField(0)]
    public string Name { get; set; } = "";
    
    [BcsField(1)]
    public uint Age { get; set; }
    
    [BcsField(2)]
    public bool IsActive { get; set; }
}

// Serialize struct
var user = new User { Name = "Alice", Age = 30, IsActive = true };
var userData = BcsSerializer.Serialize(user);
var deserializedUser = BcsSerializer.Deserialize<User>(userData);
```

### Working with Options

```csharp
using OneOf;
using OneOf.Types;

[BcsStruct]
public class Person
{
    [BcsField(0)]
    public string Name { get; set; } = "";
    
    // Nullable primitive
    [BcsField(1)] 
    public uint? Age { get; set; }
    
    // Optional reference type (use OneOf<None, T>)
    [BcsField(2)]
    public OneOf<None, Address> HomeAddress { get; set; } = new None();
}

[BcsStruct]
public class Address
{
    [BcsField(0)]
    public string Street { get; set; } = "";
    
    [BcsField(1)] 
    public string City { get; set; } = "";
}

// Usage
var person = new Person
{
    Name = "Bob",
    Age = 25, // Some value
    HomeAddress = new Address { Street = "123 Main St", City = "Springfield" }
};

var data = BcsSerializer.Serialize(person);
var restored = BcsSerializer.Deserialize<Person>(data);
```

### Working with Enums (Tagged Unions)

```csharp
// Define the enum interface
[BcsEnum]
public interface IShape { }

// Define variants
[BcsEnumVariant(0)]
public partial class Circle : IShape
{
    [BcsEnumData]
    public uint Radius { get; set; }
}

[BcsEnumVariant(1)]
public partial class Rectangle : IShape
{
    [BcsEnumData] 
    public uint Width { get; set; }
    
    [BcsEnumData]
    public uint Height { get; set; }
}

[BcsEnumVariant(2)]
public partial class Point : IShape
{
    // No data - unit variant
}

// Serialize enum variants
IShape circle = new Circle { Radius = 10 };
IShape rectangle = new Rectangle { Width = 20, Height = 30 };
IShape point = new Point();

var circleData = BcsSerializer.Serialize<IShape>(circle);
var rectangleData = BcsSerializer.Serialize<IShape>(rectangle);
var pointData = BcsSerializer.Serialize<IShape>(point);

// Deserialize
var deserializedCircle = BcsSerializer.Deserialize<IShape>(circleData);
var deserializedRectangle = BcsSerializer.Deserialize<IShape>(rectangleData);
```

### Low-level Reader/Writer API

For high-performance scenarios, use the low-level API:

```csharp
// Writing data
var writer = new BcsWriter();
writer.WriteString("Alice");
writer.Write32(30);
writer.WriteBool(true);
writer.WriteULEB(1000u); // ULEB128 encoding
var bytes = writer.ToArray();

// Reading data
var reader = new BcsReader(bytes);
var name = reader.ReadString();        // "Alice"
var age = reader.Read32();            // 30
var isActive = reader.ReadBool();     // true
var count = reader.ReadULEB32();      // 1000
```

## Important Rules

### ⚠️ Arrays vs Lists
- **Use `List<T>` for vectors** (variable-length)
- **Avoid `T[]` arrays** (fixed-length, requires custom serializers)

```csharp
// ✅ Correct - use List<T>
[BcsField(0)]
public List<uint> Numbers { get; set; } = [];

// ❌ Wrong - arrays need custom serializers  
[BcsField(0)]
public uint[] Numbers { get; set; } = [];
```

### ⚠️ Reference Types and Options
- **Primitive nullables**: Use `T?` (e.g., `uint?`, `bool?`)
- **Reference type options**: Use `OneOf<None, T>` (e.g., `OneOf<None, Address>`)

```csharp
// ✅ Correct
[BcsField(0)] public uint? Age { get; set; }                    // Nullable primitive
[BcsField(1)] public OneOf<None, Address> Address { get; set; } // Optional reference type

// ❌ Wrong  
[BcsField(0)] public string? Name { get; set; }  // Use OneOf<None, string> instead
```

### ⚠️ Enum Variants
- **Enum variants cannot be serialized directly** - only through their interface
- This matches Rust BCS behavior exactly

```csharp
// ✅ Correct - serialize through interface
IShape shape = new Circle { Radius = 10 };
var data = BcsSerializer.Serialize<IShape>(shape);

// ❌ Wrong - concrete variants can't be serialized directly
var circle = new Circle { Radius = 10 };
var data = BcsSerializer.Serialize(circle); // Throws InvalidOperationException
```

## Field Ordering

BCS requires deterministic field ordering:

```csharp
[BcsStruct]
public class Example
{
    [BcsField(0)]  // First field
    public string Name { get; set; } = "";
    
    [BcsField(1)]  // Second field  
    public uint Value { get; set; }
    
    // BcsField numbers must be sequential: 0, 1, 2, 3...
}
```

## Performance Tips

1. **Use ref structs**: `BcsReader` and `BcsWriter` are ref structs for zero allocations
2. **Reuse writers**: Create once, clear between uses
3. **Use spans**: `ReadBytesAsSpan()` avoids allocations
4. **Primitive arrays**: Use `ReadPrimitiveArray<T>()` for bulk reads

## Rust Compatibility

This implementation is **byte-for-byte compatible** with Rust BCS:

- ✅ Little-endian encoding
- ✅ ULEB128 canonical encoding  
- ✅ Boolean validation (0x00/0x01 only)
- ✅ UTF-8 string handling
- ✅ Vector length prefixes
- ✅ Option encoding (0x00 = None, 0x01 + data = Some)
- ✅ Enum variant indices

## Error Handling

The library provides clear error messages:

```csharp
// Invalid boolean values
BcsSerializer.Deserialize<bool>(new byte[] { 2 }); 
// → InvalidOperationException: "Invalid boolean value: 2. Expected 0 or 1."

// Non-canonical ULEB128
var reader = new BcsReader(new byte[] { 0x80, 0x00 }); 
reader.ReadULEB32();
// → InvalidOperationException: "Non-canonical ULEB128 encoding detected"

// Missing formatter
BcsSerializer.Serialize(new UnknownType());
// → InvalidOperationException: "No formatter found for type UnknownType"
```

## Building and Testing

```bash
# Build the solution
dotnet build

# Run all tests (95 tests)
dotnet test

# Run specific test categories
dotnet test --filter "RustBcsCompatibility"  # Rust compatibility tests
dotnet test --filter "CanonicalEncoding"     # BCS specification tests
```

## Project Structure

- **BcsSharp.Core**: Main library with all BCS functionality
- **BcsSharp.Tests**: Comprehensive test suite (95 tests)
- **rust-sui-bcs-test**: Rust reference implementation for compatibility testing

## Technical Implementation

### Architecture
- **Formatters**: Type-specific serialization logic (`IBcsFormatter<T>`)
- **Resolvers**: Automatic formatter discovery (`CompositeResolver.Default`)
- **Attributes**: `[BcsStruct]`, `[BcsField]`, `[BcsEnum]`, `[BcsEnumVariant]`
- **Zero-copy**: Ref structs and span-based operations

### Memory Management
- **Zero allocations** for primitive serialization
- **Minimal allocations** for complex types
- **Span-based** byte operations
- **Cached formatters** for performance

## Requirements

- **.NET 9.0** or later
- **C# 12.0** language features
- **OneOf** package (for explicit options)

## License

Apache-2.0