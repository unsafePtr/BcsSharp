use serde::{Deserialize, Deserializer, Serialize, Serializer};
use primitive_types::U256;
use std::collections::BTreeMap;

/// Custom wrapper for U256 that implements BCS-compatible serialization
#[derive(Debug, Clone, PartialEq)]
pub struct PrimitiveU256(pub U256);

// follow similar approach to Sui's U256 serialization
// https://github.com/MystenLabs/sui/blob/20ad4d4ae7bc0c0bf1f1f393156ccc57c1080fd0/external-crates/move/crates/move-core-types/src/u256.rs#L127-L145


impl Serialize for PrimitiveU256 {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {
        // Convert U256 to little-endian bytes (32 bytes) - following Sui's approach
        // primitive-types U256 stores as 4 u64 words, convert each to little-endian
        let mut bytes = [0u8; 32];
        for (i, &word) in self.0.0.iter().enumerate() {
            let start = i * 8;
            bytes[start..start + 8].copy_from_slice(&word.to_le_bytes());
        }
        bytes.serialize(serializer)
    }
}

impl<'de> Deserialize<'de> for PrimitiveU256 {
    fn deserialize<D>(deserializer: D) -> Result<Self, D::Error>
    where
        D: Deserializer<'de>,
    {
        // Following Sui's approach - deserialize as fixed 32-byte array
        let bytes: [u8; 32] = <[u8; 32]>::deserialize(deserializer)?;
        // Convert from little-endian bytes back to U256 (4 x u64 words)
        let mut words = [0u64; 4];
        for (i, chunk) in bytes.chunks_exact(8).enumerate() {
            words[i] = u64::from_le_bytes([
                chunk[0], chunk[1], chunk[2], chunk[3],
                chunk[4], chunk[5], chunk[6], chunk[7],
            ]);
        }
        Ok(PrimitiveU256(U256(words)))
    }
}

/// Represents a user in the system
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct User {
    pub id: u64,
    pub name: String,
    pub email: Option<String>,
    pub balance: PrimitiveU256,
    pub is_verified: bool,
    pub address: Option<Address>
}

/// Represents an address for a user
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct  Address {
    pub street: String,
    pub city: String,
    pub state: Option<String>,
    pub zip: String,
}

/// Different types of game assets
/// C-style enums for asset categorization
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub enum AssetType {
    Weapon = 100,
    Armor = 200,
    Consumable = 300,
    Material = 1_000_000,
    Currency = 2_000_000,
}

/// Asset attributes (like damage, defense, etc.)
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct Attribute {
    pub name: String,
    pub value: u32,
}

/// Represents a game asset/item
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct GameAsset {
    pub asset_id: String,
    pub asset_type: AssetType,
    pub level: u8,
    pub attributes: Vec<Attribute>,
}

/// Transaction types supported by the system
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub enum TransactionType {
    Transfer,
    Purchase,
    Sale,
    Mint,
    Burn,
}

/// Represents a transaction between users
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct Transaction {
    pub tx_id: String,
    pub from_user: u64,
    pub to_user: u64,
    pub amount: u64,
    pub timestamp: u64,
    pub tx_type: TransactionType,
}

/// Represents an item listed on a marketplace
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct MarketplaceItem {
    pub item_id: String,
    pub seller: u64,
    pub asset: GameAsset,
    pub price: u64,
    pub listed_at: u64,
    pub is_active: bool,
}

/// legendary asset with additional properties
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct LegendaryAsset {
    pub asset_id: String,
    pub level: u8
}

/// rarity levels for game assets
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub enum Rarity {
    Common,
    Uncommon(u32),
    Rare(Vec<u32>),
    Epic(String),
    Legendary((String, u64, LegendaryAsset)),
}

/// Global game statistics
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct GlobalStats {
    pub total_users: u64,
    pub total_transactions: u64,
    pub total_volume: u64,
    pub assets_by_rarity: Vec<(Rarity, u64)>,
}

/// Sui-compatible data structure demonstrating proper BCS usage
/// This follows Sui's conventions for on-chain data
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct SuiCompatibleData {
    /// Owner address (32-byte hex string representation)
    pub owner: String,
    /// Balance in MIST (smallest unit of SUI)
    pub balance: u64,
    /// Arbitrary metadata as raw bytes
    pub metadata: Vec<u8>,
}

/// Tuple examples for BCS serialization testing
/// Tuples are serialized as structs without field names in BCS
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct TupleExamples {
    /// Simple pair (string, number)
    pub simple_pair: (String, u32),
    /// Triple with different types
    pub triple: (u64, bool, String),
    /// Nested tuple with GameAsset
    pub nested_tuple: (String, GameAsset),
    /// Tuple with array
    pub tuple_with_array: (String, Vec<u32>),
    /// Complex tuple
    pub complex_tuple: (User, Transaction, bool),
}

/// Struct map key, shaped like a Sui ObjectID split into two words.
#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq, PartialOrd, Ord)]
pub struct ObjectId {
    pub hi: u64,
    pub lo: u64,
}

/// Using BTreeMap because it already sots keys.
/// If there is need for plain Map serilization this one can be used
/// https://github.com/diem/bcs/blob/master/src/ser.rs#L457-L516
/// Map examples for BCS serialization testing
/// Maps are serialized with keys in sorted order for deterministic output
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct MapExamples {
    /// Simple string to u32 map
    pub string_to_number: BTreeMap<String, u32>,
    /// Number to string map  
    pub number_to_string: BTreeMap<u32, String>,
    /// Complex map with nested values
    pub user_attributes: BTreeMap<String, Attribute>,
}