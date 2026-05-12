use bcs;
use serde::{Deserialize, Serialize};

// Define a BCS enum with variants (tagged union)
#[derive(Serialize, Deserialize, Debug, PartialEq)]
enum TestVariant {
    Simple(u8),
    Complex { name: String, count: u32 },
    Empty,
}

// Individual variant struct (separate from the enum)
#[derive(Serialize, Deserialize, Debug, PartialEq)]
struct SimpleVariant {
    value: u8,
}

#[derive(Serialize, Deserialize, Debug, PartialEq)]
struct ComplexVariant {
    name: String,
    count: u32,
}

#[derive(Serialize, Deserialize, Debug, PartialEq)]
struct EmptyVariant;

fn main() {
    println!("=== Rust BCS Variant Serialization Test ===\n");

    // Test 1: Serialize enum variants (standard way)
    println!("1. Enum variant serialization:");
    let simple_enum = TestVariant::Simple(42);
    let complex_enum = TestVariant::Complex { 
        name: "test".to_string(), 
        count: 123 
    };
    let empty_enum = TestVariant::Empty;

    let simple_enum_bytes = bcs::to_bytes(&simple_enum).unwrap();
    let complex_enum_bytes = bcs::to_bytes(&complex_enum).unwrap();
    let empty_enum_bytes = bcs::to_bytes(&empty_enum).unwrap();

    println!("  Simple enum: {:?} -> {:02X?}", simple_enum, simple_enum_bytes);
    println!("  Complex enum: {:?} -> {:02X?}", complex_enum, complex_enum_bytes);
    println!("  Empty enum: {:?} -> {:02X?}", empty_enum, empty_enum_bytes);
    println!();

    // Test 2: Try to serialize individual variant structs (separate types)
    println!("2. Individual variant struct serialization:");
    let simple_struct = SimpleVariant { value: 42 };
    let complex_struct = ComplexVariant { 
        name: "test".to_string(), 
        count: 123 
    };
    let empty_struct = EmptyVariant;

    let simple_struct_bytes = bcs::to_bytes(&simple_struct).unwrap();
    let complex_struct_bytes = bcs::to_bytes(&complex_struct).unwrap();
    let empty_struct_bytes = bcs::to_bytes(&empty_struct).unwrap();

    println!("  Simple struct: {:?} -> {:02X?}", simple_struct, simple_struct_bytes);
    println!("  Complex struct: {:?} -> {:02X?}", complex_struct, complex_struct_bytes);
    println!("  Empty struct: {:?} -> {:02X?}", empty_struct, empty_struct_bytes);
    println!();

    // Test 3: Compare the byte outputs
    println!("3. Byte comparison:");
    println!("  Simple - Enum vs Struct:");
    println!("    Enum:   {:02X?}", simple_enum_bytes);
    println!("    Struct: {:02X?}", simple_struct_bytes);
    println!("    Same? {}", simple_enum_bytes == simple_struct_bytes);
    println!();
    
    println!("  Complex - Enum vs Struct:");
    println!("    Enum:   {:02X?}", complex_enum_bytes);
    println!("    Struct: {:02X?}", complex_struct_bytes);
    println!("    Same? {}", complex_enum_bytes == complex_struct_bytes);
    println!();
    
    println!("  Empty - Enum vs Struct:");
    println!("    Enum:   {:02X?}", empty_enum_bytes);
    println!("    Struct: {:02X?}", empty_struct_bytes);
    println!("    Same? {}", empty_enum_bytes == empty_struct_bytes);
    println!();

    // Test 4: Try cross-deserialization (if possible)
    println!("4. Cross-deserialization test:");
    
    // Try to deserialize enum bytes as struct (this should fail or give different results)
    match bcs::from_bytes::<SimpleVariant>(&simple_enum_bytes) {
        Ok(result) => println!("  Enum -> Struct deserialization succeeded: {:?}", result),
        Err(e) => println!("  Enum -> Struct deserialization failed: {}", e),
    }

    // Try to deserialize struct bytes as enum (this should also fail)
    match bcs::from_bytes::<TestVariant>(&simple_struct_bytes) {
        Ok(result) => println!("  Struct -> Enum deserialization succeeded: {:?}", result),
        Err(e) => println!("  Struct -> Enum deserialization failed: {}", e),
    }

    println!("\n=== Analysis ===");
    println!("This test shows whether Rust BCS treats enum variants and separate structs differently.");
    println!("If the bytes are different, it means enum variants include variant index/tag information.");
    println!("If they're the same, it means both serialize just the data without enum context.");

    // Test 5: Analyze the structure of enum serialization
    println!("\n5. Detailed analysis of enum serialization:");
    println!("  Simple enum bytes: {:02X?}", simple_enum_bytes);
    if simple_enum_bytes.len() > 0 {
        println!("    First byte (variant index): 0x{:02X} = {}", simple_enum_bytes[0], simple_enum_bytes[0]);
        if simple_enum_bytes.len() > 1 {
            println!("    Data bytes: {:02X?}", &simple_enum_bytes[1..]);
        }
    }
    
    println!("  Complex enum bytes: {:02X?}", complex_enum_bytes);
    if complex_enum_bytes.len() > 0 {
        println!("    First byte (variant index): 0x{:02X} = {}", complex_enum_bytes[0], complex_enum_bytes[0]);
        if complex_enum_bytes.len() > 1 {
            println!("    Data bytes: {:02X?}", &complex_enum_bytes[1..]);
        }
    }
    
    println!("  Empty enum bytes: {:02X?}", empty_enum_bytes);
    if empty_enum_bytes.len() > 0 {
        println!("    First byte (variant index): 0x{:02X} = {}", empty_enum_bytes[0], empty_enum_bytes[0]);
        if empty_enum_bytes.len() > 1 {
            println!("    Data bytes: {:02X?}", &empty_enum_bytes[1..]);
        }
    }
}