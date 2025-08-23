use anyhow::Result;

pub fn test_uleb128_canonicality() -> Result<()> {
    println!("\n🔍 Testing ULEB128 canonical validation in Rust BCS...\n");
    
    // Test case 1: Zero with continuation byte (non-canonical)
    let non_canonical_zero = vec![0x80, 0x00];
    match bcs::from_bytes::<Vec<u32>>(&non_canonical_zero) {
        Ok(v) => println!("   ❌ Rust BCS accepted non-canonical zero: {:?}", v),
        Err(e) => println!("   ✅ Rust BCS rejected non-canonical zero: {}", e),
    }
    
    // Test case 2: 127 with unnecessary zero byte (non-canonical)
    let non_canonical_127 = vec![0x01, 0xFF, 0x00]; // length prefix 1, then value
    match bcs::from_bytes::<Vec<u8>>(&non_canonical_127) {
        Ok(v) => println!("   ❌ Rust BCS accepted non-canonical 127: {:?}", v),
        Err(e) => println!("   ✅ Rust BCS rejected non-canonical 127: {}", e),
    }
    
    // Test case 3: Check how Rust BCS encodes values
    println!("\n   📊 How Rust BCS encodes ULEB128 values:");
    
    // Vector lengths
    let empty_vec: Vec<u32> = vec![];
    let vec_127: Vec<u8> = vec![0; 127];
    let vec_128: Vec<u8> = vec![0; 128];
    
    println!("   Empty vector: {}", hex::encode(bcs::to_bytes(&empty_vec)?));
    println!("   Vector of 127 elements: {} (first few bytes)", 
        hex::encode(&bcs::to_bytes(&vec_127)?[..5]));
    println!("   Vector of 128 elements: {} (first few bytes)", 
        hex::encode(&bcs::to_bytes(&vec_128)?[..5]));
    
    // String lengths
    let str_127 = "x".repeat(127);
    let str_128 = "x".repeat(128);
    
    println!("\n   String of 127 bytes: {} (first few bytes)", 
        hex::encode(&bcs::to_bytes(&str_127)?[..5]));
    println!("   String of 128 bytes: {} (first few bytes)", 
        hex::encode(&bcs::to_bytes(&str_128)?[..5]));
    
    Ok(())
}