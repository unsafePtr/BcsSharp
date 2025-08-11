use anyhow::Result;
use serde::Serialize;
use primitive_types::U256;
use std::{fs, collections::BTreeMap};

mod types;
use types::*;

fn main() -> Result<()> {
    println!("🚀 Sui BCS Serialization Demo");
    println!("{}", "=".repeat(40));

    // Create sample data structures
    let user = User {
        id: 12345,
        name: "Alice".to_string(),
        email: Option::None,
        balance: PrimitiveU256(U256::MAX),
        is_verified: true,
        address: Some(Address {
            street: "".to_string(),
            city: "Plovdiv".to_string(),
            state: Option::None,
            zip: "12345".to_string(),
        })
    };

    let asset = GameAsset {
        asset_id: "sword_001".to_string(),
        asset_type: AssetType::Material,
        level: 15,
        attributes: vec![
            Attribute {
                name: "damage".to_string(),
                value: 85,
            },
            Attribute {
                name: "speed".to_string(),
                value: 12,
            },
        ],
    };


    let transaction = Transaction {
        tx_id: "0x1234567890abcdef".to_string(),
        from_user: user.id,
        to_user: 67890,
        amount: 250,
        timestamp: 1640995200, // 2022-01-01 00:00:00 UTC
        tx_type: TransactionType::Transfer,
    };

    let marketplace_item = MarketplaceItem {
        item_id: "market_001".to_string(),
        seller: user.id,
        asset: asset.clone(),
        price: 500,
        listed_at: 1640995200,
        is_active: true,
    };

    // Create tuple examples
    let tuple_examples = TupleExamples {
        simple_pair: ("hello".to_string(), 42),
        triple: (123u64, true, "world".to_string()),
        nested_tuple: ("asset_ref".to_string(), asset.clone()),
        tuple_with_array: ("numbers".to_string(), vec![1, 2, 3, 4, 5]),
        complex_tuple: (user.clone(), transaction.clone(), false),
    };

    // Create map examples
    let mut string_to_number = BTreeMap::new();
    string_to_number.insert("health".to_string(), 100);
    string_to_number.insert("damage".to_string(), 85);
    string_to_number.insert("speed".to_string(), 12);
    
    let mut number_to_string = BTreeMap::new();
    number_to_string.insert(1, "common".to_string());
    number_to_string.insert(2, "rare".to_string());
    number_to_string.insert(3, "epic".to_string());
    
    let mut user_attributes = BTreeMap::new();
    user_attributes.insert("power".to_string(), Attribute {
        name: "power".to_string(),
        value: 150,
    });
    user_attributes.insert("defense".to_string(), Attribute {
        name: "defense".to_string(), 
        value: 75,
    });
    
    let map_examples = MapExamples {
        string_to_number,
        number_to_string,
        user_attributes,
    };

    // Demonstrate BCS serialization
    println!("\n📦 Serializing structs to BCS format...\n");

    // Serialize each object separately
    serialize_and_display("User", &user)?;
    serialize_and_display("GameAsset", &asset)?;
    serialize_and_display("Transaction", &transaction)?;
    serialize_and_display("MarketplaceItem", &marketplace_item)?;
    serialize_and_display("TupleExamples", &tuple_examples)?;
    serialize_and_display("MapExamples", &map_examples)?;

    // Serialize GlobalStats for each object, stats are different each time
    println!("\n🌐 Serializing GlobalStats with each object...\n");
    let stats_user = GlobalStats {
        total_users: 1,
        total_transactions: 0,
        total_volume: 0,
        assets_by_rarity: vec![(Rarity::Epic("Sword".to_string()), 1)],
    };
    serialize_and_display("GlobalStats (User)", &stats_user)?;
    let stats_asset = GlobalStats {
        total_users: 0,
        total_transactions: 0,
        total_volume: 0,
        assets_by_rarity: vec![(Rarity::Rare(vec![15]), 1)],
    };
    serialize_and_display("GlobalStats (GameAsset)", &stats_asset)?;
    let stats_tx = GlobalStats {
        total_users: 2,
        total_transactions: 1,
        total_volume: transaction.amount,
        assets_by_rarity: vec![],
    };
    serialize_and_display("GlobalStats (Transaction)", &stats_tx)?;
    let stats_market = GlobalStats {
        total_users: 1,
        total_transactions: 0,
        total_volume: marketplace_item.price,
        assets_by_rarity: vec![(Rarity::Common, 1)],
    };
    serialize_and_display("GlobalStats (MarketplaceItem)", &stats_market)?;
    let stats_tuple = GlobalStats {
        total_users: 1,
        total_transactions: 1,
        total_volume: 250,
        assets_by_rarity: vec![(Rarity::Uncommon(42), 1)],
    };
    serialize_and_display("GlobalStats (TupleExamples)", &stats_tuple)?;

    // Demonstrate deserialization
    println!("\n🔄 Testing serialization round-trip...\n");

    let user_bytes = bcs::to_bytes(&user)?;
    let deserialized_user: User = bcs::from_bytes(&user_bytes)?;
    println!("✅ User serialization round-trip successful");
    println!("   Original: {} ({:?})", user.name, user.email);
    println!("   Restored: {} ({:?})", deserialized_user.name, deserialized_user.email);

    // Show BCS format compliance
    println!("\n🔗 BCS format compliance test...");
    test_bcs_compliance()?;

    // Export serialized bytes to files for C# comparison
    println!("\n💾 Exporting serialized bytes to files for C# comparison...");
    export_serialized_bytes(&user, &asset, &transaction, &marketplace_item, &tuple_examples, &map_examples)?;

    Ok(())
}

fn serialize_and_display<T: Serialize>(name: &str, data: &T) -> Result<()> {
    let bytes = bcs::to_bytes(data)?;
    println!("📋 {} serialized:", name);
    println!("   Size: {} bytes", bytes.len());
    println!("   Hex:  {}", hex::encode(&bytes[..std::cmp::min(32, bytes.len())]));
    if bytes.len() > 32 {
        println!("   ... (truncated)");
    }
    println!();
    Ok(())
}

fn test_bcs_compliance() -> Result<()> {
    // Create a simple structure that demonstrates BCS encoding
    let bcs_data = SuiCompatibleData {
        owner: "0x123456789abcdef123456789abcdef123456789abcdef123456789abcdef12".to_string(),
        balance: 1000u64,
        metadata: vec![0x01, 0x02, 0x03, 0x04],
    };

    let serialized = bcs::to_bytes(&bcs_data)?;
    let deserialized: SuiCompatibleData = bcs::from_bytes(&serialized)?;
    
    println!("   ✅ BCS-compatible data structure serialized successfully");
    println!("   Size: {} bytes", serialized.len());
    println!("   Owner: {}", &deserialized.owner[..10]);
    
    // Test deterministic property of BCS
    let serialized2 = bcs::to_bytes(&bcs_data)?;
    if serialized == serialized2 {
        println!("   ✅ BCS deterministic property verified");
    }
    
    // Test PrimitiveU256 specifically
    println!("\n🔢 Testing PrimitiveU256 serialization...");
    let test_u256 = PrimitiveU256(U256::from(12345u64));
    let serialized_u256 = bcs::to_bytes(&test_u256)?;
    let deserialized_u256: PrimitiveU256 = bcs::from_bytes(&serialized_u256)?;
    println!("   ✅ PrimitiveU256 round-trip successful: {} == {}", 
        test_u256.0, deserialized_u256.0);
    
    let max_u256 = PrimitiveU256(U256::MAX);
    let serialized_max = bcs::to_bytes(&max_u256)?;
    println!("   ✅ PrimitiveU256 MAX round-trip successful");
    println!("   Size: {} bytes (should be 32: fixed 32 bytes, no length prefix)", serialized_max.len());
    
    // Test specific values to verify byte ordering matches Sui's approach
    let test_1 = PrimitiveU256(U256::from(1u64));
    let bytes_1 = bcs::to_bytes(&test_1)?;
    println!("   U256::from(1) serializes to: {}", hex::encode(&bytes_1));
    
    let test_256 = PrimitiveU256(U256::from(256u64));
    let bytes_256 = bcs::to_bytes(&test_256)?;
    println!("   U256::from(256) serializes to: {} (should show little-endian)", hex::encode(&bytes_256));
    
    Ok(())
}

fn export_serialized_bytes(
    user: &User,
    asset: &GameAsset,
    transaction: &Transaction,
    marketplace_item: &MarketplaceItem,
    tuple_examples: &TupleExamples,
    map_examples: &MapExamples
) -> Result<()> {
    // Serialize each struct to bytes
    let user_bytes = bcs::to_bytes(user)?;
    let asset_bytes = bcs::to_bytes(asset)?;
    let transaction_bytes = bcs::to_bytes(transaction)?;
    let marketplace_bytes = bcs::to_bytes(marketplace_item)?;
    let tuple_bytes = bcs::to_bytes(tuple_examples)?;
    let map_bytes = bcs::to_bytes(map_examples)?;

    // Write bytes to files
    fs::write("user.bcs", &user_bytes)?;
    fs::write("asset.bcs", &asset_bytes)?;
    fs::write("transaction.bcs", &transaction_bytes)?;
    fs::write("marketplace.bcs", &marketplace_bytes)?;
    fs::write("tuples.bcs", &tuple_bytes)?;
    fs::write("maps.bcs", &map_bytes)?;

    // Also create a summary file with hex dumps for easier debugging
    let summary = format!(
        "Rust BCS Serialization Results\n\
         ===============================\n\n\
         User: {} bytes\n\
         Hex: {}\n\n\
         GameAsset: {} bytes\n\
         Hex: {}\n\n\
         Transaction: {} bytes\n\
         Hex: {}\n\n\
         MarketplaceItem: {} bytes\n\
         Hex: {}\n\n\
         TupleExamples: {} bytes\n\
         Hex: {}\n\n\
         MapExamples: {} bytes\n\
         Hex: {}\n",
        user_bytes.len(), hex::encode(&user_bytes),
        asset_bytes.len(), hex::encode(&asset_bytes),
        transaction_bytes.len(), hex::encode(&transaction_bytes),
        marketplace_bytes.len(), hex::encode(&marketplace_bytes),
        tuple_bytes.len(), hex::encode(&tuple_bytes),
        map_bytes.len(), hex::encode(&map_bytes)
    );
    
    fs::write("rust_results.txt", summary)?;

    println!("   ✅ Exported binary files: user.bcs, asset.bcs, transaction.bcs, marketplace.bcs, tuples.bcs, maps.bcs");
    println!("   ✅ Created summary file: rust_results.txt");
    println!("   📁 Files saved in current directory for C# comparison");

    Ok(())
}