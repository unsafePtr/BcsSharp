use anyhow::Result;
use serde::Serialize;

fn main() -> Result<()> {
    println!("Testing empty string BCS serialization in Rust");
    
    // Test empty string
    let empty_string = String::new();
    let serialized_empty = bcs::to_bytes(&empty_string)?;
    println!("Empty string '' serialized as: {}", hex::encode(&serialized_empty));
    println!("Length: {} bytes", serialized_empty.len());
    
    // Test other strings
    let test_strings = vec![
        "".to_string(),
        "A".to_string(),
        "Alice".to_string(),
        "Plovdiv".to_string(),
        "12345".to_string(),
    ];
    
    for s in test_strings {
        let serialized = bcs::to_bytes(&s)?;
        println!("String '{}' serialized as: {}", s, hex::encode(&serialized));
    }
    
    Ok(())
}